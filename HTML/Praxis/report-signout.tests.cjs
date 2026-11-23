const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require('playwright');
(async () => {
    const browser = await chromium.launch({ headless: true, channel: 'msedge' });
    try {
        for (const theme of ['light', 'dark']) {
            const page = await browser.newPage({ colorScheme: theme });
            let accessRequests = 0;
            await page.route('https://praxis.test/**', route => {
                const url = new URL(route.request().url());
                if (url.pathname === '/api/network-failure') return route.abort('failed');
                if (url.pathname.startsWith('/api/failure/')) return route.fulfill({
                    status: Number(url.pathname.split('/').pop()),
                    json: { message: 'Fixture request failed.', requestId: 'd79c442319ab' }
                });
                if (url.pathname === '/api/auth/pages') accessRequests++;
                if (url.pathname.startsWith('/api/')) return route.fulfill({ json: {} });
                if (url.pathname.endsWith('.js') && !url.pathname.endsWith('/auth.js'))
                    return route.fulfill({ body: '', contentType: 'application/javascript' });
                const file = path.join(process.cwd(), 'Praxis.UI', url.pathname);
                return route.fulfill({ body: fs.readFileSync(file), contentType:
                    url.pathname.endsWith('.js') ? 'application/javascript' : url.pathname.endsWith('.css') ? 'text/css' : 'text/html' });
            });
            await page.goto('https://praxis.test/report.html');
            for (const status of [401, 403, 500, 503]) {
                await page.evaluate(status => fetch('api/failure/' + status + '?private=value').catch(() => {}), status);
                await page.getByRole('alert').filter({ hasText: 'HTTP ' + status }).waitFor();
            }
            await page.evaluate(() => fetch('api/network-failure').catch(() => {}));
            await page.getByRole('alert').filter({ hasText: 'network error' }).waitFor();
            assert.equal(await page.getByRole('alert').count(), 5);
            assert.match(await page.locator('#praxis-request-errors').textContent(), /d79c442319ab/);
            assert.doesNotMatch(await page.locator('#praxis-request-errors').textContent(), /private=value/);
            await page.getByRole('button', { name: 'Dismiss request error' }).first().click();
            assert.equal(await page.getByRole('alert').count(), 4);
            await page.evaluate(() => {
                localStorage.setItem('praxis.reportPortal.persistentAccessToken', 'old-session');
                sessionStorage.setItem('praxis.reportPortal.accessToken', 'old-session');
            });
            assert.equal(await page.locator('#sign-out-button').isVisible(), true);
            await page.locator('#sign-out-button').click();
            await page.locator('#login-panel').waitFor({ state: 'visible' });
            assert.equal(await page.evaluate(() => localStorage.getItem('praxis.reportPortal.persistentAccessToken')), null);
            assert.equal(await page.evaluate(() => sessionStorage.getItem('praxis.reportPortal.accessToken')), null);
            assert.equal(accessRequests, 0, 'Explicit sign-out must not restore the old session');
            await page.close();
        }
        console.log('PASS: HTTP/network errors stay visible with references and dismissal; sign-out works without report startup scripts, clears both tokens, and opens login in light/dark.');
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
