const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require('playwright');
(async () => {
    const root = process.env.PRAXIS_WORKSPACE || process.cwd();
    const browser = await chromium.launch({ headless: true, channel: 'msedge' });
    try {
        const page = await browser.newPage();
        const errors = [];
        page.on('pageerror', error => errors.push(error.message));
        let status = 401, adminStatus = 200;
        await page.route('https://praxis.test/**', route => {
            const url = new URL(route.request().url());
            if (url.pathname === '/api/configuration') return route.fulfill({ status, json: status === 200
                ? { services: [], teams: [], reportTimeZone: 'Pacific', reportIanaTimeZone: 'America/Los_Angeles', reportDialingDays: [1,2,3,4,5] }
                : { message: status === 401 ? 'Missing or invalid bearer token.' : 'Report service unavailable.' } });
            if (url.pathname === '/api/rbac/me') return route.fulfill({ status: adminStatus, json: adminStatus === 500
                ? { message: 'Global RBAC could not complete the request.', requestId: '6bdb89f261f2' } : {} });
            if (url.pathname.startsWith('/api/')) return route.fulfill({ json: {} });
            const file = path.join(root, 'Praxis.UI', url.pathname);
            if (!fs.existsSync(file)) return route.fulfill({ status: 404 });
            let body = fs.readFileSync(file);
            if (url.pathname.endsWith('.html')) body = body.toString().replace(/<script\b[^>]*>[\s\S]*?<\/script>/g, '');
            return route.fulfill({ body, contentType: url.pathname.endsWith('.css') ? 'text/css' : 'text/html' });
        });
        for (const theme of ['light', 'dark']) {
            await page.emulateMedia({ colorScheme: theme });
            for (status of [401, 503]) {
                await page.goto('https://praxis.test/report.html');
                await page.evaluate(() => {
                    localStorage.setItem('praxis.reportPortal.persistentAccessToken', 'fixture-session');
                    window.redirects = 0;
                    window.ByteForgeSortIndicator = { update() {} };
                    window.ByteForgePraxisAuth = { redirect() { window.redirects++; } };
                    window.ByteForgePraxisAdmin = { clear() {}, select() {}, load() { return Promise.resolve([{ key: 'PraxisReports', label: 'Reports', route: 'report.html' }]); } };
                });
                const source = fs.readFileSync(path.join(root, 'ByteForge.Toolkit/HTML/Praxis/report-portal.js'), 'utf8')
                    .replace('    restoreSession();', '    window.__reportErrorTest = { apiPost, hideMessage }; restoreSession();');
                await page.addScriptTag({ content: source });
                await page.locator('#report-message').waitFor({ state: 'visible' });
                const text = await page.locator('#report-message').textContent();
                assert.match(text, new RegExp('HTTP ' + status + '; api/configuration'));
                assert.match(text, status === 401 ? /Missing or invalid bearer token/ : /Report service unavailable/);
                assert.equal(await page.locator('#report-content').isVisible(), false);
                assert.equal(await page.locator('#sign-out-button').isVisible(), true);
                assert.equal(await page.evaluate(() => redirects), 0);
                assert.equal(await page.evaluate(() => localStorage.getItem('praxis.reportPortal.persistentAccessToken')), 'fixture-session');
                assert.equal(new URL(page.url()).pathname, '/report.html');
                const saveError = await page.evaluate(() => __reportErrorTest.apiPost('api/configuration', {}).then(() => '', error => error.message));
                assert.match(saveError, new RegExp('HTTP ' + status));
                assert.equal(await page.evaluate(() => redirects), 0);
                await page.locator('#sign-out-button').click();
                assert.equal(await page.evaluate(() => redirects), 1);
                assert.equal(await page.evaluate(() => localStorage.getItem('praxis.reportPortal.persistentAccessToken')), null);
            }
        }
        status = 200;
        for (const theme of ['light', 'dark']) {
            await page.emulateMedia({ colorScheme: theme });
            for (adminStatus of [500, 403]) {
                await page.goto('https://praxis.test/report.html');
                await page.evaluate(() => {
                    localStorage.setItem('praxis.reportPortal.persistentAccessToken', 'fixture-session');
                    window.redirects = 0;
                    window.ByteForgeSortIndicator = { update() {} };
                    window.ByteForgePraxisAuth = { redirect() { window.redirects++; } };
                    window.ByteForgePraxisAdmin = { clear() {}, select() {}, load() { return Promise.resolve([{ key: 'PraxisReports', label: 'Reports', route: 'report.html' }]); } };
                });
                const source = fs.readFileSync(path.join(root, 'ByteForge.Toolkit/HTML/Praxis/report-portal.js'), 'utf8')
                    .replace('    restoreSession();', '    window.__reportErrorTest = { apiPost, hideMessage }; restoreSession();');
                await page.addScriptTag({ content: source });
                await page.locator('#report-content').waitFor({ state: 'visible' });
                if (adminStatus === 500) {
                    await page.locator('#report-service-message').waitFor({ state: 'visible' });
                    assert.match(await page.locator('#report-service-message').textContent(), /HTTP 500; api\/rbac\/me/);
                    assert.match(await page.locator('#report-service-message').textContent(), /6bdb89f261f2/);
                    await page.evaluate(() => __reportErrorTest.hideMessage());
                    assert.equal(await page.locator('#report-service-message').isVisible(), true, 'Report updates must not erase the administration warning');
                } else {
                    assert.equal(await page.locator('#report-service-message').isVisible(), false, 'Ordinary non-admin denial is not a service outage');
                }
                assert.equal(await page.evaluate(() => redirects), 0);
                assert.equal(await page.evaluate(() => localStorage.getItem('praxis.reportPortal.persistentAccessToken')), 'fixture-session');
            }
        }
        assert.deepEqual(errors, []);
        console.log('PASS: persistent administration 500/reference warning, normal non-admin 403, and startup and save 401/503 errors stay on report, preserve sign-in, show status/detail, and allow explicit sign-out in both themes.');
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
