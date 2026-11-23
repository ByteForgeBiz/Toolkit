// Run with Node and Playwright available through NODE_PATH. No application server required.
const { chromium } = require('playwright');
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const htmlRoot = path.resolve(__dirname, '..');
const css = fs.readFileSync(path.join(htmlRoot, 'CSS/ByteForge-date-time-picker.css'), 'utf8');
const script = fs.readFileSync(path.join(__dirname, 'ByteForge-date-time-picker.js'), 'utf8');
const workspaceCssPath = path.resolve(htmlRoot, '../../PraxisAPI/Content/Praxis/workspace.css');
const workspaceCss = fs.existsSync(workspaceCssPath) ? fs.readFileSync(workspaceCssPath, 'utf8') : '';

(async () => {
    const browser = await chromium.launch({ channel: 'msedge', headless: true });
    try {
        const page = await browser.newPage({ viewport: { width: 1100, height: 800 } });
        const errors = [];
        page.on('pageerror', error => errors.push(error.message));
        for (const theme of ['light', 'dark']) {
            for (const [width, height] of [[640, 480], [400, 350], [280, 300]]) {
                const frameHtml = `<style>
                    ${workspaceCss}
                    :root {color-scheme:${theme}; --panel:light-dark(#fff,#1a2230); --text:light-dark(#172033,#f2f5fa); --muted:light-dark(#667085,#aab4c3); --border:light-dark(#dce3ed,#344054); --input:light-dark(#fff,#111827); --accent:light-dark(#1769e0,#67a7ff)}
                    * {box-sizing:border-box} body {margin:0;padding:12px;font:14px 'Segoe UI';background:var(--panel);color:var(--text)}
                    input {background:var(--input);color:var(--text);border:1px solid var(--border);padding:8px}
                    footer {position:sticky;bottom:0;height:60px;background:var(--panel)}
                    ${css}</style><div style="height:180px">Agent script</div>
                    <input id="value" value="2026-09-14T09:15"><div id="picker"></div><div style="height:200px">Notes</div><footer>Save record</footer>
                    <script>${script}</script><script>window.picker = ByteForgeDateTimePicker.createPicker({root:document.querySelector('#picker'),input:document.querySelector('#value')});</script>`;
                await page.setContent(`<iframe style="width:${width}px;height:${height}px" title="Agent script"></iframe>`);
                await page.locator('iframe').evaluate((el, html) => { el.srcdoc = html; }, frameHtml);
                const frame = page.frameLocator('iframe');
                await frame.locator('.bf-date-time-toggle').click();
                const layout = await frame.locator('.bf-date-time-popup').evaluate(el => {
                    const box = el.getBoundingClientRect();
                    const actions = el.querySelector('.bf-date-time-actions').getBoundingClientRect();
                    return { left: box.left, right: box.right, width: innerWidth, bottom: actions.bottom, height: innerHeight,
                        overflow: document.documentElement.scrollWidth > innerWidth,
                        clock: el.querySelector('.bf-date-time-clock').getBoundingClientRect().width };
                });
                assert(!layout.overflow && layout.left >= 0 && layout.right <= layout.width, JSON.stringify(layout));
                assert(layout.bottom <= layout.height - 60, JSON.stringify(layout));
                assert(layout.clock <= 194, JSON.stringify(layout));
                if (process.env.DTP_SCREENSHOT_DIR && width >= 400) {
                    fs.mkdirSync(process.env.DTP_SCREENSHOT_DIR, { recursive: true });
                    await page.locator('iframe').screenshot({ path:path.join(process.env.DTP_SCREENSHOT_DIR, `picker-${theme}-${width}.png`) });
                }
                await frame.locator('.bf-date-time-action-primary').click();
                assert.equal(await frame.locator('#value').inputValue(), '2026-09-14T09:15');
                console.log(`PASS ${theme} iframe ${width}x${height}: Apply reachable, no horizontal overflow`);
            }
        }
        // Use a larger frame for real mouse drags and keyboard activation.
        await page.locator('iframe').evaluate(el => { el.style.width = '640px'; el.style.height = '650px'; });
        const frame = page.frameLocator('iframe');
        await frame.locator('.bf-date-time-toggle').click();
        const clock = frame.locator('.bf-date-time-clock');
        const number = frame.getByRole('button', { name: '03 hours', exact: true });
        await number.scrollIntoViewIfNeeded();
        const start = await number.boundingBox();
        const bounds = await clock.boundingBox();
        await page.mouse.move(start.x + start.width / 2, start.y + start.height / 2);
        await page.mouse.down();
        await page.mouse.move(bounds.x + bounds.width / 2, bounds.y + bounds.height * .90, { steps: 8 });
        await page.mouse.up();
        assert.equal(await frame.getByRole('textbox', { name: 'Hour, 00 through 23' }).inputValue(), '06');
        assert.equal(await clock.getAttribute('aria-label'), 'Select minutes on an analog clock');
        // Pointer release must use the release position even without a final move event.
        await clock.evaluate(el => {
            const b = el.getBoundingClientRect();
            el.dispatchEvent(new PointerEvent('pointerdown', { bubbles:true, pointerId:1, button:0, clientX:b.left+b.width/2, clientY:b.top+b.height*.1 }));
            el.dispatchEvent(new PointerEvent('pointerup', { bubbles:true, pointerId:1, button:0, clientX:b.left+b.width*.9, clientY:b.top+b.height/2 }));
        });
        assert.equal(await frame.getByRole('textbox', { name: 'Minute, 00 through 59' }).inputValue(), '15');
        await frame.locator('.bf-date-time-action-primary').click();
        assert.equal(await frame.locator('#value').inputValue(), '2026-09-14T06:15');
        await frame.locator('.bf-date-time-toggle').click();
        await frame.getByRole('button', { name: '18 hours', exact:true }).focus();
        await page.keyboard.press('Enter');
        assert.equal(await frame.getByRole('textbox', { name: 'Hour, 00 through 23' }).inputValue(), '18');
        await frame.getByRole('button', { name: 'Cancel date and time selection' }).click();
        assert.equal(await frame.locator('#value').inputValue(), '2026-09-14T06:15');
        await frame.locator('.bf-date-time-toggle').click();
        await clock.scrollIntoViewIfNeeded();
        const cancelBounds = await clock.boundingBox();
        await page.mouse.move(cancelBounds.x + cancelBounds.width * .9, cancelBounds.y + cancelBounds.height / 2);
        await page.mouse.down();
        await clock.dispatchEvent('pointercancel', { pointerId:1 });
        const cancelledHour = await frame.getByRole('textbox', { name:'Hour, 00 through 23' }).inputValue();
        await page.mouse.move(cancelBounds.x + cancelBounds.width / 2, cancelBounds.y + cancelBounds.height * .9);
        await page.mouse.up();
        assert.equal(await frame.getByRole('textbox', { name:'Hour, 00 through 23' }).inputValue(), cancelledHour);
        assert.equal(await clock.getAttribute('aria-label'), 'Select hour on a 24-hour clock');
        await page.keyboard.press('Escape');
        assert.equal(await frame.locator('#value').inputValue(), '2026-09-14T06:15');
        const display = frame.locator('.bf-date-time-display');
        await display.fill('10/30/2026 15:3');
        await display.dispatchEvent('change');
        assert.equal(await display.evaluate(el => el.validity.customError), true);
        await display.fill('10/30/2026 15:30');
        // Form validation runs before submit and may run before a change/blur.
        assert.equal(await display.evaluate(el => el.checkValidity()), true);
        assert.equal(await frame.locator('#value').inputValue(), '2026-10-30T15:30');
        await display.fill('02/30/2026 15:30');
        assert.equal(await display.evaluate(el => el.checkValidity()), false);
        await frame.locator('.bf-date-time-toggle').click();
        await frame.locator('.bf-date-time-action-primary').click();
        assert.equal(await display.evaluate(el => el.checkValidity()), true);
        await display.fill('invalid');
        await frame.locator('.bf-date-time-toggle').click();
        await frame.getByRole('button', { name: 'Clear date and time' }).click();
        assert.equal(await display.evaluate(el => el.checkValidity()), true);
        assert.equal(await frame.locator('#value').inputValue(), '');
        console.log('PASS corrected typed date, pre-blur value synchronization, invalid date, Apply and Clear recovery');
        assert.deepEqual(errors, []);
        console.log('PASS number drag, hour-to-minute transition, final release position, keyboard selection, Cancel, pointer cancellation and Escape');
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
