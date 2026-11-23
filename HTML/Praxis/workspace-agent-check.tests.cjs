const assert = require('node:assert/strict');
const fs = require('node:fs');
const { chromium } = require('playwright');

(async () => {
    const browser = await chromium.launch({ headless: true, channel: 'msedge' });
    try {
        const page = await browser.newPage();
        const errors = [];
        page.on('pageerror', error => errors.push(error.message));
        const script = fs.readFileSync('ByteForge.Toolkit/HTML/Praxis/workspace.js', 'utf8');
        const css = fs.readFileSync('PraxisAPI/Content/Praxis/workspace.css', 'utf8') +
            fs.readFileSync('ByteForge.Toolkit/HTML/Praxis/workspace-attribution.css', 'utf8');
        const view = fs.readFileSync('PraxisAPI/Views/Workspace/Index.cshtml', 'utf8');
        const template = view.match(/<dialog open id="bf-workspace-agent-check"[\s\S]*?<\/dialog>/)[0];
        function html(selection) {
            const dialog = template
                .replace(/@\(Model.NeedsAgentSelection[^\n]+/, selection ? 'Choose the agent</h2>' : 'Confirm agent mismatch</h2>')
                .replace('@Model.AgentSavePrompt', selection ? 'This report form has no agent ID. Your changes have not been saved.' : 'The agent of record is bookingagent. You are signed in as supervisor. Save these edits while keeping the existing booking agent?')
                .replace(/@if \(Model.NeedsAgentSelection\) \{([\s\S]*?)\n      \}/, (_, block) => selection ? block.replace(/@foreach[^\n]+/, '<option value="kim">Kim Simmons (kim)</option>') : '')
                .replace(/@if \(Model.NeedsAgentSelection\) \{([\s\S]*?)\} else \{([\s\S]*?)\}/, (_, yes, no) => selection ? yes : no)
                .replace('@Model.AgentConfirmation', 'server-challenge');
            return `<style>${css}</style><form id="workspace-form"><input id="Launch_FromReport" value="true" type="hidden"><textarea id="notes">Keep these notes</textarea>${dialog}<footer><span id="save-state"></span><button type="submit">Save changes</button></footer></form>`;
        }
        async function setup(selection) {
            await page.route('https://workspace.test/**', route => route.fulfill({ contentType: 'text/html', body: html(selection) }));
            await page.goto('https://workspace.test/workspace');
            await page.addScriptTag({ content: script });
            await page.evaluate(() => {
                window.submissions = [];
                document.getElementById('workspace-form').addEventListener('submit', event => {
                    if (!event.defaultPrevented) window.submissions.push(new FormData(event.target, event.submitter).get('AgentConfirmation'));
                    event.preventDefault();
                });
            });
        }
        for (const colorScheme of ['light', 'dark']) for (const width of [1000, 390]) {
            await page.emulateMedia({ colorScheme });
            await page.setViewportSize({ width, height: 700 });
            await setup(true);
            assert.equal(await page.locator('dialog').evaluate(d => d.matches(':modal')), true);
            await page.getByRole('button', { name: 'Continue' }).click();
            assert.equal(await page.evaluate(() => submissions.length), 0);
            await page.selectOption('#SelectedAgentId', 'kim');
            await page.keyboard.press('Escape');
            assert.equal(await page.locator('#notes').inputValue(), 'Keep these notes');
            await page.getByRole('button', { name: 'Save changes' }).click();
            assert.equal(await page.evaluate(() => submissions.length), 0);
            await page.getByRole('button', { name: 'Continue' }).click();
            assert.equal(await page.evaluate(() => submissions.length), 1);
            await setup(false);
            const box = await page.locator('dialog').boundingBox();
            assert.ok(box.x >= 0 && box.x + box.width <= width);
            await page.screenshot({ path: `Logs/workspace-agent-check-${colorScheme}-${width}.png` });
            await page.getByRole('button', { name: 'Cancel' }).click();
            assert.equal(await page.evaluate(() => submissions.length), 0);
            await page.getByRole('button', { name: 'Save changes' }).click();
            await page.getByRole('button', { name: 'Confirm save' }).click();
            assert.deepEqual(await page.evaluate(() => submissions), ['server-challenge']);
        }
        // A stored portal token is bridged before form submission, retaining the confirming submitter.
        await setup(false);
        await page.evaluate(() => {
            localStorage.setItem('praxis.reportPortal.persistentAccessToken', 'test-token');
            window.fetch = async () => ({ ok: true });
        });
        await page.getByRole('button', { name: 'Confirm save' }).click();
        await page.waitForFunction(() => submissions.length === 1);
        assert.deepEqual(await page.evaluate(() => submissions), ['server-challenge']);
        await setup(false);
        await page.evaluate(() => { window.fetch = async () => ({ ok: false }); });
        await page.getByRole('button', { name: 'Confirm save' }).click();
        await page.waitForFunction(() => document.getElementById('save-state').textContent.includes('could not be checked'));
        assert.equal(await page.evaluate(() => submissions.length), 0);
        assert.deepEqual(errors, []);
        console.log('PASS agent selection, confirmation, cancellation, retained inputs, light/dark responsive dialogs, and portal-session success/failure');
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
