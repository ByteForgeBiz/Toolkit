const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require('playwright');
const root = process.cwd();
const allPages = [
    { key: 'PraxisReports', label: 'Reports', route: 'report.html' },
    { key: 'PraxisScriptEditor', label: 'Script Editor', route: 'scripts.html' },
    { key: 'PraxisRbacAdmin', label: 'Users & Roles', route: 'report.html#security' }
];
(async () => {
    const browser = await chromium.launch({ headless: true, channel: process.env.PRAXIS_BROWSER_CHANNEL || "msedge" });
    try {
        const page = await browser.newPage();
        let allowed = allPages;
        let reject = false;
        let fullApplication = false;
        await page.route('https://praxis.test/**', async route => {
            const pathname = new URL(route.request().url()).pathname;
            if (pathname === '/api/auth/pages') return route.fulfill({ status: reject ? 403 : 200, json: { pages: allowed } });
            if (pathname.startsWith('/api/rbac-assets/')) {
                const style = pathname.endsWith('/style');
                const folder = style ? 'CSS' : 'JavaScript';
                const extension = style ? '.css' : '.js';
                const body = ['ByteForge-sort-indicator', 'ByteForge-rbac'].map(name => fs.readFileSync(path.join(root, 'ByteForge.Toolkit/HTML', folder, name + extension), 'utf8')).join('\n');
                return route.fulfill({ body, contentType: style ? 'text/css' : 'application/javascript' });
            }
            if (pathname === '/api/auth/portal-token') return route.fulfill({ json: { accessToken: 'fixture-session' } });
            if (pathname === '/api/configuration') return route.fulfill(allowed.some(p => p.key === 'PraxisReports')
                ? { json: { services: [], teams: [], reportTimeZone: 'Pacific', reportIanaTimeZone: 'America/Los_Angeles', dialingDays: [1,2,3,4,5] } } : { status: 403, json: {} });
            if (pathname === '/api/rbac/me') return route.fulfill(allowed.some(p => p.key === 'PraxisRbacAdmin') ? { json: { isSuperAdmin: true } } : { status: 403, json: {} });
            if (pathname === '/api/scripts') return route.fulfill({ json: { scripts: [], tokens: [], eventTypes: [] } });
            if (pathname.startsWith('/api/')) return route.fulfill({ json: [] });
            let file = path.join(root, 'PraxisAPI', pathname);
            if (!fs.existsSync(file)) return route.fulfill({ status: 404, body: '' });
            let body = fs.readFileSync(file);
            if (pathname.endsWith('.html') && !fullApplication) {
                // Exercise the shared shell against real page markup without business-data requests.
                body = body.toString().replace(/<script\b[^>]*>[\s\S]*?<\/script>/g, '');
            }
            const type = pathname.endsWith('.html') ? 'text/html' : pathname.endsWith('.css') ? 'text/css' : pathname.endsWith('.png') ? 'image/png' : 'application/javascript';
            return route.fulfill({ body, contentType: type });
        });
        for (const file of ['report.html', 'scripts.html']) {
            await page.goto('https://praxis.test/' + file);
            await page.addScriptTag({ content: fs.readFileSync(path.join(__dirname, 'admin-shell.js'), 'utf8') });
            assert.equal(await page.locator('.bf-praxis-sidebar').isVisible(), false);
            allowed = [allPages[0]];
            await page.evaluate(() => ByteForgePraxisAdmin.load('test'));
            assert.deepEqual(await page.locator('.bf-praxis-sidebar nav a').allTextContents(), ['Reports']);
            allowed = [allPages[1]];
            await page.evaluate(() => ByteForgePraxisAdmin.load('test'));
            assert.deepEqual(await page.locator('.bf-praxis-sidebar nav a').allTextContents(), ['Script Editor']);
            allowed = allPages;
            await page.evaluate(() => ByteForgePraxisAdmin.load('test'));
            assert.equal(await page.locator('.bf-praxis-sidebar nav a').count(), 3);
            await page.evaluate(() => ByteForgePraxisAdmin.select('PraxisRbacAdmin'));
            assert.equal(await page.locator('.bf-praxis-sidebar [aria-current="page"]').textContent(), 'Users & Roles');
            await page.evaluate(() => {

                document.getElementById('report-content')?.removeAttribute('hidden');
                document.getElementById('script-editor')?.removeAttribute('hidden');
            });
            for (const theme of ['light', 'dark']) {
                await page.emulateMedia({ colorScheme: theme });
                for (const width of [1440, 1024, 390]) {
                    await page.setViewportSize({ width, height: 1000 });
                    const overflow = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth);
                    assert.equal(overflow, false, file + ' fits ' + width + ' in ' + theme);
                    const output = process.env.PRAXIS_SCREENSHOT_DIR;
                    if (output) await page.screenshot({ path: path.join(output, file + '-' + theme + '-' + width + '.png') });
                }
            }
            await page.evaluate(() => ByteForgePraxisAdmin.clear());
            assert.equal(await page.locator('.bf-praxis-sidebar').isVisible(), false);
            assert.equal(await page.locator('.bf-praxis-sidebar nav a').count(), 0);
            reject = true;
            await page.evaluate(() => ByteForgePraxisAdmin.load('test').catch(() => {}));
            assert.equal(await page.locator('.bf-praxis-sidebar').isVisible(), false);
            reject = false;
        }
        fullApplication = true;
        for (const grant of [[allPages[0]], [allPages[1]], [allPages[2]], allPages]) {
            allowed = grant;
            await page.goto('https://praxis.test/report.html');
            await page.evaluate(() => { localStorage.clear(); sessionStorage.clear(); });
            await page.reload();
            await page.locator('#account-identifier').fill('fixture');
            await page.locator('#account-password').fill('fixture-password');
            await page.locator('#login-form button').click();
            await page.waitForFunction(() => document.querySelector('.bf-praxis-sidebar')?.hidden === false);
            if (grant.length === 1 && grant[0].key === 'PraxisScriptEditor') {
                await page.waitForURL('**/scripts.html');
                await page.locator('#script-editor').waitFor({ state: 'visible' });
            } else if (grant[0].key === 'PraxisRbacAdmin') {
                await page.locator('#rbac-content').waitFor({ state: 'visible' });
            } else {
                await page.locator('#report-content').waitFor({ state: 'visible' });
            }
            assert.deepEqual(await page.locator('.bf-praxis-sidebar nav a').allTextContents(), grant.map(p => p.label));
            if (grant.length === 3) {
                await page.getByRole('link', { name: 'Users & Roles', exact: true }).click();
                await page.locator('#rbac-content').waitFor({ state: 'visible' });
                await page.getByRole('link', { name: 'Reports', exact: true }).click();
                await page.locator('#report-content').waitFor({ state: 'visible' });
            }
            await page.locator('#sign-out-button').click();
            await page.waitForURL('**/login.html?**');
            await page.locator('#login-panel').waitFor({ state: 'visible' });
            assert.equal(await page.locator('.bf-praxis-sidebar').count(), 0);
            assert.equal(await page.locator('#login-panel').isVisible(), true);
        }
        allowed = allPages;
        // Observe from document creation, including the HTML paint before scripts execute.
        await page.addInitScript(() => {
            window.loginFlashed = false;
            new MutationObserver(() => {
                const panel = document.getElementById('login-panel');
                if (panel && !panel.hidden) window.loginFlashed = true;
            }).observe(document, { subtree: true, childList: true, attributes: true, attributeFilter: ['hidden'] });
        });
        await page.route('**/Scripts/Praxis/*.js*', async route => {
            await new Promise(resolve => setTimeout(resolve, 150));
            await route.fallback();
        });
        for (const theme of ['light', 'dark']) {
            await page.emulateMedia({ colorScheme: theme });
            for (const storage of ['sessionStorage', 'localStorage']) {
                await page.evaluate(storage => {
                    sessionStorage.clear(); localStorage.clear();
                    window[storage].setItem(storage === 'sessionStorage' ? 'praxis.reportPortal.accessToken' : 'praxis.reportPortal.persistentAccessToken', 'fixture-session');
                }, storage);
                for (const [file, panel] of [['report.html', '#report-content'], ['report.html#security', '#rbac-content'], ['scripts.html', '#script-editor']]) {
                    await page.goto('https://praxis.test/' + file);
                    await page.locator(panel).waitFor({ state: 'visible' });
                    assert.equal(await page.evaluate(() => window.loginFlashed), false, file + ': no login flash in ' + theme + ' with ' + storage);
                }
            }
        }
        reject = true;
        for (const file of ['report.html', 'scripts.html']) {
            await page.evaluate(() => sessionStorage.setItem('praxis.reportPortal.accessToken', 'expired-fixture'));
            await page.goto('https://praxis.test/' + file);
            await page.locator('#login-panel').waitFor({ state: 'visible' });
            assert.ok(page.url().includes('/login.html?'));
            assert.equal(await page.locator('#report-content, #script-editor').count(), 0);
        }
        reject = false;
        for (const target of ['report.html', 'report.html#security', 'scripts.html', 'https://outside.test/', '//outside.test/', '../index.html']) {
            await page.evaluate(() => { sessionStorage.clear(); localStorage.clear(); });
            await page.goto('https://praxis.test/login.html?returnUrl=' + encodeURIComponent(target));
            await page.locator('#account-identifier').fill('fixture');
            await page.locator('#account-password').fill('fixture-password');
            await page.locator('#login-form button').click();
            const expected = ['report.html', 'report.html#security', 'scripts.html'].includes(target) ? target : 'report.html#reports';
            await page.waitForURL(url => url.origin === 'https://praxis.test' && (url.pathname + url.hash).endsWith(expected === 'report.html' ? 'report.html#reports' : expected));
            assert.equal(await page.locator('#login-form').count(), 0);
        }
        allowed = [];
        await page.evaluate(() => { sessionStorage.clear(); localStorage.clear(); });
        await page.goto('https://praxis.test/login.html');
        await page.locator('#account-identifier').fill('fixture');
        await page.locator('#account-password').fill('fixture-password');
        await page.locator('#login-form button').click();
        await page.locator('#login-error').waitFor({ state: 'visible' });
        assert.equal(await page.evaluate(() => sessionStorage.getItem('praxis.reportPortal.accessToken')), null);
        for (const theme of ['light', 'dark']) {
            await page.emulateMedia({ colorScheme: theme });
            for (const width of [1440, 390]) {
                await page.setViewportSize({ width, height: 1000 });
                assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
                if (process.env.PRAXIS_SCREENSHOT_DIR) await page.screenshot({ path: path.join(process.env.PRAXIS_SCREENSHOT_DIR, 'login-' + theme + '-' + width + '.png') });
            }
        }
        console.log('PASS: dedicated login returns to Reports/Users/Editor, rejects external return URLs, and keeps accounts without page access signed out.');
        console.log('PASS: no initial sign-in flash with delayed scripts, session/persistent tokens, report/users/editor, light/dark; rejected sessions show sign-in.');
        console.log('PASS: real login/restoration code with mocked API for report-only, script-only, security-only, full admin; Reports/Users switching and sign-out.');
        console.log('PASS: report-only, editor-only, all pages, active page, sign-out, denied access; report/editor layouts at 1440/1024/390 in light/dark.');
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
