const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const source = fs.readFileSync(__dirname + '/report-portal.js', 'utf8');
function element(tag) {
    return { tag, children: [], events: {}, attributes: {}, textContent: '',
        appendChild(child) { this.children.push(child); },
        setAttribute(key, value) { this.attributes[key] = value; },
        addEventListener(key, handler) { this.events[key] = handler; } };
}
const context = {
    state: { configuration: { reportDialingDays: [1, 2, 3, 4, 5] } } ,
    document: { createElement: element },
    appendTextElement(parent, tag, text) { const child = element(tag); child.textContent = text; parent.appendChild(child); return child; },
    formatPercent: n => (n * 100).toFixed(1) + '%',
    formatNumber: (n, decimals) => n.toFixed(decimals),
    formatReportDate: date => date
};
vm.createContext(context);
vm.runInContext(source.slice(source.indexOf('    function renderHistoryDelta('), source.indexOf('    function renderHistoryChart(')), context);
const series = { id: 'dials', title: 'Dials / hr', unit: 'calls / hr', decimals: 1 };
function render(values) {
    return context.renderHistoryDelta(series, values, values.map((_, index) => ({ date: '2026-09-' + String(7 + index).padStart(2, '0') })), 'team');
}
function text(node) { return node.textContent + node.children.map(text).join(' '); }
const decreasing = render([null, 20, 12, 15, null]);
const [pill, tooltip] = decreasing.children;
assert.equal(pill.textContent, '-25.0%');
assert.match(text(tooltip), /25.0% decrease between 2026-09-08 and 2026-09-10/);
assert.match(text(tooltip), /20.0 calls \/ hr/);
assert.match(text(tooltip), /15.0 calls \/ hr/);
assert.equal(tooltip.hidden, true);
pill.events.focus(); assert.equal(tooltip.hidden, false);
pill.events.keydown({ key: 'Escape', stopPropagation() {} }); assert.equal(tooltip.hidden, true);
decreasing.events.mouseenter(); assert.equal(tooltip.hidden, false);
decreasing.events.mouseleave(); assert.equal(tooltip.hidden, true);
assert.match(text(render([10, 15])), /50.0% increase/);
assert.match(text(render([10, 10])), /no change/);
assert.match(text(render([10, 0])), /100.0% decrease/);
assert.match(text(render([0, 10])), /first chart value is zero/);
assert.match(text(render([null, 10])), /At least two chart points/);
assert.match(text(render([null, null])), /At least two chart points/);
console.log('PASS: endpoint percentages, actual dates, zero/missing values, hover, focus and Escape.');
// Monday through Sunday: weekend zeroes must not change Friday's comparison.
assert.equal(render([20, 20, 20, 20, 15, 0, 0]).children[0].textContent, '-25.0%');
assert.match(text(render([20, 20, 20, 20, 15, 0, 0])), /2026-09-07 and 2026-09-11/);
assert.equal(render([20, 20, 20, 20, 0, 0, 0]).children[0].textContent, '-100.0%');
context.state.configuration.reportDialingDays = [6, 0];
assert.equal(render([20, 20, 20, 20, 15, 10, 15]).children[0].textContent, '+50.0%');
context.state.configuration.reportDialingDays = [];
assert.throws(() => render([10, 20]), /dialing days are missing or invalid/);
console.log('PASS: weekend exclusions, real weekday zeroes, custom dialing schedule and missing configuration.');