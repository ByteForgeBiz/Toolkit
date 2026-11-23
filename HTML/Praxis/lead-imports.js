(function () {
    "use strict";
    var token = "", jobs = [], model = null, busy = false, maxBytes = 0, timeoutSeconds = 900, page = 1, sort = "createdAtUtc", direction = -1;
    var progressTimer = null, operationStarted = 0, lastSignal = 0;
    var detailTrigger = null, detailModal = null;
    function restoreDetailFocus() { if (detailTrigger && detailTrigger.isConnected) detailTrigger.focus(); detailTrigger = null; detailModal = null; }
    function el(name) { return document.getElementById("import-" + name); }
    function message(text, error) { el("progress-panel").hidden = false; el("message").textContent = text; el("message").classList.toggle("bf-import-error", !!error); }
    function reportFailure(stage, text, current, total) {
        finishProgress(stage, text, current, total, true); message(text, true);
        el("progress-panel").scrollIntoView({ block: "center" });
        ByteForgeModal.error(text, stage);
    }
    function elapsed() {
        var seconds = Math.floor((Date.now() - operationStarted) / 1000), silence = Math.floor((Date.now() - lastSignal) / 1000);
        el("elapsed").textContent = "Elapsed " + Math.floor(seconds / 60) + "m " + String(seconds % 60).padStart(2, "0") + "s" + (silence >= 10 ? " | Waiting for the next server update (" + silence + "s)." : " | Receiving progress updates.");
    }
    function showProgress(update) {
        el("progress-panel").hidden = false;
        if (update.stage === "Verifying Main records") { lastSignal = Date.now(); elapsed(); return; }
        var subtask = update.stage === "Resolving advisors";
        if (subtask) {
            el("task-panel").hidden = false;
            el("task-stage").textContent = update.stage;
            el("task-message").textContent = update.message || "";
            var taskTotal = Number(update.total) || 0, taskCurrent = Math.max(0, Number(update.current) || 0);
            if (taskTotal > 0) { el("task-progress").max = taskTotal; el("task-progress").value = Math.min(taskCurrent,taskTotal); el("task-percent").textContent = taskCurrent.toLocaleString() + " / " + taskTotal.toLocaleString(); }
            else { el("task-progress").removeAttribute("value"); el("task-percent").textContent = "Working..."; }
            lastSignal = Date.now(); elapsed(); return;
        }
        if (update.stage !== "Inserting Main records") el("task-panel").hidden = true;
        el("stage").textContent = update.stage || "Working";
        el("progress-message").textContent = update.message || "Working...";
        var total = Number(update.total) || 0, current = Math.max(0, Number(update.current) || 0);
        if (total > 0) {
            current = Math.min(current, total); el("progress").max = total; el("progress").value = current;
            el("percent").textContent = Math.floor(current / total * 100) + "% | " + current.toLocaleString() + " / " + total.toLocaleString();
        } else {
            el("progress").removeAttribute("value"); el("percent").textContent = current ? current.toLocaleString() + " processed" : "Working...";
        }
        lastSignal = Date.now(); elapsed();
    }
    function startProgress(stage, text) {
        if (progressTimer) clearInterval(progressTimer);
        el("task-panel").hidden = true;
        operationStarted = lastSignal = Date.now(); el("progress-panel").classList.remove("bf-import-failed", "bf-import-complete");
        showProgress({ stage: stage, message: text }); el("progress-panel").scrollIntoView({ block: "center" }); progressTimer = setInterval(elapsed, 1000);
    }
    function finishProgress(stage, text, current, total, failed) {
        if (progressTimer) clearInterval(progressTimer); progressTimer = null;
        if (failed) text += " Last reported stage: " + el("stage").textContent + ".";
        showProgress({ stage: stage, message: text, current: current, total: total });
        el("progress-panel").classList.toggle("bf-import-failed", !!failed); el("progress-panel").classList.toggle("bf-import-complete", !failed);
        el("elapsed").textContent = "Elapsed " + Math.floor((Date.now() - operationStarted) / 1000) + "s";
    }
    function setBusy(value) {
        busy = value;
        ["preview", "file", "list", "save", "sync", "status", "refresh", "approve", "show-rejected"].forEach(function (key) { el(key).disabled = value; });
        el("rejection-reasons").querySelectorAll("button").forEach(function (button) { button.disabled = value; });
        el("rows").querySelectorAll("button").forEach(function (button) { button.disabled = value; });
        if (!value) el("save").disabled = !model || !["Preview","MainFailed","MainImporting","Pending","Failed","Syncing"].includes(model.status) || !model.validRows;
    }
    async function api(path, payload, method) {
        var response = await fetch(path, { method: method || (payload === undefined ? "GET" : "POST"), headers: { Authorization: "Bearer " + token, Accept: "application/json", "Content-Type": "application/json" }, body: payload === undefined ? undefined : JSON.stringify(payload), cache: "no-store" });
        var data = await response.json().catch(function () { return {}; });
        if (!response.ok) throw new Error(data.message || data.Message || "The import request failed (" + response.status + "). Refresh its status before retrying.");
        return data;
    }
    // Match the Walgreens import: upload progress plus flushed NDJSON stage events.
    function progressApi(path, payload) {
        return new Promise(function (resolve, reject) {
            var xhr = new XMLHttpRequest(), offset = 0, buffer = "", result = null, failure = "";
            function event(data) {
                lastSignal = Date.now();
                if (data.type === "progress") showProgress(data);
                else if (data.type === "complete") result = data.result;
                else if (data.type === "error") { failure = data.message || "The import failed. Refresh its status before retrying."; message(failure, true); finishProgress("Import failed", failure, model ? model.importedRows || 0 : 0, model ? model.validRows : 0, true); }
            }
            function read(finalRead) {
                if ((xhr.getResponseHeader("Content-Type") || "").indexOf("application/x-ndjson") < 0) return;
                buffer += xhr.responseText.substring(offset); offset = xhr.responseText.length;
                var lines = buffer.split(/\r?\n/); buffer = lines.pop();
                if (finalRead && buffer) { lines.push(buffer); buffer = ""; }
                lines.forEach(function (line) { if (!line.trim()) return; try { event(JSON.parse(line)); } catch (error) { failure = "An import progress update was unreadable. Refresh the saved status before retrying."; } });
            }
            xhr.open("POST", path, true); xhr.timeout = (timeoutSeconds + 60) * 1000;
            xhr.setRequestHeader("Authorization", "Bearer " + token); xhr.setRequestHeader("Content-Type", "application/json"); xhr.setRequestHeader("Accept", "application/json, application/x-ndjson"); xhr.setRequestHeader("X-Praxis-Progress", "1");
            xhr.upload.onprogress = function (upload) { if (upload.lengthComputable && payload.base64) showProgress({ stage: "Uploading file", current: upload.loaded, total: upload.total, message: "Sending the selected file to Praxis." }); };
            xhr.onprogress = function () { lastSignal = Date.now(); read(false); };
            xhr.onload = function () {
                read(true);
                if (xhr.status >= 400 || result === null) {
                    try { var data = JSON.parse(xhr.responseText); if (xhr.status < 400 && !data.type) result = data; else failure = data.message || data.Message || failure; } catch (ignored) { }
                }
                if (failure || xhr.status >= 400 || result === null) reject(new Error(failure || "The request ended without an import result. Refresh the saved status before retrying."));
                else resolve(result);
            };
            xhr.onerror = function () { reject(new Error("Connection lost during the import. Refresh the saved status before retrying.")); };
            xhr.ontimeout = function () { reject(new Error("The request timed out. Refresh the saved status before retrying; do not start a replacement import.")); };
            xhr.send(JSON.stringify(payload));
        });
    }
    function apply(data) {
        // Batch responses carry counters only; retain the already reviewed source rows.
        if (data.rows === null) delete data.rows;
        if (data.rejected === null) delete data.rejected;
        if (data.warnings === null) delete data.warnings;
        if (data.advisorMissingData === null) delete data.advisorMissingData;
        model = Object.assign({}, model && model.id === data.id ? model : {}, data);
        el("review").hidden = false;
        el("heading").textContent = model.fileName + " / " + model.listName;
        renderRejections();
        el("summary").textContent = model.sourceRows + " source rows | " + model.validRows + " valid | " + model.rejectedRows + " rejected | " + model.sentRows + " acknowledged by BP | " + model.status + (model.newAdvisors ? " | " + model.newAdvisors + " advisor/firm pairs will be created on save" : "") + (model.reactivateAdvisors ? " | " + model.reactivateAdvisors + " advisors will be reactivated on save" : "") + (model.normalizedRows ? " | " + model.normalizedRows + " rows normalized for database storage; see Details for original text" : "");
        el("summary").textContent += model.warnings && model.warnings.length ? " | " + model.warnings.length + " warnings; import can continue. See Warnings." : "";
        var mainPending=["Preview","MainFailed","MainImporting"].includes(model.status);
        el("approve-label").hidden = true; el("approve").checked = true;
        el("save").hidden = !["Preview","MainFailed","MainImporting","Pending","Failed","Syncing"].includes(model.status);
        el("save").innerHTML = "&#128260; Resume import";
        el("sync").hidden = true;
        el("summary").textContent += (model.advisorMissingData && model.advisorMissingData.length ? " | " + model.advisorMissingData.length + " advisors have missing optional data; see Advisor missing data. Import can continue." : "");
        el("summary").textContent += " | " + (model.importedRows || 0) + " committed to Main";
        if (!busy && model.status !== "Preview") { operationStarted = Date.now(); showProgress({stage:mainPending?"Main import":"BP publication",current:mainPending?(model.importedRows||0):model.sentRows,total:model.validRows,message:model.error || model.status}); }
        if (model.error) message(model.error, true);
        setBusy(busy); render();
    }
    function rejectionReason(row) {
        return row.reason || "No rejection reason was saved for this entry. Refresh the import status; if it remains missing, contact support.";
    }
    function showRejected(reason) {
        el("mode").value = "rejected"; el("search").value = reason || "";
        sort = "row"; direction = 1; page = 1; render();
    }
    function renderRejections() {
        var count = Number(model.rejectedRows) || 0;
        el("show-rejected").hidden = el("rejection-summary").hidden = count === 0;
        el("show-rejected").textContent = "View " + count + " rejected " + (count === 1 ? "entry" : "entries") + " and reasons";
        el("rejection-reasons").textContent = "";
        if (!count) return;
        var rejected = model.rejected || [], groups = new Map();
        rejected.forEach(function (row) {
            var reason = rejectionReason(row);
            groups.set(reason, (groups.get(reason) || 0) + 1);
        });
        el("rejection-description").textContent = count + " source " + (count === 1 ? "entry was" : "entries were") + " skipped and not imported by this job. Select a reason to inspect the affected entries.";
        // These compact reason selectors filter the existing searchable, sortable, paged review table.
        Array.from(groups.entries()).sort(function (a, b) { return b[1] - a[1] || a[0].localeCompare(b[0]); }).forEach(function (group) {
            var item = document.createElement("p"), button = document.createElement("button");
            button.type = "button"; button.className = "report-link-button";
            button.textContent = group[1] + " " + (group[1] === 1 ? "entry" : "entries") + ": " + group[0];
            button.disabled = busy; button.onclick = function () { showRejected(group[0]); };
            item.appendChild(button); el("rejection-reasons").appendChild(item);
        });
        if (rejected.length !== count) {
            var pending = document.createElement("p");
            pending.textContent = "Some rejection details are not loaded. Refresh status to retrieve the saved reasons.";
            el("rejection-reasons").appendChild(pending);
        }
    }
    function addFields(container, title, fields) {
        var heading = document.createElement("h3"), list = document.createElement("dl");
        heading.textContent = title; list.className = "bf-import-field-grid";
        fields.forEach(function (field) {
            var pair = document.createElement("div"), label = document.createElement("dt"), value = document.createElement("dd");
            label.textContent = field[0]; value.textContent = field[1] == null || field[1] === "" ? "(blank)" : String(field[1]);
            pair.appendChild(label); pair.appendChild(value); list.appendChild(pair);
        });
        container.appendChild(heading); container.appendChild(list);
    }
    function addDuplicateTable(container, records) {
        var heading = document.createElement("h3"); heading.textContent = "Matching entries from Praxis Main (" + records.length + ")"; container.appendChild(heading);
        if (!records.length) { var none = document.createElement("p"); none.textContent = "No matching database entry was retained for this row."; container.appendChild(none); return; }
        var search = document.createElement("input"), label = document.createElement("label"), holder = document.createElement("div"), table = document.createElement("table"), nav = document.createElement("div");
        var previous = document.createElement("button"), next = document.createElement("button"), count = document.createElement("span");
        var columns = [["recordId","RecordID"],["matchedOn","Matched on"],["contact","Contact"],["phone","KeyPhone"],["listId","ListID"],["company","Company"],["importDate","Imported"],["listName","Source file"],["bpListName","BP list"],["disposition","Disposition"]];
        var pageNumber = 1, sortField = "recordId", sortDirection = 1;
        search.type = "search"; search.placeholder = "Search matching records"; label.textContent = "Search duplicates"; label.appendChild(search); container.appendChild(label);
        holder.className = "bf-import-scroll"; holder.appendChild(table); container.appendChild(holder); nav.className = "bf-import-toolbar";
        previous.type = next.type = "button"; previous.className = next.className = "report-link-button"; previous.textContent = "Previous"; next.textContent = "Next";
        nav.appendChild(previous); nav.appendChild(count); nav.appendChild(next); container.appendChild(nav);
        function draw() {
            var filtered = records.filter(function (r) { return columns.map(function (c) {return r[c[0]];}).join(" ").toLowerCase().includes(search.value.toLowerCase()); });
            filtered.sort(function (a,b) {return String(a[sortField] || "").localeCompare(String(b[sortField] || ""),undefined,{numeric:true,sensitivity:"base"}) * sortDirection;});
            var pages = Math.max(1,Math.ceil(filtered.length/25)); pageNumber = Math.max(1,Math.min(pageNumber,pages)); table.textContent = "";
            var head = document.createElement("thead"), tr = document.createElement("tr");
            columns.forEach(function (c) {var th=document.createElement("th"), button=document.createElement("button"); button.type="button";button.textContent=c[1]+(sortField===c[0]?(sortDirection===1?" \u25b2":" \u25bc"):"");th.setAttribute("aria-sort",sortField===c[0]?(sortDirection===1?"ascending":"descending"):"none");button.onclick=function(){sortDirection=sortField===c[0]?-sortDirection:1;sortField=c[0];pageNumber=1;draw();};th.appendChild(button);tr.appendChild(th);});
            head.appendChild(tr);table.appendChild(head);var body=document.createElement("tbody");
            filtered.slice((pageNumber-1)*25,pageNumber*25).forEach(function(r){var line=document.createElement("tr");columns.forEach(function(c){var cell=document.createElement("td");cell.textContent=r[c[0]]==null?"":String(r[c[0]]);line.appendChild(cell);});body.appendChild(line);});table.appendChild(body);
            count.textContent=filtered.length+(filtered.length===1?" match | Page ":" matches | Page ")+pageNumber+" of "+pages;previous.disabled=pageNumber===1;next.disabled=pageNumber===pages;
        }
        search.oninput=function(){pageNumber=1;draw();};previous.onclick=function(){pageNumber--;draw();};next.onclick=function(){pageNumber++;draw();};draw();
    }
    function showDetails(row, mode, button) {
        if (mode === "exceptions") mode = row.exceptionType === "Rejected" ? "rejected" : "warnings";
        var source = row.source || (mode === "warnings" && model.rows ? model.rows.find(function (r) {return r.row === row.row;}) : row) || row;
        var rejected = mode === "rejected", warning = mode === "warnings";
        var title = (rejected ? "Rejected entry" : warning ? "Import warning" : "Lead details") + ": source row " + row.row;
        el("detail-heading").textContent = title;
        el("detail-reason").hidden = !(rejected || warning);
        el("detail-reason").textContent = rejected ? "Reason for rejection: " + rejectionReason(row) : warning ? "Warning: " + row.reason : "";
        var content = el("detail-text"); content.textContent = "";
        if (row.originalText != null) addFields(content,"Text normalization",[["Field",row.field],["Original text",row.originalText],["Stored text",row.normalizedText]]);
        var changes = source.textChanges || [];
        changes.forEach(function (change) {addFields(content,"Text normalization: " + change.field,[["Original text",change.original],["Stored text",change.stored]]);});
        var duplicates = source.duplicates || [];
        if (duplicates.length || /already exists|duplicate/i.test(row.reason || "")) addDuplicateTable(content,duplicates);
        addFields(content,"Source lead",[["RecordID",source.recordId],["ListID",source.listId],["Contact",source.contact],["Primary phone",source.phone],["Other phone",source.phone2],["Company",source.company],["City / location",source.location],["State",source.state],["ZIP / postal code",source.postalCode],["Title",source.title],["University",source.university],["Email",source.email],["Event",source.eventType],["Event date",source.eventDate],["Advisor",source.advisorName],["Firm",source.advisorFirm],["AI / research notes",source.aiData]]);
        detailTrigger = button;
        detailModal = ByteForgeModal.show("",{title:title,contentElement:el("detail"),modalClass:"bf-import-detail-modal",maxWidth:"1100px",initialFocusSelector:"#import-detail-close",buttons:[{text:"Close",primary:true}],onClose:restoreDetailFocus});
    }
    function render() {
        var mode = el("mode").value;
        var columns = mode === "jobs" ? [["fileName", "File"], ["listName", "BP list"], ["status", "Status"], ["createdAtUtc", "Created UTC"]] : mode === "advisorMissingData" ? [["advisorName", "Advisor"], ["advisorFirm", "Firm"], ["missingFields", "Missing data"]] : mode === "exceptions" ? [["row", "Row"], ["contact", "Contact"], ["exceptionType", "Type"], ["reason", "Reason"]] : (mode === "rejected" || mode === "warnings") ? [["row", "Row"], ["contact", "Contact"], ["reason", "Reason"]] : [["row", "Row"], ["contact", "Contact"], ["phone", "BP phone"], ["eventType", "Event"], ["company", "Company"], ["state", "State"], ["postalCode", "ZIP / postal code"], ["geographyStatus", "Location check"], ["advisorName", "Advisor"], ["advisorFirm", "Firm"]];
        var source = mode === "jobs" ? jobs : model ? (model[mode] || []) : [];
        if (!columns.some(function (c) { return c[0] === sort; })) { sort = columns[0][0]; direction = 1; }
        if (mode === "exceptions" && model) source = (model.rejected || []).map(function (row) { return Object.assign({}, row, { exceptionType: "Rejected", reason: rejectionReason(row) }); })
            .concat((model.warnings || []).map(function (row) { return Object.assign({}, row, { exceptionType: "Warning" }); }));
        if (mode === "rejected") source = source.map(function (row) { return Object.assign({}, row, { reason: rejectionReason(row) }); });
        var search = el("search").value.toLowerCase();
        var list = source.filter(function (row) { return columns.map(function (c) { return row[c[0]]; }).join(" ").toLowerCase().includes(search); }).sort(function (a, b) { return String(a[sort] == null ? "" : a[sort]).localeCompare(String(b[sort] == null ? "" : b[sort]), undefined, { numeric: true, sensitivity: "base" }) * direction; });
        var size = Number(el("size").value), pages = Math.max(1, Math.ceil(list.length / size)); page = Math.max(1, Math.min(page, pages));
        el("columns").textContent = ""; var head = document.createElement("tr");
        columns.forEach(function (c) { var th = document.createElement("th"), button = document.createElement("button"); button.type = "button"; button.textContent = c[1] + (sort === c[0] ? (direction === 1 ? " \u25b2" : " \u25bc") : ""); th.setAttribute("aria-sort", sort === c[0] ? (direction === 1 ? "ascending" : "descending") : "none"); button.onclick = function () { direction = sort === c[0] ? -direction : 1; sort = c[0]; page = 1; render(); }; th.appendChild(button); head.appendChild(th); });
        var action = document.createElement("th"); action.textContent = "Action"; head.appendChild(action); el("columns").appendChild(head); el("rows").textContent = "";
        list.slice((page - 1) * size, page * size).forEach(function (row) {
            var tr = document.createElement("tr"); columns.forEach(function (c) { var td = document.createElement("td"); td.textContent = row[c[0]] == null ? "" : row[c[0]]; tr.appendChild(td); });
            var td = document.createElement("td"), button = document.createElement("button"); button.type = "button"; button.className = "report-link-button"; button.innerHTML = mode === "jobs" ? "&#128065;&#65039; Open" : "&#128065;&#65039; Details";
            button.disabled = busy; button.onclick = mode === "jobs" ? function () { open(row.id); } : function () { showDetails(row, mode, button); };
            td.appendChild(button);
            if(mode === "jobs") {
                var remove = document.createElement("button"); remove.type = "button"; remove.className = "report-link-button"; remove.innerHTML = "&#10060; Delete"; remove.disabled = busy;
                remove.onclick = function () { deleteEntry(row); }; td.appendChild(document.createTextNode(" ")); td.appendChild(remove);
            }
            tr.appendChild(td); el("rows").appendChild(tr);
        });
        if (!list.length) { var empty = document.createElement("tr"), cell = document.createElement("td"); cell.colSpan = columns.length + 1; cell.textContent = mode === "exceptions" && !search ? "No warnings or rejected entries for this import." : "No records match this view."; empty.appendChild(cell); el("rows").appendChild(empty); }
        el("count").textContent = list.length + " records | Page " + page + " of " + pages;
        el("prev").disabled = page <= 1; el("next").disabled = page >= pages;
    }
    async function load() {
        setBusy(true); el("retry").hidden = true;
        try {
            await ByteForgePraxisAdmin.load(token); ByteForgePraxisAdmin.select("PraxisLeadImports");
            var data = await api("api/lead-imports"); jobs = data.jobs; maxBytes = data.maxFileBytes; timeoutSeconds = data.operationTimeoutSeconds || 900;
            el("configuration").textContent = "Caller ID: " + data.callerId + " | Record type: " + data.recordType + " | Maximum file size: " + Math.floor(maxBytes / 1048576) + " MB";
            el("page").hidden = false; document.getElementById("session-status").hidden = false; document.getElementById("sign-out-button").hidden = false;
            message("Choose a file to review, or open a saved import."); render();
        } catch (error) { message(error.message, true); el("retry").hidden = false; }
        finally { setBusy(false); }
    }
    async function open(id) {
        if (busy) return; setBusy(true); message("Loading saved import...");
        try { model = null; el("approve").checked = false; apply(await api("api/lead-imports/" + id + "/status")); el("mode").value = "exceptions"; el("search").value = ""; sort = "row"; direction = 1; page = 1; message(model.error || "Saved import loaded.", !!model.error); render(); el("review").scrollIntoView({ block: "center" }); }
        catch (error) { message(error.message, true); } finally { setBusy(false); }
    }
    function deleteEntry(row) {
        if(busy) return;
        ByteForgeModal.confirm("Delete the saved import entry for " + row.fileName + " / " + row.listName + "? Imported Praxis records, advisors and the BP list will be kept.", function () { removeEntry(row); }, null, "Delete saved import entry", "Delete", "Cancel");
    }
    async function removeEntry(row) {
        if(busy) return;
        setBusy(true); message("Deleting saved import entry...");
        try {
            await api("api/lead-imports/" + row.id,undefined,"DELETE"); jobs = jobs.filter(function (job) { return job.id !== row.id; });
            if(model && model.id === row.id) { model = null; el("review").hidden = true; el("detail").hidden = true; el("progress-panel").hidden = true; }
            render(); message("Saved import entry deleted. Imported records and the BP list were kept.");
        } catch(error) { message(error.message,true); } finally { setBusy(false); }
    }
    el("form").onsubmit = async function (event) {
        event.preventDefault(); if (busy) return; model = null; el("review").hidden = true; setBusy(true); startProgress("Reading selected file", "Preparing the selected file for upload."); message("Validating file and checking existing Praxis records...");
        try {
            var file = el("file").files[0]; if (!file || file.size > maxBytes) throw new Error("Choose a file within the configured size limit.");
            var base64 = await new Promise(function (resolve, reject) { var reader = new FileReader(); reader.onload = function () { resolve(String(reader.result).split(",")[1]); }; reader.onerror = function () { reject(new Error("Unable to read the file.")); }; reader.readAsDataURL(file); });
            var data = await progressApi("api/lead-imports/preview", { fileName: file.name, base64: base64, listName: el("list").value.trim() });
            model = null; apply(data); el("mode").value = "exceptions"; page = 1; render();
            await completeImport();
        } catch (error) { reportFailure("Import failed",error.message,0,0); } finally { setBusy(false); }
    };
    async function completeImport() {
        try {
            if (!model.validRows) throw new Error("No valid entries were found. See the rejected rows for details.");
            if (["Preview","MainFailed","MainImporting"].includes(model.status)) {
                message("Importing valid entries into Praxis. " + model.rejectedRows + " rejected rows will be skipped.");
                apply(await progressApi("api/lead-imports/" + model.id + "/save", { excludeRejected: true }));
                if (model.status !== "Pending") throw new Error(model.error || "Main import paused. Resume this saved import to continue.");
            }
            if (["Pending","Failed","Syncing"].includes(model.status)) {
                message("Sending entries to BP...");
                apply(await progressApi("api/lead-imports/" + model.id + "/sync", {}));
                if (model.status !== "Synced") throw new Error(model.error || "BP upload interrupted. Resume this saved import to retry.");
            }
            var text="Import complete: " + model.importedRows + " Main records and " + model.sentRows + " entries processed by BP. " + model.rejectedRows + " rejected " + (model.rejectedRows===1?"row":"rows") + " skipped; see Rejected rows for details.";
            finishProgress("Import complete","Main insertion and BP upload completed.",model.validRows,model.validRows,false); message(text);
        } catch (error) {
            reportFailure("Import interrupted",error.message,model.importedRows||0,model.validRows);
            try { apply(await api("api/lead-imports/"+model.id+"/status?includeRows=false")); } catch(ignored) { }
        }
    }
    el("save").onclick = el("sync").onclick = async function () {
        if(busy || !model) return; setBusy(true); startProgress("Resuming import","Continuing this saved import.");
        try { await completeImport(); } finally { setBusy(false); }
    };
    el("show-rejected").onclick = function () { showRejected(); };
    el("status").onclick = function () { if (model) open(model.id); };
    el("approve").onchange = function () { setBusy(false); };
    el("retry").onclick = el("refresh").onclick = load;
    ["mode", "size"].forEach(function (key) { el(key).onchange = function () { page = 1; render(); }; });
    el("search").oninput = function () { page = 1; render(); };
    el("prev").onclick = function () { page--; render(); }; el("next").onclick = function () { page++; render(); };
    el("detail-close").onclick = function () { ByteForgeModal.close(detailModal, restoreDetailFocus); };
    el("settings-button").onclick = function () { el("settings").hidden = !el("settings").hidden; this.setAttribute("aria-expanded", String(!el("settings").hidden)); };
    function signOut() {
        token = ""; model = null; jobs = []; el("page").hidden = true; ByteForgePraxisAdmin.clear();
        try { sessionStorage.removeItem("praxis.reportPortal.accessToken"); localStorage.removeItem("praxis.reportPortal.persistentAccessToken"); } catch (ignored) {}
        fetch("api/config-session", { method: "DELETE", credentials: "same-origin", keepalive: true }).catch(function () {}); ByteForgePraxisAuth.redirect();
    }
    document.getElementById("sign-out-button").onclick = function () { if (!busy) signOut(); };
    window.addEventListener("beforeunload", function (event) { if (busy) { event.preventDefault(); event.returnValue = ""; } });
    try { token = sessionStorage.getItem("praxis.reportPortal.accessToken") || localStorage.getItem("praxis.reportPortal.persistentAccessToken") || ""; } catch (ignored) {}
    if (window.ByteForgeAccount) window.ByteForgeAccount.attach(document.getElementById("session-status"), { getToken: function () { return token; }, onPasswordChanged: signOut });
    if (token) load(); else ByteForgePraxisAuth.redirect();
}());
