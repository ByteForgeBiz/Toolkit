const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const crypto = require('node:crypto').webcrypto;
let calls = [];
const window = { crypto, fetch: (url, options) => { calls.push({ url, body: options.body }); return Promise.resolve({ ok: true }); } };
vm.runInNewContext(fs.readFileSync(require('node:path').join(__dirname, 'ByteForge-effort-client.js'), 'utf8'), { window, Map, URLSearchParams, FormData, Uint8Array });
async function run() {
    await window.ByteForgeEffortClient.fetch('/Agent', { body: 'Company=Test&__RequestVerificationToken=one' });
    await window.ByteForgeEffortClient.fetch('/Agent', { body: 'Company=Test&__RequestVerificationToken=two' });
    const first = new URLSearchParams(calls[0].body).get('effortId');
    assert.match(first, /^[0-9a-f-]{36}$/i);
    assert.equal(new URLSearchParams(calls[1].body).get('effortId'), first, 'A lost response retry must reuse its ID even if the CSRF token changes');
    await window.ByteForgeEffortClient.fetch('/Agent', { body: 'Company=Changed' });
    assert.notEqual(new URLSearchParams(calls[2].body).get('effortId'), first, 'Changed payload must use a new ID');
    const form = new FormData(); form.set('Disposition', 'Voicemail');
    await window.ByteForgeEffortClient.fetch('/Disposition', { body: form });
    assert.match(calls[3].body.get('effortId'), /^[0-9a-f-]{36}$/i);
    assert.equal(calls[3].body.get('Disposition'), 'Voicemail');
    console.log('5 effort client identity checks passed.');
}
run().catch(error => { console.error(error); process.exitCode = 1; });
