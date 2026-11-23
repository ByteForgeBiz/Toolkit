const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const source = fs.readFileSync(__dirname + '/auth.js', 'utf8');
function fixture({ persistent = 'old', temporary = '', status = 200, delay = Promise.resolve(), apiStatus = 200 } = {}) {
    const local = new Map(persistent ? [['praxis.reportPortal.persistentAccessToken', persistent]] : []);
    const session = new Map(temporary ? [['praxis.reportPortal.accessToken', temporary]] : []);
    const calls = [], redirects = [];
    const location = { href: 'https://praxis.test/report.html', origin: 'https://praxis.test', pathname: '/report.html', hash: '', replace: url => redirects.push(url) };
    const storage = map => ({ getItem: key => map.get(key) || null, setItem: (key, value) => map.set(key, value), removeItem: key => map.delete(key) });
    const window = { ByteForgePraxisRequestErrors: true, addEventListener() {}, fetch: async (input, options) => {
        calls.push({ url: String(input), options });
        if (String(input).endsWith('api/auth/refresh')) {
            await delay;
            return { status, ok: status === 200, json: async () => ({ accessToken: 'new' }) };
        }
        return { status: apiStatus, ok: apiStatus === 200 };
    } };
    const context = { window, location, document: { getElementById: () => null }, sessionStorage: storage(session), localStorage: storage(local), URL, Headers, Promise, Date };
    vm.runInNewContext(source, context);
    return { window, calls, redirects, local };
}
(async () => {
    const options = { headers: { Authorization: 'Bearer old' } };
    let release;
    const f = fixture({ delay: new Promise(resolve => { release = resolve; }) });
    const requests = [f.window.fetch('api/rbac/me', options), f.window.fetch('api/auth/pages', options)];
    assert.equal(f.calls.length, 1, 'Concurrent startup waits for one renewal');
    release();
    await Promise.all(requests);
    assert.equal(f.local.get('praxis.reportPortal.persistentAccessToken'), 'new');
    for (const call of f.calls.slice(1)) assert.equal(call.options.headers.get('Authorization'), 'Bearer new');
    await f.window.fetch('api/save', { method: 'POST', headers: options.headers, body: 'payload' });
    assert.equal(f.calls.length, 4, 'Do not replay writes or renew each request');
    assert.equal(f.calls[3].options.body, 'payload');
    assert.equal(f.calls[3].options.headers.get('Authorization'), 'Bearer new', 'Old component-held bearer is replaced');
    f.local.set('praxis.reportPortal.persistentAccessToken', 'different-account');
    await f.window.fetch('api/save', { method: 'POST', headers: options.headers });
    assert.equal(f.calls[4].options.headers.Authorization, 'Bearer old', 'Never replace a component token with another account login');
    const temporary = fixture({ temporary: 'temporary' });
    await temporary.window.fetch('api/auth/pages', { headers: { Authorization: 'Bearer temporary' } });
    assert.equal(temporary.calls.length, 1, 'A temporary login is never promoted');
    const expired = fixture({ temporary: 'temporary', apiStatus: 401 });
    await expired.window.fetch('api/metrics', { method: 'POST', headers: { Authorization: 'Bearer temporary' }, body: 'payload' });
    assert.equal(expired.calls.length, 1, 'Do not replay a rejected request');
    assert.equal(expired.redirects.length, 1, 'Regular expired sessions return to login');
    const forbidden = fixture({ temporary: 'temporary', apiStatus: 403 });
    await forbidden.window.fetch('api/metrics', { headers: { Authorization: 'Bearer temporary' } });
    assert.equal(forbidden.redirects.length, 0, 'Permissions errors must not log the user out');
    const outage = fixture({ status: 503 });
    await outage.window.fetch('api/auth/pages', options);
    assert.equal(outage.local.get('praxis.reportPortal.persistentAccessToken'), 'old');
    assert.deepEqual(outage.redirects, []);
    const rejected = fixture({ status: 401 });
    await rejected.window.fetch('api/auth/pages', options);
    assert.match(rejected.redirects[0], /^login.html\?returnUrl=report.html/);
    const unrelated = fixture();
    await unrelated.window.fetch('https://other.test/api/auth/pages', options);
    assert.equal(unrelated.calls.length, 1, 'Never send renewal or rotate tokens for another origin');
    let signedOut;
    const race = fixture({ delay: new Promise(resolve => { signedOut = resolve; }) });
    const pending = race.window.fetch('api/save', { method: 'POST', headers: options.headers });
    race.local.clear(); signedOut();
    await assert.rejects(pending, /session changed/);
    assert.equal(race.local.size, 0, 'Renewal must not undo sign-out');
    assert.equal(race.calls.length, 1, 'Do not send queued writes after sign-out');
    console.log('PASS: remembered renewal, concurrent startup, token rotation, writes, temporary login, outages, reauthentication, origin and sign-out races');
})().catch(error => { console.error(error); process.exitCode = 1; });
