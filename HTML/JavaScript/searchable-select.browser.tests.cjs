const assert = require('node:assert/strict');
const path = require('node:path');
const { chromium } = require('playwright');
(async () => {
    const browser = await chromium.launch({ headless: true, channel: 'msedge' });
    try {
        const page = await browser.newPage({ viewport: { width: 390, height: 844 } });
        await page.setContent('<style>body{font:16px Segoe UI;margin:20px}form{max-width:350px}</style>' +
            '<form><label for="advisor">Advisor</label><select id="advisor" name="FinalAdvisorId" required data-bf-searchable-select data-search-placeholder="Search advisors">' +
            '<option value="">Choose an advisor</option><option value="1" data-firm="First firm">Chris Glasscock</option>' +
            '<option value="2" data-firm="Second firm">David Roberts</option><option value="3">Frank Lepore</option><option value="4">John Mara</option>' +
            '<option value="5" disabled>Unavailable</option></select><button type="reset">Reset</button><button type="submit">Save</button></form>');
        await page.addStyleTag({ path: path.join(__dirname, '../CSS/ByteForge-searchable-select.css') });
        await page.evaluate(() => {
            window.changes = []; window.submits = 0;
            document.querySelector('form').addEventListener('submit', event => { event.preventDefault(); window.submits++; });
            document.querySelector('select').addEventListener('change', event => window.changes.push(event.target.options[event.target.selectedIndex].dataset.firm));
        });
        await page.addScriptTag({ path: path.join(__dirname, 'ByteForge-searchable-select.js') });
        const control = page.locator('#advisor-control');
        await control.click();
        await page.getByRole('combobox').fill('roberts');
        assert.equal(await page.getByRole('option').count(), 1);
        await page.getByRole('combobox').press('Enter');
        assert.equal(await page.locator('#advisor').inputValue(), '2');
        assert.deepEqual(await page.evaluate(() => window.changes), ['Second firm']);
        assert.equal(await page.evaluate(() => new FormData(document.querySelector('form')).get('FinalAdvisorId')), '2');
        assert.equal(await page.evaluate(() => window.submits), 0, 'Enter chooses without submitting the form');
        await control.press('ArrowDown'); await page.getByRole('combobox').fill('zzz');
        assert.equal(await page.getByRole('status').innerText(), 'No matching options.');
        await page.getByRole('combobox').press('Escape'); assert.equal(await control.getAttribute('aria-expanded'), 'false');
        await page.getByRole('button', { name: 'Reset', exact: true }).click();
        await page.waitForFunction(() => document.querySelector('#advisor-control').textContent.includes('Choose an advisor'));
        await page.getByRole('button', { name: 'Save', exact: true }).click();
        assert.equal(await control.getAttribute('aria-invalid'), 'true');
        assert.equal(await page.getByRole('combobox').evaluate(el => el === document.activeElement), true);
        await page.getByRole('combobox').fill(''); await page.getByRole('combobox').press('ArrowDown'); await page.getByRole('combobox').press('Enter');
        assert.equal(await page.locator('#advisor').inputValue(), '2');
        for (const colorScheme of ['light', 'dark']) {
            await page.emulateMedia({ colorScheme }); await control.click();
            assert.equal(await page.getByRole('option').count(), 4, 'disabled choice stays unavailable');
            assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
            if (process.env.QA_SCREENSHOT_DIR) await page.screenshot({ path: path.join(process.env.QA_SCREENSHOT_DIR, 'searchable-advisor-' + colorScheme + '.png'), fullPage: true });
            await page.getByRole('combobox').press('Escape');
        }
        await page.locator('#advisor').evaluate(el => { el.disabled = true; });
        await page.waitForFunction(() => document.querySelector('#advisor-control').disabled);
        console.log('PASS: searchable picker filtering, selection, posted value, events, keyboard, reset, validation, disabled state, light/dark mobile layout');
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
