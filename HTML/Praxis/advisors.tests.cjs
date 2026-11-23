const assert=require('node:assert/strict');
const fs=require('node:fs');const path=require('node:path');const {chromium}=require('playwright');
(async()=>{const browser=await chromium.launch({headless:true,channel:'msedge'});try{
for(const scenario of ['allowed','denied','failure','anonymous']){
 const context=await browser.newContext();const page=await context.newPage();page.setDefaultTimeout(10000);
 if(scenario!=='anonymous')await context.addInitScript(()=>sessionStorage.setItem('praxis.reportPortal.accessToken','fixture'));
 const errors=[];page.on('pageerror',e=>errors.push(e.message));
 const rows=Array.from({length:12},(_,i)=>({id:i+1,revisionId:i+10,advisorName:i===0?'Anna <Test>':'Advisor '+i,advisorFirm:i<2?'Old Firm':'Firm '+i,advisorEmail:'a'+i+'@example.test',advisorFirst:'Anna',advisorLast:'Test',active:i!==1,include1031:false}));
 let plan=null,saved=0,synced=0,lastBody;
 await page.route('https://praxis.test/**',route=>{
  const url=new URL(route.request().url());assert.equal(url.href.includes('fixture'),false);
  if(url.pathname==='/api/rbac-assets/account-script')return route.fulfill({body:fs.readFileSync(path.join(process.cwd(),'ByteForge.Toolkit/HTML/JavaScript/ByteForge-account.js')),contentType:'application/javascript'});
  if(url.pathname==='/api/auth/profile'){assert.equal(route.request().headers().authorization,'Bearer fixture');return route.fulfill({json:{displayName:'Morgan Example',username:'morgan',email:'morgan@example.test'}});}
  if(url.pathname==='/api/advisor-lists/access')return route.fulfill({status:403,json:{message:'Not permitted'}});
  if(url.pathname==='/api/auth/pages')return route.fulfill({json:{pages:[{key:scenario==='denied'?'PraxisReports':'PraxisAdvisorEditor',route:'advisors.html',label:'Advisors'}]}});
  if(url.pathname==='/api/advisors')return route.fulfill({status:scenario==='failure'?500:200,json:{advisors:rows,jobs:[]}});
  if(url.pathname==='/api/advisors/import-preview'){
   lastBody=route.request().postDataJSON();assert.equal(lastBody.fileName,'advisors.csv');
   plan={id:'import-1',status:'Preview',before:[rows[0]],after:[{...rows[0],advisorName:'Annie'}, {id:0,advisorName:'Ben',advisorFirm:'Other',active:true}],total:4,verified:0,lists:Array.from({length:4},(_,i)=>({name:'Praxis list '+i,total:1,verified:0}))};return route.fulfill({json:plan});
  }
  if(url.pathname==='/api/advisors/preview'){
   lastBody=route.request().postDataJSON();const before=lastBody.advisor?(lastBody.advisor.id?[rows[0]]:[]):rows.filter(a=>a.advisorFirm===lastBody.oldFirm);
   const after=lastBody.advisor?[lastBody.advisor]:before.map(a=>({...a,advisorFirm:lastBody.newFirm}));
   plan={id:'plan-1',status:'Preview',before,after,total:4,verified:0,lists:Array.from({length:4},(_,i)=>({name:'Praxis list '+i,total:1,verified:0}))};return route.fulfill({json:plan});
  }
  if(url.pathname.endsWith('/save')){saved++;plan.status=plan.before.length?'Pending':'Synced';return route.fulfill({json:plan});}
  if(url.pathname.endsWith('/sync')){synced++;plan.status='Synced';plan.verified=4;return route.fulfill({json:plan});}
  if(url.pathname.endsWith('/history'))return route.fulfill({json:[rows[0]]});
  if(url.pathname.startsWith('/api/rbac-assets/'))return route.fulfill({body:'',contentType:url.pathname.endsWith('style')?'text/css':'application/javascript'});
  if(url.pathname==='/login.html')return route.fulfill({body:'Login',contentType:'text/html'});
  const file=path.join(process.cwd(),'PraxisAPI',url.pathname);return route.fulfill({body:fs.readFileSync(file),contentType:file.endsWith('.html')?'text/html':file.endsWith('.css')?'text/css':file.endsWith('.png')?'image/png':'application/javascript'});
 });
 await page.goto('https://praxis.test/advisors.html');
 if(scenario==='anonymous')await page.waitForURL('**/login.html?*');
 else if(scenario==='denied')await page.getByText('Your role does not grant access to Advisors.').waitFor();
 else if(scenario==='failure')await page.getByText('Unable to load advisors. Please retry.').waitFor();
 else{
  await page.getByRole('button',{name:'My profile: Morgan Example',exact:true}).waitFor();
  assert.equal(await page.locator('#advisor-message').evaluate(e=>e.getBoundingClientRect().height),0);
  assert.equal(await page.locator('#advisor-page h1').evaluate(e=>getComputedStyle(e).fontSize),'24px');
  for(const colorScheme of ['light','dark']){await page.emulateMedia({colorScheme});await page.screenshot({path:path.join(process.cwd(),'Logs','advisors-header-'+colorScheme+'.png')});}
  await page.locator('#session-status').click();await page.getByRole('dialog',{name:'My TELCO profile'}).waitFor();await page.getByRole('button',{name:'Close profile',exact:true}).click();
  await page.getByText('12 advisors | Page 1 of 2').waitFor();await page.getByRole('button',{name:'Next',exact:true}).click();await page.getByText('12 advisors | Page 2 of 2').waitFor();
  await page.locator('#advisor-search').fill('Anna');await page.getByRole('button',{name:'Edit Anna <Test>',exact:true}).click();
  await page.getByRole('dialog').waitFor();await page.getByLabel('Display name',{exact:true}).fill('Annie');await page.getByRole('button',{name:'Review changes'}).click();
  await page.getByText('Review the affected advisors and lists, then save.').waitFor();assert.equal(lastBody.advisor.revisionId,10);
  assert.match(await page.locator('#advisor-changes').innerText(),/Anna <Test>[\s\S]*Annie/);
  page.once('dialog',d=>d.dismiss());await page.keyboard.press('Escape');assert.equal(await page.getByRole('dialog').isVisible(),true);
  for(const colorScheme of ['light','dark']){await page.emulateMedia({colorScheme});await page.screenshot({path:path.join(process.cwd(),'Logs','advisors-modal-'+colorScheme+'.png')});}
  await page.setViewportSize({width:390,height:844});await page.screenshot({path:path.join(process.cwd(),'Logs','advisors-modal-mobile.png')});assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),true);
  await page.getByRole('button',{name:'Save and synchronize'}).click();await page.getByRole('dialog').waitFor({state:'detached'});assert.equal(saved,1);assert.equal(synced,1);
  await page.setViewportSize({width:1280,height:900});await page.getByRole('button',{name:'Rename firm',exact:false}).click();
  await page.getByLabel('Current firm').selectOption('Old Firm');await page.getByLabel('New firm name').fill('New Firm');await page.getByRole('button',{name:'Review firm rename'}).click();
  await page.getByText('2 advisor(s); 4 BP record(s) across 4 configured lists.').waitFor();assert.equal(plan.after[1].active,false);
  await page.screenshot({path:path.join(process.cwd(),'Logs','advisors-firm-modal.png')});await page.getByRole('button',{name:'Rename and synchronize'}).click();await page.getByRole('dialog').waitFor({state:'detached'});assert.equal(saved,2);
  await page.getByRole('button',{name:'Add advisor',exact:false}).click();await page.getByRole('dialog').waitFor();
  assert.equal(await page.locator('#advisor-history-button').isVisible(),false);
  await page.getByLabel('Display name',{exact:true}).fill('New Advisor');await page.getByLabel('Firm',{exact:true}).fill('New Firm');await page.getByRole('button',{name:'Review changes'}).click();
  await page.getByText('Review the affected advisors and lists, then save.').waitFor();assert.equal(lastBody.advisor.id,0);assert.equal(lastBody.advisor.active,true);
  const priorSync=synced;await page.locator('#advisor-save').click();await page.getByRole('dialog').waitFor({state:'detached'});assert.equal(synced,priorSync);
  await page.getByRole('button',{name:'Import XLSX/CSV',exact:false}).click();
  await page.locator('#advisor-import-file').setInputFiles({name:'advisors.csv',mimeType:'text/csv',buffer:Buffer.from('Id,AdvisorName,AdvisorFirm\n1,Annie,Old Firm\n,Ben,Other')});
  await page.getByRole('button',{name:'Review import',exact:false}).click();await page.getByText('Review every proposed change before saving.',{exact:false}).waitFor();
  assert.match(await page.locator('#advisor-import-impact').innerText(),/1 additions, 1 existing advisors/);
  for(const colorScheme of ['light','dark']){await page.emulateMedia({colorScheme});await page.screenshot({path:path.join(process.cwd(),'Logs','advisors-import-'+colorScheme+'.png')});}
  await page.setViewportSize({width:390,height:844});await page.screenshot({path:path.join(process.cwd(),'Logs','advisors-import-mobile.png')});assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),true);

  await page.getByRole('button',{name:'Save import and synchronize',exact:false}).click();await page.getByRole('dialog').waitFor({state:'detached'});assert.equal(saved,4);

 }
 assert.deepEqual(errors,[]);console.log(scenario+' passed');await context.close();
}
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1;});
