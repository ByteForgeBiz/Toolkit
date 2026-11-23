const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const source = fs.readFileSync(__dirname + '/report-portal.js', 'utf8');
const notice = { hidden: true, textContent: '' };
let payload;
let rendered = 0;
const context = {
    state: {},
    document: { getElementById: () => notice },
    formatNumber: String,
    getBounds: () => ({ fromIso: '2026-09-08', toIso: '2026-09-15' }),
    queryString: () => '',
    beginCardLoading: () => {},
    completeCardLoading: (section, request) => request,
    apiGet: () => Promise.resolve(payload),
    buildAgentRows: () => {},
    renderAgents: () => { rendered++; }
};
vm.createContext(context);
vm.runInContext(source.slice(source.indexOf('    function loadMetrics('), source.indexOf('    function loadHistory(')), context);
vm.runInContext(source.slice(source.indexOf('    function metricParameters('), source.indexOf('    function loadProgress(')), context);
(async () => {
    payload = { isPartial: true, missingTeamEvidence: [{ agent: 'mtritschler', date: '2026-09-14', excludedCallCount: 2 }] };
    await context.loadMetrics();
    assert.equal(rendered, 2, 'Clear old rows while loading, then render the partial report');
    assert.equal(notice.hidden, false);
    assert.match(notice.textContent, /incomplete/);
    assert.match(notice.textContent, /mtritschler on 2026-09-14 \(2 calls\)/);
    payload = { isPartial: false, missingTeamEvidence: [] };
    await context.loadMetrics();
    assert.equal(rendered, 4);
    assert.equal(notice.hidden, true, 'Recovered evidence clears the notice');
    assert.equal(notice.textContent, '');
    payload = {};
    await context.loadMetrics();
    assert.equal(notice.hidden, true, 'Older API responses remain compatible');
    context.renderMetricCompleteness({ missingTeamEvidence: [{ agent: '', date: '2026-09-14', excludedCallCount: 1 }] });
    assert.match(notice.textContent, /Unknown agent/);
    console.log('PASS: partial report renders, missing evidence is visible, recovery clears notice, older responses work.');
})().catch(error => { console.error(error); process.exitCode = 1; });
