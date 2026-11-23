/* ByteForgeTransfer: same-origin, antiforgery-protected UI for the OneDrive transfer API. */
(function () {
    'use strict';
    class ByteForgeTransfer {
        constructor() {
            this.base = document.body.dataset.base;
            this.page = 1; this.pages = 1; this.sort = 'created'; this.descending = true;
            this.notice = document.getElementById('notice');
            const saved = localStorage.getItem('bf-transfer-theme');
            if (saved) document.body.dataset.theme = saved;
            document.getElementById('theme-toggle').addEventListener('click', () => {
                const dark = document.body.dataset.theme === 'dark' || (!document.body.dataset.theme && matchMedia('(prefers-color-scheme:dark)').matches);
                document.body.dataset.theme = dark ? 'light' : 'dark'; localStorage.setItem('bf-transfer-theme', document.body.dataset.theme);
            });
            if (document.getElementById('jobs')) this.bindJobs();
            if (document.getElementById('connect-form')) this.bindSettings();
        }
        message(text, error) { this.notice.textContent = text; this.notice.classList.toggle('bf-error', !!error); }
        async request(path, form) {
            const response = await fetch(this.base + path, { method: form ? 'POST' : 'GET', body: form || undefined, credentials: 'same-origin', redirect: 'error' });
            const type = response.headers.get('content-type') || '';
            const data = type.includes('application/json') ? await response.json() : { error: response.status === 403 ? 'Permission denied.' : 'Sign in again or check the server response.' };
            if (!response.ok) throw new Error(data.error || ('Request failed: HTTP ' + response.status));
            return data;
        }
        async postForm(event, success) {
            event.preventDefault(); const form = event.currentTarget; const button = form.querySelector('button[type=submit]');
            button.disabled = true;
            try { await success(await this.request(form.action.substring(location.origin.length + this.base.length), new FormData(form))); }
            catch (error) { this.message(error.message, true); }
            finally { button.disabled = false; }
        }
        bindSettings() {
            const connect = document.getElementById('connect-form'); let polling = false;
            connect.addEventListener('submit', event => this.postForm(event, async data => {
                const link = document.getElementById('verification-link'); const url = new URL(data.verificationUrl);
                if (url.protocol !== 'https:') throw new Error('Microsoft returned an invalid sign-in URL.');
                document.getElementById('device-code').hidden = false; link.href = url.href;
                document.getElementById('user-code').textContent = data.code;
                const deadline = new Date(data.expiresUtc); polling = true;
                while (polling && new Date() < deadline) {
                    await new Promise(resolve => setTimeout(resolve, 5000));
                    try {
                        const status = await this.request('onedrive/poll', new FormData(connect));
                        if (status.state === 'connected') {
                            polling = false; document.getElementById('poll-status').textContent = 'Microsoft account connected.';
                            const connection = document.getElementById('connection-status');
                            connection.className = 'bf-connection bf-connected';
                            connection.innerHTML = '<span aria-hidden="true">&#9989;</span> <strong>OneDrive connected</strong>';
                            this.message('OneDrive connected.');
                        }
                    } catch (error) { polling = false; this.message(error.message, true); }
                }
                if (polling) { polling = false; this.message('Sign-in code expired. Start Microsoft sign-in again.', true); }
            }));
        }
        bindJobs() {
            document.getElementById('transfer-form').addEventListener('submit', event => this.postForm(event, async data => { document.querySelector('[name=RequestId]').value = crypto.randomUUID(); this.message('Transfer queued: ' + data.id); this.page = 1; await this.loadJobs(); }));
            document.getElementById('refresh-jobs').addEventListener('click', () => this.loadJobs());
            document.getElementById('previous-page').addEventListener('click', () => { this.page = Math.max(1, this.page - 1); this.loadJobs(); });
            document.getElementById('next-page').addEventListener('click', () => { this.page = Math.min(this.pages, this.page + 1); this.loadJobs(); });
            let debounce;
            document.getElementById('job-search').addEventListener('input', () => { clearTimeout(debounce); debounce = setTimeout(() => { this.page = 1; this.loadJobs(); }, 300); });
            document.getElementById('page-size').addEventListener('change', () => { this.page = 1; this.loadJobs(); });
            document.getElementById('list-settings-toggle').addEventListener('click', event => { const panel = document.getElementById('list-settings'); panel.hidden = !panel.hidden; event.currentTarget.setAttribute('aria-expanded', String(!panel.hidden)); });
            document.querySelectorAll('[data-sort]').forEach(button => button.addEventListener('click', () => {
                this.descending = this.sort === button.dataset.sort ? !this.descending : false; this.sort = button.dataset.sort; this.page = 1;
                document.querySelectorAll('[data-sort]').forEach(b => { b.textContent = b.dataset.sort.charAt(0).toUpperCase() + b.dataset.sort.slice(1) + (b.dataset.sort === this.sort ? (this.descending ? ' \u25bc' : ' \u25b2') : ''); b.closest('th').setAttribute('aria-sort', b.dataset.sort === this.sort ? (this.descending ? 'descending' : 'ascending') : 'none'); });
                this.loadJobs();
            }));
            this.loadJobs(); setInterval(() => { if (!document.hidden) this.loadJobs(); }, 10000);
        }
        async loadJobs() {
            const serial = this.serial = (this.serial || 0) + 1;
            try {
                const query = new URLSearchParams({ search: document.getElementById('job-search').value, sort: this.sort, descending: String(this.descending), page: String(this.page), pageSize: document.getElementById('page-size').value });
                const data = await this.request('ui/jobs?' + query);
                if (serial !== this.serial) return;
                this.page = data.page; this.pages = data.pages;
                const tbody = document.querySelector('#jobs tbody'); tbody.textContent = '';
                data.rows.forEach(job => {
                    const row = document.createElement('tr');
                    [new Date(job.CreatedUtc).toLocaleString(), job.Request.Direction, job.Request.OneDrivePath, job.State, job.Error || (job.State === 'completed' ? job.Bytes.toLocaleString() + ' bytes' : '')].forEach(value => { const cell = document.createElement('td'); cell.textContent = value; row.appendChild(cell); });
                    const id = document.createElement('code'); id.textContent = job.Id; row.children[0].appendChild(id);
                    if (document.getElementById('can-remove').value === 'true' && job.State !== 'running' && job.State !== 'queued') {
                        const remove = document.createElement('button'); remove.type = 'button'; remove.innerHTML = '&#10060; Delete'; remove.title = 'Remove job history; keep transferred files';
                        remove.addEventListener('click', async () => { remove.disabled = true; const form = new FormData(document.getElementById('job-token')); form.append('id', job.Id); try { await this.request('ui/jobs/remove', form); await this.loadJobs(); } catch (e) { this.message(e.message, true); remove.disabled = false; } });
                        row.children[4].appendChild(remove);
                    }
                    tbody.appendChild(row);
                });
                if (!data.total) { const row = tbody.insertRow(); const cell = row.insertCell(); cell.colSpan = 5; cell.textContent = 'No matching transfers.'; }
                document.getElementById('page-summary').textContent = data.total + ' jobs | Page ' + data.page + ' of ' + data.pages;
                document.getElementById('previous-page').disabled = this.page <= 1; document.getElementById('next-page').disabled = this.page >= this.pages;
            } catch (error) { this.message(error.message, true); }
        }
    }
    document.addEventListener('DOMContentLoaded', () => new ByteForgeTransfer());
}());
