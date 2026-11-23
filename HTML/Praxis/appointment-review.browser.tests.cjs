const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../../..');
const app = path.join(root, 'PraxisAPI');
(async () => {
    const browser = await chromium.launch({ headless: true, channel: 'msedge' });
    try {
        for (const theme of ['light', 'dark']) {
            const page = await browser.newPage({ colorScheme: theme, viewport: { width: 1440, height: 1000 } });
            const errors = []; page.on('pageerror', error => errors.push(error.message));
            let revision = 1, conflict = false, validated = false, rejected = false, rejectionReason = '';
            const listQueries = [];
            const attempts = [];
            const record = { id: '101', contact: 'QA Customer', keyPhone: '2125550100', rrMobilePhone: '2125550100; 12125550101', companyName: 'Example', agent: 'test.agent',
                appointmentDateTime: '2026-10-02T10:30:00', appointmentTimeZone: 'America/New_York', callDateTime: '2026-09-28T10:30:00', finalAdvisorName: 'Test Advisor' };
            const detail = () => ({ editor: { record, finalAdvisorId: 1, advisors: [{ id: 1, name: 'Test Advisor', firm: 'Example' }],
                appointmentTimeZones: [{ id: 'America/New_York', displayName: 'Eastern' }] }, review: { revision: String(revision), isValidated: validated,
                    isRejected: rejected, rejectedBy: 'qa.supervisor', rejectedAtUtc: '2026-10-01T18:00:00Z', rejectionReason }, confirmationAttempts: attempts });
            await page.addInitScript(() => sessionStorage.setItem('praxis.reportPortal.accessToken', 'fixture-token'));
            await page.route('http://qa.test/**', async route => {
                const url = new URL(route.request().url());
                const json = data => route.fulfill({ contentType: 'application/json', body: JSON.stringify(data) });
                if (url.pathname === '/api/auth/pages') return json({ pages: [{ key: 'PraxisAppointmentReview', route: 'appointment-review.html', label: 'Appointment QA' }, { key: 'PraxisConfirmations', route: 'confirmations.html', label: 'Confirmations' }] });
                if (url.pathname.endsWith('/confirmation-settings')) return json({ date: null, fromDate: '2026-10-01', timeZone: 'America/Los_Angeles' });
                if (url.pathname.startsWith('/api/rbac-assets/')) return route.fulfill({ contentType: url.pathname.endsWith('style') ? 'text/css' : 'text/javascript', body: '' });
                if (['/api/appointment-reviews/101', '/api/confirmations/101'].includes(url.pathname)) {
                    if (route.request().method() === 'POST') {
                        if (conflict) return route.fulfill({ status: 409, contentType: 'application/json', body: JSON.stringify({ message: 'This appointment changed. Reload it and review the latest data.' }) });
                        const body = route.request().postDataJSON();
                        assert.equal(body.revision, String(revision));
                        if (body.confirmationAttempt) {
                            assert.equal(validated, true); record.appointmentConfirmed = body.confirmed;
                            attempts.unshift({ confirmed: body.confirmed, notes: body.notes, attemptedBy: 'qa.supervisor', attemptedAtUtc: '2026-10-01T18:00:00Z', appointmentAt: record.appointmentDateTime, appointmentTimeZone: record.appointmentTimeZone });
                        }
                        else if (body.revertRejection) { assert.equal(rejected, true); rejected = false; validated = false; rejectionReason = ""; }
                        else if (body.reject) { rejected = true; validated = false; rejectionReason = body.rejectionReason; }
                        else if (body.validate) validated = true;
                        else record[body.field[0].toLowerCase() + body.field.slice(1)] = body.value.trim();
                        revision++;
                    }
                    return json(detail());
                }
                if (['/api/appointment-reviews', '/api/confirmations'].includes(url.pathname)) {
                    listQueries.push(url.searchParams);
                    if (url.searchParams.has('search') && !url.searchParams.get('search').trim())
                        return route.fulfill({ status: 400, contentType: 'application/json', body: JSON.stringify({ message: 'Invalid list filters.' }) });
                    const confirmationRequest = url.pathname === '/api/confirmations';
                    if (confirmationRequest) {
                        for (const forbidden of ['validated', 'rejected', 'agentId', 'bookingAgent', 'confirmations']) assert.equal(url.searchParams.has(forbidden), false);
                    }
                    let visible = rejected === (url.searchParams.get('rejected') === 'true') && validated === (confirmationRequest || url.searchParams.get('validated') === 'true');
                    if (confirmationRequest || url.searchParams.get('confirmations') === 'true') {
                        const status = record.appointmentConfirmed ? 'confirmed' : attempts.length ? 'attempted' : 'notAttempted';
                        visible = visible && (!url.searchParams.has('appointmentDate') || url.searchParams.get('appointmentDate') === '2026-10-02') && (['all', status].includes(url.searchParams.get('confirmationStatus')) || url.searchParams.get('confirmationStatus') === 'unconfirmed' && !record.appointmentConfirmed);
                    }
                    return json({ items: visible ? [record] : [], totalRows: visible ? 1 : 0, totalPages: visible ? 1 : 0 });
                }
                const file = path.join(app, url.pathname);
                if (!fs.existsSync(file)) return route.fulfill({ status: 404 });
                return route.fulfill({ contentType: file.endsWith('.js') ? 'text/javascript' : file.endsWith('.css') ? 'text/css' : 'text/html', body: fs.readFileSync(file) });
            });
            async function checkAppointmentPicker(surface) {
                await page.getByRole('button', { name: 'Edit Appointment date/time', exact: true }).click();
                const stored = page.locator('#qa-field-AppointmentDateTime');
                assert.equal(await stored.getAttribute('type'), 'hidden');
                const display = page.getByRole('textbox', { name: 'Appointment date/time', exact: true });
                assert.equal(await display.inputValue(), '10/02/2026 10:30');
                await page.getByRole('button', { name: 'Open appointment date/time picker', exact: true }).click();
                await page.getByRole('button', { name: 'Cancel date and time selection', exact: true }).click();
                assert.equal(await stored.inputValue(), '2026-10-02T10:30');
                await page.getByRole('button', { name: 'Open appointment date/time picker', exact: true }).click();
                await page.screenshot({ path: path.join(root, 'Logs', surface + '-picker-open-' + theme + '.png'), fullPage: true });
                await page.getByRole('textbox', { name: 'Hour, 00 through 23', exact: true }).fill('14');
                await page.getByRole('textbox', { name: 'Minute, 00 through 59', exact: true }).fill('45');
                await page.getByRole('button', { name: 'Apply date and time selection', exact: true }).click();
                assert.equal(await stored.inputValue(), '2026-10-02T14:45');
                await page.screenshot({ path: path.join(root, 'Logs', surface + '-picker-' + theme + '.png'), fullPage: true });
                await page.getByRole('button', { name: 'Cancel editing Appointment date/time', exact: true }).click();
                assert.equal(record.appointmentDateTime, '2026-10-02T10:30:00');
                await page.getByRole('button', { name: 'Edit Appointment date/time', exact: true }).click();
                await page.getByRole('textbox', { name: 'Appointment date/time', exact: true }).fill('10/02/2026 14:45');
                await page.getByRole('button', { name: 'Save Appointment date/time', exact: true }).click();
                await page.getByText('Correction saved.', { exact: false }).waitFor();
                assert.equal(record.appointmentDateTime, '2026-10-02T14:45');
                record.appointmentDateTime = '2026-10-02T10:30:00';
            }
            await page.goto('http://qa.test/appointment-review.html');
            await page.getByRole('button', { name: 'QA Customer', exact: true }).waitFor();
            assert.equal(listQueries.at(-1).has('search'), false, 'initial blank search is omitted');
            for (const input of ['  QA Customer  ', '   ', '']) {
                await page.locator('#qa-search').fill(input);
                const response = page.waitForResponse(r => new URL(r.url()).pathname === '/api/appointment-reviews');
                await page.locator('#qa-search-form').evaluate(form => form.requestSubmit());
                assert.equal((await response).status(), 200);
                await page.waitForFunction(() => document.getElementById('qa-workspace').getAttribute('aria-busy') === 'false');
                assert.equal(listQueries.at(-1).get('search'), input.trim() || null, 'search is trimmed or omitted');
            }
            await page.getByRole('button', { name: 'QA Customer', exact: true }).click();
            await page.getByRole('button', { name: 'Edit Contact name', exact: true }).waitFor();
            assert.equal(await page.locator('#qa-fields input').count(), 0, 'saved fields display as values');
            assert.equal(await page.locator('[data-field=KeyPhone] dd span').textContent(), '(212) 555-0100');
            assert.equal(await page.locator('[data-field=RrMobilePhone] dd span').textContent(), '(212) 555-0100; (212) 555-0101');
            await checkAppointmentPicker('qa');
            await page.getByRole('button', { name: 'Edit Phone', exact: true }).click();
            assert.equal(await page.locator('#qa-field-KeyPhone').inputValue(), '2125550100');
            await page.getByRole('button', { name: 'Cancel editing Phone', exact: true }).click();
            await page.getByRole('button', { name: 'Edit Appointment time zone', exact: true }).click();
            assert.equal(await page.locator('#qa-field-AppointmentTimeZone option:checked').textContent(), 'Eastern');
            await page.getByRole('button', { name: 'Cancel editing Appointment time zone', exact: true }).click();
            await page.getByRole('button', { name: 'Edit Contact name', exact: true }).click();
            await page.locator('#qa-field-Contact').fill('Corrected customer');
            assert.equal(await page.locator('#qa-validate').isDisabled(), true);
            await page.getByRole('button', { name: 'Save Contact name', exact: true }).click();
            await page.getByText('Correction saved.', { exact: false }).waitFor();
            assert.equal(await page.locator('#qa-fields input').count(), 0, 'save returns to read view');
            await page.getByRole('button', { name: 'Edit Company', exact: true }).click();
            await page.locator('#qa-field-CompanyName').fill('Discard this company');
            await page.getByRole('button', { name: 'Cancel editing Company', exact: true }).click();
            assert.equal(record.companyName, 'Example');
            await page.locator('#qa-splitter').focus(); await page.keyboard.press('ArrowRight');
            assert.equal(await page.locator('#qa-splitter').getAttribute('aria-valuenow'), '42');
            await page.evaluate(() => { document.activeElement.blur(); window.scrollTo(0, 0); });
            await page.screenshot({ path: path.join(root, 'Logs', 'appointment-qa-' + theme + '.png'), fullPage: true });
            conflict = true;
            await page.locator('#qa-validate').click();
            await page.getByText('This appointment changed.', { exact: false }).waitFor();
            assert.equal(await page.locator('#qa-validate').isDisabled(), true);

            assert.equal(errors.length, 0);
            page.on('dialog', dialog => dialog.accept());
            conflict = false;
            await page.locator('#qa-reload').click();
            await page.waitForFunction(() => !document.getElementById('qa-validate').disabled);
            await page.locator('#qa-validate').click();
            await page.getByText('Appointment validated and available', { exact: false }).waitFor();
            assert.equal(validated, true); assert.deepEqual(errors, []);
            await page.getByRole('link', { name: 'Confirmations', exact: true }).click();
            await page.getByRole('button', { name: 'Corrected customer', exact: true }).click();
            await checkAppointmentPicker('confirmations');
            assert.equal(new URL(page.url()).pathname, '/confirmations.html');
            assert.equal(await page.locator('[data-field=KeyPhone] dd span').textContent(), '(212) 555-0100');
            assert.equal(await page.locator('#qa-revert').isHidden(), true);
            assert.equal(await page.locator('#qa-reject').isHidden(), true);
            assert.equal(await page.getByRole('button', { name: 'Edit Agent login', exact: true }).count(), 0);
            assert.equal(await page.locator('#qa-date').inputValue(), '');
            assert.equal(await page.locator('#qa-confirmation-status').inputValue(), 'unconfirmed');
            await page.locator('#qa-date').fill('2026-10-03');
            await page.waitForFunction(() => document.querySelectorAll('#qa-rows tr').length === 0);
            await page.locator('#qa-date').fill('2026-10-02');
            await page.getByRole('button', { name: 'Corrected customer', exact: true }).waitFor();
            await page.locator('#qa-date').fill('');
            await page.getByRole('button', { name: 'Corrected customer', exact: true }).click();
            await page.locator('#qa-attempt-notes').fill('Left voicemail');
            await page.locator('#qa-attempt-form button').click();
            await page.getByText('Confirmation attempt saved.', { exact: false }).waitFor();
            assert.equal(validated, true, 'attempt does not revoke approval');
            assert.equal(attempts.length, 1);
            await page.locator('#qa-confirmation-status').selectOption('attempted');
            await page.getByRole('button', { name: 'Corrected customer', exact: true }).click();
            await page.locator('#qa-attempt-outcome').selectOption('true');
            await page.locator('#qa-attempt-notes').fill('Confirmed with contact');
            await page.locator('#qa-attempt-form button').click();
            await page.getByText('Confirmation attempt saved.', { exact: false }).waitFor();
            assert.equal(await page.locator('#qa-rows tr').count(), 0, 'confirmed booking leaves attempted filter');
            await page.locator('#qa-confirmation-status').selectOption('confirmed');
            await page.getByRole('button', { name: 'Corrected customer', exact: true }).click();
            await page.waitForFunction(() => document.querySelectorAll('#qa-attempts li').length === 2);
            assert.equal(await page.locator('#qa-attempts li').count(), 2, 'attempt history retained');
            assert.equal(validated, true, 'confirmation does not revoke approval');
            await page.screenshot({ path: path.join(root, 'Logs', 'appointment-confirmations-' + theme + '.png'), fullPage: true });
            await page.getByRole('link', { name: 'Appointment QA', exact: true }).click();
            await page.locator('#qa-filter').selectOption('true');
            await page.getByRole('button', { name: 'Corrected customer', exact: true }).click();
            await page.locator('#qa-reject').waitFor();
            page.removeAllListeners('dialog');
            page.once('dialog', dialog => dialog.dismiss());
            await page.locator('#qa-reject').click();
            assert.equal(rejected, false, 'cancel does not reject');
            page.once('dialog', dialog => dialog.accept('Duplicate appointment'));
            await page.locator('#qa-reject').click();
            await page.getByText('QA Rejected. Appointment retained', { exact: false }).waitFor();
            assert.equal(rejected, true); assert.equal(validated, false);
            assert.equal(await page.locator('#qa-rows tr').count(), 0, 'rejected appointment leaves validated list');
            await page.locator('#qa-filter').selectOption('rejected');
            await page.getByRole('button', { name: 'Corrected customer', exact: true }).click();
            await page.getByText('QA Rejected by qa.supervisor', { exact: false }).waitFor();
            assert.match(await page.locator('#qa-decision').textContent(), /Duplicate appointment/);
            assert.equal(await page.locator('#qa-fields button').count(), 0, 'rejected detail is read-only');
            assert.equal(await page.locator('#qa-validate').isHidden(), true);
            assert.equal(await page.locator('#qa-reject').isHidden(), true);
            page.once('dialog', dialog => dialog.dismiss());
            await page.locator('#qa-revert').click();
            assert.equal(rejected, true, 'cancel preserves rejection');
            page.once('dialog', dialog => dialog.accept());
            await page.locator('#qa-revert').click();
            await page.getByText('Rejection reverted.', { exact: false }).waitFor();
            assert.equal(rejected, false); assert.equal(validated, false);
            assert.equal(await page.locator('#qa-rows tr').count(), 0);
            await page.locator('#qa-filter').selectOption('false');
            await page.getByRole('button', { name: 'Corrected customer', exact: true }).click();
            await page.getByRole('button', { name: 'Edit Contact name', exact: true }).waitFor();
            assert.equal(await page.locator('#qa-revert').isHidden(), true);
            assert.deepEqual(errors, []);
            await page.setViewportSize({ width: 390, height: 844 });
            assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true);
            await page.close();
            console.log('PASS: ' + theme + ' search, editing, concurrency, validation, confirmation attempts/history/filters, rejection and mobile width');
        }
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
