const fs = require('fs');
const path = require('path');
const assert = require('assert/strict');
const { chromium } = require('playwright');
const root = path.resolve('PaydayPerx/Walgreens/src/PaydayPerx.Walgreens.Web');
const toolkit = path.resolve('ByteForge.Toolkit/HTML/Walgreens');
function fn(source, name) {
 const start = source.indexOf('function ' + name + '('); assert(start >= 0);
 const open = source.indexOf('{', start); let depth = 1, i = open + 1;
 while (depth && i < source.length) { if(source[i] === '{') depth++; if(source[i] === '}') depth--; i++; }
 return source.slice(start,i);
}
(async()=>{
 const browser = await chromium.launch({headless:true,channel:'msedge'});
 try {
 const page = await browser.newPage({ viewport: { width: 1180,height:400 } });
 const css = fs.readFileSync(path.join(root,'Content/ReportMockup/report-mockup.css'),'utf8');
 const html = `<html><head><meta charset="utf-8"><style>${css}</style></head><body><div class="walgreens-report" style="background:var(--bg);min-height:100vh"><section class="topbar report-filterbar"><div class="topbar-actions"><div class="filter-control"><label for="reportTeam">Team</label><select id="reportTeam"><option value="all">All</option><option value="telecom">Telecom</option><option value="bpo">BPO</option></select></div><div class="filter-control"><label for="startDate">Start</label><input type="date" id="startDate" value="2026-09-01"></div><div class="filter-control"><label for="endDate">End</label><input type="date" id="endDate" value="2026-10-01"></div><button class="text-button primary-button" id="applyFiltersButton">Apply</button></div></section><main class="page"><p class="report-meta">&#9888; All includes records with an unassigned team. These records are excluded from Telecom and BPO until their team can be verified.</p></main></div></body></html>`;
 await page.route('https://fixture.invalid/**',route=>route.fulfill({contentType:'text/html',body:html}));
 for(const [script,func] of [['walgreens-report-mockup.js','applyReportDateRange'],['walgreens-agent-report.js','applyDateRange']]) {
   const source=fs.readFileSync(path.join(toolkit,script),'utf8');
   assert.equal(source,fs.readFileSync(path.join(root,'Scripts',script),'utf8'));
   for(const team of ['all','telecom','bpo']) {
    await page.goto('https://fixture.invalid/Admin');
    await page.addScriptTag({content:fn(source,func)+`; document.getElementById('applyFiltersButton').onclick=${func};`});
    await page.selectOption('#reportTeam',team);
    await Promise.all([page.waitForURL(url=>url.searchParams.get('team')===team),page.click('#applyFiltersButton')]);
    const url=new URL(page.url());assert.equal(url.searchParams.get('startDate'),'2026-09-01');assert.equal(url.searchParams.get('endDate'),'2026-10-01');
    console.log('PASS '+script+' applies '+team+' with dates');
   }
 }
 for(const theme of ['light','dark']) {
   await page.goto('https://fixture.invalid/Admin');
   await page.evaluate(theme=>{document.documentElement.dataset.theme=theme;document.documentElement.style.colorScheme=theme;},theme);
   await page.focus('#reportTeam');
   const colors=await page.locator('#reportTeam').evaluate(el=>({fg:getComputedStyle(el).color,bg:getComputedStyle(el).backgroundColor,weight:getComputedStyle(el).fontWeight}));
   assert.notEqual(colors.fg,colors.bg);assert(Number(colors.weight)<=600);
   await page.screenshot({path:path.resolve('Logs/WalgreensTeamChecks/team-'+theme+'.png')});
   console.log('PASS '+theme+' team filter styles '+JSON.stringify(colors));
 }
 } finally {await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
