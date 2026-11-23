(function (global) {
    "use strict";
    var registered = false;

    /** Registers portable inline styles and sanitizes clipboard conversion once. */
    function registerFormats() {
        if (registered) return;
        var Clipboard = global.Quill.import("modules/clipboard");
        class ByteForgeClipboard extends Clipboard {
            convert(data, formats) {
                return super.convert({ html: global.ByteForgeScriptHtml.sanitize(data.html), text: data.text }, formats);
            }
        }
        global.Quill.register("modules/clipboard", ByteForgeClipboard, true);
        ["align", "font", "size"].forEach(function (name) {
            var format = global.Quill.import("attributors/style/" + name);
            if (name === "font") format.whitelist = global.ByteForgeScriptHtml.fonts;
            if (name === "size") format.whitelist = global.ByteForgeScriptHtml.sizes;
            global.Quill.register(format, true);
        });
        registered = true;
    }

    /** Adapts Quill's full toolbar to the script-section HTML contract. */
    function ByteForgeQuillEditor(root) {
        if (!global.Quill) throw new Error("Quill 2 is required by the script editor.");
        registerFormats();
        this.input = root.querySelector("[data-rich-text-input]");
        var toolbar = root.querySelector("[data-rich-text-toolbar]");
        toolbar.className = "bf-script-format-toolbar";
        toolbar.innerHTML = toolbarHtml();
        this.quill = new global.Quill(root.querySelector("[data-rich-text-surface]"), {
            theme: "snow",
            bounds: root,
            formats: ["bold", "italic", "underline", "strike", "script", "font", "size", "color", "background", "align", "header", "list", "blockquote", "indent", "link"],
            modules: {
                toolbar: { container: toolbar, handlers: {
                    undo: function () { this.quill.history.undo(); },
                    redo: function () { this.quill.history.redo(); }
                } },
                history: { userOnly: true },
                clipboard: { matchers: [[Node.ELEMENT_NODE, function (node, delta) {
                    var margin = node.style.marginLeft;
                    if (/^(3|6|9|12|15|18|21|24)em$/.test(margin)) {
                        var Delta = global.Quill.import("delta");
                        return delta.compose(new Delta().retain(delta.length(), { indent: parseInt(margin, 10) / 3 }));
                    }
                    return delta;
                }]] }
            }
        });
        this.toolbar = toolbar;
        this.editor = this.quill.root;
        this.editor.setAttribute("role", "textbox");
        this.editor.setAttribute("aria-multiline", "true");
        this.editor.setAttribute("aria-label", "Section content");
        this.editor.setAttribute("data-rich-text-surface", "");
        this.quill.container.removeAttribute("data-rich-text-surface");
        this.quill.container.removeAttribute("contenteditable");
        var self = this;
        this.quill.on("text-change", function () { self.sync(); });
        this.quill.on("selection-change", function (range) { if (range) self.selection = range; });
        toolbar.addEventListener("mousedown", function (event) {
            if (event.target.closest("button")) event.preventDefault();
        });
        toolbar.querySelectorAll("button").forEach(function (button) {
            var label = button.getAttribute("data-label");
            button.setAttribute("aria-label", label);
            button.title = label;
        });
        toolbar.querySelectorAll(".ql-picker").forEach(function (picker) {
            var label = picker.classList.contains("ql-font") ? "Font" : picker.classList.contains("ql-size") ? "Font size" : picker.classList.contains("ql-header") ? "Paragraph style" : picker.classList.contains("ql-align") ? "Text alignment" : picker.classList.contains("ql-color") ? "Text color" : "Highlight color";
            picker.querySelector(".ql-picker-label").setAttribute("aria-label", label);
        });
        var tooltip = root.querySelector(".ql-tooltip");
        tooltip.querySelector("input").setAttribute("aria-label", "Link URL");
        tooltip.querySelector(".ql-action").setAttribute("aria-label", "Save or edit link");
        tooltip.querySelector(".ql-remove").setAttribute("aria-label", "Remove link");
        this.setHtml(this.input.value || "");
    }

    /** Builds grouped standard Quill controls with accessible names. */
    function toolbarHtml() {
        function button(format, label, value, text) {
            return '<button type="button" class="ql-' + format + '" data-label="' + label + '"' + (value ? ' value="' + value + '"' : '') + '>' + (text || '') + '</button>';
        }
        return '<span class="ql-formats"><select class="ql-font" aria-label="Font"><option selected>Default font</option>' + global.ByteForgeScriptHtml.fonts.map(function (font) { return '<option value="' + font + '">' + font + '</option>'; }).join('') + '</select>' +
            '<select class="ql-size" aria-label="Font size"><option selected>Default size</option>' + global.ByteForgeScriptHtml.sizes.map(function (size) { return '<option value="' + size + '">' + size.replace('px', '') + '</option>'; }).join('') + '</select>' +
            '<select class="ql-header" aria-label="Paragraph style"><option selected>Normal</option><option value="3">Heading</option><option value="4">Subheading</option></select></span>' +
            '<span class="ql-formats">' + button('bold', 'Bold') + button('italic', 'Italic') + button('underline', 'Underline') + button('strike', 'Strikethrough') + button('script', 'Subscript', 'sub') + button('script', 'Superscript', 'super') + '</span>' +
            '<span class="ql-formats"><select class="ql-color" aria-label="Text color"></select><select class="ql-background" aria-label="Highlight color"></select></span>' +
            '<span class="ql-formats"><select class="ql-align" aria-label="Text alignment"><option selected></option><option value="center"></option><option value="right"></option><option value="justify"></option></select>' + button('list', 'Numbered list', 'ordered') + button('list', 'Bulleted list', 'bullet') + button('indent', 'Decrease indent', '-1') + button('indent', 'Increase indent', '+1') + '</span>' +
            '<span class="ql-formats">' + button('blockquote', 'Quote') + button('link', 'Insert link') + button('clean', 'Clear formatting') + '</span>' +
            '<span class="ql-formats">' + button('undo', 'Undo', '', '&#8617;') + button('redo', 'Redo', '', '&#8618;') + '</span>';
    }

    /** Exports portable HTML for saving and read-only viewers. */
    ByteForgeQuillEditor.prototype.getHtml = function () {
        return global.ByteForgeScriptHtml.sanitize(this.quill.getSemanticHTML().replace(/&nbsp;/g, " "));
    };
    /** Publishes edits to the existing hidden form field. */
    ByteForgeQuillEditor.prototype.sync = function () {
        this.input.value = this.getHtml();
        this.input.dispatchEvent(new Event("input", { bubbles: true }));
    };
    /** Loads a section and isolates its undo history from other sections. */
    ByteForgeQuillEditor.prototype.setHtml = function (html) {
        this.quill.setContents(this.quill.clipboard.convert({ html: global.ByteForgeScriptHtml.sanitize(html) }), "silent");
        this.quill.history.clear();
        this.selection = { index: 0, length: 0 };
        this.input.value = this.getHtml();
    };
    /** Inserts a dynamic field at the last selection, replacing selected text. */
    ByteForgeQuillEditor.prototype.insertText = function (text) {
        var range = this.quill.getSelection() || this.selection || { index: 0, length: 0 };
        var Delta = global.Quill.import("delta");
        this.quill.history.cutoff();
        this.quill.updateContents(new Delta().retain(range.index).delete(range.length).insert(text || "", this.quill.getFormat(range.index, range.length)), "user");
        this.quill.setSelection(range.index + (text || "").length, 0, "user");
        this.quill.history.cutoff();
    };
    global.ByteForgeQuillEditor = ByteForgeQuillEditor;
}(window));
