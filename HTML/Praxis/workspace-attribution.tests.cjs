const assert = require('node:assert/strict');
const fs = require('node:fs');
const { chromium } = require('playwright');

(async () => {
    const browser = await chromium.launch({ headless: true, channel: 'msedge' });
    try {
        const page = await browser.newPage();
        const failures = [];
        page.on('pageerror', error => failures.push(error.message));
        const view = fs.readFileSync('PraxisAPI/Views/Workspace/Index.cshtml', 'utf8');
        const dialog = view.match(/<dialog id="bf-workspace-attribution-warning"[\s\S]*?<\/dialog>/)[0]
            .replace('@Model.AttributionWarning', fs.readFileSync('PraxisAPI/Services/WorkspaceService.cs', 'utf8').match(/model\.AttributionWarning = "([^"]+)"/)[1]);
        const css = fs.readFileSync('PraxisAPI/Content/Praxis/workspace.css', 'utf8') +
            fs.readFileSync('ByteForge.Toolkit/HTML/Praxis/workspace-attribution.css', 'utf8');
        const script = fs.readFileSync('ByteForge.Toolkit/HTML/Praxis/workspace.js', 'utf8');
        for (const colorScheme of ['light', 'dark']) {
            for (const width of [1000, 390]) {
                await page.emulateMedia({ colorScheme });
                await page.setViewportSize({ width, height: 700 });
                await page.setContent(`<style>${css}</style><form id="workspace-form"><button type="submit">Save changes</button></form>${dialog}`);
                await page.addScriptTag({ content: script });
                assert.equal(await page.locator('dialog').evaluate(node => node.matches(':modal')), true);
                const box = await page.locator('dialog').boundingBox();
                assert.ok(box.x >= 0 && box.x + box.width <= width);
                assert.equal(await page.locator('dialog button').evaluate(node => node === document.activeElement), true);
                await page.screenshot({ path: `Logs/workspace-attribution-${colorScheme}-${width}.png` });
                await page.keyboard.press('Escape');
                assert.equal(await page.locator('dialog').evaluate(node => node.open), false);
                await page.waitForFunction(() => document.querySelector('#workspace-form button') === document.activeElement);
                await page.locator('dialog').evaluate(node => node.showModal());
                await page.locator('dialog button').click();
                assert.equal(await page.locator('dialog').evaluate(node => node.open), false);
            }
        }
        await page.setContent('<form id="workspace-form"><button type="submit">Save changes</button></form>');
        await page.addScriptTag({ content: script });
        assert.equal(await page.locator('dialog').count(), 0);
        assert.deepEqual(failures, []);
        console.log('PASS warning modal: light/dark, desktop/mobile, focus, Escape, close, and no-warning state');
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
