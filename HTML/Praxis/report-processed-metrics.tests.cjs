const fs = require('fs'), path = require('path'), assert = require('node:assert/strict');
const { chromium } = require('playwright');

(async () => {
    const root = process.env.PRAXIS_WORKSPACE || path.resolve(__dirname, '../../..');
    const browser = await chromium.launch({ channel: 'msedge', headless: true });
    try {
        const page = await browser.newPage();
        const errors = [], requests = [];
        let mode = 'normal', release;
        page.on('pageerror', error => errors.push(error.message));
        await page.route('https://praxis.test/**', async route => {
            const url = new URL(route.request().url());
            if (url.pathname === '/api/appointments') {
                assert.equal(url.searchParams.get('includeQa'), 'true');
                return route.fulfill({ json: { totalRows: 3, page: 1, totalPages: 1,
                    items: ['Approved', 'Pending', 'Rejected'].map((qaStatus, index) => ({
                        id: String(index + 1), contact: qaStatus + ' prospect', qaStatus,
                        appointmentDateTime: '2026-10-06T14:45:00', appointmentTimeZone: 'America/New_York',
                        appointmentConfirmed: false })) } });
            }
            if (url.pathname.startsWith('/api/metrics/')) {
                requests.push(url);
                if (mode === 'delayed') await new Promise(resolve => { release = resolve; });
                if (mode === 'failure') return route.fulfill({ status: 503, json: 'Processed metrics unavailable' });
                const row = { date: '2026-09-25', agent: 'csparks', agentName: 'Christina Sparks', dials: 110,
                    humanContacts: 16, appointments: 1, aodSeconds: 11683, afterCallWorkSeconds: 100, callDurationSeconds: 1000 };
                return route.fulfill({ json: { source: 'DataImport.scoped', activityAvailableToDate: '2026-09-25',
                    items: mode === 'empty' ? [] : [row], missingTeamEvidence: [] } });
            }
            const file = path.join(root, 'PraxisAPI', url.pathname);
            if (!fs.existsSync(file)) return route.fulfill({ status: 404, body: '' });
            let body = fs.readFileSync(file);
            if (url.pathname.endsWith('.html')) body = body.toString().replace(/<script\b[^>]*>[\s\S]*?<\/script>/g, '');
            return route.fulfill({ body, contentType: url.pathname.endsWith('.css') ? 'text/css' : 'text/html' });
        });
        await page.goto('https://praxis.test/report.html');
        await page.evaluate(() => { window.ByteForgeSortIndicator = { update() {} }; });
        const source = fs.readFileSync(path.join(__dirname, 'report-portal.js'), 'utf8')
            .replace('    restoreSession();', '    window.__metricsTest = { state, loadMetrics, loadHistory, loadAppointments, populateScope };');
        await page.addScriptTag({ content: source });
        await page.evaluate(() => {
            __metricsTest.state.configuration = { services: ['Praxis', 'Praxis_EEP', 'Praxis_IEP'],
                teams: ['Praxis', 'Praxis_EEP', 'Praxis_IEP'], reportTimeZone: 'Pacific Standard Time',
                reportIanaTimeZone: 'America/Los_Angeles', reportDialingDays: [1, 2, 3, 4, 5] };
            __metricsTest.populateScope();
            document.getElementById('report-content').hidden = false;
            document.getElementById('from-date').value = '2026-09-20';
            document.getElementById('to-date').value = '2026-09-26';
        });
        for (const theme of ['light', 'dark']) {
            await page.emulateMedia({ colorScheme: theme });
            await page.evaluate(() => {
                __metricsTest.state.canEditAppointments = true;
                __metricsTest.state.history = { items: [{ appointments: 2, humanContacts: 10, dials: 20, aodSeconds: 3600 }] };
                return __metricsTest.loadAppointments();
            });
            assert.equal(await page.locator('#appointment-table-body tr:not(.bf-report-qa-separator)').count(), 3);
            assert.equal(await page.locator('#appointment-table-body .bf-report-qa-approved').textContent(), 'Approved');
            assert.equal(await page.locator('#appointment-table-body .report-pill.is-unknown').textContent(), 'Pending');
            assert.match(await page.locator('#appointment-table-body .bf-report-qa-separator').textContent(), /excluded from totals/);
            assert.equal(await page.locator('#appointment-table-body .bf-report-qa-reject').textContent(), 'Reject');
            assert.equal(await page.locator('#appointment-table-body [data-appointment-edit-id]').count(), 2);
            assert.equal(await page.locator('#appointments-total').textContent(), '2', 'rejected list row does not inflate the stored total');
            for (const [service, team] of [['', ''], ['Praxis_EEP', ''], ['', 'Praxis_IEP'], ['Praxis_EEP', 'Praxis_EEP']]) {
                await page.selectOption('#service-filter', service);
                await page.selectOption('#team-filter', team);
                const before = requests.length;
                await page.evaluate(() => Promise.all([__metricsTest.loadMetrics(), __metricsTest.loadHistory()]));
                for (const request of requests.slice(before)) {
                    assert.equal(request.searchParams.get('service') || '', service);
                    assert.equal(request.searchParams.get('team') || '', team);
                }
                assert.match(await page.locator('#agent-table-body').textContent(), /Christina Sparks/);
                assert.match(await page.locator('#agent-metrics-source').textContent(), /2026-09-25/);
            }
            mode = 'delayed'; release = null;
            await page.evaluate(() => { window.metricsPending = __metricsTest.loadMetrics().catch(() => {}); });
            assert.match(await page.locator('#agent-table-body').textContent(), /Loading agent metrics/);
            // Wait on the request interception without a fixed sleep.
            while (!release) await new Promise(resolve => setImmediate(resolve));
            mode = 'failure'; release();
            await page.evaluate(() => window.metricsPending);
            const failed = await page.locator('#agent-table-body').textContent();
            assert.match(failed, /could not be loaded/);
            assert.ok(!failed.includes('No agents match'));
            assert.equal(await page.locator('#agent-summary').textContent(), 'Unavailable');
            assert.equal(await page.locator('[data-loading-source="metrics"]').getAttribute('aria-busy'), null);
            mode = 'empty';
            await page.evaluate(() => __metricsTest.loadMetrics());
            assert.match(await page.locator('#agent-table-body').textContent(), /No agents match/);
            mode = 'normal';
            await page.evaluate(() => __metricsTest.loadMetrics());
            assert.match(await page.locator('#agent-table-body').textContent(), /Christina Sparks/);
        }
        assert.deepEqual(errors, []);
        console.log('PASS processed metrics browser: service/team propagation, loading, unavailable, empty, recovery, both themes.');
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
