const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const source = fs.readFileSync(__dirname + '/report-portal.js', 'utf8');
const fields = { 'service-filter': { value: 'Praxis_EEP' }, 'team-filter': { value: 'Praxis_EEP' } };
const requests = [];
const context = {
    state: {}, document: { getElementById: id => fields[id] ||= {} },
    getBounds: () => ({ fromIso: '2026-09-25T07:00:00Z', toIso: '2026-09-26T07:00:00Z' }),
    queryString: value => new URLSearchParams(Object.entries(value).filter(([,v]) => v !== undefined)).toString(),
    apiGet: url => new Promise((resolve,reject) => requests.push({ url,resolve,reject })),
    beginCardLoading() {}, buildAgentRows() {}, renderAgents() {}, renderHistorySummary() {}, renderHistory() {},
    completeCardLoading: (_,request) => request
};
vm.createContext(context);
vm.runInContext(source.slice(source.indexOf('    function loadMetrics('),source.indexOf('    function loadProgress(')),context);
(async()=>{
    const old = context.loadMetrics();
    assert.equal(new URLSearchParams(requests[0].url.split('?')[1]).get('service'),'Praxis_EEP');
    fields['service-filter'].value='Praxis_IEP'; fields['team-filter'].value='Praxis_IEP';
    const current=context.loadMetrics();
    requests[1].resolve({items:[{agent:'ksimmons',dials:41}]}); await current;
    requests[0].resolve({items:[{agent:'csparks',dials:110}]}); await old;
    assert.equal(context.state.metrics.items[0].agent,'ksimmons');
    const h1=context.loadHistory(); const h2=context.loadHistory();
    assert.equal(new URLSearchParams(requests[2].url.split('?')[1]).get('team'),'Praxis_IEP');
    requests[3].resolve({items:[{dials:41}]}); await h2;
    requests[2].resolve({items:[{dials:110}]}); await h1;
    assert.equal(context.state.history.items[0].dials,41);
    const failure=context.loadMetrics(); assert.equal(context.state.metrics,null);
    requests[4].reject(new Error('offline')); await assert.rejects(failure,/offline/);
    assert.equal(context.state.metricsLoading,false);
    assert.match(context.state.metricsError,/could not be loaded/);
    const recovered=context.loadMetrics();
    assert.equal(context.state.metricsError,'');
    assert.equal(context.state.metricsLoading,true);
    requests[5].resolve({items:[]}); await recovered;
    assert.equal(context.state.metricsLoading,false);
    assert.equal(context.state.metricsError,'');
    const agent=context.metricParameters(context.getBounds(),'ksimmons');
    assert.equal(agent.agent,'ksimmons'); assert.equal(agent.service,'Praxis_IEP'); assert.equal(agent.team,'Praxis_IEP');
    console.log('PASS: service/team propagation, agent history scope, late responses, and failure cleanup.');
})().catch(error=>{console.error(error);process.exitCode=1;});
