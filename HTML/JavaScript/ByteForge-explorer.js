/* Browser UI for the authenticated loopback helper. Payloads and office credentials stay in native code. */
(function () {
    'use strict';
    const $ = id => document.getElementById(id);
    let session = '', currentPlan, plans = [], queuePage = 1, queueSort = 'CreatedUtc', queueDescending = true;
    async function api(path, data = {}) {
        const response = await fetch(path, { method: 'POST', headers: { 'Content-Type': 'application/json', 'X-Transfer-Session': session }, body: JSON.stringify(data), redirect: 'error', credentials: 'omit' });
        const result = await response.json();
        if (!response.ok) throw new Error(result.error || 'Request failed.');
        return result;
    }
    function notice(text, error = false) {
        $('notice').textContent = text; $('notice').classList.toggle('error', error);
        const dialog = document.querySelector('dialog[open]');
        if (dialog && error) { let message = dialog.querySelector('.bf-dialog-error'); if (!message) { message = document.createElement('p'); message.className = 'bf-dialog-error'; message.setAttribute('role','alert'); dialog.prepend(message); } message.textContent = text; }
    }
    async function run(action) { try { return await action(); } catch (error) { notice(error.message, true); } }
    function cell(row, text) { const td = document.createElement('td'); td.textContent = text; row.append(td); return td; }
    function button(text, action) { const b = document.createElement('button'); b.type = 'button'; b.textContent = text; b.addEventListener('click', () => run(action)); return b; }
    class ByteForgeExplorerPane {
        constructor(side, title) {
            this.side = side; this.path = ''; this.snapshot = ''; this.page = 1; this.sort = 'name'; this.descending = false; this.selected = new Map(); this.serial = 0;
            this.history = []; this.favorites = []; try { this.favorites = JSON.parse(localStorage.getItem('bf-explorer-favorites-' + side) || '[]'); } catch {}
            this.el = $('pane-template').content.firstElementChild.cloneNode(true); this.el.querySelector('h2').textContent = title; $('panes').append(this.el);
            this.el.querySelector('[data-action=direct]').hidden = side !== 'office';
            this.el.querySelector('form').addEventListener('submit', e => { e.preventDefault(); run(() => this.open(this.el.querySelector('[name=path]').value)); });
            this.el.querySelector('[name=favorites]').onchange = e => { if (e.target.value) run(() => this.open(e.target.value)); };
            this.renderFavorites();
            this.el.querySelector('[name=search]').addEventListener('input', () => { clearTimeout(this.debounce); this.debounce = setTimeout(() => { this.page = 1; run(() => this.load()); }, 250); });
            this.el.querySelector('[name=size]').addEventListener('change', () => { this.page = 1; run(() => this.load()); });
            this.el.querySelectorAll('[data-sort]').forEach(b => b.addEventListener('click', () => { this.descending = this.sort === b.dataset.sort ? !this.descending : false; this.sort = b.dataset.sort; this.page = 1; run(() => this.load()); }));
            this.el.querySelectorAll('[data-action]').forEach(b => b.addEventListener('click', () => run(async () => {
                const a = b.dataset.action;
                if (a === 'direct') { direct.root.hidden = false; direct.destination(); direct.choose(); run(() => direct.history()); }
                if (a === 'back' && this.history.length) await this.open(this.history.pop(), false);
                if (a === 'favorite' && this.path) { this.favorites = this.favorites.includes(this.path) ? this.favorites.filter(p => p !== this.path) : [...this.favorites, this.path]; try { localStorage.setItem('bf-explorer-favorites-' + side, JSON.stringify(this.favorites)); } catch {} this.renderFavorites(); }
                if (a === 'up') { const trimmed = this.path.replace(/[\\/]+$/, ''); const parent = trimmed.replace(/[\\/][^\\/]+$/, ''); await this.open(parent === trimmed || /^\\\\[^\\]+$/.test(parent) ? '' : /^[A-Za-z]:$/.test(parent) ? parent + '\\' : parent); }
                if (a === 'roots') await this.open('');
                if (a === 'refresh') { this.snapshot = ''; this.selected.clear(); await this.load(); }
                if (a === 'prev' || a === 'next') { this.page += a === 'prev' ? -1 : 1; await this.load(); }
                if (a === 'options') { const panel = this.el.querySelector('.bf-explorer-options'); panel.hidden = !panel.hidden; b.setAttribute('aria-expanded', String(!panel.hidden)); }
                if (a === 'select-all') { this.rows.forEach(r => b.checked ? this.selected.set(r.Path, r) : this.selected.delete(r.Path)); this.renderRows(); }
            })));
            this.el.addEventListener('dragover', e => { if (e.dataTransfer.types.includes('application/x-byteforge-side')) { e.preventDefault(); e.dataTransfer.dropEffect = 'copy'; this.el.classList.add('bf-drop-target'); } });
            this.el.addEventListener('dragleave', e => { if (!this.el.contains(e.relatedTarget)) this.el.classList.remove('bf-drop-target'); });
            this.el.addEventListener('dragover', e => { if (side === 'office' && Array.from(e.dataTransfer.types).includes('Files')) { e.preventDefault(); e.dataTransfer.dropEffect = 'copy'; this.el.classList.add('bf-drop-target'); } });
            this.el.addEventListener('drop', e => {
                if (side === 'office' && Array.from(e.dataTransfer.types).includes('Files')) {
                    e.preventDefault(); this.el.classList.remove('bf-drop-target');
                    if (Array.from(e.dataTransfer.items || []).some(i => i.webkitGetAsEntry?.()?.isDirectory)) { notice('Drop files rather than folders for direct upload.',true); return; }
                    direct.root.hidden=false; run(() => direct.selectFiles(Array.from(e.dataTransfer.files),this.path)); return;
                }
                const from = e.dataTransfer.getData('application/x-byteforge-side'); if (from && from !== side) { e.preventDefault(); run(() => prepare(side === 'office' ? 'toOffice' : 'toLocal')); } this.el.classList.remove('bf-drop-target');
            });
        }
        renderFavorites() { const select = this.el.querySelector('[name=favorites]'); select.replaceChildren(new Option('Favorite folders...', '')); this.favorites.forEach(p => select.add(new Option(p, p))); }
        async open(path, remember = true) {
            if (remember && path !== this.path) this.history.push(this.path);
            this.path = path; this.snapshot = ''; this.page = 1; this.selected.clear(); this.el.querySelector('[name=path]').value = path;
            this.el.querySelector('[data-action=back]').disabled = !this.history.length;
            const crumbs = this.el.querySelector('.bf-explorer-breadcrumbs'); crumbs.replaceChildren(button('Drives', () => this.open('')));
            const normalized = path.replace(/\//g, '\\'), root = normalized.match(/^(?:[A-Za-z]:\\|\\\\[^\\]+\\[^\\]+)/)?.[0];
            if (root) { crumbs.append(button(root, () => this.open(root))); let current = root; normalized.slice(root.length).split('\\').filter(Boolean).forEach(part => { current = current.replace(/\\$/, '') + '\\' + part; const target = current; crumbs.append(button(part, () => this.open(target))); }); }
            await this.load();
        }
        async load() {
            const serial = ++this.serial;
            const data = await api('/api/list', { side: this.side, path: this.path, snapshot: this.snapshot, page: this.page, pageSize: Number(this.el.querySelector('[name=size]').value), search: this.el.querySelector('[name=search]').value, sort: this.sort, descending: this.descending });
            if (serial !== this.serial) return;
            this.snapshot = data.snapshot; this.rows = data.rows; this.page = data.page;
            this.el.querySelector('[data-action=prev]').disabled = this.page <= 1; this.el.querySelector('[data-action=next]').disabled = this.page >= data.pages;
            this.el.querySelectorAll('[data-sort]').forEach(b => { b.textContent = b.dataset.sort + (b.dataset.sort === this.sort ? (this.descending ? ' \u25bc' : ' \u25b2') : ''); b.parentElement.setAttribute('aria-sort', b.dataset.sort === this.sort ? (this.descending ? 'descending' : 'ascending') : 'none'); });
            this.total = data.total; this.renderRows();
        }
        renderRows() {
            const body = this.el.querySelector('tbody'); body.replaceChildren();
            this.rows.forEach(entry => {
                const row = document.createElement('tr'); row.draggable = true; row.setAttribute('aria-selected', String(this.selected.has(entry.Path)));
                const check = document.createElement('input'); check.type = 'checkbox'; check.checked = this.selected.has(entry.Path); check.setAttribute('aria-label', 'Select ' + entry.Name);
                check.addEventListener('change', () => { if (check.checked) this.selected.set(entry.Path, entry); else this.selected.delete(entry.Path); this.renderRows(); }); cell(row, '').append(check);
                const display=window.ByteForgeFileDisplay;
                const name = button(entry.Name, () => entry.Folder ? this.open(entry.Path) : undefined); name.prepend(display.icon(entry)); name.className = 'bf-explorer-name'; cell(row, '').append(name);
                cell(row, entry.Folder ? 'Folder' : (entry.Name.includes('.') ? entry.Name.split('.').pop() : 'File')); const size=cell(row,entry.Folder?'':display.size(entry.Bytes));size.className='bf-file-size';size.title=entry.Folder?'':entry.Bytes.toLocaleString()+' bytes';cell(row,display.date(entry.CreatedUtc));cell(row,display.date(entry.ModifiedUtc));
                row.addEventListener('dragstart', e => { if (!this.selected.has(entry.Path)) { this.selected.clear(); this.selected.set(entry.Path, entry); } e.dataTransfer.setData('application/x-byteforge-side', this.side); e.dataTransfer.effectAllowed = 'copy'; });
                body.append(row);
            });
            if (!this.rows.length) { const row = body.insertRow(); const td = row.insertCell(); td.colSpan = 6; td.textContent = 'No matching items.'; }
            this.el.querySelector('.summary').textContent = this.total + ' items | Page ' + this.page + ' | ' + this.selected.size + ' selected';
            mobileSelection();
        }
        async tree() {
            const nav = this.el.querySelector('.bf-explorer-tree'); nav.replaceChildren();
            const data = await api('/api/list', { side: this.side, path: '', pageSize: 100 });
            const add = (parent, entries) => {
                const ul = document.createElement('ul'); parent.append(ul);
                entries.filter(e => e.Folder).forEach(entry => {
                    const li = document.createElement('li'); ul.append(li);
                    const expand = button('+', async () => {
                        if (li.querySelector('ul')) { li.querySelector('ul').remove(); expand.textContent = '+'; return; }
                        const children = []; let snapshot = '', page = 1, data;
                        do { data = await api('/api/list', { side: this.side, path: entry.Path, snapshot, page, pageSize: 100 }); snapshot = data.snapshot; children.push(...data.rows); page++; } while (page <= data.pages);
                        add(li, children); expand.textContent = '\u2212';
                    }); expand.setAttribute('aria-label', 'Expand ' + entry.Name);
                    const open = button(entry.Name, () => this.open(entry.Path)); open.prepend(window.ByteForgeFileDisplay.icon(entry)); li.append(expand, open);
                });
            };
            add(nav, data.rows);
        }
    }
    let local, office, direct;
    function mobileSelection() {
        if (!local || !office) return;
        $('mobile-selection').textContent = local.selected.size + ' local / ' + office.selected.size + ' office selected. Office: ' + (office.path || 'choose a folder') + '. Local: ' + (local.path || 'choose a folder');
    }
    async function prepare(direction) {
        const source = direction === 'toOffice' ? local : office, target = direction === 'toOffice' ? office : local;
        if (!source.selected.size || !target.path) throw new Error('Select source items and open a destination folder.');
        notice('Preparing selection and checking exact destination paths...');
        currentPlan = await api('/api/prepare', { direction, paths: [...source.selected.keys()], destination: target.path });
        showPlan();
    }
    function showPlan() {
        const rows = $('conflict-rows'); rows.replaceChildren(); $('apply-all').value = '';
        $('plan-summary').textContent = currentPlan.Items.length + ' entries; ' + currentPlan.Items.filter(i => i.Conflict).length + ' conflicts. ' + (currentPlan.Revision > 1 ? 'Completed entries stay delivered; review applies to remaining entries.' : 'No payload has been uploaded.');
        currentPlan.Items.forEach(item => {
            if (item.State === 'completed' || item.Choice === 'skip') return;
            const row = document.createElement('tr'); cell(row, item.Source.Path); cell(row, item.Destination); cell(row, item.Reserved ? 'Reserved by queue/selection' : item.Conflict ? 'Already exists' : item.Existing ? 'Merge folder' : 'New');
            const td = cell(row, item.Conflict ? '' : 'Copy');
            if (item.Conflict) {
                const select = document.createElement('select'); select.dataset.item = item.PayloadId; select.setAttribute('aria-label', 'Conflict decision for ' + item.Source.Name);
                [['', 'Choose...'], ['overwrite', 'Overwrite'], ['keepBoth', 'Keep both'], ['skip', 'Skip']].forEach(([value, text]) => { if (value === 'overwrite' && (item.Reserved || item.Source.Folder || item.Existing?.Folder)) return; select.add(new Option(text, value)); }); td.append(select);
            }
            rows.append(row);
        });
        $('conflicts').showModal(); notice('Review the prepared transfer.');
    }
    function renderQueue() {
        const search = $('queue-search').value.toLowerCase(), size = Number($('queue-size').value);
        const rows = plans.filter(p => JSON.stringify(p).toLowerCase().includes(search)).sort((a,b) => String(a[queueSort]).localeCompare(String(b[queueSort])) * (queueDescending ? -1 : 1));
        const pages = Math.max(1, Math.ceil(rows.length / size)); queuePage = Math.min(queuePage, pages); $('queue').replaceChildren();
        rows.slice((queuePage - 1) * size, queuePage * size).forEach(plan => {
            const row = document.createElement('tr'); cell(row, new Date(plan.CreatedUtc).toLocaleString()); cell(row, plan.Direction === 'toOffice' ? 'Local \u2192 office' : 'Office \u2192 local'); cell(row, plan.State);
            const progress = cell(row, plan.Items.filter(i => i.State === 'completed' || i.Choice === 'skip').length + ' / ' + plan.Items.length + ' entries');
            const active = plan.Items.find(i => i.State !== 'completed' && i.Choice !== 'skip');
            const detail = document.createElement('small'); detail.textContent = plan.Error || (active ? active.State + (active.ProgressBytes ? ' (' + active.ProgressBytes.toLocaleString() + ' bytes)' : '') + ': ' + active.Destination : plan.Id); progress.append(detail);
            const actions = cell(row, '');
            if (plan.State === 'failed') actions.append(button('Retry', async () => { await api('/api/control', { action: 'retry', id: plan.Id }); await refresh(); }));
            if (plan.State === 'needs-decision') actions.append(button('Review conflicts', async () => { currentPlan = await api('/api/review', { id: plan.Id }); showPlan(); }));
            if (plan.State === 'review' || plan.State === 'draft') actions.append(button('Review', () => { currentPlan = plan; showPlan(); }));
            if (!['completed','cancelled'].includes(plan.State)) actions.append(button('Cancel', async () => { await api('/api/control', { action: 'cancel', id: plan.Id }); await refresh(); }));
            $('queue').append(row);
        });
        $('queue-summary').textContent = rows.length + ' operations | Page ' + queuePage + ' of ' + pages; $('queue-prev').disabled = queuePage <= 1; $('queue-next').disabled = queuePage >= pages;
    }
    async function refresh() { const status = await api('/api/status'); plans = status.plans; $('queue-pause').disabled = status.paused; $('queue-resume').disabled = !status.paused; renderQueue(); return status; }
    document.addEventListener('DOMContentLoaded', () => run(async () => {
        const launch = decodeURIComponent(location.hash.slice(1)); history.replaceState(null, '', '/');
        if (launch) { session = (await api('/session', { token: launch })).session; try { sessionStorage.setItem('bf-explorer-session', session); } catch {} }
        else { try { session = sessionStorage.getItem('bf-explorer-session') || ''; } catch {} }
        let theme; try { theme = localStorage.getItem('bf-explorer-theme'); } catch {} if (theme) document.documentElement.dataset.theme = theme;
        $('theme').onclick = () => { const dark = document.documentElement.dataset.theme === 'dark' || (!document.documentElement.dataset.theme && matchMedia('(prefers-color-scheme:dark)').matches); document.documentElement.dataset.theme = dark ? 'light' : 'dark'; try { localStorage.setItem('bf-explorer-theme', dark ? 'light' : 'dark'); } catch {} };
        local = new ByteForgeExplorerPane('local', 'This computer'); office = new ByteForgeExplorerPane('office', 'Office');
        direct = new window.ByteForgeDirectUpload($('direct-upload-app'), {destination:() => office.path, api:(operation,args) => api('/api/direct/'+operation,args)});
        $('show-local').onclick = () => { $('panes').dataset.active='local'; $('show-local').setAttribute('aria-pressed','true'); $('show-office').setAttribute('aria-pressed','false'); };
        $('show-office').onclick = () => { $('panes').dataset.active='office'; $('show-local').setAttribute('aria-pressed','false'); $('show-office').setAttribute('aria-pressed','true'); };
        window.addEventListener('beforeunload', e => { if (direct.running) { e.preventDefault(); e.returnValue=''; } });
        const status = await refresh(); $('endpoint').value = status.endpoint || '';
        $('frame-origins').value = (status.frameOrigins || []).join('\n');
        $('settings-open').onclick = () => $('settings').showModal(); document.querySelectorAll('[data-close]').forEach(b => b.onclick = () => $(b.dataset.close).close());
        $('settings-form').onsubmit = e => { e.preventDefault(); run(async () => { await api('/api/settings', { endpoint: $('endpoint').value, key: $('key').value, frameOrigins: $('frame-origins').value }); $('key').value = ''; notice('Connection settings saved.'); }); };
        $('connect').onclick = () => run(async () => {
            const data = await api('/api/connect'); $('signin').replaceChildren(); const link = document.createElement('a'); const url = new URL(data.verificationUrl); if (url.protocol !== 'https:') throw new Error('Invalid verification URL.'); link.href = url.href; link.target = '_blank'; link.rel = 'noopener'; link.textContent = 'Open Microsoft sign-in'; $('signin').append(link, document.createTextNode(' and enter ' + data.code));
            while (new Date() < new Date(data.expiresUtc)) { await new Promise(r => setTimeout(r, 5000)); const poll = await api('/api/poll'); if (poll.state === 'connected') { $('signin').textContent = 'Microsoft account connected.'; return; } } throw new Error('Sign-in expired. Try again.');
        });
        $('check').onclick = () => run(async () => { await api('/api/check'); notice('Local and office endpoints use the same OneDrive.'); await office.open(''); await office.tree(); });
        $('to-office').onclick = () => run(() => prepare('toOffice')); $('to-local').onclick = () => run(() => prepare('toLocal'));
        $('apply-all').onchange = () => document.querySelectorAll('[data-item]').forEach(s => { if ([...s.options].some(o => o.value === $('apply-all').value)) s.value = $('apply-all').value; });
        $('confirm-copy').onclick = () => run(async () => { const choices = {}; document.querySelectorAll('[data-item]').forEach(s => choices[s.dataset.item] = s.value); $('confirm-copy').disabled = true; try { await api('/api/confirm', { id: currentPlan.Id, choices }); $('conflicts').close(); notice('Copy queued. The tray helper will continue if you close this page.'); await refresh(); } finally { $('confirm-copy').disabled = false; } });
        $('cancel-plan').onclick = () => run(async () => { await api('/api/control', { action: 'cancel', id: currentPlan.Id }); $('conflicts').close(); await refresh(); });
        $('conflicts').addEventListener('cancel', e => { e.preventDefault(); $('cancel-plan').click(); });
        $('queue-pause').onclick = () => run(async () => { await api('/api/control', { action: 'pause' }); await refresh(); }); $('queue-resume').onclick = () => run(async () => { await api('/api/control', { action: 'resume' }); await refresh(); });
        $('queue-options-toggle').onclick = () => { $('queue-options').hidden = !$('queue-options').hidden; $('queue-options-toggle').setAttribute('aria-expanded', String(!$('queue-options').hidden)); };
        $('queue-search').oninput = $('queue-size').onchange = () => { queuePage = 1; renderQueue(); }; $('queue-prev').onclick = () => { queuePage--; renderQueue(); }; $('queue-next').onclick = () => { queuePage++; renderQueue(); };
        document.querySelectorAll('[data-queue-sort]').forEach(b => b.onclick = () => { queueDescending = queueSort === b.dataset.queueSort ? !queueDescending : false; queueSort = b.dataset.queueSort; queuePage = 1; document.querySelectorAll('[data-queue-sort]').forEach(x => x.parentElement.setAttribute('aria-sort', x === b ? (queueDescending ? 'descending' : 'ascending') : 'none')); renderQueue(); });
        await local.open(''); await local.tree();
        if (status.endpoint) { await run(() => office.open('')); await run(() => office.tree()); }
        else $('settings').showModal();
        notice('Ready. Open a folder on each side, select items, then copy or drag across panes.');
        setInterval(() => { if (!document.hidden) run(refresh); }, 2000);
    }));
}());
