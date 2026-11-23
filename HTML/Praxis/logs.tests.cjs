const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require('playwright');
(async () => {
    const browser = await chromium.launch({ headless: true, channel: process.env.PRAXIS_BROWSER_CHANNEL || 'msedge' });
    try {
        const page = await browser.newPage();
        let allowed = false;
        let requestToken;
        await page.route('https://praxis.test/**', async route => {
            const url = new URL(route.request().url());
            if (url.pathname === '/api/config-session/logs') {
                requestToken = route.request().headers().authorization;
                return route.fulfill({ status: allowed ? 200 : 403, json: {} });
            }
            if (url.pathname === '/Admin/Logs/Chunk') return route.fulfill({ json: { Content: '<script>window.logInjected=true</script>\nolder entry\n', Before: 0 } });
            if (url.pathname === '/Admin/Logs') return route.fulfill({ body: '<h1>Application Logs</h1>' });
            if (url.pathname === '/login.html') return route.fulfill({ body: '<h1>Login</h1>' });
            const file = path.join(process.cwd(), 'Praxis.UI', url.pathname);
            if (fs.existsSync(file) && fs.statSync(file).isFile()) return route.fulfill({ body: fs.readFileSync(file),
                contentType: file.endsWith('.js') ? 'application/javascript' : file.endsWith('.css') ? 'text/css' : 'text/html' });
            return route.fulfill({ json: {} });
        });
        await page.goto('https://praxis.test/logs.html');
        await page.waitForURL('**/login.html?returnUrl=logs.html');
        await page.evaluate(() => sessionStorage.setItem('praxis.reportPortal.accessToken', 'viewer-token'));
        await page.goto('https://praxis.test/logs.html');
        await page.locator('#logs-status').filter({ hasText: 'does not grant' }).waitFor();
        assert.equal(requestToken, 'Bearer viewer-token');
        allowed = true;
        await page.reload();
        await page.waitForURL('**/Admin/Logs');
        await page.setContent(`<link rel="stylesheet" href="https://praxis.test/Content/Praxis/report.css">
            <link rel="stylesheet" href="https://praxis.test/Content/Praxis/logs.css">
            <main class="report-shell bf-log-page"><h1>Application Logs</h1>
            <a class="bf-log-open" href="?id=test" data-name="PraxisUI_test.log" data-chunk="https://praxis.test/Admin/Logs/Chunk?id=test" data-download="https://praxis.test/Admin/Logs/Download?id=test">PraxisUI_test.log</a>
            <dialog id="bf-log-dialog" class="bf-log-dialog" aria-labelledby="bf-log-title">
            <h2 id="bf-log-title"></h2><button id="bf-log-close">Close</button><a id="bf-log-download" hidden>Download</a>
            <button id="bf-log-older" data-url="https://praxis.test/Admin/Logs/Chunk?id=test" data-before="100">Load older entries</button>
            <span id="bf-log-message" role="status"></span><pre id="bf-log-preview" class="bf-log-preview">newest entry</pre></dialog></main>`);
        await page.addScriptTag({ path: 'ByteForge.Toolkit/HTML/Praxis/logs.js' });
        await page.locator('.bf-log-open').click();
        await page.locator('#bf-log-message').filter({ hasText: 'Newest entries shown.' }).waitFor();
        assert.equal(await page.locator('#bf-log-dialog').evaluate(el => el.open), true);
        assert.equal(await page.locator('#bf-log-title').textContent(), 'PraxisUI_test.log');
        assert.match(await page.locator('#bf-log-download').getAttribute('href'), /Download\?id=test/);
        assert.equal(await page.evaluate(() => window.logInjected), undefined);
        await page.locator('#bf-log-older').evaluate(el => { el.hidden = false; el.dataset.before = '100'; });
        await page.locator('#bf-log-older').click();
        await page.locator('#bf-log-message').filter({ hasText: 'Start of file loaded.' }).waitFor();
        assert.equal(await page.locator('#bf-log-older').isVisible(), false);
        assert.equal(await page.evaluate(() => window.logInjected), undefined);
        assert.match(await page.locator('#bf-log-preview').textContent(), /older entry/);
        for (const scheme of ['light', 'dark']) {
            await page.emulateMedia({ colorScheme: scheme });
            const colors = await page.locator('#bf-log-preview').evaluate(element => {
                const css = getComputedStyle(element); return [css.color, css.backgroundColor];
            });
            assert.notEqual(colors[0], colors[1]);
        }
        await page.keyboard.press('Escape');
        assert.equal(await page.locator('#bf-log-dialog').evaluate(el => el.open), false);
        await page.locator('.bf-log-open').click();
        await page.locator('#bf-log-close').click();
        assert.equal(await page.locator('#bf-log-dialog').evaluate(el => el.open), false);
        console.log('PASS: log viewer sign-in, permission denial, session exchange, safe older chunks, light/dark styles');
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
