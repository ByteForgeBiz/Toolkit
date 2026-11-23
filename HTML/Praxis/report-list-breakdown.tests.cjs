// Browser regression against the real report DOM. Requires Playwright and Edge.
const fs = require('fs'), path = require('path'), assert = require('node:assert/strict');
const { chromium } = require('playwright');

(async () => {
    const root = process.env.PRAXIS_WORKSPACE || path.resolve(__dirname, '../../..');
    const browser = await chromium.launch({ channel: 'msedge', headless: true });
    try {
        const page = await browser.newPage();
        const errors = [];
        page.on('pageerror', error => errors.push(error.message));
        await page.route('https://praxis.test/**', async route => {
            const url = new URL(route.request().url());
            const file = path.join(root, 'PraxisAPI', url.pathname);
            if (!fs.existsSync(file)) return route.fulfill({ status: 404, body: '' });
            let body = fs.readFileSync(file);
            if (url.pathname.endsWith('.html')) body = body.toString().replace(/<script\b[^>]*>[\s\S]*?<\/script>/g, '');
            return route.fulfill({ body, contentType: url.pathname.endsWith('.css') ? 'text/css' : 'text/html' });
        });
        await page.goto('https://praxis.test/report.html');
        await page.evaluate(() => { window.ByteForgeSortIndicator = { update() {} }; });
        let source = fs.readFileSync(path.join(__dirname, 'report-portal.js'), 'utf8');
        source = source.replace('    restoreSession();', '    window.__listTest = { state, renderProgress };');
        await page.addScriptTag({ content: source });
        const item = { listName: 'Praxis_EEP_0920', totalRecords: 678, completedRecords: 40, remainingRecords: 638,
            attemptBuckets: [240, 200, 120, 60, 40, 18].map((phoneCount, attempts) => ({ phoneCount, attempts })),
            dispositions: [{ disposition: 'No disposition', records: 638 }, { disposition: 'Wrong Number', records: 40 }] };
        await page.evaluate(item => {
            document.getElementById('report-content').hidden = false;
            __listTest.state.progress = { items: [item, { ...item, listName: 'Praxis_IEP_0921' }], missingConfiguredLists: [] };
            __listTest.renderProgress();
        }, item);
        for (const colorScheme of ['light', 'dark']) {
            await page.emulateMedia({ colorScheme });
            for (const width of [1280, 375]) {
                await page.setViewportSize({ width, height: 1000 });
                assert.equal(await page.locator('.bf-list-attempt-column').count(), 12);
                assert.ok(await page.locator('.bf-list-attempt-column').first().getAttribute('aria-label') === '0 attempts: 240 phone numbers');
                assert.ok(await page.locator('.bf-list-card').evaluateAll(cards => cards.every(card => card.scrollWidth <= card.clientWidth + 1)));
                const first = page.locator('.bf-list-dispositions').first();
                await first.locator('summary').click();
                assert.equal(await first.getAttribute('open'), '');
                assert.ok((await first.textContent()).includes('Wrong Number'));
                await first.locator('summary').click();
                if (process.env.PRAXIS_SCREENSHOTS) {
                    await page.locator('[data-loading-source="progress"]').screenshot({ path: path.join(process.env.PRAXIS_SCREENSHOTS, `lists-${colorScheme}-${width}.png`) });
                }
            }
        }
        await page.evaluate(() => {
            __listTest.state.progress = { items: [], missingConfiguredLists: ['Unmapped list'] };
            __listTest.renderProgress();
        });
        assert.ok((await page.locator('#list-progress').textContent()).includes('Unmapped list: no mapped source records found.'));
        await page.evaluate(() => {
            __listTest.state.progress = { items: [{ listName: '<script>unsafe</script>', totalRecords: 0, completedRecords: 0, remainingRecords: 0 }] };
            __listTest.renderProgress();
        });
        assert.equal(await page.locator('#list-progress script').count(), 0);
        assert.ok((await page.locator('#list-progress').textContent()).includes('Attempt breakdown unavailable.'));
        assert.deepEqual(errors, []);
        console.log('PASS calling-list cards: light/dark, desktop/mobile, dispositions, missing data, safe text.');
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
