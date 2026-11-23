const assert=require('node:assert/strict');const fs=require('node:fs');const path=require('node:path');const {chromium}=require('playwright');
(async()=>{const browser=await chromium.launch({headless:true,channel:'msedge'});try{
for(const allowed of [false,true]){
 const context=await browser.newContext();const page=await context.newPage();page.setDefaultTimeout(10000);
 await context.addInitScript(()=>sessionStorage.setItem('praxis.reportPortal.accessToken','fixture'));
 let discovery=0,saves=0,openCount=0;const errors=[];page.on('pageerror',e=>errors.push(e.message));
 const campaigns=[{id:'camp',name:'Praxis'},{id:'other',name:'Praxis_1031'}];
 const binding=(i)=>({listId:'list-'+i,name:'Praxis_'+i,campaignId:'camp',campaignName:'Praxis',observedCampaignName:'Praxis',fields:['RecordID','Phone','AdvisorName','AdvisorFirm'],fieldsCheckedAtUtc:'2026-09-24T00:00:00Z',recordIdField:'RecordID',phoneField:'Phone',nameField:'AdvisorName',firmField:'AdvisorFirm'});
 await page.route('https://praxis.test/**',route=>{
  const url=new URL(route.request().url());
  if(url.pathname==='/api/auth/pages')return route.fulfill({json:{pages:[{key:'PraxisAdvisorEditor',label:'Advisors',route:'advisors.html'}]}});
  if(url.pathname==='/api/advisors')return route.fulfill({json:{advisors:[],jobs:[]}});
  if(url.pathname==='/api/advisor-lists/access')return route.fulfill({status:allowed?200:403,json:{canConfigure:allowed}});
  if(url.pathname==='/api/advisor-lists/open'){openCount++;return route.fulfill({json:{token:'draft',lists:Array.from({length:6},(_,i)=>binding(i)),catalog:Array.from({length:8},(_,i)=>({id:'list-'+i,name:'Praxis_'+i})),campaigns}});}
  if(url.pathname==='/api/advisor-lists/discover'){discovery++;const body=route.request().postDataJSON();assert.equal(body.token,'draft');const b=binding(Number(body.listId.split('-')[1]));b.fields=['RecordID','Phone','AdvisorFirm'];b.nameField=null;return route.fulfill({json:b});}
  if(url.pathname==='/api/advisor-lists/save'){saves++;const b=route.request().postDataJSON();assert.equal(b.token,'draft');assert.equal(b.lists.length,6);assert.equal(b.lists.find(x=>x.listId==='list-0').nameField,null);assert.equal(b.lists.find(x=>x.listId==='list-7').campaignId,'other');return route.fulfill({json:{lists:b.lists}});}
  if(url.pathname.startsWith('/api/rbac-assets/'))return route.fulfill({body:'',contentType:url.pathname.endsWith('style')?'text/css':'application/javascript'});
  const file=path.join(process.cwd(),'PraxisAPI',url.pathname);return route.fulfill({body:fs.readFileSync(file),contentType:file.endsWith('.html')?'text/html':file.endsWith('.css')?'text/css':file.endsWith('.png')?'image/png':'application/javascript'});
 });
 await page.goto('https://praxis.test/advisors.html');await page.locator('#advisor-page').waitFor();
 if(!allowed){await page.waitForTimeout(150);assert.equal(await page.locator('#advisor-configure-lists').isVisible(),false);assert.equal(openCount,0);}
 else{
  await page.getByRole('button',{name:'Configure lists',exact:false}).click();await page.getByText('6 lists | Page 1 of 2').waitFor();assert.equal(discovery,0);
  const dialog=page.locator('.byte-forge-modal-overlay').filter({has:page.locator('.bf-advisor-list-config')});await dialog.getByRole('button',{name:'Next',exact:true}).click();await page.getByText('6 lists | Page 2 of 2').waitFor();
  await page.getByLabel('Search configured lists',{exact:true}).fill('Praxis_0');await page.getByText('1 lists | Page 1 of 1').waitFor();
  await dialog.getByRole('button',{name:'Refresh fields',exact:false}).click();await page.getByText('Mappings: Praxis_0',{exact:true}).waitFor();assert.equal(discovery,1);
  assert.equal(await page.getByRole('combobox',{name:'Advisor name field',exact:true}).inputValue(),'');
  for(const colorScheme of ['light','dark']){await page.emulateMedia({colorScheme});await page.waitForTimeout(350);await page.screenshot({path:path.join(process.cwd(),'Logs','advisor-lists-'+colorScheme+'.png')});}
  await page.setViewportSize({width:390,height:844});await page.screenshot({path:path.join(process.cwd(),'Logs','advisor-lists-mobile.png')});assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),true);
  const mappings=page.locator('.byte-forge-modal-overlay').filter({has:page.locator('.bf-advisor-mappings')}); assert.equal(await dialog.evaluate(e=>e.inert),true); await page.getByRole('combobox',{name:'Campaign association',exact:true}).focus(); await page.keyboard.press('Shift+Tab'); assert.equal(await page.getByRole('button',{name:'Done with mappings',exact:false}).evaluate(e=>e===document.activeElement),true); await page.keyboard.press('Escape');await mappings.waitFor({state:'detached'});assert.equal(await dialog.isVisible(),true);assert.equal(await dialog.evaluate(e=>e.inert),false);assert.equal(await dialog.getByRole('button',{name:'Mappings',exact:false}).evaluate(e=>e===document.activeElement),true); page.once('dialog',d=>d.dismiss());await page.keyboard.press('Escape');assert.equal(await dialog.isVisible(),true);
  await page.getByLabel('Find BP list',{exact:true}).fill('Praxis_7');await page.getByRole('combobox',{name:'Available BP lists',exact:true}).selectOption('list-7');await dialog.getByRole('button',{name:'Add list',exact:false}).click();await page.getByText('Mappings: Praxis_7',{exact:true}).waitFor();
  await page.getByRole('combobox',{name:'Campaign association',exact:true}).selectOption('other'); await page.getByRole('button',{name:'Done with mappings',exact:false}).click();await mappings.waitFor({state:'detached'});
  await page.getByLabel('Search configured lists',{exact:true}).fill('Praxis_1');await dialog.getByRole('row').filter({has:page.getByRole('cell',{name:'Praxis_1',exact:true})}).getByRole('button',{name:'Remove',exact:false}).click();
  await dialog.getByRole('button',{name:'Save list configuration',exact:false}).click();await dialog.waitFor({state:'detached'});assert.equal(saves,1);assert.equal(discovery,2);
 }
 assert.deepEqual(errors,[]);console.log('list configuration '+(allowed?'allowed':'denied')+' passed');await context.close();
}
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1;});
