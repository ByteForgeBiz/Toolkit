const { chromium } = require('playwright');
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
(async () => {
    const browser = await chromium.launch({ channel: 'msedge', headless: true });
    try {
        for (const theme of ['light', 'dark']) {
            for (const existing of [false, true]) {
                const page = await browser.newPage({ colorScheme: theme, viewport: { width: 375, height: 812 } });
                let available = true;
                await page.route('http://versions.test/**', route => {
                    if (route.request().url().endsWith('/api/auth/version')) return route.fulfill({
                        status: available ? 200 : 503, contentType: 'application/json',
                        body: JSON.stringify({ version: '2.4.26.1020b <test>' })
                    });
                    if (route.request().url().endsWith('/footer.js')) return route.fulfill({
                        contentType: 'text/javascript', body: fs.readFileSync(path.join(__dirname, 'ByteForge-version-footer.js'))
                    });
                    if (route.request().url().endsWith('/footer.css')) return route.fulfill({
                        contentType: 'text/css', body: fs.readFileSync(path.join(__dirname, '../CSS/ByteForge-footer.css'))
                    });
                    if (route.request().url().endsWith('/host')) return route.fulfill({ contentType: 'text/html',
                        body: '<iframe title="Application" src="/" style="width:100%;height:700px;border:0"></iframe>' });
                    return route.fulfill({ contentType: 'text/html', body: '<!doctype html><html><head><link rel="stylesheet" href="footer.css">' +
                        '<style>body{margin:0;color:#172033;background:#fff}@media(prefers-color-scheme:dark){body{color:#f2f5fa;background:#111720}}</style>' +
                        '</head><body><main>Content</main><dialog><footer>Dialog actions</footer></dialog>' +
                        (existing ? '<footer><span>Existing footer</span></footer>' : '') +
                        '<script src="footer.js" data-version-url="api/auth/version"></script></body>' });
                });
                await page.goto('http://versions.test/');
                await page.locator('.bf-version-footer').waitFor();
                assert.equal(await page.locator('body > footer').count(), 1);
                assert.equal(await page.locator('.bf-version-footer').textContent(), 'Version 2.4.26.1020b <test>');
                assert.equal(await page.locator('test').count(), 0, 'metadata is rendered as text');
                assert.equal(await page.locator('dialog footer').textContent(), 'Dialog actions');
                if (existing) assert.equal(await page.locator('body > footer span').textContent(), 'Existing footer');
                await page.addScriptTag({ url: 'footer.js', type: 'text/javascript' });
                // An injected script without its endpoint is a no-op; re-running the configured script is idempotent.
                await page.evaluate(() => {
                    const script = document.createElement('script'); script.src = 'footer.js';
                    script.setAttribute('data-version-url', 'api/auth/version'); document.body.appendChild(script);
                });
                await page.waitForLoadState('networkidle');
                assert.equal(await page.locator('.bf-version-footer').count(), 1);
                await page.locator('.bf-version-footer').evaluate(label => { label.textContent += ' ' + 'long-reference-'.repeat(30); });
                const style = await page.locator('body > footer').evaluate(footer => ({
                    color: getComputedStyle(footer).color, bodyColor: getComputedStyle(document.body).color,
                    position: getComputedStyle(footer).position, overflow: document.documentElement.scrollWidth > innerWidth
                }));
                assert.equal(style.color, style.bodyColor, 'footer follows the active theme');
                assert.equal(style.position, 'static');
                assert.equal(style.overflow, false, 'long references wrap at mobile width');
                await page.goto('http://versions.test/host');
                await page.frameLocator('iframe').locator('.bf-version-footer').waitFor();
                assert.equal(await page.frameLocator('iframe').locator('.bf-version-footer').textContent(), 'Version 2.4.26.1020b <test>');
                await page.goto('http://versions.test/');
                available = false;
                await page.reload();
                await page.waitForLoadState('networkidle');
                assert.equal(await page.locator('.bf-version-footer').count(), 0);
                assert.equal(await page.locator('main').textContent(), 'Content');
                await page.close();
            }
        }
        console.log('PASS: light/dark and iframe footers, mobile wrapping, idempotency, dialog preservation, safe text and unavailable version fallback');
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
