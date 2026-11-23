// Run with Playwright available on NODE_PATH. No system clipboard or live app is used.
const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');
const assert = require('assert/strict');
(async () => {
    const browser = await chromium.launch({ channel: process.env.TEST_BROWSER_CHANNEL || 'msedge', headless: true });
    try {
        const page = await browser.newPage();
        await page.emulateMedia({colorScheme: process.env.TEST_COLOR_SCHEME || 'light'});
        await page.setContent('<div id="editor"><div data-rich-text-toolbar></div><div data-rich-text-surface contenteditable="true"></div><input data-rich-text-input></div>');
        await page.addScriptTag({content: fs.readFileSync(path.join(__dirname, 'ByteForge-rich-text-editor.js'), 'utf8')});
        const result = await page.evaluate(() => {
            const editor = new ByteForgeRichTextEditor(document.getElementById('editor'));
            function paste(html, text) {
                editor.setHtml('');
                editor.editor.focus();
                const clipboard = new DataTransfer();
                if (html) clipboard.setData('text/html', html);
                clipboard.setData('text/plain', text || '');
                editor.editor.dispatchEvent(new ClipboardEvent('paste', {clipboardData: clipboard, bubbles: true, cancelable: true}));
                return editor.getHtml();
            }
            const formatted = paste('<h3>Heading</h3><p><strong>Bold</strong> <em>Italic</em> <u>Underlined</u></p><ul><li>Bullet</li></ul><ol><li>Number</li></ol><blockquote>Quote</blockquote>');
            editor.setHtml(formatted);
            const reopened = editor.getHtml();
            const styled = paste('<p><span style="font-weight:700;font-style:italic;text-decoration:underline;color:red" onclick="window.bad=true">Styled</span></p>');
            const unsafe = paste('<script>window.bad=true</script><style>body{display:none}</style><img src="x" onerror="window.bad=true"><iframe srcdoc="bad"></iframe><p data-praxis-section="true">Safe</p>');
            const plain = paste('', '<b>literal text</b>');
            return {formatted,reopened,styled,unsafe,plain,bad:!!window.bad};
        });
        for (const tag of ['h3','strong','em','u','ul','ol','li','blockquote']) assert.ok(result.formatted.includes('<' + tag + '>'), tag);
        assert.equal(result.formatted,result.reopened);
        for (const tag of ['strong','em','u']) assert.ok(result.styled.includes('<' + tag + '>'), tag);
        assert.ok(!/style=|onclick=/.test(result.styled));
        assert.ok(!/script|iframe|img|onerror|data-praxis-section|display:none/.test(result.unsafe));
        assert.ok(result.unsafe.includes('Safe'));
        assert.ok(result.plain.includes('&lt;b&gt;literal text&lt;/b&gt;'));
        assert.equal(result.bad,false);
        await page.setContent('<div id="editor"><div data-rich-text-toolbar></div><div class="byte-forge-rich-text-surface" data-rich-text-surface contenteditable="true"></div><input data-rich-text-input></div>');
        await page.addStyleTag({content: fs.readFileSync(path.join(__dirname, '../CSS/ByteForge-rich-text-editor.css'), 'utf8')});
        const replacement = await page.evaluate(() => {
            const editor = new ByteForgeRichTextEditor(document.getElementById('editor'));
            editor.setHtml('<h3>Old heading</h3><p>Old paragraph</p><h2>Old ending</h2>');
            editor.editor.focus();
            document.execCommand('selectAll');
            const clipboard = new DataTransfer();
            clipboard.setData('text/html', '<h3>New heading</h3><p>Normal <strong>bold</strong> <em>italic</em></p>');
            editor.editor.dispatchEvent(new ClipboardEvent('paste', {clipboardData: clipboard, bubbles:true, cancelable:true}));
            const html = editor.getHtml();
            const weight = getComputedStyle(editor.editor.querySelector('p')).fontWeight;
            document.execCommand('undo');
            const undo = editor.getHtml();
            document.execCommand('redo');
            const redo = editor.getHtml();
            editor.setHtml('<p>Keep <strong>old</strong> ending</p>');
            const range = document.createRange();
            range.selectNodeContents(editor.editor.querySelector('strong'));
            const selection = getSelection(); selection.removeAllRanges(); selection.addRange(range);
            clipboard.setData('text/html', '<em>new</em>');
            editor.editor.dispatchEvent(new ClipboardEvent('paste', {clipboardData: clipboard, bubbles:true, cancelable:true}));
            return {html,weight,undo,redo,partial:editor.editor.textContent,clean:!editor.editor.classList.contains('byte-forge-rich-text-replacing-all')};
        });
        assert.equal(replacement.html, '<h3>New heading</h3><p>Normal <strong>bold</strong> <em>italic</em></p>');
        assert.equal(replacement.weight, '400');
        assert.equal(replacement.undo, '<h3>Old heading</h3><p>Old paragraph</p><h2>Old ending</h2>');
        assert.equal(replacement.redo, replacement.html);
        assert.equal(replacement.partial.replace(/\u00a0/g, ' '), 'Keep new ending');
        assert.equal(replacement.clean, true);
        console.log('PASS: select-all replacement discards destination headings, preserves emphasis, and supports Undo.');
        console.log('PASS: formatted paste and reload, clipboard span emphasis, unsafe HTML removal, and plain-text fallback.');
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
