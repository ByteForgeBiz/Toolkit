/* Shared direct HTTPS uploader. Small chunks, explicit conflicts, and exact server receipts; no whole-file buffering. */
(function () {
    'use strict';
    const uuid = () => crypto.randomUUID();
    const bytes = n => n.toLocaleString() + ' bytes';
    function td(row, value) { const cell = row.insertCell(); cell.textContent = value; return cell; }
    function button(html, action) { const b = document.createElement('button'); b.type = 'button'; b.innerHTML = html; b.addEventListener('click', action); return b; }
    async function digest(data) { return Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256', data)), b => b.toString(16).padStart(2,'0')).join('').toUpperCase(); }
    function base64(buffer) { let text = ''; const a = new Uint8Array(buffer); for (let i = 0; i < a.length; i += 32768) text += String.fromCharCode.apply(null, a.subarray(i, i + 32768)); return btoa(text); }
    const fileDisplay = window.ByteForgeFileDisplay = {
        size(value) { if (!value) return '0 B'; const units=['B','K','M','G','T','P']; const index=Math.min(units.length-1,Math.floor(Math.log(value)/Math.log(1024))); return (value/Math.pow(1024,index)).toLocaleString(undefined,{maximumFractionDigits:index?2:0})+' '+units[index]; },
        date(value) { return !value || value.startsWith('0001') ? '' : new Date(value).toLocaleString(); },
        icon(entry) {
            const ext=entry.Name.includes('.')?entry.Name.split('.').pop().toLowerCase():'';
            let entity='&#128196;', label='File';
            if(entry.Folder){entity=/^[A-Za-z]:[\\/]?$/.test(entry.Name)?'&#128189;':'&#128193;';label=/^[A-Za-z]:[\\/]?$/.test(entry.Name)?'Drive':'Folder';}
            else if(['exe','msi','dll','bat','cmd','ps1'].includes(ext)){entity='&#128187;';label='Application or executable';}
            else if(['zip','7z','rar','gz','tar'].includes(ext)){entity='&#128230;';label='Archive';}
            else if(['xlsx','xls','xlsm','csv','tsv'].includes(ext)){entity='&#128202;';label='Spreadsheet';}
            else if(['jpg','jpeg','png','gif','webp','bmp','svg','ico'].includes(ext)){entity='&#128444;&#65039;';label='Image';}
            else if(['mp3','wav','flac','m4a','ogg'].includes(ext)){entity='&#127925;';label='Audio';}
            else if(['mp4','avi','mov','mkv','webm'].includes(ext)){entity='&#127916;';label='Video';}
            else if(ext==='pdf'){entity='&#128213;';label='PDF';}
            const span=document.createElement('span');span.className='bf-file-icon';span.innerHTML=entity+' ';span.setAttribute('aria-hidden','true');span.title=label;return span;
        }
    };
    class ByteForgeDirectUpload {
        constructor(root, options) {
            this.root = root; this.api = options.api; this.getDestination = options.destination; this.office = !!options.office;
            this.canUpload = options.canUpload !== false; this.files = new Map(); this.plan = null; this.running = false; this.stop = false;
            this.path = ''; this.snapshot = ''; this.page = 1; this.sort = 'name'; this.descending = false;
            this.historyPage = 1; this.historySort = 'created'; this.historyDescending = true;
            root.classList.add('bf-direct');
            root.innerHTML = `<section class="du-browser" ${this.office ? '' : 'hidden'}>
<div class="du-actions"><h2>Office browser</h2><button type="button" data-a="folders" aria-expanded="false">Folders</button><button type="button" data-a="up">&#8617;&#65039; Up</button><button type="button" data-a="drives">Drives</button><button type="button" data-a="refresh">&#128260; Refresh</button><button type="button" data-a="list-options">&#9881;&#65039; Settings</button></div>
<form class="du-address"><label>Office folder<input name="office-path" placeholder="C:\\ or \\\\server\\share" spellcheck="false"></label><button>Open folder</button></form><nav class="du-crumbs" aria-label="Office breadcrumbs"></nav>
<div class="du-list-options" hidden><label>Files per page<select name="file-size"><option>20</option><option>50</option><option>100</option></select></label></div><label>Search this folder<input name="file-search" type="search"></label>
<div class="du-file-browser"><nav class="du-tree" aria-label="Office folder tree"></nav><div class="du-scroll"><table class="du-files"><thead><tr><th><button data-sort="name">Name</button></th><th class="du-secondary"><button data-sort="type">Type</button></th><th class="du-size"><button data-sort="size">Size</button></th><th class="du-secondary"><button data-sort="created">Created</button></th><th class="du-secondary"><button data-sort="modified">Modified</button></th></tr></thead><tbody></tbody></table></div></div>
<div class="du-actions du-pagination"><button data-a="prev" type="button">Previous</button><span class="du-summary"></span><button data-a="next" type="button">Next</button></div></section>
<section class="du-upload"><div class="du-actions"><h2>Direct upload to office</h2><button type="button" class="du-primary" data-a="choose">&#128228; Upload files</button></div><p class="du-destination"></p><div class="du-drop" tabindex="0" role="button" aria-label="Choose or drop files to upload">Drop files here or tap Upload files. File data goes directly to the office over HTTPS.</div><input class="du-picker" type="file" multiple hidden>
<p class="du-message" role="status" aria-live="polite"></p><div class="du-active" hidden><p class="du-active-summary"></p><progress max="100" value="0" aria-label="Direct upload progress"></progress><p class="du-progress-text" aria-live="polite"></p><div class="du-actions"><button data-a="pause" type="button">Pause</button><button data-a="resume" type="button">Resume / retry</button><button data-a="reselect" type="button">Select same files to resume</button><button data-a="review" type="button">Review conflicts</button><button data-a="cancel" type="button">&#10006;&#65039; Cancel upload</button></div></div></section>
<section class="du-history"><div class="du-actions"><h2>Direct upload history</h2><label>Search uploads<input name="history-search" type="search"></label><button type="button" data-a="history-refresh">&#128260; Refresh</button><button type="button" data-a="history-options">&#9881;&#65039; Settings</button></div><div class="du-history-options" hidden><label>Uploads per page<select name="history-size"><option>20</option><option>50</option><option>100</option></select></label></div><div class="du-scroll"><table><thead><tr><th><button data-history-sort="created">Created</button></th><th><button data-history-sort="path">Destination</button></th><th><button data-history-sort="state">State</button></th><th>Files</th><th>Actions</th></tr></thead><tbody class="du-history-rows"></tbody></table></div><div class="du-actions du-pagination"><button type="button" data-a="history-prev">Previous</button><span class="du-history-summary"></span><button type="button" data-a="history-next">Next</button></div></section>
<dialog class="du-review"><h2>Review direct upload</h2><p class="du-review-summary"></p><p class="du-dialog-error" role="alert"></p><label>Apply to all conflicts<select class="du-all"><option value="">Choose individually</option><option value="overwrite">Overwrite files</option><option value="keepBoth">Keep both</option><option value="skip">Skip</option></select></label><div class="du-scroll"><table><thead><tr><th>Filename</th><th>Destination / conflict</th><th>Decision</th></tr></thead><tbody class="du-conflicts"></tbody></table></div><div class="du-actions"><button type="button" class="du-primary" data-a="confirm">&#9989; Start upload</button><button type="button" data-a="close-review">&#10006;&#65039; Close</button></div></dialog>`;
            this.q('.du-picker').addEventListener('change', e => this.run(async () => {
                const selected = Array.from(e.target.files); e.target.value = '';
                if (this.reselect && this.plan) { this.matchFiles(selected); this.reselect = false; if (this.plan.State === 'review') this.showReview(); else await this.upload(); }
                else await this.selectFiles(selected);
            }));
            this.q('.du-drop').addEventListener('click', () => this.choose());
            this.q('.du-drop').addEventListener('keydown', e => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); this.choose(); } });
            const zone = this.office ? this.q('.du-browser') : this.q('.du-drop'); this.bindDrop(zone); if (this.office) this.bindDrop(this.q('.du-drop'));
            this.q('.du-address').onsubmit = e => { e.preventDefault(); this.run(() => this.open(this.q('[name=office-path]').value)); };
            this.root.querySelectorAll('[data-a]').forEach(b => b.addEventListener('click', () => {
                const action = b.dataset.a;
                if (action === 'choose') { this.reselect = false; this.choose(); return; }
                if (action === 'reselect') { this.reselect = true; this.q('.du-picker').click(); return; }
                this.run(() => this.action(action));
            }));
            this.root.querySelectorAll('[data-sort]').forEach(b => b.onclick = () => this.run(async () => { this.descending = this.sort === b.dataset.sort ? !this.descending : false; this.sort = b.dataset.sort; this.page = 1; await this.list(); }));
            this.root.querySelectorAll('[data-history-sort]').forEach(b => b.onclick = () => this.run(async () => { this.historyDescending = this.historySort === b.dataset.historySort ? !this.historyDescending : false; this.historySort = b.dataset.historySort; this.historyPage = 1; await this.history(); }));
            this.q('[name=file-search]').oninput = () => { clearTimeout(this.searchTimer); this.searchTimer = setTimeout(() => this.run(async () => { this.page = 1; await this.list(); }), 250); };
            this.q('[name=file-size]').onchange = () => this.run(async () => { this.page = 1; await this.list(); });
            this.q('[name=history-search]').oninput = () => { clearTimeout(this.historyTimer); this.historyTimer = setTimeout(() => this.run(async () => { this.historyPage = 1; await this.history(); }), 250); };
            this.q('[name=history-size]').onchange = () => this.run(async () => { this.historyPage = 1; await this.history(); });
            this.q('.du-all').onchange = () => this.root.querySelectorAll('[data-choice]').forEach(s => { if (Array.from(s.options).some(o => o.value === this.q('.du-all').value)) s.value = this.q('.du-all').value; });
            this.q('.du-review').addEventListener('cancel', () => this.message('Review saved. Open it from upload history to continue.'));
            this.q('[data-a=choose]').disabled = !this.canUpload;
            this.destination();
        }
        q(selector) { return this.root.querySelector(selector); }
        async run(action) { try { return await action(); } catch (e) { this.message(e.message, true); } }
        message(text, error = false) { this.q('.du-message').textContent = text; this.q('.du-message').classList.toggle('du-error', error); if (this.q('.du-review').open) this.q('.du-dialog-error').textContent = error ? text : ''; }
        destination() { const path = this.office ? this.path : this.getDestination(); this.q('.du-destination').textContent = path ? 'Destination: ' + path : 'Open an office destination folder first.'; return path; }
        choose() { if (!this.canUpload) return; if (this.running) return this.message('Pause or finish the current upload first.', true); if (!this.destination() && !this.reselect) return this.message('Open an office destination folder first.', true); this.q('.du-picker').click(); }
        bindDrop(zone) {
            zone.addEventListener('dragover', e => { if (Array.from(e.dataTransfer.types).includes('Files')) { e.preventDefault(); e.stopPropagation(); zone.classList.add('du-drag'); } });
            zone.addEventListener('dragleave', e => { if (!zone.contains(e.relatedTarget)) zone.classList.remove('du-drag'); });
            zone.addEventListener('drop', e => {
                if (!Array.from(e.dataTransfer.types).includes('Files')) return;
                e.preventDefault(); e.stopPropagation(); zone.classList.remove('du-drag');
                if (Array.from(e.dataTransfer.items || []).some(i => i.webkitGetAsEntry?.()?.isDirectory)) return this.message('Drop files rather than folders. Select the files inside the folder.', true);
                this.run(() => this.selectFiles(Array.from(e.dataTransfer.files)));
            });
        }
        async selectFiles(files, path) {
            if (!this.canUpload || this.running) throw new Error('Finish or pause the current upload first.');
            if (!files.length) return;
            if (files.length > 100) throw new Error('Select up to 100 files per direct upload.');
            path = path || this.destination(); if (!path) throw new Error('Open an office destination folder first.');
            this.root.hidden = false; this.files.clear(); const metadata = files.map(f => { const id = uuid(); this.files.set(id, f); return { Id: id, Name: f.name, Bytes: f.size }; });
            this.message('Checking names, destination permissions and space before uploading...');
            this.plan = await this.api('prepare', { Id: uuid(), Path: path, Files: metadata }); this.showActive(); this.showReview(); await this.history();
        }
        matchFiles(selected) {
            this.files.clear(); const available = selected.slice();
            for (const item of this.plan.Items) { if (item.State === 'completed' || item.State === 'skipped') continue; const index = available.findIndex(f => f.name === item.Source.Name && f.size === item.Source.Bytes); if (index >= 0) this.files.set(item.Source.Id, available.splice(index, 1)[0]); }
        }
        showActive() {
            if (!this.plan) return; this.q('.du-active').hidden = false;
            const done = this.plan.Items.filter(i => i.State === 'completed' || i.State === 'skipped').length;
            const total = this.plan.Items.reduce((a,i) => a + i.Source.Bytes,0), sent = this.plan.Items.reduce((a,i) => a + (i.State === 'skipped' ? i.Source.Bytes : i.Received),0);
            this.q('.du-active-summary').textContent = this.plan.Path + ' | ' + done + ' / ' + this.plan.Items.length + ' files | ' + this.plan.State;
            this.q('progress').value = total ? sent * 100 / total : done === this.plan.Items.length ? 100 : 0;
            this.q('.du-progress-text').textContent = bytes(sent) + ' / ' + bytes(total);
            this.q('[data-a=pause]').disabled = !this.running; this.q('[data-a=resume]').disabled = this.running || ['completed','cancelled','expired','review','needs-decision'].includes(this.plan.State);
            this.q('[data-a=review]').disabled = this.running || !['review','needs-decision'].includes(this.plan.State);
            this.q('[data-a=reselect]').disabled = this.running || ['completed','cancelled','expired'].includes(this.plan.State);
            this.q('[data-a=cancel]').disabled = ['completed','cancelled','expired'].includes(this.plan.State);
            this.q('.du-active .du-actions').hidden = ['completed','cancelled','expired'].includes(this.plan.State);
        }
        showReview() {
            this.q('.du-all').value = ''; this.q('.du-dialog-error').textContent = ''; const body = this.q('.du-conflicts'); body.replaceChildren();
            this.q('.du-review-summary').textContent = this.plan.Items.length + ' files. Resolve conflicts before sending file data. ' + (this.plan.Revision > 1 ? 'Already uploaded data is retained; completed files stay delivered.' : 'Keep both uses (1), (2), and so on.');
            // This finite selection is a conflict-decision form rather than a browsable server data list.
            for (const item of this.plan.Items) {
                if (item.State === 'completed' || item.State === 'skipped') continue;
                const row = body.insertRow(); td(row,item.Source.Name); td(row,item.Target + (item.Reserved ? ' (reserved)' : item.Conflict ? ' (already exists)' : ' (new)'));
                const c = td(row,item.Conflict ? '' : 'Copy'); if (!item.Conflict) continue;
                const select = document.createElement('select'); select.dataset.choice = item.Source.Id; select.setAttribute('aria-label','Conflict decision for ' + item.Source.Name);
                for (const [value,label] of [['','Choose...'],['overwrite','Overwrite'],['keepBoth','Keep both'],['skip','Skip']]) { if (value === 'overwrite' && (item.Reserved || item.FolderCollision)) continue; select.add(new Option(label,value)); } c.append(select);
            }
            if (!this.q('.du-review').open) this.q('.du-review').showModal();
        }
        async upload() {
            if (this.running || !this.plan) return;
            this.running = true; this.stop = false; this.showActive();
            try {
                this.plan = await this.api('status',{Id:this.plan.Id});
                for (const id of this.plan.Items.map(i => i.Source.Id)) {
                    const item = this.plan.Items.find(i => i.Source.Id === id);
                    if (this.stop) break;
                    if (item.State === 'completed' || item.State === 'skipped') continue;
                    const file = this.files.get(item.Source.Id);
                    if (item.Received < item.Source.Bytes && !file) throw new Error('Select the same files to resume this upload.');
                    // Verify every accepted prefix chunk on re-selection so different same-sized files cannot be mixed.
                    if (file) for (const chunk of item.Chunks) { if (this.stop) break; this.message('Verifying previous data: ' + file.name); const buffer = await file.slice(chunk.Offset,chunk.Offset+chunk.Length).arrayBuffer(); if (await digest(buffer) !== chunk.Hash) throw new Error('Selected file differs from the accepted upload: ' + file.name + '. Cancel and start a new upload.'); }
                    while (!this.stop && item.Received < item.Source.Bytes) {
                        const offset = item.Received; const buffer = await file.slice(offset,Math.min(offset+393216,file.size)).arrayBuffer();
                        const hash = await digest(buffer); this.message('Uploading ' + item.Source.Name + '. Keep this page open.');
                        const result = await this.api('chunk',{Id:this.plan.Id,ItemId:item.Source.Id,Offset:offset,Hash:hash,Data:base64(buffer)});
                        item.Received = result.Received; this.showActive();
                    }
                    if (this.stop) break;
                    this.message('Verifying and publishing ' + item.Source.Name + '...');
                    this.plan = await this.api('commit',{Id:this.plan.Id,ItemId:item.Source.Id}); this.showActive();
                }
                if (!this.stop) this.message('Direct upload completed. No OneDrive staging was used.'); else this.message('Paused. Resume here, or reselect the same files after reopening this page.');
            } catch (e) {
                try { this.plan = await this.api('status',{Id:this.plan.Id}); } catch {}
                this.message(e.message + (this.plan.State === 'needs-decision' ? ' Review the new destination conflicts.' : ' Resume / retry reconciles accepted chunks.'),true);
            } finally { this.running = false; this.showActive(); await this.run(() => this.history()); if (this.office) { this.snapshot = ''; await this.run(() => this.list()); } }
        }
        async action(action) {
            if (action === 'pause') { this.stop = true; return; }
            if (action === 'resume') return this.upload();
            if (action === 'review') { if (this.plan.State !== 'review') this.plan = await this.api('review',{Id:this.plan.Id}); this.showReview(); return; }
            if (action === 'cancel') { this.stop = true; this.plan = await this.api('cancel',{Id:this.plan.Id}); this.showActive(); await this.history(); return; }
            if (action === 'close-review') { this.q('.du-review').close(); this.message('Review saved. Reopen it from upload history.'); return; }
            if (action === 'confirm') {
                const choices = {}; this.root.querySelectorAll('[data-choice]').forEach(s => choices[s.dataset.choice] = s.value);
                const b = this.q('[data-a=confirm]'); b.disabled = true;
                try { this.plan = await this.api('confirm',{Id:this.plan.Id,Choices:choices}); this.q('.du-review').close(); await this.upload(); } finally { b.disabled = false; } return;
            }
            if (action === 'folders') { this.q('.du-tree').classList.toggle('du-tree-open'); this.q('[data-a=folders]').setAttribute('aria-expanded',String(this.q('.du-tree').classList.contains('du-tree-open'))); return; }
            if (action === 'drives') return this.open('');
            if (action === 'up') { const trimmed = this.path.replace(/[\\/]+$/,''); const parent = trimmed.replace(/[\\/][^\\/]+$/,''); return this.open(parent === trimmed || /^\\\\[^\\]+$/.test(parent) ? '' : /^[A-Za-z]:$/.test(parent) ? parent + '\\' : parent); }
            if (action === 'refresh') { this.snapshot = ''; return this.list(); }
            if (action === 'prev' || action === 'next') { this.page += action === 'prev' ? -1 : 1; return this.list(); }
            if (action === 'list-options') { this.q('.du-list-options').hidden = !this.q('.du-list-options').hidden; return; }
            if (action === 'history-options') { this.q('.du-history-options').hidden = !this.q('.du-history-options').hidden; return; }
            if (action === 'history-prev' || action === 'history-next') this.historyPage += action === 'history-prev' ? -1 : 1;
            return this.history();
        }
        async open(path) {
            this.path = path; this.snapshot = ''; this.page = 1; this.q('[name=office-path]').value = path; this.destination();
            const crumbs = this.q('.du-crumbs'); crumbs.replaceChildren(button('Drives',() => this.run(() => this.open(''))));
            const normalized = path.replace(/\//g,'\\'); const root = normalized.match(/^(?:[A-Za-z]:\\|\\\\[^\\]+\\[^\\]+)/)?.[0];
            if (root) { const first=button('',() => this.run(() => this.open(root))); first.textContent=root; crumbs.append(first); let current=root; for (const part of normalized.slice(root.length).split('\\').filter(Boolean)) { current=current.replace(/\\$/,'')+'\\'+part; const target=current; const crumb=button('',() => this.run(() => this.open(target))); crumb.textContent=part; crumbs.append(crumb); } }
            await this.list();
        }
        async list() {
            const serial = this.serial = (this.serial || 0) + 1;
            const data = await this.api('list',{Path:this.path,Snapshot:this.snapshot,Search:this.q('[name=file-search]').value,Sort:this.sort,Descending:this.descending,Page:this.page,PageSize:Number(this.q('[name=file-size]').value)});
            if (serial !== this.serial) return; this.snapshot=data.snapshot; this.page=data.page;
            const body=this.q('.du-files tbody'); body.replaceChildren();
            for (const entry of data.rows) { const row=body.insertRow(); const name=td(row,''); if (entry.Folder) { const b=button('',() => this.run(() => this.open(entry.Path))); b.append(fileDisplay.icon(entry),document.createTextNode(entry.Name)); b.className='du-link'; name.append(b); } else { const label=document.createElement('span');label.textContent=entry.Name;name.append(fileDisplay.icon(entry),label); }
                td(row,entry.Folder?'Folder':entry.Name.includes('.')?entry.Name.split('.').pop():'File').className='du-secondary'; const size=td(row,entry.Folder?'':fileDisplay.size(entry.Bytes));size.className='du-size';size.title=entry.Folder?'':bytes(entry.Bytes);td(row,fileDisplay.date(entry.CreatedUtc)).className='du-secondary';td(row,fileDisplay.date(entry.ModifiedUtc)).className='du-secondary'; }
            if (!data.rows.length) { const c=body.insertRow().insertCell(); c.colSpan=5; c.textContent='No matching files or folders.'; }
            this.q('.du-summary').textContent=data.total+' items | Page '+data.page+' of '+data.pages;
            this.q('[data-a=prev]').disabled=data.page<=1; this.q('[data-a=next]').disabled=data.page>=data.pages;
            this.root.querySelectorAll('[data-sort]').forEach(b => { b.textContent=b.dataset.sort+(b.dataset.sort===this.sort?(this.descending?' \u25bc':' \u25b2'):''); b.parentElement.setAttribute('aria-sort',b.dataset.sort===this.sort?(this.descending?'descending':'ascending'):'none'); });
        }
        async tree() {
            const roots=await this.api('list',{Path:'',PageSize:100}); const nav=this.q('.du-tree'); nav.replaceChildren();
            const add=(parent,rows) => { const ul=document.createElement('ul'); parent.append(ul); for (const entry of rows.filter(e => e.Folder)) { const li=document.createElement('li'); ul.append(li); const expand=button('+',() => this.run(async () => { if (li.querySelector('ul')) { li.querySelector('ul').remove(); expand.textContent='+'; return; } let page=1,snapshot='',data,children=[]; do { data=await this.api('list',{Path:entry.Path,PageSize:100,Page:page,Snapshot:snapshot}); snapshot=data.snapshot; children.push(...data.rows); page++; } while(page<=data.pages); add(li,children); expand.textContent='\u2212'; })); expand.setAttribute('aria-label','Expand '+entry.Name); const open=button('',() => this.run(() => this.open(entry.Path))); open.textContent=entry.Name; open.prepend(fileDisplay.icon(entry)); li.append(expand,open); } };
            add(nav,roots.rows);
        }
        async history() {
            const data=await this.api('history',{Page:this.historyPage,PageSize:Number(this.q('[name=history-size]').value),Search:this.q('[name=history-search]').value,Sort:this.historySort,Descending:this.historyDescending}); this.historyPage=data.page;
            const body=this.q('.du-history-rows'); body.replaceChildren();
            for (const p of data.rows) { const row=body.insertRow(); td(row,new Date(p.CreatedUtc).toLocaleString()); td(row,p.Path); td(row,p.State); td(row,p.completed+' / '+p.count); const actions=td(row,'');
                if (!['completed','cancelled','expired'].includes(p.State)) actions.append(button('&#128065;&#65039; Open',() => this.run(async () => { if (this.running) throw new Error('Pause or finish the active upload first.'); this.root.hidden=false; this.plan=await this.api('status',{Id:p.Id}); this.files.clear(); this.showActive(); if (this.plan.State==='review'||this.plan.State==='needs-decision') await this.action('review'); else this.message('Select the same files to resume uploading. Fully received files can be committed without re-selection.'); })));
                else actions.append(button('&#10060; Remove history',() => this.run(async () => { await this.api('remove',{Id:p.Id}); await this.history(); })));
            }
            if (!data.rows.length) { const c=body.insertRow().insertCell(); c.colSpan=5; c.textContent='No direct uploads.'; }
            this.q('.du-history-summary').textContent=data.total+' uploads | Page '+data.page+' of '+data.pages; this.q('[data-a=history-prev]').disabled=data.page<=1; this.q('[data-a=history-next]').disabled=data.page>=data.pages;
            this.root.querySelectorAll('[data-history-sort]').forEach(b => { b.textContent=b.dataset.historySort+(b.dataset.historySort===this.historySort?(this.historyDescending?' \u25bc':' \u25b2'):''); b.parentElement.setAttribute('aria-sort',b.dataset.historySort===this.historySort?(this.historyDescending?'descending':'ascending'):'none'); });
        }
    }
    window.ByteForgeDirectUpload=ByteForgeDirectUpload;
    document.addEventListener('DOMContentLoaded',() => {
        const root=document.getElementById('direct-upload-app'); if(!root||root.dataset.mode!=='office')return;
        const base=document.body.dataset.base;
        const uploader=new ByteForgeDirectUpload(root,{office:true,canUpload:root.dataset.upload==='true',api:async (operation,args) => {
            const response=await fetch(base+'ui/direct/'+operation,{method:'POST',credentials:'same-origin',redirect:'error',headers:{'Content-Type':'application/json','X-CSRF-Token':document.querySelector('#direct-csrf input').value},body:JSON.stringify(args)});
            const type=response.headers.get('content-type')||''; const data=type.includes('application/json')?await response.json():{error:'Request failed. Check sign-in, permissions and office connectivity.'}; if(!response.ok)throw new Error(data.error||'Upload request failed.'); return data;
        }});
        if(root.dataset.browse==='true')uploader.run(async () => { const path=new URLSearchParams(location.search).get('path')||''; await uploader.open(path); await uploader.tree(); });
        uploader.run(() => uploader.history());
        window.addEventListener('beforeunload',e => { if(uploader.running){e.preventDefault();e.returnValue='';} });
    });
}());
