const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = process.env.PRAXIS_TEST_ROOT || path.resolve(__dirname, '../../..');
(async () => {
    const browser = await chromium.launch({ headless: true, channel: 'msedge' });
    try {
        for (const theme of ['light', 'dark']) {
            const page = await browser.newPage({ colorScheme: theme });
            const errors = [];
            page.on('pageerror', error => errors.push(error.message));
            let scripts = [{ scriptKey: 'praxis-all', serviceName: 'Praxis', displayName: 'Baseline', eventType: '', bodyHtml: '<h2 data-praxis-section="true">Opening</h2><p>Baseline text</p><h2 data-praxis-section="true">Closing</h2><p>Goodbye {AGENT}</p>', isActive: true, canDelete: false }];
            let reject = false, creates = 0, deletes = 0;
            await page.addInitScript(() => sessionStorage.setItem('praxis.reportPortal.accessToken', 'fixture'));
            await page.route('https://praxis.test/**', async route => {
                const url = new URL(route.request().url());
                if (url.pathname === '/api/auth/pages') return route.fulfill({ json: { pages: [{ key: 'PraxisScriptEditor', route: 'scripts.html', label: 'Script Editor' }] } });
                if (url.pathname === '/api/scripts/new') {
                    if (reject) return route.fulfill({ status: 400, json: { message: 'Another active script already uses this event type.' } });
                    const saved = { ...route.request().postDataJSON(), scriptKey: 'script-created', canDelete: true };
                    scripts.push(saved); creates++;
                    return route.fulfill({ json: saved });
                }
                if (url.pathname === '/api/scripts/script-created') {
                    deletes++; scripts = scripts.filter(script => script.scriptKey !== 'script-created');
                    return route.fulfill({ json: {} });
                }
                if (url.pathname === '/api/scripts' && route.request().method() === 'POST') {
                    const saved = { ...scripts.find(script => script.scriptKey === route.request().postDataJSON().scriptKey), ...route.request().postDataJSON() };
                    scripts = scripts.map(script => script.scriptKey === saved.scriptKey ? saved : script);
                    return route.fulfill({ json: saved });
                }
                if (url.pathname === '/api/scripts') return route.fulfill({ json: { scripts, serviceNames: ['Praxis', 'Praxis_1031', 'Praxis_EEP', 'Praxis_IEP'], eventTypes: ['ipo'], tokens: [] } });
                if (url.pathname.startsWith('/api/')) return route.fulfill({ body: '' });
                const file = path.join(root, 'PraxisAPI', url.pathname);
                if (!fs.existsSync(file)) return route.fulfill({ status: 404, body: '' });
                const contentType = file.endsWith('.html') ? 'text/html' : file.endsWith('.css') ? 'text/css' : file.endsWith('.png') ? 'image/png' : 'application/javascript';
                return route.fulfill({ body: fs.readFileSync(file), contentType });
            });
            await page.goto('https://praxis.test/scripts.html');
            await page.locator('#script-name').waitFor();
            assert.equal(await page.locator('#delete-script').isVisible(), false);
            assert.equal(await page.locator('#script-baseline-note').isVisible(), true);
            assert.equal(await page.locator('#script-service').isEnabled(), true);
            await page.screenshot({ path: path.join(root, 'Logs/quill-praxis-' + theme + '.png'), fullPage: true });
            await page.locator('#preview-script').click();
            await page.locator('.bf-praxis-full-script').waitFor();
            assert.match(await page.locator('.bf-praxis-full-script').innerText(), /Baseline text[\s\S]*Closing[\s\S]*Goodbye \{AGENT\}/);
            await page.locator('.byte-forge-modal-button').click();
            await page.locator('#new-script').click();
            assert.equal(await page.locator('#script-active').isChecked(), false);
            await page.locator('#script-name').fill('Custom script');
            await page.locator('#script-service').selectOption('Praxis_1031');
            assert.deepEqual(await page.locator('#script-event-types option').evaluateAll(options => options.map(option => option.value)), ['ipo']);
            await page.locator('#script-event-type').fill('  custom-event  ');
            await page.locator('[data-rich-text-surface]').fill('New script content');
            await page.screenshot({ path: path.join(root, 'Logs/quill-praxis-' + theme + '.png'), fullPage: true });
            await page.locator('#preview-script').click();
            await page.locator('.bf-praxis-full-script').waitFor();
            assert.match(await page.locator('.bf-praxis-full-script').innerText(), /New script content/);
            assert.equal(creates, 0, 'Preview must not save the draft');
            await page.locator('.byte-forge-modal-button').click();
            await page.locator('#script-list button').click();
            await page.getByRole('button', { name: 'Cancel', exact: true }).click();
            assert.equal(await page.locator('#script-name').inputValue(), 'Custom script');
            reject = true;
            await page.locator('#save-script').click();
            await page.locator('#script-message.is-error').waitFor();
            assert.equal(await page.locator('#script-name').inputValue(), 'Custom script');
            reject = false;
            await page.locator('#save-script').click();
            await page.waitForFunction(() => document.getElementById('script-message').textContent.startsWith('Script saved'));
            assert.equal(creates, 1);
            assert.equal(scripts[1].serviceName, 'Praxis_1031');
            assert.equal(scripts[1].eventType, 'custom-event');
            assert.equal(await page.locator('#script-event-type').inputValue(), 'custom-event');
            assert.equal(await page.locator('#script-event-types option[value="custom-event"]').count(), 1);
            assert.match(scripts[1].bodyHtml, /New script content/);
            assert.equal(await page.locator('#script-route').textContent(), 'All services / custom-event');
            await page.locator('#script-service').selectOption('Praxis_EEP');
            await page.locator('#script-list button').first().click();
            await page.getByRole('button', { name: 'Cancel', exact: true }).click();
            assert.equal(await page.locator('#script-service').inputValue(), 'Praxis_EEP');
            await page.locator('#script-event-type').fill('');
            await page.locator('#save-script').click();
            await page.waitForFunction(() => document.getElementById('script-route').textContent === 'Praxis_EEP / all events (fallback)');
            assert.equal(scripts[1].serviceName, 'Praxis_EEP');
            await page.reload();
            await page.locator('#script-list button').filter({ hasText: 'Custom script' }).click();
            assert.equal(await page.locator('#script-service').inputValue(), 'Praxis_EEP');
            await page.locator('#delete-script').click();
            await page.getByRole('button', { name: 'Cancel', exact: true }).click();
            assert.equal(deletes, 0);
            await page.locator('#delete-script').click();
            await page.getByRole('button', { name: 'Delete', exact: true }).click();
            await page.waitForFunction(() => document.getElementById('script-message').textContent === 'Script deleted.');
            assert.equal(deletes, 1);
            assert.equal(await page.locator('#script-name').inputValue(), 'Baseline');
            scripts = [];
            await page.reload();
            await page.locator('#script-empty').waitFor();
            assert.equal(await page.locator('#script-form').isVisible(), false);
            await page.locator('#new-script').click();
            assert.equal(await page.locator('#script-form').isVisible(), true);
            assert.deepEqual(errors, []);
            await page.close();
            console.log(theme + ': creation, failed save, deletion/cancel, baseline protection, unsaved changes, empty state passed');
        }
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
