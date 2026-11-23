const { chromium } = require('playwright');
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../../..');
const scripts = ['workspace.js', 'workspace-save.js'].map(name => fs.readFileSync(path.join(__dirname, name), 'utf8'));
const css = fs.readFileSync(path.join(root, 'PraxisAPI/Content/Praxis/workspace.css'), 'utf8') + fs.readFileSync(path.join(__dirname, 'workspace-save.css'), 'utf8');
const appointmentText = 'Success - Appointment Set\r\n\r\nAgent: Kim\r\nAdvisor: Advisor\r\nProspect: Jane Doe\r\nTrigger Event: IPO\r\nTrue/False/Unable to Confirm: False\r\nDate/Time of Appt/Time Zone: 10/26/2026 2:30 PM America/New_York\r\nPhone: 2125550100\r\nEmail: jane@example.test\r\nNotes: Fixture notes';
const infoText = 'Request Info - Email\r\n\r\nAgent: Kim\r\nAdvisor: Advisor\r\nProspect: Jane Doe\r\nTrigger Event: IPO\r\nTrue/False/Unable to Confirm: False\r\nPhone: 2125550100\r\nEmail: jane@example.test\r\nNotes: Fixture notes';
function html(action = '', dialog = false, error = '') {
    return `<!doctype html><html><head><style>${css}</style></head><body><form id="workspace-form" action="/workspace">
    ${error ? `<div class="notice error">${error}</div>` : ''}
    <input type="hidden" name="__RequestVerificationToken" value="fixture-token">
    <input type="hidden" name="SaveAction" id="workspace-save-action" value="${action}">
    <input type="hidden" id="Launch_FromReport" name="Launch.FromReport" value="false">
    <input type="hidden" id="Launch_AgentId" name="Launch.AgentId" value="kim">
    <input type="hidden" id="Launch_AgentLogin" name="Launch.AgentLogin" value="kim">
    ${dialog ? `<dialog open id="bf-workspace-agent-check"><p data-agent-check-status></p><select id="SelectedAgentId" name="SelectedAgentId"><option value="">Select agent</option><option value="kim">Kim</option></select><button type="button" data-agent-check-cancel hidden>Cancel</button><button type="submit" formnovalidate>Continue</button></dialog>` : ''}
    <div class="panel"><label class="field">Prospect<input id="Contact" name="Contact" value="Jane Doe"></label>
    <label class="field">Phone<input id="KeyPhone" name="KeyPhone" pattern="[0-9]{10}" value="2125550100"></label>
    <label class="field">Email<input id="EmailAddress" name="EmailAddress" type="email" value="jane@example.test"></label>
    <label class="field">Trigger event<select id="EventAccurate" name="EventAccurate"><option value="">Select</option><option selected value="False">False</option></select></label>
    <select id="advisor-select" name="FinalAdvisorId"><option value="7" data-name="Advisor">Advisor</option></select>
    <input type="hidden" id="AppointmentDateTime" name="AppointmentDateTime" value="2026-10-26T14:30">
    <label class="field">Time zone<input id="AppointmentTimeZone" name="AppointmentTimeZone" value="America/New_York"></label>
    <label class="field">Call notes<textarea id="notes" name="AgentNotes">Fixture notes</textarea></label></div>
    <footer><span id="save-state" aria-live="polite">No unsaved changes</span><div class="bf-workspace-save-actions">
    <button type="button" id="workspace-copy-again" class="plain-action" hidden>Copy again</button>
    <button class="primary" type="submit" name="workspaceAction" value="Save" formnovalidate>Save</button>
    <button class="primary" type="submit" name="workspaceAction" value="SetAppointment" formnovalidate>Set Appointment</button>
    <button class="primary bf-workspace-send-info" type="submit" name="workspaceAction" value="SendInfo" formnovalidate>Send Info</button>
    </div></footer></form>${scripts.map(text => `<script>${text}</script>`).join('')}</body></html>`;
}
let checks = 0;
function check(value, message) { assert.ok(value, message); checks++; }
(async () => {
    const browser = await chromium.launch({ channel: 'msedge', headless: true });
    try {
        async function fixture(mode) {
            const page = await browser.newPage();
            let posts = [];
            let release;
            await page.addInitScript(() => {
                window.fixtureCopies = [];
                window.fixtureDenyCopy = false;
                Object.defineProperty(navigator, 'clipboard', { value: { writeText: async text => {
                    if (window.fixtureDenyCopy) throw new Error('Fixture permission denial');
                    window.fixtureCopies.push(text);
                } } });
                document.execCommand = () => false;
            });
            await page.route('https://praxis.test/workspace', async route => {
                if (route.request().method() === 'GET') return route.fulfill({ contentType: 'text/html', body: html() });
                posts.push(route.request().postData());
                if (mode === 'deferred') await new Promise(resolve => { release = resolve; });
                if (mode === 'failed') return route.fulfill({ status: 400, contentType: 'text/html', body: html('SetAppointment', false, 'Fixture validation failure') });
                if (mode === 'confirmation' && posts.length === 1) return route.fulfill({ contentType: 'text/html', body: html('SendInfo', true) });
                const info = route.request().postData().includes('SendInfo');
                if (route.request().postData().includes('name="workspaceAction"\r\n\r\nSave\r\n'))
                    return route.fulfill({ contentType: 'application/json', body: JSON.stringify({ ok: true, action: 'Save', clipboardText: null, message: 'Changes saved.', agentId: 'kim', agentLogin: 'kim' }) });
                return route.fulfill({ contentType: 'application/json', body: JSON.stringify({ ok: true, action: info ? 'SendInfo' : 'SetAppointment', clipboardText: info ? infoText : appointmentText, agentId: 'kim', agentLogin: 'kim' }) });
            });
            await page.goto('https://praxis.test/workspace');
            return { page, posts, release: () => release() };
        }
        let f = await fixture('deferred');
        await f.page.getByRole('button', { name: 'Set Appointment', exact: true }).click();
        await f.page.waitForFunction(() => document.querySelector('[name=workspaceAction]').disabled);
        check(f.posts.length === 1, 'Click must issue one save');
        check((await f.page.evaluate(() => window.fixtureCopies)).length === 0, 'Clipboard must remain untouched before save acknowledgement');
        await f.page.evaluate(() => document.querySelector('form').requestSubmit(document.querySelector('button[value=SetAppointment]')));
        check(f.posts.length === 1, 'Repeated submit must not duplicate an in-flight save');
        f.release();
        await f.page.getByText('Saved and copied.', { exact: true }).waitFor();
        check((await f.page.evaluate(() => window.fixtureCopies))[0] === appointmentText, 'Appointment must copy its server-confirmed template');
        check(!(await f.page.locator('body').innerText()).includes('Success - Appointment Set'), 'Generated template must not be displayed');
        check(f.posts[0].includes('fixture-token') && f.posts[0].includes('SetAppointment'), 'Save must retain anti-forgery token and chosen outcome');
        await f.page.close();

        f = await fixture();
        // A previous failed outcome validation must not leave fields required for Save.
        await f.page.locator('#EmailAddress').fill('');
        await f.page.locator('#EventAccurate').selectOption('');
        await f.page.getByRole('button', { name: 'Send Info', exact: true }).click();
        check(f.posts.length === 0, 'Send Info must still require contact outcome fields');
        await f.page.locator('#Contact').fill('');
        await f.page.locator('#KeyPhone').fill('');
        await f.page.locator('#notes').fill('Reached voicemail; left message.');
        await f.page.getByRole('button', { name: 'Save', exact: true }).click();
        await f.page.getByText('Changes saved.', { exact: true }).waitFor();
        check(f.posts.length === 1 && f.posts[0].includes('Reached voicemail; left message.'), 'Save must submit ordinary call notes without outcome fields');
        check((await f.page.evaluate(() => window.fixtureCopies)).length === 0, 'Plain Save must not overwrite the clipboard');
        check(await f.page.locator('#workspace-copy-again').isHidden(), 'Plain Save must not offer outcome clipboard retry');
        await f.page.getByRole('button', { name: 'Send Info', exact: true }).click();
        check(f.posts.length === 1, 'Send Info requirements must be restored after plain Save');
        await f.page.close();

        f = await fixture();
        await f.page.evaluate(() => { document.getElementById('AppointmentDateTime').value = ''; document.getElementById('AppointmentTimeZone').value = ''; });
        await f.page.getByRole('button', { name: 'Set Appointment', exact: true }).click();
        check(f.posts.length === 0, 'Appointment must require a booking');
        await f.page.getByRole('button', { name: 'Send Info', exact: true }).click();
        await f.page.getByText('Saved and copied.', { exact: true }).waitFor();
        check((await f.page.evaluate(() => window.fixtureCopies))[0] === infoText, 'Send Info must save without an appointment and copy its own template');
        await f.page.close();

        f = await fixture('failed');
        await f.page.getByRole('button', { name: 'Set Appointment', exact: true }).click();
        await f.page.getByText('Fixture validation failure').waitFor();
        check((await f.page.evaluate(() => window.fixtureCopies)).length === 0, 'Rejected save must never copy');
        await f.page.close();

        f = await fixture();
        await f.page.evaluate(() => { window.fixtureDenyCopy = true; });
        await f.page.getByRole('button', { name: 'Send Info', exact: true }).click();
        await f.page.getByRole('button', { name: 'Copy again', exact: true }).waitFor();
        check((await f.page.locator('#save-state').innerText()).includes('Saved successfully'), 'Clipboard denial must not disguise save success');
        await f.page.evaluate(() => { window.fixtureDenyCopy = false; document.getElementById('notes').value = 'Unsaved edits'; });
        await f.page.getByRole('button', { name: 'Copy again', exact: true }).click();
        await f.page.getByText('Saved and copied.', { exact: true }).waitFor();
        check(f.posts.length === 1, 'Copy again must not resave');
        check((await f.page.evaluate(() => window.fixtureCopies))[0] === infoText, 'Copy again must retain the last saved snapshot, not unsaved edits');
        await f.page.close();

        f = await fixture('confirmation');
        await f.page.getByRole('button', { name: 'Send Info', exact: true }).click();
        await f.page.locator('#SelectedAgentId').waitFor();
        check((await f.page.evaluate(() => window.fixtureCopies)).length === 0, 'Attribution prompt is not a successful save');
        await f.page.locator('#SelectedAgentId').selectOption('kim');
        await f.page.getByRole('button', { name: 'Continue', exact: true }).click();
        await f.page.getByText('Saved and copied.', { exact: true }).waitFor();
        check((await f.page.evaluate(() => window.fixtureCopies))[0] === infoText, 'Agent confirmation must preserve Send Info selection');
        check(await f.page.locator('#bf-workspace-agent-check').count() === 0, 'Successful attribution must close and remove the prompt');
        await f.page.getByRole('button', { name: 'Send Info', exact: true }).click();
        await f.page.waitForFunction(() => window.fixtureCopies.length === 2);
        check(f.posts.length === 3, 'A later save must not reopen an already-completed attribution prompt');
        for (const colorScheme of ['light', 'dark']) {
            await f.page.emulateMedia({ colorScheme });
            await f.page.screenshot({ path: path.join(root, `Logs/PraxisWorkspaceOutcomes-${colorScheme}.png`), fullPage: true });
            check(await f.page.locator('.bf-workspace-save-actions .primary').count() === 3, `${colorScheme}: Save and both outcomes remain present`);
        }
        await f.page.setViewportSize({ width: 390, height: 844 });
        check(await f.page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth), 'Save actions must fit mobile width');
        await f.page.close();
        console.log(`${checks} Praxis save/clipboard browser checks passed.`);
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
