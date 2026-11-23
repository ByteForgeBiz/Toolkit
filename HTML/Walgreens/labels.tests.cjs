const assert = require('assert/strict');
const fs = require('fs');
const vm = require('vm');
const path = require('path');
const window = {};
vm.runInNewContext(fs.readFileSync(path.join(__dirname,'walgreens-labels.js'),'utf8'), {window});
const label = window.ByteForgeWalgreensLabels.outcome;
for(const value of ['Send Info','send info','GK Send Info','  SEND INFO  ']) assert.equal(label(value),'GK Send Info');
for(const value of ['Opt-In','Send Voucher','Company Send Info',null,undefined]) assert.equal(label(value),value);
// Run the actual report export formatter. Customer data and free-form notes must stay intact.
const source=fs.readFileSync(path.join(__dirname,'walgreens-report-mockup.js'),'utf8');
const formatter=source.match(/        function getCompanyExportValue\(row, key\) \{[\s\S]*?\n        \}/)[0];
const context={window}; vm.createContext(context);vm.runInContext(formatter,context);
assert.equal(context.getCompanyExportValue({status:'Send Info'},'status'),'GK Send Info');
assert.equal(context.getCompanyExportValue({disposition:'Send Info'},'disposition'),'GK Send Info');
assert.equal(context.getCompanyExportValue({company:'Send Info'},'company'),'Send Info');
assert.equal(context.getCompanyExportValue({agentNotes:'Send Info'},'agentNotes'),'Send Info');
console.log('13 outcome label and export checks passed.');
