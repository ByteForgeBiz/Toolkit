const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const source = fs.readFileSync(__dirname + '/ByteForge-page-performance.js', 'utf8');
function run(readyState, navigation, reject = false) {
    const scheduled = [], sent = [], listeners = {};
    const form = { action: '/pdp/Diagnostics/PagePerformance' };
    vm.runInNewContext(source, {
        document: { readyState, getElementById: () => form },
        FormData: class { constructor() { this.fields = {}; } append(k, v) { this.fields[k] = v; } },
        window: {
            performance: { getEntriesByType: () => navigation ? [navigation] : [] },
            setTimeout: fn => scheduled.push(fn),
            addEventListener: (name, fn, options) => { assert.equal(options.once, true); listeners[name] = fn; },
            fetch: (url, options) => { sent.push({ url, options }); return reject ? Promise.reject(new Error('offline')) : Promise.resolve(); }
        }
    });
    return { scheduled, sent, listeners };
}
const navigation = { loadEventEnd: 0, responseStart: 1200.4, domContentLoadedEventEnd: 1700.6 };
const pending = run('loading', navigation);
assert.equal(pending.sent.length, 0);
pending.listeners.load();
assert.equal(pending.sent.length, 0);
navigation.loadEventEnd = 2200.8;
pending.scheduled.shift()();
assert.equal(pending.sent[0].url, '/pdp/Diagnostics/PagePerformance');
assert.equal(pending.sent[0].options.body.fields.loadMs, 2201);
assert.equal(pending.sent[0].options.body.fields.responseMs, 1200);
assert.equal(pending.sent[0].options.body.fields.domReadyMs, 1701);
assert.equal(pending.sent[0].options.credentials, 'same-origin');
const complete = run('complete', navigation, true);
complete.scheduled.shift()(); // Offline telemetry is swallowed without affecting the page.
for (const entry of [null, { loadEventEnd: 0 }]) {
    const missing = run('complete', entry);
    missing.scheduled.shift()();
    assert.equal(missing.sent.length, 0);
}
setImmediate(() => console.log('Page performance checks passed.'));
