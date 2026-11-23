const { chromium } = require(process.env.PRAXIS_PLAYWRIGHT);
const path = require('node:path');
const assert = require('node:assert/strict');
(async () => {
 const browser = await chromium.launch({ channel: 'msedge', headless: true });
 try {
  for (const colorScheme of ['light', 'dark']) {
   const page = await browser.newPage({ colorScheme });
   await page.setContent('<style>:root { color-scheme: light dark; --accent: blue; --text: light-dark(black,white); } body { color: var(--text); }</style><div class="script-guide-step-body"><p>Default <strong class="script-cue" style="color:rgb(255,255,0);background-color:rgb(0,0,0);font-family:Georgia;font-size:24px">SAY + ASK</strong><span style="color:rgb(255,0,0);background-color:rgb(255,255,0)">Important</span></p></div>');
   await page.addStyleTag({ path: path.join(__dirname, 'workspace-script-guide.css') });
   const styles = await page.locator('strong').evaluate(n => { const s = getComputedStyle(n); return [s.color,s.backgroundColor,s.fontFamily,s.fontSize]; });
   assert.deepEqual(styles,['rgb(255, 255, 0)','rgb(0, 0, 0)','Georgia','24px']);
   assert.equal(await page.locator('span').evaluate(n => getComputedStyle(n).backgroundColor), 'rgb(255, 255, 0)');
   assert.equal(await page.locator('p').evaluate(n => getComputedStyle(n).color), colorScheme === 'dark' ? 'rgb(255, 255, 255)' : 'rgb(0, 0, 0)');
   await page.close();
  }
  console.log('Script guide formatting passed in light and dark themes.');
 } finally { await browser.close(); }
})().catch(e => { console.error(e); process.exitCode = 1; });