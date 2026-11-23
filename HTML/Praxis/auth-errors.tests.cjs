const assert = require('node:assert/strict');
const fs = require('node:fs');
const { chromium } = require('playwright');

(async () => {
    const browser = await chromium.launch({ headless: true, channel: process.env.PRAXIS_BROWSER_CHANNEL || 'msedge' });
    try {
        const page = await browser.newPage();
        await page.route('https://praxis.test/**', route => {
            if (route.request().url().includes('/api/')) return route.fulfill({ status: 500, json: { message: 'Save failed', requestId: '123456abcdef' } });
            return route.fulfill({ contentType: 'text/html', body: '<main><h1>Advisors</h1></main>' });
        });
        await page.goto('https://praxis.test/advisors.html');
        await page.addScriptTag({ content: fs.readFileSync('ByteForge.Toolkit/HTML/Praxis/auth.js', 'utf8') });
        await page.evaluate(() => {
            for (const message of ['ResizeObserver loop completed with undelivered notifications.', 'ResizeObserver loop limit exceeded']) {
                window.dispatchEvent(new ErrorEvent('error', { message, filename: location.href }));
            }
        });
        assert.equal(await page.locator('#praxis-request-errors').count(), 0, 'Layout notifications must not claim a save/startup failure');
        await page.evaluate(() => window.dispatchEvent(new ErrorEvent('error', {
            message: 'Advisor script failed', filename: location.origin + '/Scripts/Praxis/advisors.js', error: new Error('Advisor script failed')
        })));
        assert.match(await page.locator('#praxis-request-errors').textContent(), /Advisor script failed/);
        await page.locator('#praxis-request-errors button').click();
        await page.evaluate(() => window.dispatchEvent(new ErrorEvent('error', {
            message: 'ResizeObserver loop limit exceeded', filename: location.href, error: new Error('A real exception')
        })));
        assert.match(await page.locator('#praxis-request-errors').textContent(), /ResizeObserver loop limit exceeded/);
        await page.locator('#praxis-request-errors button').click();
        await page.evaluate(() => fetch('/api/advisors', { method: 'POST' }));
        await page.locator('#praxis-request-errors').filter({ hasText: 'Save failed' }).waitFor();
        assert.match(await page.locator('#praxis-request-errors').textContent(), /123456abcdef/);
        console.log('PASS: layout notifications ignored; real script exceptions and failed saves remain visible');
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
