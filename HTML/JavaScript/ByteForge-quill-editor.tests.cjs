const { chromium } = require('playwright');
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
(async () => {
    const browser = await chromium.launch({ channel: 'msedge', headless: true });
    try {
        for (const colorScheme of ['light', 'dark']) {
            const page = await browser.newPage({ colorScheme });
            await page.setContent('<div id="editor" class="byte-forge-rich-text"><div class="byte-forge-rich-text-toolbar" data-rich-text-toolbar><button type="button" data-command="bold">Bold</button><button type="button" data-command="undo">Undo</button><button type="button" data-command="redo">Redo</button></div><div class="byte-forge-rich-text-surface" data-rich-text-surface></div><textarea data-rich-text-input hidden></textarea></div><button id="token">Insert field</button>');
            for (const file of ['../Vendor/Quill/quill.snow.css', '../CSS/ByteForge-rich-text-editor.css']) await page.addStyleTag({ path: path.join(__dirname, file) });
            for (const file of ['../Vendor/Quill/quill.js', 'ByteForge-script-html.js', 'ByteForge-quill-editor.js']) await page.addScriptTag({ path: path.join(__dirname, file) });
            await page.evaluate(() => {
                window.editor = new ByteForgeQuillEditor(document.getElementById('editor'));
                editor.setHtml('<h3>Heading</h3><p>Normal <strong>bold</strong> <em>italic</em> <u>underline</u> {AGENT}</p><ul><li>Bullet</li></ul><ol><li>Number</li></ol><blockquote>Quote</blockquote>');
            });
            const html = await page.evaluate(() => editor.getHtml());
            for (const tag of ['h3', 'strong', 'em', 'u', 'ul', 'ol', 'li', 'blockquote']) assert.ok(html.includes('<' + tag + '>'), tag);
            assert.match(html, /\{AGENT\}/);
            assert.equal(await page.evaluate(() => { editor.setHtml(editor.getHtml()); return editor.getHtml(); }), html);
            await page.evaluate(() => {
                editor.setHtml('<p>Old content</p>');
                editor.quill.setSelection(0, editor.quill.getLength() - 1);
            });
            await page.evaluate(() => {
                const clipboard = new DataTransfer();
                clipboard.setData('text/html', '<h3>New heading</h3><p>Normal <strong>bold</strong></p><img src="x" onerror="window.bad=true"><script>window.bad=true</script>');
                editor.editor.dispatchEvent(new ClipboardEvent('paste', { clipboardData: clipboard, bubbles: true, cancelable: true }));
            });
            const pasted = await page.evaluate(() => editor.getHtml());
            assert.match(pasted, /New heading/);
            assert.ok(!/Old content|img|script|onerror/.test(pasted));
            await page.getByRole('button', { name: 'Undo', exact: true }).click();
            assert.match(await page.evaluate(() => editor.getHtml()), /Old content/);
            await page.getByRole('button', { name: 'Redo', exact: true }).click();
            assert.equal(await page.evaluate(() => editor.getHtml()), pasted);
            await page.evaluate(() => {
                editor.setHtml('<p>Hello name!</p>');
                editor.quill.setSelection(6, 4);
                document.getElementById('token').onclick = () => editor.insertText('{AGENT}');
            });
            await page.locator('#token').click();
            assert.equal(await page.locator('.ql-editor').innerText(), 'Hello {AGENT}!');
            await page.evaluate(() => { editor.setHtml('<p>Other section</p>'); editor.quill.history.undo(); });
            assert.equal(await page.locator('.ql-editor').innerText(), 'Other section');
            await page.evaluate(() => {
                editor.setHtml('<p>Styled {AGENT}</p>');
                editor.quill.setSelection(0, 6);
            });
            await page.getByRole('button', { name: 'Bold', exact: true }).click();
            assert.match(await page.evaluate(() => editor.getHtml()), /<strong>Styled<\/strong>/);
            await page.locator('.ql-font .ql-picker-label').click();
            await page.locator('.ql-font .ql-picker-item[data-value="Georgia"]').click();
            await page.locator('.ql-size .ql-picker-label').click();
            await page.locator('.ql-size .ql-picker-item[data-value="24px"]').click();
            await page.locator('.ql-color .ql-picker-label').click();
            await page.locator('.ql-color .ql-picker-item[data-value="#e60000"]').click();
            assert.match(await page.evaluate(() => editor.getHtml()), /color: rgb\(230, 0, 0\)/);
            await page.getByRole('button', { name: 'Insert link', exact: true }).click();
            await page.getByRole('textbox', { name: 'Link URL', exact: true }).fill('https://example.com/');
            await page.getByRole('textbox', { name: 'Link URL', exact: true }).press('Enter');
            assert.match(await page.evaluate(() => editor.getHtml()), /href="https:\/\/example.com\/"/);
            await page.evaluate(() => {
                editor.quill.formatText(0, 6, { color: '#ff0000', background: '#ffff00', strike: true, link: 'https://example.com/' }, 'user');
                editor.quill.formatLine(0, 1, { align: 'center', indent: 1 }, 'user');
            });
            const rich = await page.evaluate(() => editor.getHtml());
            assert.match(rich, /font-family: Georgia/);
            assert.match(rich, /font-size: 24px/);
            assert.match(rich, /text-align: center/);
            assert.match(rich, /margin-left: 3em/);
            assert.match(rich, /background-color: rgb\(255, 255, 0\)/);
            assert.match(rich, /href="https:\/\/example.com\/"/);
            assert.match(rich, /<s>/);
            assert.equal(await page.evaluate(() => { editor.setHtml(editor.getHtml()); return editor.getHtml(); }), rich);
            const unsafe = await page.evaluate(() => ByteForgeScriptHtml.sanitize('<p style="position:fixed;background-image:url(https://bad.test);color:#ff0000" onclick="bad()"><a href="javascript:bad()">Safe</a><img src=x onerror=bad()></p>'));
            assert.ok(!/position|background-image|onclick|javascript|onerror|<img/.test(unsafe));
            assert.match(unsafe, /color: rgb\(255, 0, 0\)/);
            const style = await page.locator('.ql-editor').evaluate(el => ({ text: getComputedStyle(el).color, bg: getComputedStyle(el.parentElement).backgroundColor, weight: getComputedStyle(el).fontWeight }));
            assert.notEqual(style.text, style.bg);
            assert.equal(style.weight, '400');
            await page.screenshot({ path: path.join(__dirname, '../../../Logs/quill-' + colorScheme + '.png'), fullPage: true });
            await page.close();
            console.log('PASS ' + colorScheme + ': toolbar font/size/color/link controls, rich HTML round trip, paste, undo/redo, fields, section history, unsafe markup, theme');
        }
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
