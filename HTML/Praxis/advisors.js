(function () {
    "use strict";
    var modal = null, busy = false, plan = null, firmMode = false, importMode = false;
    var token = "", rows = [], current = null, page = 1, sort = "advisorName", direction = 1;
    var fields = [
        ["advisorName", "Display name", "text", true], ["advisorFirm", "Firm", "text", true],
        ["advisorFirst", "First name", "text"], ["advisorLast", "Last name", "text"],
        ["advisorEmail", "Email", "email"], ["advisorPhone", "Office phone", "tel"],
        ["website", "Website", "url"], ["zoomLink", "Meeting link", "url"],
        ["phoneLink", "Scheduling link", "url"], ["advisorTimeZone", "Time zone", "text"]
    ];
    var form = document.getElementById("advisor-form");
    function el(id) { return document.getElementById("advisor-" + id); }
    fields.forEach(function (field) {
        var label = document.createElement("label"), input = document.createElement("input");
        label.textContent = field[1]; input.name = field[0]; input.type = field[2]; input.required = !!field[3];
        input.maxLength = ({advisorName:50,advisorFirm:255,advisorFirst:50,advisorLast:50,advisorEmail:100,advisorPhone:20,website:255,zoomLink:500,phoneLink:1000,advisorTimeZone:10})[field[0]];
        if (field[2] === "url") { input.pattern = "https?://.+"; input.title = "Enter a full http:// or https:// URL."; }
        label.appendChild(input); el("fields").appendChild(label);
    });
    function values() {
        var result = {};
        fields.forEach(function (field) { result[field[0]] = form.elements[field[0]].value.trim(); });
        ["active", "include1031"].forEach(function (key) { result[key] = form.elements[key].checked; });
        return result;
    }
    function changes() {
        var draft = values();
        return Object.keys(draft).filter(function (key) { return draft[key] !== (current[key] == null ? "" : current[key]); });
    }
    function leave() { if (busy) return false; return !current || !changes().length || window.confirm("Discard your unsaved preview changes?"); }
    function edit(row) {
        if (!leave()) return;
        current = row;
        fields.forEach(function (field) { form.elements[field[0]].value = row[field[0]] || ""; });
        ["active", "include1031"].forEach(function (key) { form.elements[key].checked = !!row[key]; });
        el("heading").textContent = row.id ? "Edit " + row.advisorName : "Add advisor";
        el("history-button").hidden = !row.id;
        el("save").textContent = row.id ? "Save and synchronize" : "Add advisor";
        el("modal-message").textContent = "";
        el("identity").textContent = row.id ? "Advisor ID: " + row.id : "A new Advisor ID will be assigned when saved.";
        el("editor").hidden = false; el("review").hidden = true; el("history").hidden = true; plan = null; firmMode = false; importMode = false;
        openModal(el("editor"), row.id ? "Edit advisor" : "Add advisor");
    }
    function cell(tr, value) { var td = document.createElement("td"); td.textContent = value; tr.appendChild(td); return td; }
    function render() {
        var search = el("search").value.trim().toLowerCase(), filter = el("filter").value;
        var list = rows.filter(function (row) {
            return (filter === "all" || row.active === (filter === "active")) &&
                [row.id, row.advisorName, row.advisorFirm, row.advisorEmail].join(" ").toLowerCase().includes(search);
        }).sort(function (a, b) { return String(a[sort] == null ? "" : a[sort]).localeCompare(String(b[sort] == null ? "" : b[sort]), undefined, { numeric: true, sensitivity: "base" }) * direction || a.id - b.id; });
        var size = Number(el("size").value), pages = Math.max(1, Math.ceil(list.length / size));
        page = Math.min(page, pages); el("rows").textContent = "";
        list.slice((page - 1) * size, page * size).forEach(function (row) {
            var tr = document.createElement("tr");
            cell(tr, row.advisorName); cell(tr, row.advisorFirm); cell(tr, row.advisorEmail);
            var status = document.createElement("span");
            status.className = "bf-advisor-status " + (row.active ? "bf-advisor-status-active" : "bf-advisor-status-inactive");
            status.textContent = row.active ? "Active" : "Inactive";
            cell(tr, "").appendChild(status);
            var button = document.createElement("button"); button.type = "button"; button.className = "report-link-button";
            button.innerHTML = "&#9999;&#65039; Edit"; button.setAttribute("aria-label", "Edit " + row.advisorName);
            button.onclick = function () { edit(row); }; cell(tr, "").appendChild(button); el("rows").appendChild(tr);
        });
        if (!list.length) { var empty = document.createElement("tr"); cell(empty, "No advisors match your search.").colSpan = 5; el("rows").appendChild(empty); }
        el("count").textContent = list.length + " advisors | Page " + page + " of " + pages;
        el("prev").disabled = page <= 1; el("next").disabled = page >= pages;
        document.querySelectorAll("[data-sort]").forEach(function (button) {
            button.parentElement.setAttribute("aria-sort", button.dataset.sort === sort ? (direction === 1 ? "ascending" : "descending") : "none");
            button.textContent = ({advisorName:"Advisor",advisorFirm:"Firm",advisorEmail:"Email",active:"Status"})[button.dataset.sort] + (button.dataset.sort === sort ? (direction === 1 ? " ▲" : " ▼") : "");
        });
    }
    function message(text) { el("message").textContent = text; }
    function load() {
        if (!leave()) return;
        current = null; el("editor").hidden = true; el("page").hidden = true; el("retry").hidden = true;
        el("refresh").disabled = true; el("page").setAttribute("aria-busy", "true"); message("Loading advisors...");
        ByteForgePraxisAdmin.load(token).then(function (pages) {
            if (!pages.some(function (item) { return item.key === "PraxisAdvisorEditor"; })) throw new Error("Your role does not grant access to Advisors.");
            return fetch("api/advisors", { headers: { Authorization: "Bearer " + token, Accept: "application/json" }, cache: "no-store" });
        }).then(function (response) {
            if (response.status === 401) { signOut(); throw new Error("Your session expired. Please sign in again."); }
            if (!response.ok) throw new Error(response.status === 403 ? "Your role does not grant access to Advisors." : "Unable to load advisors. Please retry.");
            return response.json();
        }).then(function (data) {
            rows = data.advisors || []; renderJobs(data.jobs || []); page = 1; render(); el("page").hidden = false; message("");
            if(window.ByteForgePraxisAdvisorLists) window.ByteForgePraxisAdvisorLists.attach(token,el("configure-lists"));
            document.getElementById("session-status").hidden = false; document.getElementById("sign-out-button").hidden = false;
        }).catch(function (error) { message(error.message); el("retry").hidden = false; })
            .finally(function () { el("refresh").disabled = false; el("page").setAttribute("aria-busy", "false"); });
    }
    form.addEventListener("submit", function (event) {
        event.preventDefault(); var draft = values(), changed = changes(); el("changes").textContent = "";
        changed.forEach(function (key) {
            var tr = document.createElement("tr"), field = fields.find(function (item) { return item[0] === key; });
            function display(value) { return typeof value === "boolean" ? (value ? "Yes" : "No") : value || "(blank)"; }
            cell(tr, field ? field[1] : key === "active" ? "Active" : "1031 routing"); cell(tr, display(current[key])); cell(tr, display(draft[key])); el("changes").appendChild(tr);
        });
        if (!changed.length) { var tr = document.createElement("tr"); cell(tr, "No changes to review.").colSpan = 3; el("changes").appendChild(tr); }
        el("review").hidden = false;
        preview({ advisor: Object.assign({ id: current.id, revisionId: current.revisionId }, draft) });
    });
    form.addEventListener("input", function () { el("review").hidden = true; plan = null; });
    el("reset").onclick = function () { var row = current; if (leave()) { current = null; edit(row); } };
    el("close").onclick = closeModal;
    el("add").onclick = function(){ if(busy || modal)return;var row={id:0,revisionId:0,active:true,include1031:false};fields.forEach(function(f){row[f[0]]='';});edit(row); };
    ["search", "filter", "size"].forEach(function (key) { el(key).addEventListener(key === "search" ? "input" : "change", function () { page = 1; render(); }); });
    document.querySelectorAll("[data-sort]").forEach(function (button) { button.onclick = function () { direction = sort === button.dataset.sort ? -direction : 1; sort = button.dataset.sort; page = 1; render(); }; });
    el("prev").onclick = function () { page--; render(); }; el("next").onclick = function () { page++; render(); };
    el("settings-button").onclick = function () { el("settings").hidden = !el("settings").hidden; this.setAttribute("aria-expanded", String(!el("settings").hidden)); };
    el("refresh").onclick = load; el("retry").onclick = load;

    function openModal(content, title) {
        if (modal) return;
        content.hidden = false;
        modal = ByteForgeModal.show('', {type:'none',title:title,contentElement:content,
            modalClass:'bf-advisor-modal',bodyClass:'bf-advisors',maxWidth:'980px',hideFooter:true,
            showClose:false,closeOnOverlay:false,initialFocusSelector:'input, select',
            onClose:function(){content.hidden=true;modal=null;current=null;plan=null;el('search').focus();}});
        setBusy(false);
        modal.addEventListener('keydown', function(event) {
            if (event.key === 'Escape') { event.preventDefault(); event.stopImmediatePropagation(); closeModal(); }
            if (event.key === 'Tab') {
                var focusable=Array.from(modal.querySelectorAll('button,input,select,a[href]')).filter(function(e){return !e.disabled && e.getClientRects().length;});
                var first=focusable[0],last=focusable[focusable.length-1];
                if(event.shiftKey && document.activeElement===first){event.preventDefault();last.focus();}
                else if(!event.shiftKey && document.activeElement===last){event.preventDefault();first.focus();}
            }
        },true);
    }
    function closeModal() {
        if(busy || !leave()) return;
        if(importMode && el('import-file').files.length && !window.confirm('Discard this import preview?')) return;
        if(firmMode && el('new-firm').value && !window.confirm('Discard this firm rename?')) return;
        ByteForgeModal.close(modal, function(){current=null;plan=null;modal=null;el('editor').hidden=true;el('firm-editor').hidden=true;el('import-editor').hidden=true;el('search').focus();});
    }
    function setBusy(value) {
        busy=value;
        if(modal){modal.setAttribute('aria-busy',String(value));modal.querySelectorAll('button,input,select').forEach(function(e){if(value){e._advisorDisabled=e.disabled;e.disabled=true;}else if(e._advisorDisabled!==undefined){e.disabled=e._advisorDisabled;delete e._advisorDisabled;}});}
        el('save').disabled=value || !plan; el('firm-save').disabled=value || !plan;el('import-save').disabled=value || !plan;
    }
    function modalMessage(text) { if(!modal){message(text);return;} el(importMode?'import-message':firmMode?'firm-message':'modal-message').textContent=text; }
    async function api(url,body) {
        var options={headers:{Authorization:'Bearer '+token,Accept:'application/json'},cache:'no-store'};
        if(body!==undefined){options.method='POST';options.headers['Content-Type']='application/json';options.body=JSON.stringify(body);}
        var response=await fetch(url,options), data=await response.json().catch(function(){return {};});
        if(response.status===401){signOut();throw new Error('Your session expired.');}
        if(!response.ok) throw new Error(data.message || data.Message || 'The request failed. Your changes have not been confirmed.');
        return data;
    }
    function impact(model,target) {
        var root=el(target);root.textContent='';
        var description=document.createElement('p');description.textContent=model.after.length+' advisor(s); '+model.total+' BP record(s) across '+model.lists.length+' configured lists.';root.appendChild(description);
        var table=document.createElement('table');table.setAttribute('aria-label','Affected advisors');
        var head=document.createElement('tr');['Advisor','Previous firm','New firm'].forEach(function(t){var th=document.createElement('th');th.textContent=t;head.appendChild(th);});table.appendChild(head);
        model.after.forEach(function(a,i){var tr=document.createElement('tr');cell(tr,a.advisorName);cell(tr,(model.before.find(function(b){return b.id===a.id;})||{}).advisorFirm||'(new advisor)');cell(tr,a.advisorFirm);table.appendChild(tr);});root.appendChild(table);
        model.lists.forEach(function(list){var p=document.createElement('p');p.textContent=list.name+': '+list.verified+' / '+list.total+' records acknowledged'+(list.unassignedRecords?' ('+list.unassignedRecords+' records have no usable advisor assignment)':'')+(list.skippedReason?' — '+list.skippedReason:'');root.appendChild(p);});
    }
    async function preview(body) {
        plan=null;setBusy(true);modalMessage(current && !current.id ? 'Checking the new advisor...' : 'Checking advisors and affected BP lists...');
        try{plan=await api('api/advisors/preview',body);impact(plan,firmMode?'firm-impact':'list-impact');el(firmMode?'firm-review':'review').hidden=false;modalMessage('Review the affected advisors and lists, then save.');}
        catch(error){modalMessage(error.message);}
        finally{setBusy(false);}
    }
    async function synchronize(id) {
        var result;
        do {
            result=await api('api/advisors/'+id+'/sync',{});
            modalMessage(result.verified+' / '+result.total+' BP records acknowledged. '+(result.error||''));
            if(result.status==='Failed') throw new Error('Advisor changes are saved. BP synchronization needs attention: '+result.error);
        }while(result.status==='Pending');
        return result;
    }
    async function savePlan() {
        if(!plan || busy)return; setBusy(true);modalMessage('Saving advisor revisions...');
        try {
            var saved;
            try { saved=await api('api/advisors/'+plan.id+'/save',{}); }
            catch(error) { saved=await api('api/advisors/'+plan.id+'/status'); if(saved.status==='Preview') throw error; }
            if(saved.status==='Pending'||saved.status==='Failed') await synchronize(plan.id);
            current=null;plan=null;modalMessage('Saved. All requested BP updates were verified.');
            var closing=modal;modal=null;ByteForgeModal.close(closing,function(){el('editor').hidden=true;el('firm-editor').hidden=true;el('import-editor').hidden=true;busy=false;load();});
        } catch(error) { modalMessage(error.message);setBusy(false); }
    }
    function renderJobs(jobs) {
        var root=el('jobs');root.textContent='';
        jobs.forEach(function(job){var p=document.createElement('p'),button=document.createElement('button');
            p.textContent='Advisor update '+job.id+': '+job.status+'. '+(job.error||'');
            button.type='button';button.className='report-link-button';button.innerHTML='&#128260; Resume synchronization';
            button.onclick=async function(){if(busy)return;busy=true;button.disabled=true;try{await synchronize(job.id);message('BP synchronization completed.');}catch(error){message(error.message);}finally{busy=false;button.disabled=false;load();}};
            p.appendChild(button);root.appendChild(p);});
    }
    el('import-save').onclick=savePlan;el('save').onclick=savePlan;el('firm-save').onclick=savePlan;
    el('firm-open').onclick=function(){
        if(busy||modal)return;current=null;plan=null;firmMode=true;importMode=false;el('firm-review').hidden=true;el('firm-message').textContent='';el('new-firm').value='';
        var select=el('old-firm');select.textContent='';Array.from(new Set(rows.map(function(r){return r.advisorFirm;}))).sort().forEach(function(firm){var option=document.createElement('option');option.value=option.textContent=firm;select.appendChild(option);});
        openModal(el('firm-editor'),'Rename firm');
    };
    el('firm-close').onclick=closeModal;
    el('firm-form').onsubmit=function(event){event.preventDefault();preview({oldFirm:el('old-firm').value,newFirm:el('new-firm').value.trim()});};
    el('firm-form').oninput=function(){plan=null;el('firm-review').hidden=true;};
    el('import-open').onclick=function(){
        if(busy||modal)return;current=null;plan=null;firmMode=false;importMode=true;
        el('import-form').reset();el('import-review').hidden=true;el('import-errors').textContent='';el('import-message').textContent='';
        openModal(el('import-editor'),'Import advisors');
    };
    el('import-close').onclick=closeModal;
    el('import-file').onchange=function(){plan=null;el('import-review').hidden=true;el('import-errors').textContent='';};
    el('template').onclick=function(){
        var header=['Id','RevisionId'].concat(fields.map(function(f){return f[0][0].toUpperCase()+f[0].slice(1);})).concat(['Active','Include1031']).join(',');
        var url=URL.createObjectURL(new Blob([header+'\r\n'],{type:'text/csv;charset=utf-8'})),a=document.createElement('a');a.href=url;a.download='advisors-template.csv';a.click();setTimeout(function(){URL.revokeObjectURL(url);},1000);
    };
    el('import-form').onsubmit=async function(event){
        event.preventDefault();if(busy)return;plan=null;setBusy(true);el('import-review').hidden=true;el('import-errors').textContent='';modalMessage('Validating import and checking affected BP lists...');
        try{
            var file=el('import-file').files[0];
            var base64=await new Promise(function(resolve,reject){var reader=new FileReader();reader.onload=function(){resolve(String(reader.result).split(',')[1]);};reader.onerror=function(){reject(new Error('Unable to read the selected file.'));};reader.readAsDataURL(file);});
            var result=await api('api/advisors/import-preview',{fileName:file.name,base64:base64});
            if(result.errors){el('import-errors').textContent=result.errors.join('\n');modalMessage('Nothing saved. Correct the file and review again.');return;}
            plan=result;impact(plan,'import-impact');renderImportChanges(plan);el('import-review').hidden=false;modalMessage('Review every proposed change before saving. All advisor rows will be saved together.');
        }catch(error){modalMessage(error.message);}finally{setBusy(false);}
    };
    function renderImportChanges(model){
        var root=el('import-impact');root.textContent='';
        var summary=document.createElement('p');summary.textContent=model.after.filter(function(a){return !a.id;}).length+' additions, '+model.before.length+' existing advisors; '+model.total+' BP records across '+model.lists.length+' lists.';root.appendChild(summary);
        model.lists.forEach(function(list){var p=document.createElement('p');p.textContent=list.name+': '+list.total+' BP records'+(list.unassignedRecords?' ('+list.unassignedRecords+' without a usable assignment)':'');root.appendChild(p);});
        var search=document.createElement('input');search.type='search';search.placeholder='Search proposed changes';search.setAttribute('aria-label','Search proposed changes');root.appendChild(search);
        var table=document.createElement('table'),thead=document.createElement('thead'),head=document.createElement('tr'),body=document.createElement('tbody');
        var entries=[];model.after.forEach(function(a){var before=model.before.find(function(b){return b.id===a.id;});fields.map(function(f){return f[0];}).concat(['active','include1031']).forEach(function(key){if(!before || (before[key]||'')!==(a[key]||''))entries.push([a.advisorName,a.id?'Update #'+a.id:'Add',(fields.find(function(f){return f[0]===key;})||[key,key==='active'?'Active':'1031 routing'])[1],before?String(before[key]==null?'':before[key]):'(new)',String(a[key]==null?'':a[key])]);});});
        var sortColumn=0,order=1,index=0;
        ['Advisor','Action','Field','Current','Proposed'].forEach(function(label,n){var th=document.createElement('th'),button=document.createElement('button');button.type='button';button.className='report-link-button';button.textContent=label;button.dataset.label=label;button.onclick=function(){order=sortColumn===n?-order:1;sortColumn=n;index=0;draw();};th.appendChild(button);head.appendChild(th);});thead.appendChild(head);table.appendChild(thead);table.appendChild(body);var scroll=document.createElement('div');scroll.className='bf-advisor-scroll';scroll.appendChild(table);root.appendChild(scroll);
        var previous=document.createElement('button'),next=document.createElement('button'),count=document.createElement('span');previous.type=next.type='button';previous.textContent='Previous';next.textContent='Next';root.appendChild(previous);root.appendChild(count);root.appendChild(next);
        function draw(){var found=entries.filter(function(row){return row.join(' ').toLowerCase().includes(search.value.toLowerCase());}).sort(function(a,b){return a[sortColumn].localeCompare(b[sortColumn])*order;});var pages=Math.max(1,Math.ceil(found.length/25));index=Math.max(0,Math.min(index,pages-1));body.textContent='';found.slice(index*25,index*25+25).forEach(function(row){var tr=document.createElement('tr');row.forEach(function(v){cell(tr,v);});body.appendChild(tr);});Array.from(head.children).forEach(function(th,n){th.setAttribute('aria-sort',n===sortColumn?(order===1?'ascending':'descending'):'none');th.firstChild.textContent=th.firstChild.dataset.label+(n===sortColumn?(order===1?' ▲':' ▼'):'');});count.textContent=' '+found.length+' field changes | Page '+(index+1)+' of '+pages+' ';previous.disabled=index===0;next.disabled=index>=pages-1;}
        search.oninput=function(){index=0;draw();};previous.onclick=function(){index--;draw();};next.onclick=function(){index++;draw();};draw();
    }
    el('history-button').onclick=async function(){
        setBusy(true);modalMessage('Loading advisor history...');
        try{var history=await api('api/advisors/'+current.id+'/history');el('history').textContent=history.map(function(r){return 'Revision '+r.revisionId+': '+r.advisorName+' / '+r.advisorFirm+(r.createdAtUtc?' | '+r.createdAtUtc+' UTC':'')+(r.changedBy?' | '+r.changedBy:'');}).join('\n');el('history').hidden=false;modalMessage('');}
        catch(error){modalMessage(error.message);}finally{setBusy(false);}
    };

    function signOut() {
        if (modal) ByteForgeModal.close(modal); modal = null;
        current = null; token = ""; rows = []; el("page").hidden = true; ByteForgePraxisAdmin.clear();
        try { sessionStorage.removeItem("praxis.reportPortal.accessToken"); localStorage.removeItem("praxis.reportPortal.persistentAccessToken"); } catch (ignored) { }
        fetch("api/config-session", { method: "DELETE", credentials: "same-origin", keepalive: true }).catch(function () {});
        ByteForgePraxisAuth.redirect();
    }
    document.getElementById("sign-out-button").onclick = function () { if (leave()) signOut(); };
    window.addEventListener("beforeunload", function (event) { if (busy || (current && changes().length) || (firmMode && modal && el("new-firm").value) || (importMode && modal && el("import-file").files.length)) { event.preventDefault(); event.returnValue = ""; } });
    try { token = sessionStorage.getItem("praxis.reportPortal.accessToken") || localStorage.getItem("praxis.reportPortal.persistentAccessToken") || ""; } catch (ignored) { }
    if (window.ByteForgeAccount) window.ByteForgeAccount.attach(document.getElementById("session-status"), { getToken: function () { return token; }, onPasswordChanged: signOut });
    if (token) load(); else ByteForgePraxisAuth.redirect();
}());
