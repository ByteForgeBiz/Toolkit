const assert = require('node:assert/strict');
const entry = require('./ByteForge-company-entry.js');

function check(text, id, inbound, blank, expected) {
    const input = { value: text, setCustomValidity(value) { this.error = value; },
        reportValidity() {}, focus() {} };
    assert.equal(entry.validate(input, { value: id }, inbound, blank, true, () => {}), expected);
    assert.equal(input.error === '', expected);
}
check('New company', '', true, false, true);
check('New company', '', true, true, true);
check('A', '', true, false, true);
check('   ', '', true, false, false);
check('', '', true, true, true);
check('New company', '', false, false, false);
check('New company', '', false, true, false);
check('Selected company', '17', true, false, true);
check('Selected company', '17', false, false, true);
check('', '', false, true, true);

const status = { textContent: '', classList: { toggle(name, value) { status.isError = value; } } };
entry.setStatus(status, 'No matching existing company was found. This company cannot be saved.', true, true);
assert.match(status.textContent, /use the name you typed/);
assert.equal(status.isError, false);
entry.setStatus(status, 'Companies could not be loaded.', true, true);
assert.equal(status.isError, true);
check('New company after failed search', '', true, false, true);
console.log('PASS company entry: inbound typing, blank drafts, selected companies, outbound restrictions, failed search');
