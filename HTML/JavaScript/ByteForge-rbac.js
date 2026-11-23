(function (global) {
    "use strict";

    function ByteForgeRbacAdmin(options) {
        if (!options || !options.root || !options.apiBase || !options.getToken) throw new Error("RBAC root, API base, and token provider are required.");
        this.options = options;
        this.root = options.root;
        this.state = { view: options.rolesOnly ? "roles" : "users", search: "", page: 1, pageSize: 10, sort: options.rolesOnly ? "priority" : "username", direction: options.rolesOnly ? "desc" : "asc", users: [], roles: [], privileges: [], me: null };
        this.build();
    }

    function isAuthenticationRedirect(response) {
        return !!(response && response.redirected && /\/(?:account\/)?login(?:\.html)?(?:[/?#]|$)/i.test(response.url || ""));
    }

    // Read-only preview of the access selected in the user editor; never changes grants.
    function effectivePermissions(projects, active, superAdmin) {
        var rows = [];
        projects.forEach(function (project) {
            if (!project.hasAccess && !superAdmin) return;
            var roles = (project.roles || []).filter(function (role) { return role.isActive && project.roleIds.indexOf(role.id) >= 0; });
            var enabled = active && (project.hasAccess || superAdmin);
            var admin = superAdmin || roles.some(function (role) { return String(role.name).toLowerCase() === "admin"; });
            var catalog = {}, grants = {};
            (project.privileges || []).forEach(function (permission) { catalog[permission.name.toLowerCase()] = permission; });
            roles.forEach(function (role) { (role.privileges || []).forEach(function (name) {
                var key = name.toLowerCase();
                if (!grants[key]) grants[key] = { name: name, sources: [] };
                grants[key].sources.push(role.displayName || role.name);
            }); });
            Object.keys(catalog).forEach(function (key) { if (!grants[key]) grants[key] = { name: catalog[key].name, sources: [] }; });
            Object.keys(catalog).concat(Object.keys(grants).filter(function (key) { return !catalog[key]; })).forEach(function (key) {
                var permission = catalog[key], grant = grants[key];
                if (permission && permission.isActive === false) return;
                rows.push({ project: project.displayName || project.projectKey, category: permission && permission.category || "Permissions", name: grant.name, displayName: permission && permission.displayName || grant.name, granted: enabled && (admin || grant.sources.length > 0), source: !enabled ? "No access: account inactive or project access disabled" : admin ? (superAdmin ? "Super administrator (full access)" : "Admin (full access)") : grant.sources.length ? grant.sources.join(", ") : "Not granted" });
            });
        });
        return rows;
    }

    function installPermissions(form, tabs, getRows, showPanel) {
        var tab = document.createElement("button"), panel = document.createElement("div");
        tab.type = "button"; tab.className = "bf-rbac-tab"; tab.textContent = "Permissions"; tab.setAttribute("role", "tab"); tab.setAttribute("aria-selected", "false"); tab.setAttribute("data-editor-tab", "permissions"); tabs.appendChild(tab);
        panel.className = "bf-rbac-editor-panel bf-rbac-permissions"; panel.setAttribute("data-editor-panel", "permissions"); panel.hidden = true;
        panel.innerHTML = '<p class="bf-rbac-secondary">Checked permissions are available through the selected access and roles. This list is read-only and previews unsaved changes.</p><span class="bf-rbac-section-label">Permissions</span><div class="bf-rbac-user-permission-picker" role="group" aria-label="User permissions"></div>';
        form.appendChild(panel);
        // A compact permission checklist mirrors the Roles picker rather than a paginated table.
        function render() {
            var picker = panel.querySelector(".bf-rbac-user-permission-picker"), rows = getRows(); picker.textContent = "";
            var projects = [];
            rows.forEach(function (row) { if (projects.indexOf(row.project) < 0) projects.push(row.project); });
            projects.forEach(function (project) {
                var section = document.createElement("section"); section.className = "bf-rbac-permission-project bf-rbac-role-page-picker";
                if (!form.classList.contains("bf-rbac-project-user-form")) ByteForgeRbacAdmin.prototype.text(section, "h4", project, "bf-rbac-permission-project-title");
                var categories = [];
                rows.filter(function (row) { return row.project === project; }).forEach(function (row) { if (categories.indexOf(row.category) < 0) categories.push(row.category); });
                categories.forEach(function (category) {
                    var group = document.createElement("div"); group.className = "bf-rbac-role-page-group";
                    ByteForgeRbacAdmin.prototype.text(group, "strong", category, "bf-rbac-category");
                    rows.filter(function (row) { return row.project === project && row.category === category; }).forEach(function (row) {
                        var label = document.createElement("label"), input = document.createElement("input"); label.className = "bf-rbac-check bf-rbac-page-check";
                        input.type = "checkbox"; input.checked = row.granted; input.disabled = true; label.title = row.name + " — " + row.source;
                        label.appendChild(input); label.appendChild(document.createTextNode(row.displayName)); group.appendChild(label);
                    }); section.appendChild(group);
                }); picker.appendChild(section);
            });
            if (!rows.length) ByteForgeRbacAdmin.prototype.text(picker, "span", "No permissions are available for the selected project access.", "bf-rbac-secondary");
        }
        tab.addEventListener("click", function () { render(); showPanel("permissions"); });
        form.addEventListener("change", render); render();
    }

    ByteForgeRbacAdmin.prototype.build = function () {
        this.root.classList.add("byte-forge-rbac");
        var rolesOnly = !!this.options.rolesOnly, usersOnly = !!this.options.usersOnly;
        this.root.innerHTML =
            '<div class="bf-rbac-header"><div><h2 class="bf-rbac-title">' + (rolesOnly ? 'Roles' : usersOnly ? 'Users' : 'Users and roles') + '</h2><p class="bf-rbac-subtitle">' + (rolesOnly ? 'Project-scoped roles, pages, and action permissions.' : 'Project-scoped access backed by global TELCO identities.') + '</p></div>' +
            '<div class="bf-rbac-actions"><input class="bf-rbac-search" type="search" aria-label="Search" placeholder="' + (rolesOnly ? 'Search roles' : 'Search users') + '">' +
            '<button class="bf-rbac-button is-secondary bf-rbac-settings-button" type="button" aria-expanded="false">&#9881;&#65039; Settings</button>' +
            '<button class="bf-rbac-button bf-rbac-add" type="button">&#10133; Add ' + (rolesOnly ? 'role' : 'user') + '</button></div></div>' +
            (rolesOnly || usersOnly ? '' : '<div class="bf-rbac-tabs" role="tablist"><button class="bf-rbac-tab" data-view="users" role="tab" aria-selected="true">Users</button><button class="bf-rbac-tab" data-view="roles" role="tab" aria-selected="false">Roles</button></div>') +
            '<div class="bf-rbac-message" role="status" hidden></div><div class="bf-rbac-settings" hidden><label>Rows per page <select><option>10</option><option>25</option><option>50</option></select></label></div>' +
            '<div class="bf-rbac-table-wrap"><table class="bf-rbac-table"><thead></thead><tbody></tbody></table></div>' +
            '<div class="bf-rbac-pager"><button class="bf-rbac-button is-secondary bf-rbac-prev" type="button">Previous</button><span></span><button class="bf-rbac-button is-secondary bf-rbac-next" type="button">Next</button></div>';
        this.bind();
    };

    ByteForgeRbacAdmin.prototype.bind = function () {
        var self = this;
        this.root.querySelector(".bf-rbac-search").addEventListener("input", function () { self.state.search = this.value; self.state.page = 1; self.render(); });
        this.root.querySelector(".bf-rbac-settings-button").addEventListener("click", function () {
            var panel = self.root.querySelector(".bf-rbac-settings"), open = panel.hidden;
            panel.hidden = !open; this.setAttribute("aria-expanded", open ? "true" : "false");
        });
        this.root.querySelector(".bf-rbac-settings select").addEventListener("change", function () { self.state.pageSize = Number(this.value); self.state.page = 1; self.render(); });
        this.root.querySelector(".bf-rbac-add").addEventListener("click", function () { self.state.view === "users" ? self.openUser() : self.openRole(); });
        this.root.querySelector(".bf-rbac-prev").addEventListener("click", function () { self.state.page -= 1; self.render(); });
        this.root.querySelector(".bf-rbac-next").addEventListener("click", function () { self.state.page += 1; self.render(); });
        Array.prototype.forEach.call(this.root.querySelectorAll(".bf-rbac-tab"), function (button) {
            button.addEventListener("click", function () {
                self.state.view = button.getAttribute("data-view"); self.state.page = 1;
                self.state.sort = self.state.view === "users" ? "username" : "priority";
                self.state.direction = self.state.view === "users" ? "asc" : "desc";
                self.root.querySelector(".bf-rbac-search").placeholder = self.state.view === "users" ? "Search users" : "Search roles";
                self.root.querySelector(".bf-rbac-add").innerHTML = self.state.view === "users" ? "&#10133; Add user" : "&#10133; Add role";
                Array.prototype.forEach.call(self.root.querySelectorAll(".bf-rbac-tab"), function (tab) { tab.setAttribute("aria-selected", tab === button ? "true" : "false"); });
                self.render();
            });
        });
    };

    ByteForgeRbacAdmin.prototype.load = function () {
        var self = this;
        this.message("Loading access administration...", false);
        var requests = this.options.rolesOnly ? [this.api("me"), this.api("roles"), this.api("privileges")] : this.options.usersOnly ? [this.api("me"), this.api("users"), this.api("roles"), this.api("privileges")] : [this.api("me"), this.api("users"), this.api("roles"), this.api("privileges")];
        return Promise.all(requests).then(function (values) {
            self.state.me = values[0];
            if (self.options.rolesOnly) { self.state.roles = values[1] || []; self.state.privileges = values[2] || []; }
            else if (self.options.usersOnly) { self.state.users = values[1] || []; self.state.roles = values[2] || []; self.state.privileges = values[3] || []; }
            else { self.state.users = values[1] || []; self.state.roles = values[2] || []; self.state.privileges = values[3] || []; }
            self.message("", false); self.render(); return self;
        }).catch(function (error) { self.message(error.message, true); throw error; });
    };

    ByteForgeRbacAdmin.prototype.render = function () {
        var self = this, rows = this.filtered(), pageSize = this.state.pageSize;
        var pages = Math.max(1, Math.ceil(rows.length / pageSize));
        this.state.page = Math.min(Math.max(1, this.state.page), pages);
        rows = rows.slice((this.state.page - 1) * pageSize, this.state.page * pageSize);
        var head = this.root.querySelector("thead"), body = this.root.querySelector("tbody");
        head.textContent = ""; body.textContent = "";
        var columns = this.state.view === "users" ? [["username","User"],["roles","Roles"],["isActive","Status"],["lastLoginAtUtc","Last login"],["actions","Actions"]] : [["displayName","Role"],["priority","Priority"],["userCount","Users"],["isActive","Status"],["actions","Actions"]];
        var tr = document.createElement("tr");
        columns.forEach(function (column) {
            var th = document.createElement("th");
            if (column[0] !== "actions" && column[0] !== "roles") {
                var button = document.createElement("button"); button.type = "button"; button.className = "bf-rbac-sort";
                button.textContent = column[1]; button.setAttribute("data-rbac-sort", column[0]);
                button.addEventListener("click", function () { self.sort(column[0]); }); th.appendChild(button);
            } else th.textContent = column[1];
            tr.appendChild(th);
        }); head.appendChild(tr);
        global.ByteForgeSortIndicator.update(head.querySelectorAll(".bf-rbac-sort"), "data-rbac-sort", { key: this.state.sort, direction: this.state.direction });
        if (!rows.length) this.empty(body, columns.length, "No matching " + this.state.view + ".");
        rows.forEach(function (row) { body.appendChild(self.state.view === "users" ? self.userRow(row) : self.roleRow(row)); });
        this.root.querySelector(".bf-rbac-pager span").textContent = rows.length ? "Page " + this.state.page + " of " + pages + " · " + this.filtered().length + " rows" : "0 rows";
        this.root.querySelector(".bf-rbac-prev").disabled = this.state.page <= 1;
        this.root.querySelector(".bf-rbac-next").disabled = this.state.page >= pages;
    };

    ByteForgeRbacAdmin.prototype.filtered = function () {
        var search = this.state.search.trim().toLowerCase(), view = this.state.view;
        var rows = (view === "users" ? this.state.users : this.state.roles).filter(function (row) {
            if (!search) return true;
            return Object.keys(row).some(function (key) { return String(row[key] == null ? "" : row[key]).toLowerCase().indexOf(search) >= 0; });
        });
        var key = this.state.sort, direction = this.state.direction === "asc" ? 1 : -1;
        return rows.slice().sort(function (a, b) { var left = a[key], right = b[key]; if (typeof left === "string") { left = left.toLowerCase(); right = String(right || "").toLowerCase(); } return left < right ? -direction : left > right ? direction : 0; });
    };

    ByteForgeRbacAdmin.prototype.sort = function (key) { this.state.direction = this.state.sort === key && this.state.direction === "asc" ? "desc" : "asc"; this.state.sort = key; this.state.page = 1; this.render(); };
    ByteForgeRbacAdmin.prototype.userRow = function (user) {
        var self = this, tr = document.createElement("tr"), identity = document.createElement("td");
        this.text(identity, "span", user.displayName || user.username, "bf-rbac-primary"); this.text(identity, "span", user.username + " · " + user.email, "bf-rbac-secondary"); tr.appendChild(identity);
        var roles = document.createElement("td"); (user.roles || []).forEach(function (role) { self.pill(roles, role, ""); }); if (user.isSuperAdmin) this.pill(roles, "Super admin", "is-global"); if (!(user.roles || []).length && !user.isSuperAdmin) this.text(roles, "span", "No roles", "bf-rbac-secondary"); tr.appendChild(roles);
        var active = user.isActive && (user.hasProjectAccess || user.isSuperAdmin), status = document.createElement("td"); this.pill(status, active ? "Active" : "Inactive", active ? "is-global" : "is-muted"); tr.appendChild(status);
        var login = document.createElement("td"); login.textContent = user.lastLoginAtUtc ? new Date(user.lastLoginAtUtc).toLocaleString() : "Never"; tr.appendChild(login);
        var actions = document.createElement("td"), wrap = document.createElement("div"); wrap.className = "bf-rbac-row-actions";
        wrap.appendChild(this.action("&#9999;&#65039; Edit", function () { self.openUser(user); }, false, user.canManage === false));
        if (user.canManage === false) this.text(wrap, "span", user.id === this.state.me.userId ? "Your account — use My profile to edit your name or password." : "Read-only: outside your management permissions.", "bf-rbac-secondary");
        if (user.canManage !== false && this.options.supportsInvitations && user.canSendInvitation) wrap.appendChild(this.action("&#128232; " + (user.hasActiveInvitation ? "Resend invite" : "Send invite"), function () { self.inviteUser(user); }, false));
        if (user.canManage !== false && user.canResendInvitation) wrap.appendChild(this.action("&#128232; Resend invitation", function () { self.inviteUser(user); }, false));
        var blocked = user.canManage === false || user.id === this.state.me.userId || user.isSuperAdmin;
        wrap.appendChild(this.action("&#10060; Remove access", function () { self.revokeUser(user); }, true, blocked)); actions.appendChild(wrap); tr.appendChild(actions); return tr;
    };
    ByteForgeRbacAdmin.prototype.roleRow = function (role) {
        var self = this, tr = document.createElement("tr"), name = document.createElement("td");
        this.text(name, "span", role.displayName || role.name, "bf-rbac-primary"); this.text(name, "span", role.name + (role.description ? " · " + role.description : ""), "bf-rbac-secondary"); tr.appendChild(name);
        [role.priority, role.userCount].forEach(function (value) { var td = document.createElement("td"); td.textContent = value; tr.appendChild(td); });
        var status = document.createElement("td"); this.pill(status, role.isActive ? "Active" : "Inactive", role.isActive ? "is-global" : "is-muted"); tr.appendChild(status);
        var actions = document.createElement("td"); actions.appendChild(this.action("&#9999;&#65039; Edit", function () { self.openRole(role); }, false, false)); tr.appendChild(actions); return tr;
    };

    // Administrative record fields are not the signed-in user's login credentials.
    ByteForgeRbacAdmin.prototype.protectProfileFields = function (form, user) {
        var fields = {}, drafts = {}, bytes = new Uint32Array(2);
        global.crypto.getRandomValues(bytes);
        var prefix = "bf-record-" + bytes[0].toString(36) + bytes[1].toString(36);
        form.setAttribute("data-lpignore", "true"); form.setAttribute("data-1p-ignore", "true"); form.setAttribute("data-form-type", "other");
        Array.prototype.forEach.call(form.querySelectorAll("[data-rbac-field]"), function (control, index) {
            var key = control.getAttribute("data-rbac-field"); fields[key] = control;
            control.name = prefix + "-" + index; control.id = control.name;
            control.setAttribute("data-lpignore", "true"); control.setAttribute("data-1p-ignore", "true"); control.setAttribute("data-bwignore", "true"); control.setAttribute("data-form-type", "other");
            var label = control.parentNode.querySelector("label"); if (label) label.htmlFor = control.id;
            drafts[key] = key === "password" ? "" : (user && user[key] || ""); control.value = drafts[key];
            control.addEventListener("input", function (event) {
                if (event.isTrusted) drafts[key] = control.value;
                else control.value = drafts[key];
            });
        });
        fields.verify = function () {
            var unchanged = true;
            Object.keys(drafts).forEach(function (key) { if (fields[key].value !== drafts[key]) { fields[key].value = drafts[key]; unchanged = false; } });
            return unchanged;
        };
        return fields;
    };

    ByteForgeRbacAdmin.prototype.openUser = function (user) {
        var self = this, editing = !!user;
        var projectName = this.state.me.projectKey, modal = this.modal(editing ? "Edit user" : "Add user", "Manage access and roles for " + projectName + ". Profile changes apply to the shared TELCO account across projects.");
        var form = document.createElement("form"); form.className = "bf-rbac-global-user-form bf-rbac-project-user-form"; form.autocomplete = "off"; modal.panel.classList.add("is-global-user-editor");
        var invitationField = !editing && this.options.supportsInvitations ? '<label class="bf-rbac-check bf-rbac-invitation-option"><input name="sendInvitation" type="checkbox"> Send invitation by email</label><span class="bf-rbac-check-help">The user will choose a password from the invitation link.</span>' : '';
        form.innerHTML = '<div class="bf-rbac-editor-tabs" role="tablist"></div><div class="bf-rbac-editor-panel" data-editor-panel="profile"><div class="bf-rbac-global-profile"><div class="bf-rbac-profile-fields"><div class="bf-rbac-field"><label>Username</label><input data-rbac-field="username" maxlength="100" autocomplete="off" autocapitalize="none" spellcheck="false" data-lpignore="true" data-1p-ignore="true" data-bwignore required></div><div class="bf-rbac-field"><label>Display name</label><input data-rbac-field="displayName" maxlength="200" required></div><div class="bf-rbac-field"><label>Email</label><input data-rbac-field="email" type="email" maxlength="254" autocomplete="off" data-lpignore="true" data-1p-ignore="true" data-bwignore required></div>' + (editing ? '<span class="bf-rbac-secondary">Profile changes apply across the TELCO projects this user can access.</span><div class="bf-rbac-field"><label>New password (optional)</label><input data-rbac-field="password" type="password" minlength="8" autocomplete="new-password" data-lpignore="true" data-1p-ignore="true" data-bwignore><span class="bf-rbac-secondary">Leave blank to keep the current password. Changing it applies to every project this user can access.</span></div>' : '<div class="bf-rbac-field"><label>Initial password (new identities only)</label><input data-rbac-field="password" type="password" autocomplete="new-password" data-lpignore="true" data-1p-ignore="true" data-bwignore><span class="bf-rbac-secondary">An exact existing global identity keeps its current password.</span></div>' + invitationField) + '</div><fieldset class="bf-rbac-group bf-rbac-project-access"><legend>Project access</legend><label class="bf-rbac-check"><input name="hasProjectAccess" type="checkbox"><span class="bf-rbac-project-access-name"></span></label><span class="bf-rbac-check-help">Turning access off also removes this project\'s role assignments.</span></fieldset></div></div><div class="bf-rbac-role-panels"><div class="bf-rbac-editor-panel" data-project-panel="roles" hidden><fieldset class="bf-rbac-group"><legend class="bf-rbac-project-roles-name"></legend><div class="bf-rbac-check-grid bf-rbac-role-options"></div></fieldset></div></div>';
        var profileFields = self.protectProfileFields(form, user); modal.panel.appendChild(form); form.querySelector(".bf-rbac-project-access-name").textContent = projectName + " access"; form.querySelector(".bf-rbac-project-roles-name").textContent = projectName + " roles"; profileFields.username.value = user ? user.username : ""; profileFields.displayName.value = user ? user.displayName : ""; profileFields.email.value = user ? user.email : ""; form.elements.hasProjectAccess.checked = user ? user.hasProjectAccess || user.isSuperAdmin : true;
        if (editing && ((this.state.me.isClientAdmin && !this.state.me.isProjectAdmin) || user.isSuperAdmin) && !this.state.me.isSuperAdmin) {
            [profileFields.username, profileFields.displayName, profileFields.email].forEach(function (control) { control.disabled = true; });
            form.querySelector(".bf-rbac-profile-fields > .bf-rbac-secondary").textContent = "Profile changes for this account require a Super Admin.";
        }
        if (!editing && this.options.inviteWithoutPassword) {
            profileFields.password.parentNode.querySelector(".bf-rbac-secondary").textContent = "Leave blank to email a password setup invitation for a new account. An existing account keeps its current password.";
        }
        profileFields.password.minLength = 8;
        if (editing && user.isSuperAdmin && !this.state.me.isSuperAdmin) profileFields.password.disabled = true;
        if (!editing) {
            if (form.elements.sendInvitation) form.elements.sendInvitation.parentNode.remove();
            profileFields.password.parentNode.querySelector(".bf-rbac-secondary").textContent = "Leave blank to email a welcome invitation with a link to create a password. Enter a password to set it directly. Existing identities keep their password.";
        }
        this.state.roles.filter(function (role) { return role.isActive || (user && (user.roleIds || []).indexOf(role.id) >= 0); }).forEach(function (role) {
            var label = document.createElement("label"); label.className = "bf-rbac-check"; var input = document.createElement("input"); input.type = "checkbox"; input.value = role.id; input.checked = !!user && (user.roleIds || []).indexOf(role.id) >= 0; label.appendChild(input); label.appendChild(document.createTextNode(role.displayName || role.name)); form.querySelector(".bf-rbac-role-options").appendChild(label);

        });
        var protectsAdminRole = user && (user.isSuperAdmin || (this.state.me && user.id === this.state.me.userId && this.state.me.isProjectAdmin));
        if (protectsAdminRole) Array.prototype.forEach.call(form.querySelectorAll(".bf-rbac-role-options input"), function (input) { var role = self.state.roles.filter(function (item) { return item.id === Number(input.value); })[0]; if (role && String(role.name).toLowerCase() === "admin") { input.checked = true; input.disabled = true; } });
        if (!form.querySelector(".bf-rbac-role-options").children.length) this.text(form.querySelector(".bf-rbac-role-options"), "span", "No roles are configured for this project.", "bf-rbac-secondary");
        var tabs = form.querySelector(".bf-rbac-editor-tabs"), profileTab = document.createElement("button"), rolesTab = document.createElement("button");
        profileTab.type = rolesTab.type = "button"; profileTab.className = rolesTab.className = "bf-rbac-tab"; profileTab.setAttribute("role", "tab"); rolesTab.setAttribute("role", "tab"); profileTab.setAttribute("aria-selected", "true"); rolesTab.setAttribute("aria-selected", "false"); profileTab.textContent = "Profile"; rolesTab.textContent = projectName + " Roles"; tabs.appendChild(profileTab); tabs.appendChild(rolesTab);
        function showPanel(name) { form.querySelector('[data-editor-panel="profile"]').hidden = name !== "profile"; form.querySelector('[data-project-panel="roles"]').hidden = name !== "roles"; profileTab.setAttribute("aria-selected", name === "profile" ? "true" : "false"); rolesTab.setAttribute("aria-selected", name === "roles" ? "true" : "false"); form.querySelector('[data-editor-panel="permissions"]').hidden = name !== "permissions"; tabs.querySelector('[data-editor-tab="permissions"]').setAttribute("aria-selected", String(name === "permissions")); }
        installPermissions(form, tabs, function () { return effectivePermissions([{ projectKey: projectName, roles: self.state.roles, privileges: self.state.privileges, hasAccess: form.elements.hasProjectAccess.checked, roleIds: Array.prototype.map.call(form.querySelectorAll(".bf-rbac-role-options input:checked"), function (input) { return Number(input.value); }) }], !user || user.isActive, !!(user && user.isSuperAdmin)); }, showPanel);
        if (user && (user.isSuperAdmin || (this.state.me && user.id === this.state.me.userId))) form.elements.hasProjectAccess.disabled = true;
        if (form.elements.sendInvitation) {
            form.elements.sendInvitation.checked = true;
            var updateInvitation = function () { var sending = form.elements.sendInvitation.checked; profileFields.password.disabled = sending; profileFields.password.required = !sending && !!self.options.requirePasswordWithoutInvitation; };
            form.elements.sendInvitation.addEventListener("change", updateInvitation); updateInvitation();
        }
        function updateRolesTab() { var hasAccess = form.elements.hasProjectAccess.checked; rolesTab.hidden = !hasAccess; if (!hasAccess && rolesTab.getAttribute("aria-selected") === "true") showPanel("profile"); }
        profileTab.addEventListener("click", function () { showPanel("profile"); }); rolesTab.addEventListener("click", function () { showPanel("roles"); }); form.elements.hasProjectAccess.addEventListener("change", updateRolesTab); updateRolesTab();
        this.dialogActions(modal, function () {
            if (!profileFields.verify()) { self.message("Profile fields changed automatically. Review the restored values before saving.", true); return; } if (!form.reportValidity()) return;
            if (!editing && !form.elements.hasProjectAccess.checked) { self.message("Grant project access before creating a user.", true); return; }
            var payload = { id: user ? user.id : 0, username: profileFields.username.value, displayName: profileFields.displayName.value, email: profileFields.email.value, password: profileFields.password && !profileFields.password.disabled ? profileFields.password.value : "", sendInvitation: !!(form.elements.sendInvitation && form.elements.sendInvitation.checked), hasProjectAccess: form.elements.hasProjectAccess.checked, roleIds: Array.prototype.map.call(form.querySelectorAll(".bf-rbac-role-options input:checked"), function (input) { return Number(input.value); }) };
            self.save("users", payload, modal);
        });
    };

    ByteForgeRbacAdmin.prototype.openRole = function (role) {
        var self = this, modal = this.modal(role ? "Edit Role" : "New Role", "", true), form = document.createElement("form"); form.className = "bf-rbac-role-form"; modal.panel.classList.add("is-role-editor");
        form.innerHTML = '<div class="bf-rbac-role-details"><div class="bf-rbac-field"><label>Role name</label><input name="name" maxlength="100" required></div><div class="bf-rbac-field"><label>Display name</label><input name="displayName" maxlength="200" required></div><div class="bf-rbac-field"><label>Description</label><textarea name="description" maxlength="1000" rows="4"></textarea></div><div class="bf-rbac-field"><label>Landing page</label><select name="landingController" required><option value="">Select a landing page</option></select></div><div class="bf-rbac-field"><label>Priority</label><input name="priority" type="number" min="0" max="10000" required><span class="bf-rbac-field-help">Higher numbers win when a user has multiple roles.</span></div><div class="bf-rbac-field"><label class="bf-rbac-check bf-rbac-role-active"><input name="isActive" type="checkbox"> Active</label></div></div><div class="bf-rbac-role-pages"><span class="bf-rbac-section-label">Permissions</span><div class="bf-rbac-role-page-picker bf-rbac-privilege-options" role="group" aria-label="Role permissions"></div></div>';
        modal.content.appendChild(form); form.elements.name.value = role ? role.name : ""; form.elements.name.readOnly = !!role; form.elements.displayName.value = role ? role.displayName : ""; form.elements.description.value = role ? role.description || "" : ""; form.elements.priority.value = role ? role.priority : 100; form.elements.isActive.checked = role ? role.isActive : true;
        var landing = form.elements.landingController, currentLanding = role ? role.landingController || "" : "";
        this.state.privileges.filter(function (privilege) { return privilege.isPage !== false; }).forEach(function (privilege) { var option = document.createElement("option"); option.value = privilege.name; option.textContent = privilege.displayName || privilege.name; landing.appendChild(option); });
        if (currentLanding && !Array.prototype.some.call(landing.options, function (option) { return option.value.toLowerCase() === currentLanding.toLowerCase(); })) { var currentOption = document.createElement("option"); currentOption.value = currentLanding; currentOption.textContent = currentLanding + " (invalid current value)"; landing.appendChild(currentOption); }
        landing.value = currentLanding;
        var category = null, group = null, picker = form.querySelector(".bf-rbac-privilege-options"); this.state.privileges.forEach(function (privilege) { var nextCategory = privilege.category || "General"; if (nextCategory !== category) { category = nextCategory; group = document.createElement("div"); group.className = "bf-rbac-role-page-group"; self.text(group, "strong", category, "bf-rbac-category"); picker.appendChild(group); } var label = document.createElement("label"), input = document.createElement("input"); label.className = "bf-rbac-check bf-rbac-page-check"; input.type = "checkbox"; input.value = privilege.name; input.checked = !!role && (role.privileges || []).indexOf(privilege.name) >= 0; label.appendChild(input); label.appendChild(document.createTextNode(privilege.displayName || privilege.name)); group.appendChild(label); });
        if (!this.state.privileges.length) this.text(picker, "span", "No permissions were published by this project.", "bf-rbac-secondary");
        this.dialogActions(modal, function () { if (!form.reportValidity()) return; self.save("roles", { id: role ? role.id : 0, name: form.elements.name.value, displayName: form.elements.displayName.value, description: form.elements.description.value, landingController: form.elements.landingController.value, priority: Number(form.elements.priority.value), isActive: form.elements.isActive.checked, privileges: Array.prototype.map.call(form.querySelectorAll(".bf-rbac-privilege-options input:checked"), function (input) { return input.value; }) }, modal); }, "Save role");
    };

    ByteForgeRbacAdmin.prototype.save = function (path, payload, modal) { var self = this; modal.save.disabled = true; this.api(path, { method: "POST", body: JSON.stringify(payload) }).then(function (result) { modal.backdrop.remove(); self.message(result.message || "Saved.", false, true); return self.load(); }).catch(function (error) { modal.save.disabled = false; self.message(error.message, true); }); };
    ByteForgeRbacAdmin.prototype.inviteUser = function (user) { var self = this; this.api("users/" + user.id + "/invite", { method: "POST", body: "{}" }).then(function (result) { self.message(result.message || "Invitation sent.", false, true); return self.load(); }).catch(function (error) { self.message(error.message, true); }); };
    ByteForgeRbacAdmin.prototype.revokeUser = function (user) { var self = this, method = this.options.revokeMethod || "DELETE"; if (!global.confirm("Remove " + user.username + " from " + this.state.me.projectKey + "? The global identity and other project access will be preserved.")) return; this.api("users/" + user.id, { method: method, body: method === "POST" ? "{}" : null }).then(function (result) { self.message(result.message, false, true); return self.load(); }).catch(function (error) { self.message(error.message, true); }); };
    ByteForgeRbacAdmin.prototype.api = function (path, init) { var self = this; init = init || {}; init.headers = init.headers || {}; init.headers.Accept = "application/json"; init.credentials = init.credentials || "same-origin"; var token = this.options.getToken(); if (token) init.headers.Authorization = "Bearer " + token; if (init.body && this.options.formEncoded) { var payload = JSON.parse(init.body), fields = new URLSearchParams(); Object.keys(payload).forEach(function (key) { var value = payload[key]; if (Array.isArray(value)) value.forEach(function (item) { fields.append(key, item); }); else fields.append(key, value == null ? "" : value); }); if (typeof this.options.getAntiForgeryToken === "function") fields.append("__RequestVerificationToken", this.options.getAntiForgeryToken() || ""); init.body = fields.toString(); init.headers["Content-Type"] = "application/x-www-form-urlencoded; charset=UTF-8"; } else if (init.body) init.headers["Content-Type"] = "application/json"; return fetch(this.options.apiBase.replace(/\/$/, "") + "/" + path, init).then(function (response) { if ((response.status === 401 || isAuthenticationRedirect(response)) && typeof self.options.onUnauthorized === "function") self.options.onUnauthorized(); return response.text().then(function (text) { var data = {}, parsed = true; try { data = text ? JSON.parse(text) : {}; } catch (ignored) { parsed = false; data = { message: text }; } if (!response.ok) throw new Error(data.message || data.Message || data.exceptionMessage || data.ExceptionMessage || data.error || "RBAC request failed with HTTP " + response.status + "."); if (!parsed) throw new Error("The RBAC endpoint returned an unexpected response. Sign in again and retry."); return data; }); }); };
    ByteForgeRbacAdmin.prototype.modal = function (title, intro, structured) { var self = this, backdrop = document.createElement("div"), panel = document.createElement("div"), content = panel; backdrop.className = "bf-rbac-dialog-backdrop"; panel.className = "bf-rbac-dialog"; panel.setAttribute("role", "dialog"); panel.setAttribute("aria-modal", "true"); if (structured) { panel.classList.add("is-structured"); var header = document.createElement("div"), close = document.createElement("button"); header.className = "bf-rbac-dialog-header"; self.text(header, "h3", title, ""); close.type = "button"; close.className = "bf-rbac-dialog-close"; close.innerHTML = "&#10006;&#65039;"; close.setAttribute("aria-label", "Close dialog"); close.title = "Close"; close.addEventListener("click", function () { backdrop.remove(); }); header.appendChild(close); panel.appendChild(header); content = document.createElement("div"); content.className = "bf-rbac-dialog-body"; panel.appendChild(content); } else { this.text(panel, "h3", title, ""); this.text(panel, "p", intro, "bf-rbac-dialog-intro"); } backdrop.appendChild(panel); document.body.appendChild(backdrop); backdrop.addEventListener("click", function (event) { if (event.target === backdrop) backdrop.remove(); }); return { backdrop: backdrop, panel: panel, content: content, structured: !!structured }; };
    ByteForgeRbacAdmin.prototype.dialogActions = function (modal, onSave, saveLabel) { var actions = document.createElement("div"), cancel = document.createElement("button"), save = document.createElement("button"); actions.className = "bf-rbac-dialog-actions"; cancel.type = save.type = "button"; cancel.className = "bf-rbac-button is-secondary"; save.className = "bf-rbac-button"; cancel.textContent = "Cancel"; save.innerHTML = saveLabel || "&#128190; Save"; cancel.addEventListener("click", function () { modal.backdrop.remove(); }); save.addEventListener("click", onSave); actions.appendChild(cancel); actions.appendChild(save); modal.panel.appendChild(actions); modal.save = save; setTimeout(function () { var first = modal.panel.querySelector("input:not([disabled]):not([readonly]), textarea:not([disabled]), select:not([disabled])"); if (first) first.focus(); }, 0); };
    ByteForgeRbacAdmin.prototype.action = function (html, handler, danger, disabled) { var button = document.createElement("button"); button.type = "button"; button.className = "bf-rbac-action" + (danger ? " is-danger" : ""); button.innerHTML = html; button.disabled = !!disabled; button.addEventListener("click", handler); return button; };
    ByteForgeRbacAdmin.prototype.pill = function (parent, text, modifier) { return this.text(parent, "span", text, "bf-rbac-pill " + modifier); };
    ByteForgeRbacAdmin.prototype.text = function (parent, tag, text, className) { var node = document.createElement(tag); node.textContent = text == null ? "" : text; if (className) node.className = className; parent.appendChild(node); return node; };
    ByteForgeRbacAdmin.prototype.empty = function (body, columns, text) { var row = document.createElement("tr"), cell = document.createElement("td"); cell.colSpan = columns; cell.className = "bf-rbac-empty"; cell.textContent = text; row.appendChild(cell); body.appendChild(row); };
    ByteForgeRbacAdmin.prototype.message = function (text, error, success) {
        var message = this.root.querySelector(".bf-rbac-message");
        var dialogs = document.querySelectorAll(".bf-rbac-dialog-backdrop");
        if (text && dialogs.length) {
            message.textContent = ""; message.hidden = true;
            var previousFocus = document.activeElement;
            var modal = this.modal(error ? "Error" : success ? "Success" : "Information", "");
            var level = 1000;
            Array.prototype.forEach.call(dialogs, function (dialog) { level = Math.max(level, parseInt(global.getComputedStyle(dialog).zIndex, 10) || 1000); });
            modal.backdrop.style.zIndex = String(level + 1);
            modal.panel.setAttribute("role", error ? "alertdialog" : "dialog");
            modal.panel.setAttribute("aria-label", error ? "Error" : success ? "Success" : "Information");
            var body = this.text(modal.panel, "p", text, "bf-rbac-message" + (error ? " is-error" : success ? " is-success" : ""));
            body.style.whiteSpace = "pre-wrap";
            var actions = document.createElement("div"), close = document.createElement("button");
            actions.className = "bf-rbac-dialog-actions";
            close.type = "button"; close.className = "bf-rbac-button"; close.textContent = "OK";
            function dismiss() { modal.backdrop.remove(); if (previousFocus && previousFocus.isConnected) previousFocus.focus(); }
            close.addEventListener("click", dismiss);
            modal.backdrop.addEventListener("click", function (event) { if (event.target === modal.backdrop) dismiss(); });
            modal.backdrop.addEventListener("keydown", function (event) {
                if (event.key === "Escape") { event.preventDefault(); event.stopPropagation(); dismiss(); }
                else if (event.key === "Tab") { event.preventDefault(); close.focus(); }
            });
            actions.appendChild(close); modal.panel.appendChild(actions); close.focus();
            return;
        }
        message.textContent = text || ""; message.className = "bf-rbac-message" + (error ? " is-error" : success ? " is-success" : ""); message.hidden = !text;
    };

    function ByteForgeGlobalRbacAdmin(options) {
        if (!options || !options.root || !options.apiBase || !options.getToken) throw new Error("Global RBAC root, API base, and token provider are required.");
        this.options = options; this.root = options.root;
        this.state = { search: "", page: 1, pageSize: 10, sort: "username", direction: "asc", users: [], projects: [], me: null };
        this.build();
    }

    ByteForgeGlobalRbacAdmin.prototype.build = function () {
        this.root.classList.add("byte-forge-rbac");
        this.root.innerHTML = '<div class="bf-rbac-header"><div><h2 class="bf-rbac-title">Users</h2><p class="bf-rbac-subtitle">Manage global TELCO identities, project access, and super-administrator status.</p></div><div class="bf-rbac-actions"><input class="bf-rbac-search" type="search" aria-label="Search global identities" placeholder="Search identities"><button class="bf-rbac-button is-secondary bf-rbac-settings-button" type="button" aria-expanded="false">&#9881;&#65039; Settings</button><button class="bf-rbac-button bf-rbac-add" type="button">&#10133; Add identity</button></div></div><div class="bf-rbac-message" role="status" hidden></div><div class="bf-rbac-settings" hidden><label>Rows per page <select><option>10</option><option>25</option><option>50</option></select></label></div><div class="bf-rbac-table-wrap"><table class="bf-rbac-table"><thead></thead><tbody></tbody></table></div><div class="bf-rbac-pager"><button class="bf-rbac-button is-secondary bf-rbac-prev" type="button">Previous</button><span></span><button class="bf-rbac-button is-secondary bf-rbac-next" type="button">Next</button></div>';
        var self = this;
        this.root.querySelector(".bf-rbac-search").addEventListener("input", function () { self.state.search = this.value; self.state.page = 1; self.render(); });
        this.root.querySelector(".bf-rbac-settings-button").addEventListener("click", function () { var panel = self.root.querySelector(".bf-rbac-settings"), open = panel.hidden; panel.hidden = !open; this.setAttribute("aria-expanded", open ? "true" : "false"); });
        this.root.querySelector(".bf-rbac-settings select").addEventListener("change", function () { self.state.pageSize = Number(this.value); self.state.page = 1; self.render(); });
        this.root.querySelector(".bf-rbac-add").addEventListener("click", function () { self.openUser(); });
        this.root.querySelector(".bf-rbac-prev").addEventListener("click", function () { self.state.page -= 1; self.render(); });
        this.root.querySelector(".bf-rbac-next").addEventListener("click", function () { self.state.page += 1; self.render(); });
    };

    ByteForgeGlobalRbacAdmin.prototype.load = function () {
        var self = this, add = this.root.querySelector(".bf-rbac-add"); this.message("Loading global identities...", false); add.disabled = true;
        return Promise.all([this.api("me"), this.api("users")]).then(function (values) {
            self.state.me = values[0]; self.state.users = values[1] || []; self.message("Loading project access...", false); self.render();
            return self.api("projects").then(function (projects) { self.state.projects = projects || []; add.disabled = false; add.removeAttribute("title"); self.message("", false); return self; }).catch(function (error) { self.state.projects = []; add.disabled = true; add.title = "Project catalogs are unavailable."; self.message("Identities loaded, but project access is unavailable: " + error.message, true); return self; });
        }).catch(function (error) { self.message("Global identities could not be loaded: " + error.message, true); throw error; });
    };

    ByteForgeGlobalRbacAdmin.prototype.filtered = function () {
        var search = this.state.search.trim().toLowerCase(), key = this.state.sort, direction = this.state.direction === "asc" ? 1 : -1;
        return this.state.users.filter(function (user) { return !search || [user.username, user.displayName, user.email].some(function (value) { return String(value || "").toLowerCase().indexOf(search) >= 0; }); }).sort(function (a, b) { var left = a[key], right = b[key]; if (typeof left === "string") { left = left.toLowerCase(); right = String(right || "").toLowerCase(); } return left < right ? -direction : left > right ? direction : 0; });
    };

    ByteForgeGlobalRbacAdmin.prototype.render = function () {
        var self = this, filtered = this.filtered(), pages = Math.max(1, Math.ceil(filtered.length / this.state.pageSize)); this.state.page = Math.min(Math.max(1, this.state.page), pages);
        var rows = filtered.slice((this.state.page - 1) * this.state.pageSize, this.state.page * this.state.pageSize), head = this.root.querySelector("thead"), body = this.root.querySelector("tbody"); head.textContent = ""; body.textContent = "";
        var columns = [["username","User"],["isSuperAdmin","Global access"],["isActive","Status"],["lastLoginAtUtc","Last login"],["actions","Actions"]], tr = document.createElement("tr");
        columns.forEach(function (column) { var th = document.createElement("th"); if (column[0] !== "actions") { var button = document.createElement("button"); button.type = "button"; button.className = "bf-rbac-sort"; button.textContent = column[1]; button.setAttribute("data-global-rbac-sort", column[0]); button.addEventListener("click", function () { self.state.direction = self.state.sort === column[0] && self.state.direction === "asc" ? "desc" : "asc"; self.state.sort = column[0]; self.state.page = 1; self.render(); }); th.appendChild(button); } else th.textContent = column[1]; tr.appendChild(th); }); head.appendChild(tr);
        global.ByteForgeSortIndicator.update(head.querySelectorAll(".bf-rbac-sort"), "data-global-rbac-sort", { key: this.state.sort, direction: this.state.direction });
        if (!rows.length) this.empty(body, columns.length, "No matching global identities.");
        rows.forEach(function (user) { var row = document.createElement("tr"), identity = document.createElement("td"); self.text(identity, "span", user.displayName || user.username, "bf-rbac-primary"); self.text(identity, "span", user.username + " · " + user.email, "bf-rbac-secondary"); row.appendChild(identity); var globalAccess = document.createElement("td"); self.pill(globalAccess, user.isSuperAdmin ? "Super administrator" : "Standard identity", user.isSuperAdmin ? "is-global" : "is-muted"); row.appendChild(globalAccess); var status = document.createElement("td"); self.pill(status, user.isActive ? "Active" : "Inactive", user.isActive ? "is-global" : "is-muted"); row.appendChild(status); var login = document.createElement("td"); login.textContent = user.lastLoginAtUtc ? new Date(user.lastLoginAtUtc).toLocaleString() : "Never"; row.appendChild(login); var actions = document.createElement("td"); actions.appendChild(self.action("&#9999;&#65039; Edit", function () { self.openUser(user); }, false)); if (user.canResendInvitation) actions.appendChild(self.action("&#128232; Resend invitation", function () { self.inviteUser(user); }, false)); if (user.canDelete && !user.isSuperAdmin && user.id !== self.state.me.userId) actions.appendChild(self.action("&#10060; Delete", function () { self.deleteUser(user); }, false)); row.appendChild(actions); body.appendChild(row); });
        this.root.querySelector(".bf-rbac-pager span").textContent = filtered.length ? "Page " + this.state.page + " of " + pages + " · " + filtered.length + " rows" : "0 rows"; this.root.querySelector(".bf-rbac-prev").disabled = this.state.page <= 1; this.root.querySelector(".bf-rbac-next").disabled = this.state.page >= pages;
    };

    ByteForgeGlobalRbacAdmin.prototype.deleteUser = function (user) {
        var self = this;
        if (!global.confirm("Permanently delete " + user.username + "? This identity has no project access. This cannot be undone.")) return;
        this.api("users/" + user.id, { method: "DELETE" }).then(function (result) { self.message(result.message, false, true); return self.load(); }).catch(function (error) { self.message(error.message, true); });
    };

    ByteForgeGlobalRbacAdmin.prototype.openUser = function (user) {
        var self = this, editing = !!user, modal = this.modal(editing ? "Edit user" : "Add user", "Manage the global profile, project access, and project-specific roles."), form = document.createElement("form");
        form.className = "bf-rbac-global-user-form"; form.autocomplete = "off"; modal.panel.classList.add("is-global-user-editor");
        form.innerHTML = '<div class="bf-rbac-editor-tabs" role="tablist"></div><div class="bf-rbac-editor-panel" data-editor-panel="profile"><div class="bf-rbac-global-profile"><div class="bf-rbac-profile-fields"><div class="bf-rbac-field"><label>Username</label><input data-rbac-field="username" maxlength="100" autocomplete="off" autocapitalize="none" spellcheck="false" data-lpignore="true" data-1p-ignore="true" data-bwignore required></div><div class="bf-rbac-field"><label>Display name</label><input data-rbac-field="displayName" maxlength="200" required></div><div class="bf-rbac-field"><label>Email</label><input data-rbac-field="email" type="email" maxlength="254" autocomplete="off" data-lpignore="true" data-1p-ignore="true" data-bwignore required></div><div class="bf-rbac-field"><label>' + (editing ? 'New password (optional)' : 'Initial password (optional)') + '</label><input data-rbac-field="password" type="password" autocomplete="new-password" data-lpignore="true" data-1p-ignore="true" data-bwignore><span class="bf-rbac-secondary">' + (editing ? 'Leave blank to keep the current password.' : 'If left blank, an invitation is sent for every selected project.') + '</span></div><div class="bf-rbac-profile-status"><label class="bf-rbac-check"><input name="isActive" type="checkbox"> Account active</label><label class="bf-rbac-check"><input name="isSuperAdmin" type="checkbox"> Super administrator</label><span class="bf-rbac-check-help">Grants access to every configured project and assigns its Admin role.</span></div></div><fieldset class="bf-rbac-group bf-rbac-project-access"><legend>Project access</legend><div class="bf-rbac-check-grid bf-rbac-project-options"></div><span class="bf-rbac-check-help">Select at least one project when creating a user. Each selected project gets its own Roles tab.</span></fieldset></div></div><div class="bf-rbac-role-panels"></div>';
        var profileFields = self.protectProfileFields(form, user); modal.panel.appendChild(form); profileFields.username.value = user ? user.username : ""; profileFields.displayName.value = user ? user.displayName : ""; profileFields.email.value = user ? user.email : ""; form.elements.isActive.checked = user ? user.isActive : true; form.elements.isSuperAdmin.checked = user ? user.isSuperAdmin : false;
        if (user && this.state.me && user.id === this.state.me.userId) { form.elements.isActive.disabled = true; form.elements.isSuperAdmin.disabled = true; }
        var tabs = form.querySelector(".bf-rbac-editor-tabs"), profileTab = document.createElement("button"); profileTab.type = "button"; profileTab.className = "bf-rbac-tab"; profileTab.setAttribute("role", "tab"); profileTab.setAttribute("aria-selected", "true"); profileTab.textContent = "Profile"; tabs.appendChild(profileTab);
        function assignment(project) { return (project.users || []).filter(function (item) { return user && item.id === user.id; })[0] || { hasProjectAccess: false, roleIds: [] }; }
        this.state.projects.forEach(function (project) {
            var current = assignment(project), key = project.projectKey;
            var label = document.createElement("label"), access = document.createElement("input"); label.className = "bf-rbac-check"; access.type = "checkbox"; access.value = key; access.checked = !!current.hasProjectAccess; label.appendChild(access); label.appendChild(document.createTextNode(project.displayName || key)); form.querySelector(".bf-rbac-project-options").appendChild(label);
            var panel = document.createElement("div"); panel.className = "bf-rbac-editor-panel"; panel.hidden = true; panel.setAttribute("data-project-panel", key);
            var group = document.createElement("fieldset"); group.className = "bf-rbac-group"; var legend = document.createElement("legend"); legend.textContent = (project.displayName || key) + " roles"; group.appendChild(legend);
            var options = document.createElement("div"); options.className = "bf-rbac-check-grid bf-rbac-project-role-options"; options.setAttribute("data-project-roles", key);
            (project.roles || []).filter(function (role) { return role.isActive || (current.roleIds || []).indexOf(role.id) >= 0; }).forEach(function (role) { var roleLabel = document.createElement("label"), roleInput = document.createElement("input"); roleLabel.className = "bf-rbac-check"; roleInput.type = "checkbox"; roleInput.value = role.id; roleInput.setAttribute("data-role-name", (role.name || "").toLowerCase()); roleInput.checked = (current.roleIds || []).indexOf(role.id) >= 0; roleLabel.appendChild(roleInput); roleLabel.appendChild(document.createTextNode(role.displayName || role.name)); options.appendChild(roleLabel); });
            if (!(project.roles || []).length) self.text(options, "span", "No roles are configured for this project.", "bf-rbac-secondary"); group.appendChild(options); panel.appendChild(group); form.querySelector(".bf-rbac-role-panels").appendChild(panel);
        });
        function showPanel(name) { Array.prototype.forEach.call(form.querySelectorAll(".bf-rbac-editor-panel"), function (panel) { panel.hidden = panel.getAttribute("data-editor-panel") !== name && panel.getAttribute("data-project-panel") !== name; }); Array.prototype.forEach.call(tabs.querySelectorAll(".bf-rbac-tab"), function (tab) { tab.setAttribute("aria-selected", tab.getAttribute("data-project") === name || tab.getAttribute("data-editor-tab") === name || (name === "profile" && !tab.hasAttribute("data-project") && !tab.hasAttribute("data-editor-tab")) ? "true" : "false"); }); }
        installPermissions(form, tabs, function () {
            return effectivePermissions(self.state.projects.map(function (project) {
                var access = form.querySelector('.bf-rbac-project-options input[value="' + project.projectKey + '"]');
                return { projectKey: project.projectKey, displayName: project.displayName, roles: project.roles, privileges: project.privileges, hasAccess: access.checked, roleIds: Array.prototype.map.call(form.querySelectorAll('[data-project-roles="' + project.projectKey + '"] input:checked'), function (input) { return Number(input.value); }) };
            }), form.elements.isActive.checked, form.elements.isSuperAdmin.checked);
        }, showPanel);
        profileTab.addEventListener("click", function () { showPanel("profile"); });
        function rebuildRoleTabs() { Array.prototype.forEach.call(tabs.querySelectorAll("[data-project]"), function (tab) { tab.remove(); }); Array.prototype.forEach.call(form.querySelectorAll(".bf-rbac-project-options input:checked"), function (access) { var project = self.state.projects.filter(function (item) { return item.projectKey === access.value; })[0], tab = document.createElement("button"); tab.type = "button"; tab.className = "bf-rbac-tab"; tab.setAttribute("role", "tab"); tab.setAttribute("aria-selected", "false"); tab.setAttribute("data-project", access.value); tab.textContent = (project ? project.displayName : access.value) + " Roles"; tab.addEventListener("click", function () { showPanel(access.value); }); tabs.insertBefore(tab, tabs.querySelector('[data-editor-tab="permissions"]')); }); var visible = tabs.querySelector('[aria-selected="true"]'); if (!visible) showPanel("profile"); }
        Array.prototype.forEach.call(form.querySelectorAll(".bf-rbac-project-options input"), function (access) { access.addEventListener("change", function () { if (!this.checked && tabs.querySelector('[data-project="' + this.value + '"][aria-selected="true"]')) showPanel("profile"); rebuildRoleTabs(); }); });
        function applySuperAdminState() { var enabled = form.elements.isSuperAdmin.checked; Array.prototype.forEach.call(form.querySelectorAll(".bf-rbac-project-options input"), function (access) { if (enabled) access.checked = true; access.disabled = enabled; }); Array.prototype.forEach.call(form.querySelectorAll('.bf-rbac-project-role-options input[data-role-name="admin"]'), function (role) { if (enabled) role.checked = true; role.disabled = enabled; }); rebuildRoleTabs(); }
        form.elements.isSuperAdmin.addEventListener("change", applySuperAdminState); applySuperAdminState();
        this.dialogActions(modal, function () { if (!profileFields.verify()) { self.message("Profile fields changed automatically. Review the restored values before saving.", true); return; } if (!form.reportValidity()) return; var projects = self.state.projects.map(function (project) { var access = form.querySelector('.bf-rbac-project-options input[value="' + project.projectKey + '"]'), roles = form.querySelectorAll('[data-project-roles="' + project.projectKey + '"] input:checked'); return { projectKey: project.projectKey, hasAccess: !!access.checked, roleIds: Array.prototype.map.call(roles, function (input) { return Number(input.value); }) }; }); if (!editing && !projects.some(function (project) { return project.hasAccess; })) { self.message("Select at least one project for the new user.", true); return; } var payload = { id: user ? user.id : 0, username: profileFields.username.value, displayName: profileFields.displayName.value, email: profileFields.email.value, password: profileFields.password.value, isActive: form.elements.isActive.checked, isSuperAdmin: form.elements.isSuperAdmin.checked, projects: projects }; modal.save.disabled = true; self.api("users", { method: "POST", body: JSON.stringify(payload) }).then(function (result) { modal.backdrop.remove(); self.message(result.message || "Saved.", false, true); return self.load(); }).catch(function (error) { modal.save.disabled = false; self.message(error.message, true); }); });
    };

    ByteForgeGlobalRbacAdmin.prototype.inviteUser = ByteForgeRbacAdmin.prototype.inviteUser;
    ByteForgeGlobalRbacAdmin.prototype.api = ByteForgeRbacAdmin.prototype.api;
    ByteForgeGlobalRbacAdmin.prototype.protectProfileFields = ByteForgeRbacAdmin.prototype.protectProfileFields;
    ByteForgeGlobalRbacAdmin.prototype.modal = ByteForgeRbacAdmin.prototype.modal;
    ByteForgeGlobalRbacAdmin.prototype.dialogActions = ByteForgeRbacAdmin.prototype.dialogActions;
    ByteForgeGlobalRbacAdmin.prototype.action = ByteForgeRbacAdmin.prototype.action;
    ByteForgeGlobalRbacAdmin.prototype.pill = ByteForgeRbacAdmin.prototype.pill;
    ByteForgeGlobalRbacAdmin.prototype.text = ByteForgeRbacAdmin.prototype.text;
    ByteForgeGlobalRbacAdmin.prototype.empty = ByteForgeRbacAdmin.prototype.empty;
    ByteForgeGlobalRbacAdmin.prototype.message = ByteForgeRbacAdmin.prototype.message;
    global.ByteForgeRbacAdmin = ByteForgeRbacAdmin;
    global.ByteForgeGlobalRbacAdmin = ByteForgeGlobalRbacAdmin;
}(window));
