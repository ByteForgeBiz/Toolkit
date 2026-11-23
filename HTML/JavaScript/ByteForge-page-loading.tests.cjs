// Run with Node and Playwright. Uses synthetic pages only; no production requests.
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const { chromium } = require('playwright');
const source = fs.readFileSync(path.join(__dirname, 'ByteForge-page-loading.js'), 'utf8');
(async () => {
    const browser = await chromium.launch({ channel: 'msedge', headless: true });
    let checks = 0;
    try {
        for (const theme of ['light', 'dark']) {
            const page = await browser.newPage({ colorScheme: theme });
            const errors = [];
            let outgoingState;
            page.on('pageerror', error => errors.push(error.message));
            page.on('console', message => { if (message.text().startsWith('loading-state:')) outgoingState = JSON.parse(message.text().slice(14)); });
            await page.route('https://loading.test/**', async route => {
                if (route.request().url().includes('/next')) await new Promise(resolve => setTimeout(resolve, 900));
                await route.fulfill({ contentType: 'text/html', body: `<html><head></head><body>
                    <a id="next" href="/next">Report</a><a id="skip" href="#section">Section</a>
                    <form action="/next"><input required name="q"><button>Search</button></form>
                    <script>${source}</script></body></html>` });
            });
            await page.goto('https://loading.test/start');
            const visible = () => page.locator('.bf-page-loading').isVisible();
            await page.evaluate(() => window.addEventListener('click', () => {
                const overlay = document.querySelector('.bf-page-loading');
                const panel = document.querySelector('.bf-page-loading-panel');
                console.log('loading-state:' + JSON.stringify({ visible: !!overlay && !overlay.hidden,
                    bg: panel && getComputedStyle(panel).backgroundColor, fg: panel && getComputedStyle(panel).color }));
            }));
            await page.locator('#next').click();
            assert.equal(outgoingState.visible, true); checks++;
            const colors = outgoingState;
            assert.equal(colors.bg, theme === 'dark' ? 'rgb(20, 35, 41)' : 'rgb(255, 255, 255)'); checks++;
            assert.notEqual(colors.bg, colors.fg); checks++;
            await page.waitForURL('**/next');
            assert.equal(await visible(), false); checks++;
            await page.goBack();
            assert.equal(await visible(), false); checks++;
            // Observe default handling without leaving the fixture for each exception.
            await page.evaluate(() => { window.testPrevent = e => e.preventDefault(); window.addEventListener('click', window.testPrevent); });
            for (const options of [
                { href: '#section' }, { href: '/start#section' }, { href: 'mailto:test@example.com' },
                { href: 'https://external.test/' }, { href: '/Export' }, { href: '/DownloadXlsx' },
                { href: '/file.pdf' }, { href: '/next', target: '_blank' },
                { href: '/next', download: true }, { href: '/next', ctrlKey: true },
                { href: '/next', metaKey: true }, { href: '/next', shiftKey: true },
                { href: '/next', off: true }, { href: '/next', cancelled: true }
            ]) {
                await page.evaluate(opts => {
                    ByteForgePageLoading.hide();
                    const a = document.createElement('a'); a.href = opts.href;
                    if (opts.target) a.target = opts.target;
                    if (opts.download) a.download = 'file';
                    if (opts.off) a.dataset.bfPageLoading = 'off';
                    if (opts.cancelled) a.onclick = e => e.preventDefault();
                    document.body.appendChild(a);
                    a.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true, ...opts }));
                    a.remove();
                }, options);
                assert.equal(await visible(), false, JSON.stringify(options)); checks++;
            }
            await page.evaluate(() => ByteForgePageLoading.show());
            if (process.env.LOADING_SCREENSHOTS) {
                fs.mkdirSync(process.env.LOADING_SCREENSHOTS, { recursive: true });
                await page.screenshot({ path: path.join(process.env.LOADING_SCREENSHOTS, theme + '.png') });
            }
            await page.keyboard.press('Escape');
            assert.equal(await visible(), false); checks++;
            await page.evaluate(() => ByteForgePageLoading.show());
            await page.getByRole('button', { name: 'Dismiss' }).click();
            assert.equal(await visible(), false); checks++;
            await page.emulateMedia({ reducedMotion: 'reduce' });
            await page.evaluate(() => ByteForgePageLoading.show());
            assert.equal(await page.locator('.bf-page-loading-spinner').evaluate(el => getComputedStyle(el).animationName), 'none'); checks++;
            await page.evaluate(() => window.dispatchEvent(new PageTransitionEvent('pageshow', { persisted: true })));
            assert.equal(await visible(), false); checks++;
            await page.evaluate(() => window.removeEventListener('click', window.testPrevent));
            // Native validation must not display an overlay; AJAX submit handlers must win.
            await page.locator('form button').click();
            assert.equal(await visible(), false); checks++;
            await page.locator('input').fill('test');
            await page.evaluate(() => { window.testSubmit = e => e.preventDefault(); document.querySelector('form').addEventListener('submit', window.testSubmit); });
            await page.locator('form button').click();
            assert.equal(await visible(), false); checks++;
            await page.evaluate(() => {
                document.querySelector('form').removeEventListener('submit', window.testSubmit);
                window.addEventListener('submit', () => console.log('loading-state:' + JSON.stringify({
                    visible: !document.querySelector('.bf-page-loading').hidden
                })));
            });
            outgoingState = null;
            await page.locator('form button').click();
            assert.equal(outgoingState.visible, true); checks++;
            assert.equal(await visible(), false); checks++;
            await page.evaluate(() => { const f = document.createElement('iframe'); f.src = '/frame'; document.body.appendChild(f); });
            await page.frameLocator('iframe').locator('#next').waitFor();
            const parentUrl = page.url();
            await page.frameLocator('iframe').locator('#next').click();
            assert.equal(page.url(), parentUrl); checks++;
            assert.equal(await visible(), false); checks++;
            assert.deepEqual(errors, []); checks++;
            await page.close();
        }
        console.log(`${checks} browser checks passed (light/dark, navigation, history, validation, AJAX, exclusions, dismissal, reduced motion).`);
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
