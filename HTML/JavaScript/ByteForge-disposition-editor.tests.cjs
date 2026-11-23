const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

class Element {
    constructor(tag) { this.tagName = tag; this.children = []; this.style = {}; this.value = ''; }
    appendChild(child) { this.children.push(child); }
    focus() { this.focused = true; }
    set innerHTML(value) { throw new Error('Editor must render untrusted labels as text.'); }
}
const context = { window: {}, document: { createElement: tag => new Element(tag) } };
vm.runInNewContext(fs.readFileSync(path.join(__dirname, 'ByteForge-disposition-editor.js'), 'utf8'), context);
const root = new Element('section');
const errors = [];
const editor = new context.window.ByteForgeDispositionEditor(root, {
    calls: [{ value: 101, label: '<script>untrusted</script>' }],
    dispositions: ['No need', '<img src=x>'], onError: message => errors.push(message)
});
const [call, disposition, reason] = root.children.map(label => label.children[0]);
assert.equal(call.value, '101');
assert.equal(call.children[1].textContent, '<script>untrusted</script>');
assert.equal(disposition.value, '');
assert.equal(editor.read(), null);
assert.match(errors.pop(), /replacement disposition/);
disposition.value = 'Invented';
assert.equal(editor.read(), null);
disposition.value = 'No need';
assert.equal(editor.read(), null);
reason.value = 'x'.repeat(501);
assert.equal(editor.read(), null);
reason.value = '  Duplicate booking  ';
assert.equal(JSON.stringify(editor.read()), JSON.stringify({ primaryKey: 101, disposition: 'No need', reason: 'Duplicate booking' }));
call.value = '102';
assert.equal(editor.read(), null);
editor.setDisabled(true);
assert.ok([call, disposition, reason].every(control => control.disabled));
editor.setDisabled(false);
assert.ok([call, disposition, reason].every(control => !control.disabled && control.style.fontWeight === '400'));
const multiple = new Element('section');
new context.window.ByteForgeDispositionEditor(multiple, { calls: [{ value: 1 }, { value: 2 }] });
assert.equal(multiple.children[0].children[0].value, '');
console.log('Disposition editor checks passed: required choices, exact target, safe labels, reason limits, payload and disabled state.');
