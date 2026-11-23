const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require('playwright');
(async () => {
    const browser = await chromium.launch({ headless: true, channel: 'msedge' });
    try {
        for (const scenario of ['allowed', 'denied', 'expired', 'anonymous']) {
            const context = await browser.newContext();
            const page = await context.newPage();
            let requests = 0;
            await context.addInitScript(value => {
                if (value !== 'anonymous') sessionStorage.setItem('praxis.reportPortal.accessToken', 'fixture-token');
            }, scenario);
            await page.route('https://praxis.test/**', route => {
                const url = new URL(route.request().url());
                assert.equal(url.href.includes('fixture-token'), false, 'Tokens must not enter URLs.');
                if (url.pathname === '/api/config-session') {
                    requests++;
                    assert.equal(route.request().method(), 'POST');
                    assert.equal(route.request().headers().authorization, 'Bearer fixture-token');
                    return route.fulfill({ status: scenario === 'denied' ? 403 : scenario === 'expired' ? 401 : 200, json: {} });
                }
                if (url.pathname === '/Admin/Configuration' || url.pathname === '/login.html')
                    return route.fulfill({ contentType: 'text/html', body: '<p>Destination</p>' });
                const file = path.join(process.cwd(), 'PraxisAPI', url.pathname);
                return route.fulfill({ body: fs.readFileSync(file), contentType: url.pathname.endsWith('.html') ? 'text/html' : url.pathname.endsWith('.css') ? 'text/css' : 'application/javascript' });
            });
            await page.goto('https://praxis.test/configuration.html');
            if (scenario === 'allowed') await page.waitForURL('**/Admin/Configuration');
            else if (scenario === 'denied') {
                await page.getByText(/does not grant configuration access/).waitFor();
                assert.equal(new URL(page.url()).pathname, '/configuration.html');
                for (const colorScheme of ['light', 'dark']) {
                    await page.emulateMedia({ colorScheme });
                    await page.setViewportSize({ width: 390, height: 800 });
                    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
                }
            } else await page.waitForURL('**/login.html?returnUrl=configuration.html');
            assert.equal(requests, scenario === 'anonymous' ? 0 : 1);
            await context.close();
        }
        console.log('PASS: configuration handoff, bearer header, denied/expired/anonymous access, and light/dark mobile layout.');
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
