const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');
const assert = require('assert/strict');
const repo = process.env.MAINPROJECTS_ROOT || path.resolve(__dirname, '../../..');
const web = path.join(repo, 'PaydayPerx/Walgreens/src/PaydayPerx.Walgreens.Web');
const rows = JSON.parse(fs.readFileSync(path.join(repo, 'Logs/WalgreensTeamChecks/script-after.json'), 'utf8').replace(/^\uFEFF/, ''));
const Steps = rows.filter(r => r.IsActive).map(r => ({Id:r.StepId,Label:r.StepLabel,Type:r.StepType,Next:r.NextStep,Yes:r.YesStep,No:r.NoStep,Text:r.StepText,Note:r.StepNote,Anchor:r.Anchor}));
const view = fs.readFileSync(path.join(web,'Views/Home/Agent.cshtml'),'utf8');
const marker = view.match(/<strong class="bf-required-marker"[^>]*>\*<\/strong>/)[0];
const html = `<div class="agent-script-panel" data-agent-script-panel data-agent-name="Test Agent">
<ol class="agent-script-steps" data-agent-script-steps></ol><div data-agent-script-card></div><div data-agent-script-actions></div></div>
<form class="agent-intake-form"><section class="agent-form-section agent-form-section-primary"><div class="agent-form-grid">
<label><span>Email ${marker}</span><input name="Email" type="email" required value="agent-test@example.test"></label>
<label><span>Decision Maker Job Title ${marker}</span><input name="MainContactJobTitle" required value="HR Manager"></label>
<label><span>First Name ${marker}</span><input name="FirstName" required value="Test"></label>
<label><span>Participants ${marker}</span><input name="EstimatedParticipants" value="50"></label></div>
<input name="Program" type="radio" data-voucher-program value="Voucher Program Only" required>
<input name="Program" type="radio" data-clinic-program value="Clinic">
<input name="Program" type="radio" data-clinic-program value="Clinic + Vouchers">
<p data-voucher-only-message hidden>Fewer than 50: voucher only</p>
<div data-vaccination-details><input name="Vaccinations" type="checkbox" value="Flu Trivalent"></div>
<div data-clinic-only><input name="EstimatedClinics" required></div>
<div data-location-details><input name="AdditionalLocations[0].City"></div>
</section></form><script id="walgreensCallScriptData" type="application/json">${JSON.stringify({Steps}).replace(/</g,'\\u003c')}</script>`;
let checks=0;
function check(value, message){assert(value,message);checks++;}
(async()=>{
 const browser=await chromium.launch({channel:'msedge',headless:true});
 try {
 const page=await browser.newPage({viewport:{width:1280,height:1000}});
 await page.setContent(html);
 await page.addScriptTag({path:path.join(__dirname,'walgreens-labels.js')});
 await page.addStyleTag({path:path.join(__dirname,'site.css')});
 await page.addScriptTag({path:path.join(__dirname,'walgreens-intake.js')});
 await page.evaluate(()=>{
  window.intakeForm=document.querySelector('form');
  window.updateProgramFieldVisibility=()=>ByteForgeWalgreensIntake.updateProgramFieldVisibility(intakeForm,false,50);
  intakeForm.addEventListener('change',updateProgramFieldVisibility);
  updateProgramFieldVisibility();
 });
 await page.addScriptTag({path:path.join(__dirname,'walgreens-call-script.js')});
 check(await page.locator('[data-agent-script-jump]').count()===9,'Nine menu entries');
 for(const id of ['Discovery','lead','voucher-offer']) check(await page.locator(`[data-agent-script-jump="${id}"]`).count()===0,'Removed '+id);
 for(const program of ['Voucher Program Only','Clinic','Clinic + Vouchers']) {
  await page.locator('[data-agent-script-jump="background"]').click();
  await page.locator(`[data-agent-script-program="${program}"]`).click();
  check(await page.locator('input[name=Program]:checked').inputValue()===program,'Selected program synchronized');
  check((await page.locator('.agent-script-step.is-active').innerText()).includes(program==='Voucher Program Only'?'Voucher Program Only':'Clinic or Both Programs'),'Correct interest branch');
  check(await page.locator('[data-clinic-only]').isVisible()===(program!=='Voucher Program Only'),'Clinic details visibility');
  await page.locator('[data-agent-script-action=yes]').click();
  check((await page.locator('.agent-script-step.is-active').innerText()).includes(program==='Voucher Program Only'?'Close for Voucher':'Close for Clinic'),'Correct closing');
 }
 await page.locator('[data-agent-script-jump="rebuttals"]').click();
 await page.locator('[data-agent-script-action=yes]').click();
 check(await page.locator('input[name=Program]:checked').inputValue()==='Voucher Program Only','Rebuttal acceptance switches clinic to voucher');
 await page.locator('[data-agent-script-jump="Program Interest - Greater than 50 Participants"]').click();
 check(await page.locator('[data-agent-script-program=Clinic]').count()===1,'Direct clinic jump asks for the clinic choice');
 await page.locator('[name=EstimatedParticipants]').fill('49');
 await page.locator('[name=EstimatedParticipants]').dispatchEvent('change');
  await page.locator('[data-agent-script-jump=background]').click();
 check(await page.locator('[data-agent-script-program=Clinic]').isDisabled(),'Clinic blocked below 50');
 await page.locator('[name=EstimatedParticipants]').fill('500');
 await page.locator('[name=EstimatedParticipants]').dispatchEvent('change');
 await page.locator('[data-agent-script-program="Voucher Program Only"]').click();
 check(!await page.locator('[data-vaccination-details]').isVisible(),'Voucher 500 does not require clinic vaccination fields');
 await page.locator('[data-agent-script-action=no]').click();
 check((await page.locator('[data-agent-script-card]').innerText()).includes('45%'),'New rebuttal displayed');
 await page.locator('[data-agent-script-action=no]').click();
 check(await page.locator('[data-agent-script-action=confirm-not-interested]').count()===1,'True decline action available with configured ID');
 // Exercise the real Opt-In payload and validation functions retained by the view.
 for(const name of ['buildOptInPayload','validateOptInForm']) {
  const match=view.match(new RegExp('        function '+name+'\\(\\) \\{[\\s\\S]*?\\n        \\}'));
  assert(match,'Missing actual '+name); await page.addScriptTag({content:match[0]});
 }
 check(await page.evaluate(()=>buildOptInPayload().get('submissionAction'))==='submit','Opt-In uses submit action');
 check(await page.evaluate(()=>validateOptInForm()),'Voucher complete form validates');
 await page.locator('[name=Email]').fill('');
 check(!await page.evaluate(()=>validateOptInForm()),'Email required');
 await page.locator('[name=Email]').fill('agent-test@example.test');
 await page.locator('[name=MainContactJobTitle]').fill('');
 check(!await page.evaluate(()=>validateOptInForm()),'Job title required');
 check(/data-request-outcome="MoreInfo"[\s\S]*?>GK Send Info<\/button>/.test(view),'Gatekeeper Send Info retains its existing action');
 check(/data-request-outcome="SendVoucher" data-template-action="SEND VOUCHER">Send Referral Voucher<\/button>/.test(view),'Referral voucher button restores existing outcome and template action');
 // Exercise the actual voucher payload, including the intake snapshot and separate outcome.
 for(const name of ['buildMoreInfoPayload','validateSendInfoProgram']) {
  const match=view.match(new RegExp('        function '+name+'\\([^)]*\\) \\{[\\s\\S]*?\\n        \\}'));
  assert(match,'Missing actual '+name); await page.addScriptTag({content:match[0]});
 }
 await page.evaluate(()=>{window.intakeValue=name=>{const field=intakeForm.elements.namedItem(name);return field ? field.value : '';};});
 const voucher=await page.evaluate(()=>Object.fromEntries(buildMoreInfoPayload('SendVoucher')));
 check(voucher.Outcome==='SendVoucher' && voucher.StepId==='voucher-offer','Voucher remains a separate outcome');
 check(voucher.ContactEmail==='agent-test@example.test' && voucher['intake.Email']===voucher.ContactEmail,'Voucher includes email and intake snapshot');
 check(await page.evaluate(()=>validateSendInfoProgram('SendVoucher')),'Voucher request does not require clinic details');
 for(const theme of ['light','dark']) {
  await page.evaluate(t=>document.documentElement.dataset.theme=t,theme);
  const sizes=await page.locator('.bf-required-marker').first().evaluate(el=>({marker:parseFloat(getComputedStyle(el).fontSize),label:parseFloat(getComputedStyle(el.parentElement).fontSize),color:getComputedStyle(el).color}));
  check(sizes.marker>=sizes.label*1.5,'Larger required marker in '+theme);
  await page.screenshot({path:path.join(repo,`Logs/WalgreensTeamChecks/script-${theme}.png`),fullPage:true});
 }
 console.log(`${checks} script browser checks passed.`);
 } finally {await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
