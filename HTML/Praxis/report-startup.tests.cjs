const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require('playwright');
(async () => {
    const browser = await chromium.launch({ headless: true, channel: 'msedge' });
    const read = file => fs.readFileSync(path.join(process.cwd(), 'ByteForge.Toolkit/HTML', file), 'utf8');
    const assets = {
        script: ['application/javascript', read('JavaScript/ByteForge-sort-indicator.js') + '\n' + read('JavaScript/ByteForge-rbac.js')],
        style: ['text/css', read('CSS/ByteForge-sort-indicator.css') + '\n' + read('CSS/ByteForge-rbac.css')],
        'account-script': ['application/javascript', read('JavaScript/ByteForge-account.js')],
        'account-style': ['text/css', read('CSS/ByteForge-account.css')]
    };
    try {
        for (const theme of ['light', 'dark']) {
            const page = await browser.newPage({ colorScheme: theme });
            const errors = [];
            let profileRequests = 0;
            let missingProfileCredentials = 0;
            page.on('pageerror', e => errors.push(e.message));
            await page.addInitScript(() => localStorage.setItem('praxis.reportPortal.persistentAccessToken', 'fixture'));
            await page.route('https://praxis.test/**', route => {
                const url = new URL(route.request().url());
                const asset = assets[url.pathname.replace('/api/rbac-assets/', '')];
                if (asset) return route.fulfill({ contentType: asset[0], body: asset[1] });
                if (url.pathname === '/api/auth/profile') {
                    profileRequests++;
                    const authenticated = route.request().headers().authorization === 'Bearer fixture';
                    if (!authenticated) missingProfileCredentials++;
                    return route.fulfill({ status: authenticated ? 200 : 401,
                        json: authenticated ? { profile: { username: 'fixture', displayName: 'Fixture' } } : { message: 'Sign in again.' } });
                }
                if (url.pathname === '/api/auth/pages') return route.fulfill({ json: { pages: [{ key: 'PraxisReports', label: 'Reports', route: 'report.html' }] } });
                if (url.pathname === '/api/configuration' || url.pathname === '/api/rbac/me')
                    return route.fulfill({ status: 403, json: { message: 'Fixture permission denied.' } });
                if (url.pathname.startsWith('/api/')) return route.fulfill({ json: {} });
                const file = path.join(process.cwd(), 'Praxis.UI', url.pathname);
                if (!fs.existsSync(file)) return route.fulfill({ status: 404 });
                const type = url.pathname.endsWith('.js') ? 'application/javascript' : url.pathname.endsWith('.css') ? 'text/css' : url.pathname.endsWith('.png') ? 'image/png' : 'text/html';
                return route.fulfill({ body: fs.readFileSync(file), contentType: type });
            });
            await page.goto('https://praxis.test/report.html');
            await page.locator('#report-message').waitFor({ state: 'visible' });
            assert.match(await page.locator('#report-message').textContent(), /HTTP 403/);
            assert.equal(await page.locator('#sign-out-button').isVisible(), true);
            assert.deepEqual(errors, [], 'Real startup assets must initialize without undefined dependencies');
            assert.ok(profileRequests > 0, 'Exercise the real account component');
            assert.equal(missingProfileCredentials, 0, 'The first profile request must include the restored bearer token');
            await page.evaluate(() => window.dispatchEvent(new ErrorEvent('error', { filename: location.origin + '/Scripts/Praxis/report-portal.js', message: 'Fixture startup error' })));
            await page.getByRole('alert').filter({ hasText: 'Fixture startup error' }).waitFor();
            await page.close();
        }
        console.log('PASS: complete unmodified portal scripts initialize with local RBAC assets; denied data and startup errors stay visible, with sign-out available in both themes.');
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
