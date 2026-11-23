(function () {
    "use strict";
    window.ByteForgePraxisAdvisorLists = { attach: attach };
    function attach(token, button) {
        if (!button || button.dataset.attached) return;
        button.dataset.attached = "true";
        var modal, root, status, draft, rows = [], dirty = false, busy = false, page = 1, sort = "name", order = 1;
        var search, body, count, previous, next, save, editor, catalog, filter, campaign, mappingModal;
        var maps = [["recordIdField", "BP field holding Praxis record ID"], ["phoneField", "BP field holding Praxis phone"], ["nameField", "Advisor name field"], ["firmField", "Advisor firm field"]];
        async function api(path, data) {
            var options = { headers: { Authorization: "Bearer " + token, Accept: "application/json" }, cache: "no-store" };
            if (data !== undefined) { options.method = "POST"; options.headers["Content-Type"] = "application/json"; options.body = JSON.stringify(data); }
            var response = await fetch("api/advisor-lists/" + path, options);
            var result = await response.json().catch(function () { return {}; });
            if (!response.ok) throw new Error(result.message || result.Message || (response.status === 403 ? "Your role cannot configure advisor lists." : "Unable to load list configuration. Please retry."));
            return result;
        }
        function node(tag, text, parent) { var e = document.createElement(tag); if (text) e.textContent = text; if (parent) parent.appendChild(e); return e; }
        function action(label, parent, run) { var e = node("button", "", parent); e.type = "button"; e.className = "report-link-button"; e.innerHTML = label; e.onclick = run; return e; }
        function option(select, value, text) { var o = node("option", text, select); o.value = value; }
        function setBusy(value) {
            busy = value; root.setAttribute("aria-busy", String(value));
            root.querySelectorAll("button,input,select").forEach(function (e) {
                if (value) { e._bfDisabled = e.disabled; e.disabled = true; }
                else if (e._bfDisabled !== undefined) { e.disabled = e._bfDisabled; delete e._bfDisabled; }
            });
            save.disabled = value || !draft || !dirty;
        }
        function close() {
            if (busy || (dirty && !window.confirm("Discard unsaved list configuration changes?"))) return;
            ByteForgeModal.close(modal); modal = null; dirty = false; button.focus();
        }
        function changed() { dirty = true; save.disabled = busy; }
        function draw() {
            if (!draft) return;
            var found = rows.filter(function (r) { return [r.name, r.campaignName].join(" ").toLowerCase().includes(search.value.toLowerCase()); })
                .sort(function (a, b) { return String(a[sort] || "").localeCompare(String(b[sort] || "")) * order; });
            var pages = Math.max(1, Math.ceil(found.length / 5)); page = Math.min(page, pages); body.textContent = "";
            found.slice((page - 1) * 5, page * 5).forEach(function (row) {
                var tr = node("tr", "", body); node("td", row.name, tr); node("td", row.campaignName || "Choose campaign", tr);
                node("td", row.fieldsCheckedAtUtc ? ((row.nameField || "No name field") + " / " + (row.firmField || "No firm field")) : "Fields need discovery", tr);
                var td = node("td", "", tr);
                action("&#9999;&#65039; Mappings", td, function () { edit(row); }).dataset.listId = row.listId;
                action("&#128260; Refresh fields", td, function () { refresh(row); });
                action("&#10060; Remove", td, function () { rows = rows.filter(function (r) { return r !== row; }); changed(); draw(); });
            });
            if (!found.length) { var empty = node("tr", "", body); node("td", "No configured lists match.", empty).colSpan = 4; }
            count.textContent = found.length + " lists | Page " + page + " of " + pages;
            previous.disabled = page <= 1; next.disabled = page >= pages;
            root.querySelectorAll("th[data-sort]").forEach(function (th) { th.setAttribute("aria-sort", th.dataset.sort === sort ? (order === 1 ? "ascending" : "descending") : "none"); th.firstChild.textContent = th.dataset.label + (th.dataset.sort === sort ? (order === 1 ? " ▲" : " ▼") : ""); });
        }
        function edit(row) {
            if (mappingModal) return;
            editor = node("section"); editor.className = "bf-advisors bf-advisor-mappings";
            node("p", "List ID: " + row.listId, editor);
            var label = node("label", "Campaign association", editor); campaign = node("select", "", label);
            draft.campaigns.forEach(function (c) { option(campaign, c.id, c.name); }); campaign.value = row.campaignId;
            campaign.onchange = function () { row.campaignId = campaign.value; row.campaignName = campaign.selectedOptions[0].textContent; changed(); draw(); };
            node("p", "Observed BP binding: " + (row.observedCampaignName || "Not discovered") + ". Association does not bind or unbind the BP list.", editor);
            var grid = node("div", "", editor); grid.className = "bf-advisor-mapping-rows";
            maps.forEach(function (mapping, index) {
                var l = node("label", mapping[1], grid), select = node("select", "", l);
                option(select, "", index < 2 ? "Choose field" : "Do not send this field");
                (row.fields || []).forEach(function (f) { option(select, f, f); }); select.value = row[mapping[0]] || "";
                select.onchange = function () { row[mapping[0]] = select.value || null; changed(); draw(); };
            });
            node("p", "Discovered fields: " + ((row.fields || []).join(", ") || "None discovered") + ". Last read: " + (row.fieldsCheckedAtUtc ? new Date(row.fieldsCheckedAtUtc).toLocaleString() : "Never"), editor);
            node("p", "Changes stay in your draft. Use Save list configuration in the previous dialog to save them.", editor);
            function closeMappings() { ByteForgeModal.close(mappingModal, function () {
                mappingModal = null; modal.inert = false; modal.removeAttribute("aria-hidden");
                var trigger = Array.from(body.querySelectorAll("button[data-list-id]")).find(function (b) { return b.dataset.listId === row.listId; });
                (trigger || search).focus();
            }); }
            action("&#10006;&#65039; Done with mappings", editor, closeMappings);
            mappingModal = ByteForgeModal.show("", { type: "none", title: "Mappings: " + row.name, contentElement: editor, modalClass: "bf-advisor-modal", bodyClass: "bf-advisors", maxWidth: 720, hideFooter: true, showClose: false, closeOnOverlay: false, initialFocusSelector: "select" });
            campaign.focus(); modal.inert = true; modal.setAttribute("aria-hidden", "true");
            mappingModal.addEventListener("keydown", function (event) {
                if (event.key === "Escape") { event.preventDefault(); event.stopImmediatePropagation(); closeMappings(); }
                if (event.key === "Tab") {
                    var controls = Array.from(mappingModal.querySelectorAll("button,select")), first = controls[0], last = controls[controls.length - 1];
                    if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
                    else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
                }
            }, true);
        }
        async function refresh(row) {
            setBusy(true); status.textContent = "Reading BP fields for " + row.name + "...";
            try {
                var result = await api("discover", { token: draft.token, listId: row.listId });
                ["fields", "fieldsCheckedAtUtc", "observedCampaignId", "observedCampaignName", "name"].forEach(function (key) { row[key] = result[key]; });
                maps.forEach(function (m) { if (!row[m[0]] || !result.fields.includes(row[m[0]])) row[m[0]] = result[m[0]] || null; });
                changed(); draw(); edit(row); status.textContent = "Fields refreshed in this draft. Review the mappings, then save.";
            } catch (e) { status.textContent = e.message; } finally { setBusy(false); }
        }
        function fillCatalog() {
            catalog.textContent = ""; option(catalog, "", "Choose BP list");
            draft.catalog.filter(function (l) { return l.name.toLowerCase().includes(filter.value.toLowerCase()); }).sort(function (a, b) { return a.name.localeCompare(b.name); }).forEach(function (l) { option(catalog, l.id, l.name); });
        }
        async function add() {
            if (!catalog.value) return;
            if (rows.some(function (r) { return r.listId === catalog.value; })) { status.textContent = "That list is already configured."; return; }
            setBusy(true); status.textContent = "Reading the selected list's BP fields...";
            try { var row = await api("discover", { token: draft.token, listId: catalog.value }); rows.push(row); changed(); page = 1; search.value = ""; draw(); edit(row); status.textContent = "List added to this draft. Review its campaign and key mappings."; }
            catch (e) { status.textContent = e.message; } finally { setBusy(false); }
        }
        async function load() {
            setBusy(true); status.textContent = "Loading saved configuration and the BP list directory...";
            try { draft = await api("open", {}); rows = draft.lists || []; fillCatalog(); draw(); status.textContent = "Fields are read only when adding a list or using Refresh fields. Removing a row does not delete its BP list."; }
            catch (e) { status.textContent = e.message; } finally { setBusy(false); }
        }
        async function open() {
            if (modal) return;
            draft = null; dirty = false; page = 1;
            root = node("section"); root.className = "bf-advisors bf-advisor-list-config";
            status = node("p", "", root); status.setAttribute("role", "status"); status.setAttribute("aria-live", "polite");
            var toolbar = node("div", "", root); toolbar.className = "bf-advisor-toolbar";
            var label = node("label", "Find BP list", toolbar); filter = node("input", "", label); filter.type = "search"; filter.oninput = function () { if (draft) fillCatalog(); };
            label = node("label", "Available BP lists", toolbar); catalog = node("select", "", label);
            action("&#10133; Add list", toolbar, add);
            action("&#128260; Reload configuration", toolbar, function () { if (!dirty || window.confirm("Discard this draft and reload?")) { dirty = false; load(); } });
            label = node("label", "Search configured lists", root); search = node("input", "", label); search.type = "search"; search.oninput = function () { page = 1; draw(); };
            var scroll = node("div", "", root); scroll.className = "bf-advisor-scroll";
            var table = node("table", "", scroll); table.setAttribute("aria-label", "Configured BP lists");
            var head = node("tr", "", node("thead", "", table));
            [["List", "name"], ["Campaign", "campaignName"], ["Advisor fields", ""], ["Actions", ""]].forEach(function (column) {
                var th = node("th", "", head); if (!column[1]) { th.textContent = column[0]; return; }
                th.dataset.sort = column[1]; th.dataset.label = column[0]; action(column[0], th, function () { order = sort === column[1] ? -order : 1; sort = column[1]; page = 1; draw(); });
            }); body = node("tbody", "", table);
            var nav = node("div", "", root); nav.className = "bf-advisor-actions";
            previous = action("Previous", nav, function () { page--; draw(); }); count = node("span", "", nav); next = action("Next", nav, function () { page++; draw(); });
            node("p", "Record ID maps to tbl_PraxisMain.ID; phone maps to KeyPhone. Lists without mapped advisor fields are skipped. Removing an advisor mapping also skips that field in pending updates. The selected names also define the Praxis list-progress scope.", root);
            var footer = node("div", "", root); footer.className = "bf-advisor-actions";
            save = action("&#128190; Save list configuration", footer, async function () {
                setBusy(true); status.textContent = "Saving list configuration...";
                try { await api("save", { token: draft.token, lists: rows }); dirty = false; status.textContent = "List configuration saved."; setBusy(false); close(); }
                catch (e) { status.textContent = e.message; setBusy(false); }
            }); save.className = "report-button"; save.disabled = true;
            action("&#10006;&#65039; Close", footer, close);
            modal = ByteForgeModal.show("", { type: "none", title: "Configure advisor lists", contentElement: root, modalClass: "bf-advisor-modal", bodyClass: "bf-advisors", maxWidth: 1100, hideFooter: true, showClose: false, closeOnOverlay: false, initialFocusSelector: "input" });
            modal.addEventListener("keydown", function (event) {
                if (event.key === "Escape") { event.preventDefault(); event.stopImmediatePropagation(); close(); }
                if (event.key === "Tab") { var focusable = Array.from(modal.querySelectorAll("button,input,select")).filter(function (e) { return !e.disabled && e.getClientRects().length; }), first = focusable[0], last = focusable[focusable.length - 1]; if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); } else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); } }
            }, true);
            await load();
        }
        button.onclick = open;
        api("access").then(function () { button.hidden = false; }).catch(function () { button.hidden = true; });
        window.addEventListener("beforeunload", function (event) { if (dirty || busy) { event.preventDefault(); event.returnValue = ""; } });
    }
}());
