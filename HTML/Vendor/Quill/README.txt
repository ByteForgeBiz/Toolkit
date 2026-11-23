Quill 2.0.3 is vendored from https://cdn.jsdelivr.net/npm/quill@2.0.3/.
Files: dist/quill.js, dist/quill.core.css, dist/quill.snow.css, LICENSE (BSD-3-Clause).
The Praxis editor uses Snow's grouped toolbar, with ByteForge light/dark overrides.
Font, size, headings, emphasis, sub/superscript, colors, alignment, lists, indentation,
links, clear formatting and undo/redo are supported. Images, video, tables and HTML
source editing are not exposed. No CDN requests occur at runtime.
ByteForge-script-html.js and PraxisScriptService.SanitizeHtml retain only the
supported tags, safe links and whitelisted inline style property/value pairs.
Font/size/alignment export inline for agent/public pages without Quill theme CSS.
Quill indentation is exported as margin-left and restored by a clipboard matcher.
Section loads reset history; dynamic fields use the last editor selection.
When changing the formatting contract, update both sanitizers and regression tests.
Regression commands (Playwright on NODE_PATH):
  node ByteForge.Toolkit/HTML/JavaScript/ByteForge-quill-editor.tests.cjs
  node ByteForge.Toolkit/HTML/Praxis/scripts-crud.tests.cjs
  node ByteForge.Toolkit/HTML/Praxis/script-view.tests.cjs
Server tests: PraxisAPI.Tests.PraxisScriptServiceTests.
