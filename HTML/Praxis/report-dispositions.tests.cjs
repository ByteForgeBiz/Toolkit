const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const source = fs.readFileSync(__dirname + '/report-portal.js', 'utf8');
let opened;
const context = { state: {}, formatNumber: String, formatReportDate: value => value,
    openDispositionSource: (...args) => { opened = args; } };
vm.createContext(context);
vm.runInContext(source.slice(source.indexOf('    function openKpiDispositions('), source.indexOf('    function openDispositionSource(')), context);
const trigger = {};
context.state.history = { from: '2026-10-31', to: '2026-11-07', reportTimeZone: 'Pacific Standard Time',
    dispositionCountsAvailable: true, dispositions: [
        { disposition: 'No answer', calls: 40, humanContact: false },
        { disposition: 'Callback', calls: 7, humanContact: true },
        { disposition: 'callback', calls: 2, humanContact: false },
        { disposition: 'Appointment', calls: 1, humanContact: true },
        { disposition: '<script>unsafe</script>', calls: 1, humanContact: false }
    ] };
context.openKpiDispositions('dials', trigger);
assert.equal(opened[0].listName, 'Dials');
assert.equal(opened[2], 51);
assert.equal(opened[1].length, 4, 'membership changes merge into one dial disposition');
assert.equal(opened[3], trigger);
context.openKpiDispositions('contacts', trigger);
assert.equal(opened[0].listName, 'Human contacts');
assert.equal(opened[2], 8, 'only calls included by publication configuration count');
assert.equal(opened[1].length, 2);
context.state.history.dispositions.push({ disposition: 'Wrong Number', calls: 4, humanContact: false });
context.openKpiDispositions('contacts', trigger);
assert.equal(opened[2], 8, 'Wrong Numbers do not increase Human Contacts');
assert.match(opened[0].sourceContext, /Wrong Numbers: 4/);
context.state.history.dispositionCountsAvailable = false;
context.openKpiDispositions('dials', trigger);
assert.equal(opened[2], 0);
assert.match(opened[0].sourceContext, /unavailable.*Reprocess/);
context.state.history = null;
context.openKpiDispositions('contacts', trigger);
assert.match(opened[0].sourceContext, /loading or unavailable/);
console.log('PASS: card disposition totals, contact configuration, merged names, unavailable and loading states.');

(async () => {
    const path = require('node:path');
    const { chromium } = require('playwright');
    const browser = await chromium.launch({ channel: 'msedge', headless: true });
    try {
        const page = await browser.newPage();
        const root = path.resolve(__dirname, '../../..');
        const errors = [];
        page.on('pageerror', error => errors.push(error.message));
        await page.route('https://praxis.test/**', route => {
            const url = new URL(route.request().url());
            const file = path.join(root, 'Praxis.UI', url.pathname);
            if (!fs.existsSync(file)) return route.fulfill({ status: 404, body: '' });
            let body = fs.readFileSync(file);
            if (url.pathname.endsWith('.html')) body = body.toString().replace(/<script\b[^>]*>[\s\S]*?<\/script>/g, '');
            return route.fulfill({ body, contentType: url.pathname.endsWith('.css') ? 'text/css' : 'text/html' });
        });
        await page.goto('https://praxis.test/report.html');
        await page.evaluate(() => { window.ByteForgeSortIndicator = { update() {} }; });
        await page.addScriptTag({ content: source.replace('    restoreSession();', '    window.__dispositionTest = { state };') });
        await page.evaluate(() => {
            document.getElementById('report-content').hidden = false;
            __dispositionTest.state.history = { from: '2026-10-31', to: '2026-11-07', reportTimeZone: 'Pacific Standard Time',
                dispositionCountsAvailable: true, dispositions: Array.from({ length: 23 }, (_, i) => ({
                    disposition: i === 0 ? '<script>unsafe</script>' : 'Disposition ' + i,
                    calls: i + 1, humanContact: i % 2 === 0 })) };
        });
        for (const theme of ['light', 'dark']) {
            await page.emulateMedia({ colorScheme: theme });
            for (const width of [1440, 390]) {
                await page.setViewportSize({ width, height: 950 });
                const button = page.locator('[data-kpi-dispositions="dials"]');
                await button.click();
                const dialog = page.locator('dialog');
                await dialog.waitFor({ state: 'visible' });
                assert.equal(await dialog.locator('tbody tr').count(), 10);
                await dialog.getByRole('button', { name: 'Next', exact: true }).click();
                assert.match(await dialog.textContent(), /Page 2 of 3/);
                await dialog.getByRole('searchbox').fill('unsafe');
                assert.equal(await dialog.locator('tbody tr').count(), 1);
                assert.equal(await dialog.locator('tbody script').count(), 0);
                await dialog.getByRole('searchbox').fill('');
                await dialog.getByRole('button', { name: 'Disposition', exact: true }).click();
                assert.match(await dialog.textContent(), /Page 1 of 3/);
                const rect = await dialog.boundingBox();
                assert.ok(rect.x >= 0 && rect.x + rect.width <= width + 1, 'dialog fits viewport');
                const colors = await dialog.evaluate(el => ({ color: getComputedStyle(el).color, background: getComputedStyle(el).backgroundColor }));
                assert.notEqual(colors.color, colors.background);
                await page.keyboard.press('Escape');
                await page.locator('dialog').waitFor({ state: 'detached' });
                assert.equal(await button.evaluate(el => document.activeElement === el), true);
                await page.locator('[data-kpi-dispositions="contacts"]').click();
                await page.locator('dialog button', { hasText: 'Next' }).click();
                assert.equal(await page.locator('dialog tbody tr').count(), 2, 'only 12 configured contact dispositions');
                await page.getByRole('button', { name: 'Close disposition source' }).click();
            }
        }
        await page.evaluate(() => { __dispositionTest.state.history.dispositionCountsAvailable = false; });
        await page.locator('[data-kpi-dispositions="dials"]').click();
        assert.match(await page.locator('dialog').textContent(), /unavailable.*Reprocess/);
        assert.deepEqual(errors, []);
        console.log('PASS: both themes and desktop/mobile dialogs, search, sort, paging, escaped labels, contact filtering, focus return and unavailable state.');
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
