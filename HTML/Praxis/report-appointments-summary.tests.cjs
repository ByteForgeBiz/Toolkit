const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const source = fs.readFileSync(__dirname + '/report-portal.js', 'utf8');
const elements = {};
const requests = [];
const context = {
    state: {},
    document: { getElementById(id) { return elements[id] ||= { value: '', textContent: '' }; } },
    formatNumber: value => String(value),
    formatPercent: value => (value * 100).toFixed(1) + '%',
    getBounds: () => ({ fromDate: '2026-09-17', toExclusiveDate: '2026-09-26' }),
    queryString: value => new URLSearchParams(value).toString(),
    apiGet: url => new Promise((resolve, reject) => requests.push({ url, resolve, reject })),
    beginCardLoading() {}, renderAppointments() {}, setTableError() {},
    completeCardLoading: (_, request) => request
};
vm.createContext(context);
for (const [start, end] of [['loadAppointments', 'getBounds'], ['renderHistorySummary', 'renderHistory'], ['summarizeHistoryRows', 'getHistoryMetricTitle']]) {
    vm.runInContext(source.slice(source.indexOf('    function ' + start + '('),
        source.indexOf('    function ' + end + '(')), context);
}
const total = () => elements['appointments-total'].textContent;
const history = () => {
    context.state.history = { items: [{ appointments: 6, humanContacts: 227, dials: 3757, aodSeconds: 608400 }] };
    context.renderHistorySummary();
    assert.equal(elements['calls-total'].textContent, '3757', 'call summary uses historical dials');
};
(async () => {
    let pending = context.loadAppointments();
    assert.ok(!requests[0].url.includes('refresh='), 'ordinary loads retain their snapshot');
    history();
    assert.equal(total(), '6', 'summary uses the historical appointment total');
    requests.shift().resolve({ totalRows: 9, items: [{ id: 1 }] });
    await pending;
    assert.equal(total(), '6', 'booking pagination does not change campaign totals');
    assert.equal(elements['appointment-rate'].textContent, '2.6% of human contacts');
    history();
    assert.equal(total(), '6', 'late history keeps summary consistent');

    pending = context.loadAppointments(true);
    assert.ok(requests[0].url.includes('refresh=true'), 'explicit refresh rebuilds appointment data');
    requests.shift().resolve({ totalRows: 9, items: [] });
    await pending;
    const submitHandler = source.slice(source.indexOf('filterForm.addEventListener("submit"'), source.indexOf('document.getElementById("date-range")'));
    assert.ok(submitHandler.includes('refreshReport(true)'), 'Refresh report opts into rebuilding');

    const oldRequest = context.loadAppointments();
    const newRequest = context.loadAppointments();
    requests[1].resolve({ totalRows: 2, items: [] });
    await newRequest;
    requests[0].resolve({ totalRows: 9, items: [] });
    await oldRequest;
    requests.length = 0;
    assert.equal(context.state.appointments.totalRows, 2, 'older filter request must not overwrite newer results');

    pending = context.loadAppointments();
    requests.shift().resolve({ totalRows: 0, items: [] });
    await pending;
    assert.equal(total(), '6', 'empty booking list does not change campaign totals');
    pending = context.loadAppointments();
    requests.shift().reject(new Error('offline'));
    await assert.rejects(pending, /offline/);
    history();
    assert.equal(total(), '6', 'booking failure does not hide available historical metrics');
    console.log('PASS: consistent historical totals and rates across booking loads, pagination, ordering and failures.');
})().catch(error => { console.error(error); process.exitCode = 1; });
