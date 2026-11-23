const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const source = fs.readFileSync(__dirname + '/manual-sale.js', 'utf8');
const flush = () => new Promise(resolve => setImmediate(resolve));
function setup() {
    const elements = {}; const timers = new Map(); let timerId = 0;
    function element() { return { hidden: true, value: '', href: '', listeners: {}, children: [],
        addEventListener(event, fn) { this.listeners[event] = fn; }, appendChild(child) { this.children.push(child); },
        attrs: {}, text: '',
        get textContent() { return this.text + this.children.map(child => child.textContent).join(''); },
        set textContent(value) { this.text = value; this.children = []; },
        setAttribute(key,value) { this.attrs[key]=value; }, removeAttribute(key) { delete this.attrs[key]; },
        scrollIntoView() {}, focus() {}, reportValidity() { return true; }, reset() {},
        setCustomValidity(value) { this.validation = value; } }; }
    const requests = [], popups = [], profiles = [];
    const ctx = { document: { getElementById: id => elements[id] ||= element(), createElement: element },
        sessionStorage: { getItem: () => 'token', removeItem() {} }, localStorage: { getItem: () => '', removeItem() {} },
        ByteForgePraxisAuth: { redirect() {} }, ByteForgePraxisAdmin: { load: () => Promise.resolve([]) },
        window: { setTimeout(fn) { timers.set(++timerId,fn); return timerId; }, clearTimeout(id) { timers.delete(id); }, ByteForgeAccount: { attach(element, options) { profiles.push({ element, options }); } }, open() { const popup = { closed: false, close() { this.closed = true; }, location: { href: 'about:blank', replace(url) { this.href = url; } } }; popups.push(popup); return popup; } },
        location: { replace() {} },
        fetch: (path, init) => new Promise((resolve, reject) => requests.push({ path, init, resolve, reject })) };
    ctx.ByteForgeAccount = ctx.window.ByteForgeAccount;
    ctx.document.currentScript = { getAttribute: () => 'false' };
    vm.runInNewContext(fs.readFileSync(__dirname + '/../JavaScript/ByteForge-utilities.js', 'utf8'), ctx);
    vm.runInNewContext(source, ctx);
    ctx.document.getElementById('manual-phone');
    function reply(status, data) { requests.shift().resolve({ ok: status === 200, status, json: () => Promise.resolve(data) }); }
    function options(id) { reply(200, { agents: [{ name: 'Test Agent', agentId: 'test.agent' }], services: ['Praxis'], requestId: id }); }
    function submit() { elements['manual-sale-form'].listeners.submit({ preventDefault() {} }); }
    function search() { elements['manual-customer-query'].listeners.input(); for(const [id,fn] of timers) { timers.delete(id); fn(); } }
    return { elements, requests, popups, profiles, reply, options, submit, search };
}
(async () => {
    const app = setup();
    app.options('request-1'); await flush();
    assert.equal(app.elements['manual-sale-form'].hidden, false);
    assert.equal(app.elements['session-status'].hidden, false, 'profile button appears after authorization');
    assert.equal(app.profiles[0].options.getToken(), 'token', 'profile uses the current portal token');
    assert.equal(typeof app.profiles[0].options.onPasswordChanged, 'function');
    app.elements['manual-agent'].value = 'unknown'; app.submit();
    assert.equal(app.requests.length, 0, 'reject arbitrary agent text before launching');
    app.elements['manual-agent'].value = 'test.agent';
    app.elements['manual-service'].value = 'Praxis'; app.elements['manual-phone'].value = '2125550100';
    app.submit(); app.submit();
    assert.equal(app.requests.length, 1, 'double submit creates one request');
    const body = app.requests[0].init.body;
    app.reply(500, { message: 'Manual Sale could not be started.', requestId: 'qa-reference' }); await flush();
    assert.match(app.elements['manual-status'].textContent, /Reference: qa-reference/, 'server failure reference is visible');
    assert.equal(app.popups[0].closed, true);
    app.elements['manual-phone'].value = '2125550199'; app.submit();
    assert.equal(app.requests[0].init.body, body, 'uncertain retry preserves the exact request and identity');
    app.reply(400, { message: 'Choose an agent' }); await flush();
    app.submit();
    assert.equal(JSON.parse(app.requests[0].init.body).phone, '(212) 555-0199', 'definite rejection permits correcting input');
    app.reply(200, { recordId: 123, workspaceUrl: '/praxis/workspace?RecordID=123' }); await flush();
    assert.equal(app.elements['manual-open'].href, '/praxis/workspace?RecordID=123');
    assert.equal(app.popups[2].location.href, '/praxis/workspace?RecordID=123');
    app.options('request-2'); await flush();
    app.submit();
    assert.equal(JSON.parse(app.requests[0].init.body).requestId, 'request-2', 'next sale uses a fresh request identity');
    const denied = setup(); denied.reply(403, {}); await flush();
    assert.equal(denied.elements['manual-sale-form'].hidden, true);
    assert.match(denied.elements['manual-status'].textContent, /does not grant/);
    const picker = setup(); picker.options('picker-request'); await flush();
    picker.elements['manual-customer-query'].value = 'Kohli';
    picker.search();
    assert.match(picker.requests[0].path, /customers\?query=Kohli$/);
    const customer = { recordId: 69969, contact: 'Duplicate Customer', companyName: 'Example', keyPhone: '2125550100', emailAddress: 'customer@example.test', listName: 'September', listId: 'list-id', importDate: '2026-09-04T00:00:00', apptDateTime: '2026-10-21T09:00:00', advisorName: 'Test Advisor', timeZone: 'America/New_York', eventType: 'acquisition', agentNotes: '<script>history only</script>' };
    picker.reply(200, [customer, {...customer, recordId:28894, listName:'June'}]); await flush();
    const matches = picker.elements['manual-customer-rows'].children;
    assert.equal(matches.length, 2, 'duplicate names remain separate choices');
    assert.equal(matches[0].children.length,5);
    assert.match(matches[0].textContent, /69969.*Duplicate Customer.*Example.*\(212\) 555-0100.*customer@example.test/);
    assert.match(matches[1].textContent, /28894.*Duplicate Customer/);
    assert.doesNotMatch(matches[0].textContent, /September|Existing appointment/);
    picker.elements['manual-customer-query'].listeners.keydown({key:'ArrowDown',preventDefault(){}});
    assert.equal(matches[0].attrs['aria-selected'],'true');
    picker.elements['manual-customer-query'].listeners.keydown({key:'Enter',preventDefault(){}});
    assert.equal(picker.elements['manual-customer-results'].hidden,true);
    assert.equal(picker.elements['manual-phone'].readOnly, true);
    assert.match(picker.elements['manual-customer-detail'].textContent, /already has a booking/);
    assert.match(picker.elements['manual-customer-detail'].textContent, /Previous notes.*history only/s);
    assert.equal(picker.elements['manual-customer-detail'].innerHTML, undefined, 'notes are rendered as text');
    picker.elements['manual-agent'].value='test.agent'; picker.elements['manual-service'].value='Praxis';
    picker.submit();
    const selectedBody=picker.requests[0].init.body;
    assert.equal(JSON.parse(selectedBody).sourceRecordId,69969);
    picker.reply(500,{}); await flush();
    matches[1].listeners.click(); picker.elements['manual-customer-new'].listeners.click();
    picker.submit(); assert.equal(picker.requests[0].init.body, selectedBody, 'uncertain retry keeps the exact customer');
    picker.reply(400,{}); await flush();
    picker.elements['manual-customer-new'].listeners.click();
    assert.equal(picker.elements['manual-phone'].readOnly,false);
    assert.equal(picker.elements['manual-customer-detail'].hidden,true);
    picker.elements['manual-phone'].value='2125550199'; picker.submit();
    assert.equal(JSON.parse(picker.requests[0].init.body).sourceRecordId,null,'new customer has no source');
    const race=setup(); race.options('race'); await flush();
    race.elements['manual-customer-query'].value='old'; race.search();
    race.elements['manual-customer-query'].value='new'; race.search();
    const old=race.requests.shift(), latest=race.requests.shift();
    latest.resolve({ok:true,status:200,json:()=>Promise.resolve([customer])}); await flush();
    old.resolve({ok:true,status:200,json:()=>Promise.resolve([{...customer,recordId:1}])}); await flush();
    assert.equal(race.elements['manual-customer-rows'].children.length,1,'stale search cannot replace latest results');
    const typed=setup(); typed.options('typed'); await flush();
    typed.elements['manual-customer-query'].value='Example'; typed.search();
    typed.reply(200,[customer]); await flush();
    const cells=typed.elements['manual-customer-rows'].children[0].children;
    assert.equal(cells[2].children[1].textContent,'Example','company match is highlighted');
    typed.elements['manual-customer-rows'].children[0].listeners.click();
    typed.elements['manual-customer-query'].value='different'; typed.elements['manual-customer-query'].listeners.input();
    assert.equal(typed.elements['manual-customer-detail'].hidden,true,'editing clears the selected identity');
    assert.equal(typed.elements['manual-phone'].value,'');
    assert.match(typed.elements['manual-customer-query'].validation,/Choose a customer/);
    typed.elements['manual-customer-query'].listeners.keydown({key:'Escape',preventDefault(){}});
    assert.equal(typed.elements['manual-customer-results'].hidden,true);
    typed.elements['manual-customer-query'].value='2125550100'; typed.search();
    typed.reply(200,[customer]); await flush();
    assert.equal(typed.elements['manual-customer-rows'].children[0].children[3].children[1].textContent,'212) 555-0100','unformatted phone query highlights formatted digits');
    typed.elements['manual-customer-query'].listeners.blur();
    assert.equal(typed.elements['manual-customer-results'].hidden,true,'blur dismisses lookup');
    console.log('PASS: Manual Sale launcher checks, profile wiring, and visible failure references');
})().catch(error => { console.error(error); process.exitCode = 1; });
