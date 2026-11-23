(function () {
    "use strict";

    var sessionTokenKey = "praxis.reportPortal.accessToken";
    var persistentTokenKey = "praxis.reportPortal.persistentAccessToken";
    var state = { token: "", scripts: [], tokens: [], current: null, editor: null, sections: [], currentSectionIndex: -1 };
    var editorPanel = document.getElementById("script-editor");
    var scriptForm = document.getElementById("script-form");

    if (window.ByteForgeAccount) window.ByteForgeAccount.attach(document.getElementById("session-status"), { getToken: function () { return state.token; }, onPasswordChanged: signOut });
    scriptForm.addEventListener("submit", save);
    document.getElementById("new-script").addEventListener("click", function () { leaveScript(newScript); });
    document.getElementById("delete-script").addEventListener("click", deleteScript);
    document.getElementById("sign-out-button").addEventListener("click", signOut);
    state.editor = new ByteForgeQuillEditor(document.getElementById("rich-text-editor"));
    document.getElementById("script-section-name").addEventListener("input", updateSectionName);
    document.getElementById("preview-script").addEventListener("click", previewScript);
    restore();

    /** Shows the complete current draft without saving or resolving placeholders. */
    function previewScript() {
        if (!state.current || state.busy) return;
        // Reuse the rich-text sanitizer for every section, including unselected ones.
        var content = document.createElement("article");
        content.className = "bf-praxis-full-script";
        content.setAttribute("aria-label", "Full script sections");
        content.innerHTML = ByteForgeScriptHtml.sanitize(serializeSections()) || "<p>No script content.</p>";
        ByteForgeModal.show("", {
            type: "none", title: "Full Script: " + document.getElementById("script-name").value,
            contentElement: content, maxWidth: "960px", modalClass: "bf-praxis-full-script-modal",
            buttons: [{ text: "Close" }]
        });
    }
    function restore() {
        state.token = readToken();
        if (!state.token) {
            ByteForgePraxisAuth.redirect();
            return;
        }
        document.getElementById("session-status").textContent = "Restoring secure session";
        load().catch(function (failure) {
            clearToken();
            ByteForgePraxisAuth.redirect();
        });
    }

    function load() {
        return ByteForgePraxisAdmin.load(state.token).then(function (pages) {
            if (!pages.some(function (page) { return page.key === "PraxisScriptEditor"; })) {
                window.location.replace(pages[0].route);
                return null;
            }
            return api("api/scripts");
        }).then(function (model) {
            if (!model) return;
            state.scripts = model.scripts || [];
            state.tokens = model.tokens || [];
            state.eventTypes = model.eventTypes || [];
            state.serviceNames = model.serviceNames || [];
            editorPanel.hidden = false;
            document.getElementById("sign-out-button").hidden = false;
            var status = document.getElementById("session-status");
            status.textContent = "Secure session";
            status.classList.add("is-ready");
            renderTokens();
            select(state.scripts.length ? state.scripts[0].scriptKey : "");
        });
    }

    function renderList() {
        var list = document.getElementById("script-list");
        list.innerHTML = "";
        state.scripts.forEach(function (script) {
            var button = document.createElement("button");
            button.type = "button";
            button.setAttribute("aria-current", state.current && state.current.scriptKey === script.scriptKey ? "true" : "false");
            var title = document.createElement("strong");
            title.textContent = script.displayName;
            var route = document.createElement("span");
            route.textContent = routeLabel(script);
            button.appendChild(title);
            button.appendChild(route);
            button.addEventListener("click", function () { leaveScript(function () { select(script.scriptKey); }); });
            list.appendChild(button);
        });
    }

    function select(key) {
        state.current = state.scripts.filter(function (script) { return script.scriptKey === key; })[0] || null;
        showCurrent();
    }

    function showCurrent() {
        renderList();
        var publicLink = document.getElementById("public-script-link");
        publicLink.hidden = !state.current || !state.current.scriptKey || !state.current.isActive;
        if (!publicLink.hidden) publicLink.href = "script-view.html?scriptKey=" + encodeURIComponent(state.current.scriptKey);
        scriptForm.hidden = !state.current;
        document.getElementById("script-empty").hidden = !!state.current;
        if (!state.current) return;
        var serviceSelect = document.getElementById("script-service");
        serviceSelect.textContent = "";
        var services = (state.serviceNames || []).slice();
        if (state.current.serviceName && services.indexOf(state.current.serviceName) < 0) services.push(state.current.serviceName);
        services.forEach(function (service) {
            var option = document.createElement("option");
            option.value = option.textContent = service;
            serviceSelect.appendChild(option);
        });
        serviceSelect.value = state.current.serviceName || services[0] || "";
        serviceSelect.disabled = false;
        document.getElementById("delete-script").innerHTML = state.current.scriptKey ? "&#10060; Delete script" : "&#10006;&#65039; Discard draft";
        document.getElementById("delete-script").hidden = !!state.current.scriptKey && !state.current.canDelete;
        document.getElementById("script-baseline-note").hidden = !state.current.scriptKey || !!state.current.canDelete;
        document.getElementById("script-heading").textContent = state.current.displayName;
        document.getElementById("script-route").textContent = routeLabel(state.current);
        document.getElementById("script-name").value = state.current.displayName || "";
        var eventSelect = document.getElementById("script-event-type");
        var eventOptions = document.getElementById("script-event-types");
        eventOptions.textContent = "";
        var currentEvent = (state.current.eventType || "").trim();
        var choices = (state.eventTypes || []).slice();
        state.scripts.forEach(function (script) {
            var value = (script.eventType || "").trim();
            if (value && !choices.some(function (item) { return item.toLowerCase() === value.toLowerCase(); })) choices.push(value);
        });
        if (currentEvent && !choices.some(function (value) { return value.toLowerCase() === currentEvent.toLowerCase(); })) choices.push(currentEvent);
        choices.forEach(function (value) {
            var option = document.createElement("option");
            option.value = value;
            eventOptions.appendChild(option);
        });
        eventSelect.value = currentEvent;
        document.getElementById("script-active").checked = !!state.current.isActive;
        document.getElementById("script-updated").textContent = state.current.updatedAtUtc
            ? "Last saved " + new Date(state.current.updatedAtUtc).toLocaleString() : "";
        state.sections = splitSections(state.current.bodyHtml || "");
        state.currentSectionIndex = -1;
        selectSection(0);
        state.original = JSON.stringify(currentValues());
        hideMessage();
    }

    function currentValues() {
        return {
            scriptKey: state.current.scriptKey,
            serviceName: document.getElementById("script-service").value,
            displayName: document.getElementById("script-name").value.trim(),
            eventType: document.getElementById("script-event-type").value.trim(),
            bodyHtml: serializeSections(),
            isActive: document.getElementById("script-active").checked
        };
    }

    function leaveScript(action) {
        if (state.busy) return;
        if (state.current && JSON.stringify(currentValues()) !== state.original) {
            ByteForgeModal.confirm("Discard unsaved script changes?", action, null, "Unsaved changes", "Discard", "Cancel");
        } else action();
    }

    function newScript() {
        state.current = { scriptKey: "", displayName: "New script", serviceName: state.serviceNames[0], eventType: "", isActive: false,
            bodyHtml: '<h2 data-praxis-section="true">Opening</h2><p></p>' };
        showCurrent();
        document.getElementById("script-name").focus();
        document.getElementById("script-name").select();
    }

    function deleteScript() {
        if (!state.current || state.busy) return;
        if (!state.current.scriptKey) { leaveScript(function () { select(state.scripts.length ? state.scripts[0].scriptKey : ""); }); return; }
        if (!state.current.canDelete) return;
        var script = state.current;
        ByteForgeModal.confirm('Delete script "' + script.displayName + '" and all its sections? Unsaved changes will also be discarded.' + (script.isActive ? " This script is active. New agent sessions will use the service fallback, if one is active." : ""), function () {
            if (state.busy) return;
            setBusy(true);
            api("api/scripts/" + encodeURIComponent(script.scriptKey), { method: "DELETE" }).then(function () {
                state.scripts = state.scripts.filter(function (item) { return item.scriptKey !== script.scriptKey; });
                select(state.scripts.length ? state.scripts[0].scriptKey : "");
                showMessage("Script deleted.", false);
            }).catch(function (failure) { showMessage(failure.message, true); }).then(function () { setBusy(false); });
        }, null, "Delete script", "Delete", "Cancel");
    }

    function setBusy(busy) {
        state.busy = busy;
        ["save-script", "new-script", "delete-script"].forEach(function (id) { document.getElementById(id).disabled = busy; });
        scriptForm.inert = busy;
        document.getElementById("script-tokens").inert = busy;
    }

    function persistCurrentSection() {
        if (state.currentSectionIndex < 0 || !state.sections[state.currentSectionIndex]) return;
        state.sections[state.currentSectionIndex].title = document.getElementById("script-section-name").value.trim() || "Untitled section";
        state.sections[state.currentSectionIndex].bodyHtml = state.editor.getHtml();
    }

    function selectSection(index) {
        persistCurrentSection();
        document.querySelector(".script-section-content").hidden = !state.sections.length;
        document.getElementById("script-section-name").disabled = !state.sections.length;
        if (!state.sections.length) {
            state.currentSectionIndex = -1;
            state.editor.setHtml("");
            renderSectionList();
            return;
        }
        if (index < 0 || index >= state.sections.length) return;
        state.currentSectionIndex = index;
        document.getElementById("script-section-name").value = state.sections[index].title;
        state.editor.setHtml(state.sections[index].bodyHtml);
        renderSectionList();
    }

    function updateSectionName() {
        if (state.currentSectionIndex < 0 || !state.sections[state.currentSectionIndex]) return;
        state.sections[state.currentSectionIndex].title = this.value.trim() || "Untitled section";
        renderSectionList();
    }

    function renderSectionList() {
        var root = document.getElementById("script-section-list");
        root.innerHTML = "";
        state.sections.forEach(function (section, index) {
            var card = document.createElement("div");
            card.className = "bf-script-section-card";
            card.setAttribute("data-selected", index === state.currentSectionIndex ? "true" : "false");
            var handle = document.createElement("button");
            handle.type = "button";
            handle.className = "bf-script-section-handle";
            handle.innerHTML = "&#10495;";
            handle.draggable = true;
            handle.title = "Drag to reorder, or use Up and Down arrow keys";
            handle.setAttribute("aria-label", "Move section " + (index + 1) + ": " + section.title);
            handle.addEventListener("dragstart", function (event) {
                persistCurrentSection();
                state.draggedSection = section;
                event.dataTransfer.effectAllowed = "move";
                event.dataTransfer.setData("text/plain", String(index));
            });
            handle.addEventListener("dragend", function () {
                state.draggedSection = null;
                root.querySelectorAll(".is-drop-target").forEach(function (item) { item.classList.remove("is-drop-target"); });
            });
            handle.addEventListener("keydown", function (event) {
                if (event.key !== "ArrowUp" && event.key !== "ArrowDown") return;
                event.preventDefault();
                moveSection(section, index + (event.key === "ArrowUp" ? -1 : 1));
            });
            card.addEventListener("dragover", function (event) {
                if (!state.draggedSection) return;
                event.preventDefault();
                event.dataTransfer.dropEffect = "move";
                card.classList.add("is-drop-target");
            });
            card.addEventListener("dragleave", function () { card.classList.remove("is-drop-target"); });
            card.addEventListener("drop", function (event) {
                if (!state.draggedSection) return;
                event.preventDefault();
                moveSection(state.draggedSection, index);
                state.draggedSection = null;
            });
            var button = document.createElement("button");
            button.type = "button";
            button.className = "bf-script-section-select";
            button.setAttribute("aria-current", index === state.currentSectionIndex ? "true" : "false");
            var number = document.createElement("span");
            number.textContent = "Section " + (index + 1);
            var title = document.createElement("strong");
            title.textContent = section.title;
            button.appendChild(number);
            button.appendChild(title);
            button.addEventListener("click", function () { selectSection(index); });
            var remove = document.createElement("button");
            remove.type = "button";
            remove.className = "bf-script-section-delete";
            remove.innerHTML = "&#10006;&#65039;";
            remove.title = "Delete section " + (index + 1) + ": " + section.title;
            remove.setAttribute("aria-label", remove.title);
            remove.addEventListener("click", function () {
                ByteForgeModal.confirm('Delete section "' + section.title + '" and its content? Save script to apply this deletion.', function () {
                persistCurrentSection();
                var selected = state.sections[state.currentSectionIndex];
                state.sections.splice(index, 1);
                state.currentSectionIndex = -1;
                selectSection(selected === section ? Math.min(index, state.sections.length - 1) : state.sections.indexOf(selected));
                var focus = root.querySelector('[aria-current="true"]') || root.lastElementChild;
                if (focus) focus.focus();
                }, null, "Delete section", "Delete", "Cancel");
            });
            card.appendChild(handle);
            card.appendChild(button);
            card.appendChild(remove);
            root.appendChild(card);
        });
        var add = document.createElement("button");
        add.type = "button";
        add.className = "bf-script-section-add";
        add.innerHTML = "&#10133; New Section";
        add.addEventListener("click", function () {
            persistCurrentSection();
            state.sections.push({ title: "New Section", bodyHtml: "<p></p>" });
            state.currentSectionIndex = -1;
            selectSection(state.sections.length - 1);
            document.getElementById("script-section-name").focus();
            document.getElementById("script-section-name").select();
        });
        root.appendChild(add);
    }

    function moveSection(section, targetIndex) {
        var fromIndex = state.sections.indexOf(section);
        if (fromIndex < 0 || targetIndex < 0 || targetIndex >= state.sections.length || fromIndex === targetIndex) return;
        persistCurrentSection();
        var selected = state.sections[state.currentSectionIndex];
        state.sections.splice(fromIndex, 1);
        state.sections.splice(targetIndex, 0, section);
        state.currentSectionIndex = state.sections.indexOf(selected);
        renderSectionList();
        document.querySelectorAll(".bf-script-section-handle")[targetIndex].focus();
    }

    function splitSections(html) {
        var container = document.createElement("div");
        container.innerHTML = html || "";
        var root = container.children.length === 1 && container.firstElementChild.tagName === "DIV"
            ? container.firstElementChild : container;
        var sections = [];
        var current = null;

        var nodes = contentNodes(root);
        // Saved section markers are authoritative. Legacy title detection is import-only.
        if (root.querySelector('h2[data-praxis-section="true"]')) {
            nodes.forEach(function (node) {
                if (node.nodeType === window.Node.ELEMENT_NODE && node.matches('h2[data-praxis-section="true"]')) {
                    current = { title: normalizedText(node) || "Untitled section", bodyHtml: "" };
                    sections.push(current);
                } else {
                    if (!current) { current = { title: "Opening", bodyHtml: "" }; sections.push(current); }
                    current.bodyHtml += sectionNodeHtml(node, normalizedText(node), false);
                }
            });
            return sections;
        }
        nodes.forEach(function (node) {
            var text = normalizedText(node);
            if (!text || /^[-–—_]{8,}$/.test(text.replace(/\s/g, ""))) return;
            var title = sectionTitle(node, text);
            if (title) {
                var preserveTriggerLabel = title === "Trigger Event" && trimTitle(text).toLowerCase() !== "trigger event";
                if (current && current.title.toLowerCase() === title.toLowerCase()) {
                    if (preserveTriggerLabel || !isHeadingOnly(node, text, title)) {
                        current.nodes.push(sectionNodeHtml(node, text, preserveTriggerLabel));
                    }
                    return;
                }
                current = { title: title, nodes: [] };
                sections.push(current);
                if (preserveTriggerLabel || !isHeadingOnly(node, text, title)) {
                    current.nodes.push(sectionNodeHtml(node, text, preserveTriggerLabel));
                }
            } else {
                if (!current) {
                    current = { title: "Opening", nodes: [] };
                    sections.push(current);
                }
                current.nodes.push(sectionNodeHtml(node, text, false));
            }
        });

        if (!sections.length) sections.push({ title: "Script", nodes: [html || "<p></p>"] });
        return sections.filter(function (section) { return section.nodes.length > 0; }).map(function (section) {
            return { title: section.title, bodyHtml: section.nodes.join("") };
        });
    }

    function contentNodes(root) {
        var result = [];
        var blocks = /^(P|H1|H2|H3|H4|H5|H6|UL|OL|BLOCKQUOTE)$/;
        function visit(parent) {
            Array.prototype.forEach.call(parent.childNodes, function (node) {
                if (node.nodeType === window.Node.TEXT_NODE) {
                    if (normalizedText(node)) result.push(node);
                } else if (node.nodeType === window.Node.ELEMENT_NODE && blocks.test(node.tagName)) {
                    result.push(node);
                } else if (node.nodeType === window.Node.ELEMENT_NODE) {
                    visit(node);
                }
            });
        }
        visit(root);
        return result;
    }

    function sectionTitle(node, text) {
        if (node.tagName === "H2" && node.getAttribute("data-praxis-section") === "true") return trimTitle(text);
        var numbered = text.match(/^(\d{1,2}\.\s+[^\r\n]{3,120})$/);
        if (numbered) return trimTitle(numbered[1]);
        var rules = [
            [/^Call Screener\b/i, "Call screener"],
            [/^(?:VM Script|Voicemail)\b/i, "Voicemail"],
            [/^Introduction\b/i, "Introduction"],
            [/^Identify the Correct Call Path\b/i, "Choose the call path"],
            [/^IPO\s*\(/i, "Trigger Event"],
            [/^Acquisition\b|Acquisition\s*$/i, "Trigger Event"],
            [/^Executive Compensation Package or Promotion\b/i, "Trigger Event"],
            [/^Fundraise Event|^Funding Round/i, "Trigger Event"],
            [/^Trigger Event$/i, "Trigger Event"],
            [/^No\s*[–—-]\s*Event Trigger Not True\b/i, "Event Trigger Validation"],
            [/^(?:Event Trigger Validation|Event trigger not true)$/i, "Event Trigger Validation"],
            [/^Advisor Appointment Checklist\b/i, "Interest Capture"],
            [/^(?:Interest Capture|Appointment checklist)$/i, "Interest Capture"],
            [/REBUTTALS/i, "Rebuttals"],
            [/^Quick Agent Reference\b/i, "Quick agent reference"]
        ];
        for (var i = 0; i < rules.length; i++) if (rules[i][0].test(text)) return rules[i][1];
        return null;
    }

    function isHeadingOnly(node, text, title) {
        if (/^H[1-6]$/.test(node.tagName || "") && text.length < 130) return true;
        var simple = text.replace(/:$/, "").trim().toLowerCase();
        return simple === title.toLowerCase() || simple === "call screener" || simple === "vm script" ||
            simple === "introduction" || simple === "advisor appointment checklist";
    }

    function sectionNodeHtml(node, text, asTriggerLabel) {
        if (asTriggerLabel) return "<h3>" + escapeHtml(text) + "</h3>";
        return node.nodeType === window.Node.TEXT_NODE ? escapeHtml(node.nodeValue) : node.outerHTML;
    }

    function normalizedText(node) { return String(node.textContent || "").replace(/\u00a0/g, " ").replace(/\s+/g, " ").trim(); }
    function trimTitle(value) { return String(value || "").replace(/\s+/g, " ").replace(/[:.\s]+$/, "").trim(); }
    function escapeHtml(value) {
        return String(value == null ? "" : value).replace(/&/g, "&amp;").replace(/</g, "&lt;")
            .replace(/>/g, "&gt;").replace(/"/g, "&quot;").replace(/'/g, "&#39;");
    }
    function serializeSections() {
        persistCurrentSection();
        return state.sections.map(function (section) {
            return '<h2 data-praxis-section="true">' + escapeHtml(section.title) + "</h2>" + section.bodyHtml;
        }).join("");
    }

    function renderTokens() {
        var root = document.getElementById("script-tokens");
        var groups = {};
        state.tokens.forEach(function (item) { (groups[item.group] = groups[item.group] || []).push(item); });
        root.innerHTML = "";
        Object.keys(groups).forEach(function (groupName) {
            var row = document.createElement("div");
            row.className = "script-token-group";
            var heading = document.createElement("strong");
            heading.textContent = groupName;
            var buttons = document.createElement("div");
            buttons.className = "script-token-buttons";
            groups[groupName].forEach(function (item) {
                var button = document.createElement("button");
                button.type = "button";
                button.textContent = item.label;
                button.title = item.token;
                button.addEventListener("click", function () { state.editor.insertText(item.token); });
                buttons.appendChild(button);
            });
            row.appendChild(heading);
            row.appendChild(buttons);
            root.appendChild(row);
        });
    }

    function save(event) {
        event.preventDefault();
        if (!state.current || state.busy) return;
        setBusy(true);
        var creating = !state.current.scriptKey;
        api(creating ? "api/scripts/new" : "api/scripts", {
            method: "POST",
            body: JSON.stringify(currentValues())
        }).then(function (saved) {
            if (creating) state.scripts.push(saved);
            else state.scripts = state.scripts.map(function (script) { return script.scriptKey === saved.scriptKey ? saved : script; });
            select(saved.scriptKey);
            showMessage("Script saved. New agent sessions will use this version.", false);
        }).catch(function (failure) {
            showMessage(failure.message, true);
        }).then(function () { setBusy(false); });
    }

    function api(url, options) {
        options = options || {};
        options.headers = options.headers || {};
        options.headers.Accept = "application/json";
        options.headers.Authorization = "Bearer " + state.token;
        if (options.body) options.headers["Content-Type"] = "application/json";
        return fetch(url, options).then(readResponse).then(function (result) {
            if (!result.response.ok) throw new Error(errorMessage(result.data, "The request failed."));
            return result.data;
        });
    }

    function readResponse(response) {
        return response.text().then(function (text) {
            var data = null;
            if (text) { try { data = JSON.parse(text); } catch (ignored) { data = null; } }
            return { response: response, data: data };
        });
    }

    function errorMessage(data, fallback) { return data && (data.message || data.Message || data.error) || fallback; }
    function routeLabel(script) { return script.eventType ? "All services / " + script.eventType : script.serviceName + " / all events (fallback)"; }

    function readToken() {
        try { return window.sessionStorage.getItem(sessionTokenKey) || window.localStorage.getItem(persistentTokenKey) || ""; }
        catch (ignored) { return ""; }
    }
    function clearStoredTokens() {
        fetch("api/config-session", { method: "DELETE", credentials: "same-origin", keepalive: true }).catch(function () { });
        try { window.sessionStorage.removeItem(sessionTokenKey); } catch (ignored) { }
        try { window.localStorage.removeItem(persistentTokenKey); } catch (ignored) { }
    }
    function clearToken() { state.token = ""; ByteForgePraxisAdmin.clear(); clearStoredTokens(); }
    function signOut() {
        clearToken();
        state.current = null;
        editorPanel.hidden = true;
        ByteForgePraxisAuth.redirect();
        document.getElementById("sign-out-button").hidden = true;
        var status = document.getElementById("session-status");
        status.textContent = "Signed out";
        status.classList.remove("is-ready");
        hideMessage();
    }
    function showMessage(text, error) { var el = document.getElementById("script-message"); el.textContent = text; el.classList.toggle("is-error", !!error); el.hidden = false; }
    function hideMessage() { document.getElementById("script-message").hidden = true; }
}());
