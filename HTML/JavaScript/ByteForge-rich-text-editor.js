(function (global) {
    "use strict";

    var allowedBlocks = ["P", "DIV", "H2", "H3", "BLOCKQUOTE", "UL", "OL", "LI"];

    function RichTextEditor(root, options) {
        if (!root) throw new Error("A rich-text editor root element is required.");
        this.root = root;
        this.options = options || {};
        this.editor = root.querySelector("[data-rich-text-surface]");
        this.input = root.querySelector("[data-rich-text-input]");
        this.toolbar = root.querySelector("[data-rich-text-toolbar]");
        if (!this.editor || !this.input || !this.toolbar) {
            throw new Error("The rich-text editor requires toolbar, surface, and input elements.");
        }
        this.bind();
        this.setHtml(this.input.value || "");
    }

    RichTextEditor.prototype.bind = function () {
        var self = this;
        this.toolbar.addEventListener("mousedown", function (event) {
            if (event.target.closest("button")) event.preventDefault();
        });
        this.toolbar.addEventListener("click", function (event) {
            var button = event.target.closest("button[data-command]");
            if (!button) return;
            self.editor.focus();
            document.execCommand("styleWithCSS", false, false);
            document.execCommand(button.getAttribute("data-command"), false, button.getAttribute("data-value") || null);
            self.sync();
        });
        this.editor.addEventListener("input", function () { self.sync(); });
        this.editor.addEventListener("blur", function () { self.sync(); });
        this.editor.addEventListener("paste", function (event) {
            event.preventDefault();
            var clipboard = event.clipboardData || global.clipboardData;
            var html = clipboard ? clipboard.getData("text/html") : "";
            // Whole-section paste must not inherit the destination's first heading
            // or inline emphasis. Keep insertion on the native undo/redo stack.
            var replaceAll = false;
            var selection = global.getSelection();
            if (selection && selection.rangeCount && !selection.isCollapsed) {
                var selected = selection.getRangeAt(0);
                if (self.editor.contains(selected.startContainer) && self.editor.contains(selected.endContainer)) {
                    var before = document.createRange();
                    before.selectNodeContents(self.editor);
                    before.setEnd(selected.startContainer, selected.startOffset);
                    var after = document.createRange();
                    after.selectNodeContents(self.editor);
                    after.setStart(selected.endContainer, selected.endOffset);
                    if (!before.toString() && !after.toString()) {
                        replaceAll = true;
                        var entireEditor = document.createRange();
                        entireEditor.selectNodeContents(self.editor);
                        selection.removeAllRanges();
                        selection.addRange(entireEditor);
                    }
                }
            }
            if (replaceAll) self.editor.classList.add("byte-forge-rich-text-replacing-all");
            try {
                if (html) {
                    // Sanitize before insertion; clipboard HTML may contain executable markup.
                    document.execCommand("insertHTML", false, sanitizeHtml(html));
                } else {
                    document.execCommand("insertText", false, clipboard ? clipboard.getData("text/plain") : "");
                }
            } finally {
                self.editor.classList.remove("byte-forge-rich-text-replacing-all");
            }
            self.sync();
        });
        this.editor.addEventListener("keydown", function (event) {
            if (event.key === "Tab") {
                event.preventDefault();
                document.execCommand("insertText", false, "    ");
            }
        });
    };

    RichTextEditor.prototype.sync = function () {
        this.input.value = this.normalizedHtml();
        this.input.dispatchEvent(new Event("input", { bubbles: true }));
    };

    RichTextEditor.prototype.normalizedHtml = function () {
        return sanitizeHtml(this.editor.innerHTML);
    };

    // An inert template keeps clipboard resources and scripts out of the live document.
    // Browser copies often express emphasis as span styles instead of semantic tags.
    function sanitizeHtml(html) {
        var template = document.createElement("template");
        template.innerHTML = html || "";
        var clone = template.content;
        clone.querySelectorAll("script,style,iframe,object,embed,svg,math,link,meta,base,template").forEach(function (node) { node.remove(); });
        Array.prototype.slice.call(clone.querySelectorAll("*"), 0).forEach(function (node) {
            var weight = node.style.fontWeight;
            var bold = weight === "bold" || weight === "bolder" || parseInt(weight, 10) >= 600;
            var italic = /^(italic|oblique)/.test(node.style.fontStyle);
            var underline = /underline/.test(node.style.textDecoration + " " + node.style.textDecorationLine);
            Array.prototype.slice.call(node.attributes || [], 0).forEach(function (attribute) {
                node.removeAttribute(attribute.name);
            });
            if (bold && node.tagName !== "B" && node.tagName !== "STRONG") wrapContents(node, "strong");
            if (italic && node.tagName !== "I" && node.tagName !== "EM") wrapContents(node, "em");
            if (underline && node.tagName !== "U") wrapContents(node, "u");
            if (node.tagName === "B") replaceTag(node, "strong");
            else if (node.tagName === "I") replaceTag(node, "em");
            else if (allowedBlocks.indexOf(node.tagName) < 0 && ["BR", "STRONG", "EM", "U"].indexOf(node.tagName) < 0) {
                unwrap(node);
            }
        });
        var comments = document.createTreeWalker(clone, global.NodeFilter.SHOW_COMMENT);
        var commentNodes = [], comment;
        while ((comment = comments.nextNode())) commentNodes.push(comment);
        commentNodes.forEach(function (node) { node.remove(); });
        return template.innerHTML.trim();
    }

    function wrapContents(node, tagName) {
        var wrapper = document.createElement(tagName);
        while (node.firstChild) wrapper.appendChild(node.firstChild);
        node.appendChild(wrapper);
    }

    RichTextEditor.prototype.setHtml = function (html) {
        this.editor.innerHTML = sanitizeHtml(html);
        this.input.value = this.normalizedHtml();
    };

    RichTextEditor.prototype.getHtml = function () {
        this.sync();
        return this.input.value;
    };

    RichTextEditor.prototype.insertText = function (text) {
        this.editor.focus();
        document.execCommand("insertText", false, text || "");
        this.sync();
    };

    function replaceTag(node, tagName) {
        var replacement = document.createElement(tagName);
        while (node.firstChild) replacement.appendChild(node.firstChild);
        node.parentNode.replaceChild(replacement, node);
    }

    function unwrap(node) {
        var parent = node.parentNode;
        if (!parent) return;
        while (node.firstChild) parent.insertBefore(node.firstChild, node);
        parent.removeChild(node);
    }

    RichTextEditor.sanitizeHtml = sanitizeHtml;
    global.ByteForgeRichTextEditor = RichTextEditor;
}(window));
