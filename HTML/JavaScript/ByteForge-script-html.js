(function (global) {
    "use strict";
    var tags = ["P", "DIV", "H2", "H3", "H4", "BLOCKQUOTE", "UL", "OL", "LI", "BR", "STRONG", "B", "EM", "I", "U", "S", "SUB", "SUP", "SPAN", "A"];
    var sizes = ["12px", "14px", "16px", "18px", "24px", "32px"];
    var fonts = ["Arial", "Georgia", "Tahoma", "Verdana", "Times New Roman", "Courier New"];

    /** Keeps the script formatting contract while removing executable markup and arbitrary CSS. */
    function sanitize(html) {
        var template = document.createElement("template");
        template.innerHTML = html || "";
        var content = template.content;
        content.querySelectorAll("script,style,iframe,object,embed,svg,math,link,meta,base,template").forEach(function (node) { node.remove(); });
        Array.from(content.querySelectorAll("*")).forEach(function (node) {
            var style = node.style;
            var weight = style.fontWeight;
            var emphasis = [weight === "bold" || weight === "bolder" || parseInt(weight, 10) >= 600, /^(italic|oblique)/.test(style.fontStyle), /underline/.test(style.textDecoration), /line-through/.test(style.textDecoration)];
            var safe = {};
            ["color", "background-color"].forEach(function (property) {
                var value = style.getPropertyValue(property);
                if (/^#[a-f0-9]{3}(?:[a-f0-9]{3})?$/i.test(value) || /^rgb\(\s*\d{1,3}\s*,\s*\d{1,3}\s*,\s*\d{1,3}\s*\)$/.test(value)) safe[property] = value;
            });
            if (sizes.indexOf(style.fontSize) >= 0) safe["font-size"] = style.fontSize;
            var font = style.fontFamily.replace(/["']/g, "");
            if (fonts.indexOf(font) >= 0) safe["font-family"] = font;
            if (/^(left|center|right|justify)$/.test(style.textAlign)) safe["text-align"] = style.textAlign;
            var indent = /(?:^|\s)ql-indent-([1-8])(?:\s|$)/.exec(node.className);
            if (indent) safe["margin-left"] = String(Number(indent[1]) * 3) + "em";
            else if (/^(3|6|9|12|15|18|21|24)em$/.test(style.marginLeft)) safe["margin-left"] = style.marginLeft;
            var href = node.getAttribute("href") || "";
            Array.from(node.attributes).forEach(function (attribute) { node.removeAttribute(attribute.name); });
            if (tags.indexOf(node.tagName) < 0) {
                node.replaceWith.apply(node, Array.from(node.childNodes));
                return;
            }
            Object.keys(safe).forEach(function (property) { node.style.setProperty(property, safe[property]); });
            if (node.tagName === "A" && /^(https?:\/\/|mailto:)[^\s<>]+$/i.test(href)) {
                node.setAttribute("href", href);
                node.setAttribute("target", "_blank");
                node.setAttribute("rel", "noopener noreferrer");
            }
            ["strong", "em", "u", "s"].forEach(function (tag, index) {
                if (!emphasis[index] || node.tagName === tag.toUpperCase() || (index === 0 && node.tagName === "B") || (index === 1 && node.tagName === "I")) return;
                var wrapper = document.createElement(tag);
                while (node.firstChild) wrapper.appendChild(node.firstChild);
                node.appendChild(wrapper);
            });
        });
        var walker = document.createTreeWalker(content, NodeFilter.SHOW_COMMENT), comments = [], comment;
        while ((comment = walker.nextNode())) comments.push(comment);
        comments.forEach(function (node) { node.remove(); });
        return template.innerHTML.trim();
    }
    global.ByteForgeScriptHtml = { sanitize: sanitize, fonts: fonts, sizes: sizes };
}(window));
