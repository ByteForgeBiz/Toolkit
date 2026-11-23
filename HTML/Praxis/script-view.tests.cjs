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
            let missing = false, revision = 1;
            await page.route('https://praxis.test/**', async route => {
                const url = new URL(route.request().url());
                if (url.pathname === '/api/public-scripts') {
                    assert.equal(route.request().headers().authorization, undefined);
                    assert.equal(url.searchParams.get('scriptKey'), 'praxis-ipo');
                    return route.fulfill({ status: missing ? 404 : 200, json: missing ? {} : { scripts: [{ scriptKey: 'praxis-ipo', displayName: 'IPO', updatedAtUtc: '2026-10-07T12:00:00', bodyHtml: '<h2>Opening</h2><p>Hello {CustomerFirst}</p><h2>Closing</h2><p>Revision ' + revision + '</p><img src=x onerror="window.injected=true"><script>window.injected=true</script>' }] } });
                }
                const file = path.join(root, 'PraxisAPI', url.pathname);
                return route.fulfill({ body: fs.readFileSync(file), contentType: file.endsWith('.html') ? 'text/html' : file.endsWith('.css') ? 'text/css' : 'application/javascript', headers: { 'Content-Security-Policy': 'frame-ancestors https://host.test' } });
            });
            await page.route('https://host.test/**', route => route.fulfill({ body: '<iframe src="https://praxis.test/script-view.html?scriptKey=praxis-ipo" width="420" height="700"></iframe>', contentType: 'text/html' }));
            await page.goto('https://host.test/');
            const frame = page.frameLocator('iframe');
            await frame.locator('.bf-script-text').waitFor();
            assert.match(await frame.locator('#script-content').innerText(), /Opening[\s\S]*Hello \{CustomerFirst\}[\s\S]*Closing[\s\S]*Revision 1/);
            assert.equal(await frame.locator('#script-content img, #script-content script').count(), 0);
            assert.equal(await page.frames()[1].evaluate(() => window.injected), undefined);
            revision = 2;
            await frame.locator('#script-refresh').click();
            await frame.getByText('Revision 2', { exact: true }).waitFor();
            missing = true;
            await frame.locator('#script-refresh').click();
            await frame.getByText('This script is unavailable.', { exact: true }).waitFor();
            assert.equal(await frame.locator('.bf-script-text').count(), 0);
            assert.deepEqual(errors, []);
            await page.close();
        }
        console.log('Public script iframe, light/dark, sanitization, refresh and 404 checks passed.');
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
