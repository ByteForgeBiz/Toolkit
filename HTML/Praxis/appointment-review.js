(function () {
    "use strict";
    var $ = function (id) { return document.getElementById(id); };
    var confirmationPage = document.body.dataset.confirmations === "true";
    var confirmations = confirmationPage;
    var token = "", page = 1, pages = 0, sort = "contactDate", direction = "desc", selected = null, detail = null, busy = false, generation = 0;
    try { token = sessionStorage.getItem("praxis.reportPortal.accessToken") || localStorage.getItem("praxis.reportPortal.persistentAccessToken") || ""; } catch (ignored) { }
    if (!token) { ByteForgePraxisAuth.redirect(); return; }
    function message(text, error) { $("qa-status").textContent = text; $("qa-status").dataset.error = error ? "true" : "false"; }
    function api(path, body) {
        return fetch((confirmationPage ? "api/confirmations" : "api/appointment-reviews") + path, { method: body ? "POST" : "GET", cache: "no-store",
            headers: { Authorization: "Bearer " + token, "Content-Type": "application/json" }, body: body ? JSON.stringify(body) : undefined
        }).then(function (response) {
            if (response.status === 401) ByteForgePraxisAuth.redirect();
            return response.json().catch(function () { return {}; }).then(function (data) {
                if (!response.ok) { var error = new Error(data.message || data.Message || (response.status === 403 ? "Your role does not grant Appointment QA access." : "The request failed. Try again.")); error.status = response.status; throw error; }
                return data;
            });
        });
    }
    function lock(value) {
        busy = value; $("qa-workspace").setAttribute("aria-busy", value ? "true" : "false");
        Array.prototype.forEach.call($("qa-workspace").querySelectorAll("button,input,select,textarea"), function (e) { e.disabled = value; });
        if (!value) { $("qa-attempt-form").querySelector("button").disabled = !detail || !!$("qa-fields").querySelector(".is-editing"); $("qa-prev").disabled = page <= 1; $("qa-next").disabled = page >= pages; $("qa-reject").disabled = !detail || !!$("qa-fields").querySelector(".is-editing"); $("qa-validate").disabled = !detail || detail.review.isValidated || !!$("qa-fields").querySelector(".is-editing"); }
    }
    function dirty() { return !!$("qa-attempt-notes").value.trim() || Array.prototype.some.call($("qa-fields").querySelectorAll("[data-original]"), function (input) { return input.value !== input.dataset.original; }); }
    function mayLeave() { return !dirty() || window.confirm("Discard the unsaved field changes?"); }
    function clearDetail() { $("qa-attempt-form").hidden = true; $("qa-attempt-history").hidden = true; $("qa-attempt-notes").value = ""; selected = null; detail = null; $("qa-fields").textContent = ""; $("qa-title").textContent = "Select an appointment"; $("qa-decision").textContent = ""; $("qa-revert").hidden = true; $("qa-reject").hidden = true; $("qa-validate").hidden = true; $("qa-reload").hidden = true; $("qa-badge").hidden = true; }
    function load() {
        lock(true); message("Loading appointments...");
        var query = new URLSearchParams({ validated: confirmations || $("qa-filter").value === "true", rejected: !confirmations && $("qa-filter").value === "rejected", page: page, pageSize: $("qa-size").value, sort: sort, direction: direction });
        var search = $("qa-search").value.trim();
        // Web API rejects an explicitly empty string parameter during model binding.
        if (search) query.set("search", search);
        if (confirmations) { if (!confirmationPage) query.set("confirmations", "true"); if ($("qa-date").value) query.set("appointmentDate", $("qa-date").value); query.set("confirmationStatus", $("qa-confirmation-status").value); }
        if (confirmationPage) { query.delete("validated"); query.delete("rejected"); }
        return api("?" + query).then(function (data) {
            pages = data.totalPages;
            if (page > Math.max(1, pages)) { page = Math.max(1, pages); return load(); }
            $("qa-rows").textContent = "";
            data.items.forEach(function (record) {
                var row = document.createElement("tr"); row.dataset.id = record.id; row.setAttribute("aria-selected", String(record.id === selected));
                row.addEventListener("click", function (event) { if (event.target.tagName !== "BUTTON" && !busy && mayLeave()) open(record.id); });
                var cell = document.createElement("td"), button = document.createElement("button"); button.textContent = record.contact || "Record " + record.id;
                button.addEventListener("click", function () { if (!busy && mayLeave()) open(record.id); }); cell.appendChild(button); row.appendChild(cell);
                [record.companyName, record.callDateTime, record.appointmentDateTime, record.agentName || record.agent, record.finalAdvisorName].forEach(function (value) { var td = document.createElement("td"); td.textContent = value || "\u2014"; row.appendChild(td); });
                $("qa-rows").appendChild(row);
            });
            $("qa-page").textContent = data.totalRows + " appointments | Page " + page + " of " + Math.max(1, pages);
            message(data.totalRows ? "Select an appointment to review." : "No appointments match these filters.");
        }).catch(function (error) { message(error.message, true); }).finally(function () { lock(false); });
    }
    var fields = [
        ["Contact", "Contact name"], ["KeyPhone", "Phone"], ["EmailAddress", "Preferred email", "email"], ["CompanyName", "Company"],
        ["AppointmentTimeZone", "Appointment time zone", "zone"], ["AppointmentDateTime", "Appointment date/time", "datetime-local"],
        ["FinalAdvisorId", "Advisor", "advisor"], ["AppointmentConfirmed", "Appointment confirmed", "boolean"],
        ["Agent", "Agent login"], ["CallDateTime", "Contact date/time", "datetime-local"], ["ListId", "List ID"], ["ListName", "List name"],
        ["Location", "Location"], ["Title", "Title"], ["Industry", "Industry"], ["University", "University"],
        ["EventType", "Event type"], ["EventDate", "Event date"], ["EventAccurate", "Event accuracy", "accuracy"],
        ["LinkedInUrl", "LinkedIn URL", "url"], ["WorkEmail", "Work email", "email"], ["PersonalEmail", "Personal email", "email"],
        ["AgentNotes", "Agent notes", "textarea"], ["BpNotes", "BrightPattern notes", "textarea"], ["AiData", "Research", "textarea"],
        ["RrAEmails", "Rated A emails", "textarea"], ["RrAminusEmails", "Rated A-minus emails", "textarea"], ["RrBEmails", "Rated B emails", "textarea"],
        ["RrMobilePhone", "Mobile phones", "textarea"], ["RrOtherPhones", "Other phones", "textarea"], ["RrOfficePhone", "Office phones", "textarea"]
    ];
    function dateInput(value) {
        if (/^\d{4}-\d{2}-\d{2}[T ]/.test(value)) return value.replace(" ", "T").substring(0, 16);
        if (!value) return "";
        var date = new Date(value), pad = function (n) { return String(n).padStart(2, "0"); };
        return isNaN(date.getTime()) ? "" : date.getFullYear() + "-" + pad(date.getMonth() + 1) + "-" + pad(date.getDate()) + "T" + pad(date.getHours()) + ":" + pad(date.getMinutes());
    }
    var sections = [
        ["1. Customer and organization", ["Contact", "KeyPhone", "EmailAddress", "CompanyName", "Location", "Title", "Industry", "University", "LinkedInUrl"]],
        ["2. Appointment", ["AppointmentDateTime", "AppointmentTimeZone", "FinalAdvisorId", "AppointmentConfirmed"]],
        ["3. Event", ["EventType", "EventDate", "EventAccurate"]],
        ["4. Booking and list", ["Agent", "CallDateTime", "ListId", "ListName"]],
        ["5. Notes and research", ["AgentNotes", "BpNotes", "AiData"]],
        ["6. Additional contact information", ["WorkEmail", "PersonalEmail", "RrAEmails", "RrAminusEmails", "RrBEmails", "RrMobilePhone", "RrOtherPhones", "RrOfficePhone"]]
    ];
    function fieldValue(field, editor) {
        var key = field[0][0].toLowerCase() + field[0].slice(1);
        var value = field[0] === "FinalAdvisorId" ? editor.finalAdvisorId : field[0] === "AgentNotes" ? editor.agentNotes : editor.record[key];
        return value == null ? "" : String(value);
    }
    function fieldChoices(type, editor) {
        if (type === "advisor") return editor.advisors.map(function (a) { return [String(a.id), a.name + " / " + a.firm]; });
        if (type === "zone") return editor.appointmentTimeZones.map(function (z) { return [z.id, z.displayName || z.id]; });
        if (type === "boolean") return [["true", "Yes"], ["false", "No"]];
        if (type === "accuracy") return [["", "Unknown"], ["True", "True"], ["False", "False"], ["Unable to Confirm", "Unable to Confirm"]];
        return null;
    }
    function iconButton(label, entity) {
        var button = document.createElement("button"); button.type = "button"; button.className = "bf-qa-edit-icon";
        button.innerHTML = entity; button.setAttribute("aria-label", label); button.title = label; return button;
    }
    function render(data) {
        detail = data; var editor = data.editor, record = editor.record;
        $("qa-title").textContent = (record.contact || "Appointment") + " - Appointment #" + record.id;
        $("qa-decision").textContent = data.review.isRejected ? "QA Rejected by " + data.review.rejectedBy + " at " + data.review.rejectedAtUtc + ": " + data.review.rejectionReason : data.review.isValidated ? "Validated by " + data.review.validatedBy + " at " + data.review.validatedAtUtc : "Review the saved values. Select a pencil to correct a field, then validate the appointment.";
        $("qa-badge").hidden = false; $("qa-badge").textContent = data.review.isRejected ? "QA Rejected" : data.review.isValidated ? "Validated" : "Pending";
        $("qa-fields").textContent = "";
        sections.forEach(function (section) {
            var panel = document.createElement("section"); panel.className = "bf-qa-detail-section";
            var heading = document.createElement("h3"); heading.textContent = section[0]; panel.appendChild(heading);
            var list = document.createElement("dl"); list.className = "bf-qa-field-list";
            section[1].forEach(function (name) {
                var field = fields.filter(function (f) { return f[0] === name; })[0], type = field[2] || "text", value = fieldValue(field, editor);
                var item = document.createElement("div"); item.dataset.field = name;
                var term = document.createElement("dt"); term.textContent = field[1];
                var description = document.createElement("dd"), text = document.createElement("span");
                if (name === "KeyPhone") value = formatPhoneNumber(value);
                else if (["RrMobilePhone", "RrOtherPhones", "RrOfficePhone"].indexOf(name) >= 0) value = value.replace(/(?:\+?1[ .-]?)?\(?\d{3}\)?[ .-]?\d{3}[ .-]?\d{4}(?!\d)/g, function (phone) { return formatPhoneNumber(phone); });
                var choices = fieldChoices(type, editor), selectedChoice = choices && choices.filter(function (c) { return c[0] === value; })[0];
                text.textContent = selectedChoice ? selectedChoice[1] : type === "datetime-local" ? dateInput(value).replace("T", " ") || "(blank)" : value || "(blank)";
                var edit = iconButton("Edit " + field[1], "&#9999;&#65039;");
                edit.addEventListener("click", function () {
                    if (busy || !detail || !mayLeave()) return;
                    if ($("qa-fields").querySelector(".is-editing")) render(detail);
                    beginEdit(field, detail.editor);
                });
                description.appendChild(text); if (!data.review.isRejected && !(confirmations && field[0] === "AppointmentConfirmed") && !(confirmationPage && ["Agent", "CallDateTime", "ListId", "ListName"].indexOf(field[0]) >= 0)) description.appendChild(edit); item.appendChild(term); item.appendChild(description); list.appendChild(item);
            });
            panel.appendChild(list); $("qa-fields").appendChild(panel);
        });
        $("qa-attempt-form").hidden = !confirmations || !data.review.isValidated || data.review.isRejected;
        $("qa-attempt-outcome").value = record.appointmentConfirmed ? "true" : "false";
        $("qa-attempt-notes").value = "";
        var attempts = data.confirmationAttempts || [];
        $("qa-attempt-history").hidden = attempts.length === 0;
        $("qa-attempts").textContent = "";
        attempts.forEach(function (attempt) {
            var item = document.createElement("li");
            item.textContent = attempt.attemptedAtUtc + " UTC | " + (attempt.attemptedBy || "Agent not recorded") + " | " + (attempt.queueClosed ? "3 Attempts - Unconfirmed - Update Posted" : attempt.confirmed ? "Confirmed" : "Attempted - not confirmed") + " | Booking: " + attempt.appointmentAt + " " + attempt.appointmentTimeZone + " | " + attempt.notes;
            $("qa-attempts").appendChild(item);
        });
        $("qa-revert").hidden = confirmationPage || confirmations || !data.review.isRejected; $("qa-reject").hidden = confirmationPage || data.review.isRejected; $("qa-validate").hidden = confirmations || data.review.isRejected; $("qa-reload").hidden = false;
    }
    function beginEdit(field, editor) {
        var description = $("qa-fields").querySelector('[data-field="' + field[0] + '"] dd');
        description.textContent = ""; description.className = "is-editing";
        var type = field[2] || "text", choices = fieldChoices(type, editor), value = fieldValue(field, editor);
        var form = document.createElement("form"); form.className = "bf-qa-inline-form";
        var input = document.createElement(choices ? "select" : type === "textarea" ? "textarea" : "input");
        if (!choices && type !== "textarea") input.type = type === "datetime-local" ? "hidden" : type;
        if (type === "datetime-local") value = dateInput(value);
        if (choices) { if (!choices.some(function (c) { return c[0] === value; })) choices.unshift([value, value || "Not set"]); choices.forEach(function (c) { var option = document.createElement("option"); option.value = c[0]; option.textContent = c[1]; input.appendChild(option); }); }
        input.id = "qa-field-" + field[0]; input.setAttribute("aria-label", field[1]); input.value = value; input.dataset.original = value;
        var actions = document.createElement("span"); actions.className = "bf-qa-edit-actions";
        var save = iconButton("Save " + field[1], "&#128190;"); save.type = "submit";
        var cancel = iconButton("Cancel editing " + field[1], "&#10006;&#65039;");
        cancel.addEventListener("click", function () { if (busy) return; if (!detail) open(selected); else { render(detail); lock(false); } });
        form.addEventListener("submit", function (event) { event.preventDefault(); if (busy || !detail || !form.reportValidity()) return; saveChange({ field: field[0], value: input.value }, input); });
        input.addEventListener("keydown", function (event) { if (event.key === "Escape") { event.preventDefault(); cancel.click(); } });
        actions.appendChild(save); actions.appendChild(cancel); form.appendChild(input);
        var focusTarget = input;
        if (type === "datetime-local") {
            var pickerRoot = document.createElement("div"); form.appendChild(pickerRoot);
            window.ByteForgeDateTimePicker.createPicker({ root: pickerRoot, input: input, label: field[1] });
            focusTarget = pickerRoot.querySelector(".bf-date-time-display");
        }
        form.addEventListener("keydown", function (event) {
            if (event.key === "Escape" && !event.defaultPrevented) { event.preventDefault(); cancel.click(); }
        });
        form.appendChild(actions); description.appendChild(form);
        $("qa-attempt-form").querySelector("button").disabled = true; $("qa-reject").disabled = true; $("qa-validate").disabled = true; focusTarget.focus();
    }
    function open(id) {
        var request = ++generation; lock(true); message("Loading appointment details...");
        return api("/" + encodeURIComponent(id)).then(function (data) { if (request !== generation) return; selected = id; $("qa-rows").querySelectorAll("tr").forEach(function (row) { row.setAttribute("aria-selected", String(row.dataset.id === String(id))); }); render(data); message(data.review.isRejected ? "Rejected appointment: read-only QA record." : confirmations ? "Record confirmation attempts below. Booking changes require renewed QA approval." : "Review the fields below. Save each correction before validating."); }).catch(function (error) { clearDetail(); message(error.message, true); }).finally(function () { lock(false); });
    }
    function saveChange(change, input) {
        change.revision = detail.review.revision; lock(true); message(change.confirmationAttempt ? "Saving confirmation attempt..." : change.revertRejection ? "Returning appointment to QA..." : change.reject ? "Rejecting appointment..." : change.validate ? "Validating appointment..." : "Saving correction...");
        return api("/" + encodeURIComponent(selected), change).then(function (data) {
            if (input) {
                if (confirmations && !data.review.isValidated) { clearDetail(); return load().then(function () { message("Booking changed. Appointment returned to QA for approval."); }); }
                render(data);
                var selectedRow = $("qa-rows").querySelector('[data-id="' + selected + '"]');
                if (selectedRow) { selectedRow.querySelector("button").textContent = data.editor.record.contact || "Record " + selected; selectedRow.cells[1].textContent = data.editor.record.companyName || ""; }
                message("Correction saved. Review remaining fields, then validate.");
            } else { clearDetail(); return load().then(function () { message(change.confirmationAttempt ? "Confirmation attempt saved. Appointment approval preserved." : change.revertRejection ? "Rejection reverted. Appointment returned to Pending QA and requires validation before publication." : change.reject ? "QA Rejected. Appointment retained under Status: Rejected and hidden from reports and public appointment API results." : "Appointment validated and available in appointment lists and API results."); }); }
        }).catch(function (error) { message(error.message, true);
            var form = input && input.closest("form");
            if (form) { var old = form.querySelector("[role=alert]"); if (old) old.remove(); var warning = document.createElement("p"); warning.setAttribute("role", "alert"); warning.textContent = error.message; form.appendChild(warning); }
            if (error.status === 409) { detail = null; } }).finally(function () { lock(false); });
    }
    $("qa-validate").addEventListener("click", function () { if (!busy && detail && !dirty()) saveChange({ validate: true }); });
    $("qa-revert").addEventListener("click", function () {
        if (busy || !detail || !detail.review.isRejected || !mayLeave()) return;
        if (window.confirm("Revert this rejection and return the appointment to Pending QA? It will require validation before publication.")) saveChange({ revertRejection: true });
    });
    $("qa-reject").addEventListener("click", function () {
        if (busy || !detail || dirty() || $("qa-fields").querySelector(".is-editing")) return;
        var reason = window.prompt("QA Rejected: this appointment will be retained but hidden from reports and public appointment API results, and accessible under Status: Rejected. Enter the rejection reason:");
        if (reason === null) return;
        reason = reason.trim();
        if (!reason || reason.length > 1000) { message("Enter a rejection reason of 1 to 1000 characters.", true); return; }
        saveChange({ reject: true, rejectionReason: reason });
    });
    $("qa-attempt-form").addEventListener("submit", function (event) {
        event.preventDefault();
        if (busy || !detail || $("qa-fields").querySelector(".is-editing") || !this.reportValidity()) return;
        saveChange({ confirmationAttempt: true, confirmed: $("qa-attempt-outcome").value === "true", queueClosed: $("qa-attempt-outcome").value === "closed", notes: $("qa-attempt-notes").value });
    });
    function switchMode(value) {
        if (busy || !mayLeave()) return;
        confirmations = value; clearDetail();
        $("qa-tab-review").setAttribute("aria-pressed", String(!value)); $("qa-tab-confirmations").setAttribute("aria-pressed", String(value));
        $("qa-status-filter").hidden = value; $("qa-date-filter").hidden = !value; $("qa-confirmation-filter").hidden = !value;
        sort = value ? "appointmentDate" : "contactDate"; direction = value ? "asc" : "desc"; page = 1;
        document.querySelectorAll("[data-sort]").forEach(function (b) { var active = b.dataset.sort === sort; b.parentNode.setAttribute("aria-sort", active ? (direction === "asc" ? "ascending" : "descending") : "none"); b.textContent = b.dataset.label + (active ? direction === "asc" ? " \u2191" : " \u2193" : ""); });
        if (value && !$("qa-date").value) { lock(true); return api("/confirmation-settings").then(function (data) { $("qa-date").value = data.date || ""; $("qa-date").title = "Leave blank for " + data.fromDate + " onward. Today uses " + data.timeZone + "; appointment dates use their saved local time."; return load(); }).catch(function (error) { message(error.message, true); }).finally(function () { lock(false); }); }
        else return load();
    }
    $("qa-tab-review").addEventListener("click", function () { switchMode(false); });
    $("qa-tab-confirmations").addEventListener("click", function () { switchMode(true); });
    $("qa-date").addEventListener("change", reset); $("qa-confirmation-status").addEventListener("change", reset);
    $("qa-reload").addEventListener("click", function () { if (!busy && selected && mayLeave()) open(selected); });
    function reset() { if (busy || !mayLeave()) return; page = 1; clearDetail(); load(); }
    $("qa-search-form").addEventListener("submit", function (e) { e.preventDefault(); reset(); });
    $("qa-filter").addEventListener("change", reset); $("qa-size").addEventListener("change", reset); $("qa-refresh").addEventListener("click", reset);
    $("qa-settings-toggle").addEventListener("click", function () { $("qa-settings").hidden = !$("qa-settings").hidden; this.setAttribute("aria-expanded", String(!$("qa-settings").hidden)); });
    ["prev", "next"].forEach(function (name) { $("qa-" + name).addEventListener("click", function () { if (busy || !mayLeave()) return; page += name === "prev" ? -1 : 1; clearDetail(); load(); }); });
    document.querySelectorAll("[data-sort]").forEach(function (button) { var label = button.textContent.replace(/ [\u2191\u2193]$/, ""); button.dataset.label = label; button.addEventListener("click", function () {
        if (busy || !mayLeave()) return; direction = sort === button.dataset.sort && direction === "asc" ? "desc" : "asc"; sort = button.dataset.sort;
        document.querySelectorAll("[data-sort]").forEach(function (b) { b.parentNode.setAttribute("aria-sort", b === button ? direction === "asc" ? "ascending" : "descending" : "none"); b.textContent = b.dataset.label + (b === button ? direction === "asc" ? " \u2191" : " \u2193" : ""); }); page = 1; clearDetail(); load();
    }); });
    var splitter = $("qa-splitter"), layout = $("qa-layout");
    function resize(percent) { percent = Math.max(32, Math.min(60, percent)); layout.style.setProperty("--bf-qa-list-width", percent + "%"); splitter.setAttribute("aria-valuenow", String(Math.round(percent))); }
    splitter.addEventListener("pointerdown", function (event) { if (window.matchMedia("(max-width:1100px)").matches) return; event.preventDefault(); splitter.setPointerCapture(event.pointerId); });
    splitter.addEventListener("pointermove", function (event) { if (!splitter.hasPointerCapture(event.pointerId)) return; var bounds = layout.getBoundingClientRect(); resize((event.clientX - bounds.left) / bounds.width * 100); });
    splitter.addEventListener("pointerup", function (event) { if (splitter.hasPointerCapture(event.pointerId)) splitter.releasePointerCapture(event.pointerId); });
    splitter.addEventListener("keydown", function (event) { if (event.key !== "ArrowLeft" && event.key !== "ArrowRight") return; event.preventDefault(); resize(Number(splitter.getAttribute("aria-valuenow")) + (event.key === "ArrowRight" ? 2 : -2)); });
    splitter.addEventListener("dblclick", function () { resize(40); });
    window.addEventListener("beforeunload", function (event) { if (dirty()) { event.preventDefault(); event.returnValue = ""; } });
    $("qa-sign-out").addEventListener("click", function () { if (!mayLeave()) return; try { sessionStorage.removeItem("praxis.reportPortal.accessToken"); localStorage.removeItem("praxis.reportPortal.persistentAccessToken"); } catch (ignored) { } token = ""; location.replace("login.html"); });
    if (window.ByteForgeAccount) window.ByteForgeAccount.attach($("qa-profile"), { getToken: function () { return token; }, onPasswordChanged: function () { ByteForgePraxisAuth.redirect(); } });
    ByteForgePraxisAdmin.load(token).then(function () { return api("?pageSize=1"); }).then(function () { $("qa-profile").hidden = false; $("qa-workspace").hidden = false; return confirmationPage ? switchMode(true) : load(); }).catch(function (error) { message(error.message, true); });
}());
