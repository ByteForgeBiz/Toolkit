(function () {
    "use strict";
    var sessionPanel = document.getElementById("session-panel"), workspace = document.getElementById("workspace");
    var globalEditor;

    function redirectToLogin() {
        var returnUrl = window.location.pathname + window.location.search + window.location.hash;
        window.location.replace("login.html?returnUrl=" + encodeURIComponent(returnUrl));
    }

    function api(path, init) {
        init = init || {}; init.headers = init.headers || {}; init.headers.Accept = "application/json";
        if (init.body) init.headers["Content-Type"] = "application/json";
        return fetch(path, init).then(function (response) { return response.text().then(function (text) {
            var data = {}; try { data = text ? JSON.parse(text) : {}; } catch (ignored) { data = { message: text }; }
            if (!response.ok) { if (response.status === 401) redirectToLogin(); var error = new Error(data.message || data.Message || data.exceptionMessage || data.ExceptionMessage || data.error || "Request failed with HTTP " + response.status + "."); error.status = response.status; throw error; }
            return data;
        }); });
    }

    function showWorkspace(me) {
        sessionPanel.hidden = true; workspace.hidden = false; document.getElementById("sign-out").hidden = false;
        document.getElementById("session-status").textContent = me.displayName || me.username || "Global administrator";
        if (!me.isSuperAdmin) {
            document.querySelector('a[href="projects.html"]').hidden = true;
            var root = document.getElementById("global-editor"); root.textContent = "";
            var intro = document.createElement("p"); intro.textContent = "Client Admin: manage users within your assigned projects and approved permissions."; root.appendChild(intro);
            var tabs = document.createElement("div"), editorRoot = document.createElement("div"); tabs.className = "project-tabs";
            tabs.setAttribute("role", "tablist"); tabs.setAttribute("aria-label", "Your projects"); root.appendChild(tabs); root.appendChild(editorRoot);
            function openProject(project, selected) {
                Array.prototype.forEach.call(tabs.children, function (tab) { tab.setAttribute("aria-selected", tab === selected ? "true" : "false"); });
                editorRoot.textContent = "";
                new ByteForgeRbacAdmin({ root: editorRoot, apiBase: "api/projects/" + encodeURIComponent(project.projectKey), usersOnly: true,
                    getToken: function () { return null; }, onUnauthorized: redirectToLogin }).load().catch(function () {});
            }
            api("api/projects").then(function (projects) {
                if (!projects.length) { intro.textContent = "No active projects are assigned to you."; return; }
                projects.forEach(function (project, index) {
                    var tab = document.createElement("button"); tab.type = "button"; tab.className = "project-tab"; tab.setAttribute("role", "tab");
                    tab.textContent = project.displayName; tab.addEventListener("click", function () { openProject(project, tab); }); tabs.appendChild(tab);
                    if (index === 0) openProject(project, tab);
                });
            }).catch(function (reason) { intro.textContent = reason.message; });
            return;
        }
        if (!globalEditor) globalEditor = new ByteForgeGlobalRbacAdmin({ root: document.getElementById("global-editor"), apiBase: "api/global", getToken: function () { return null; }, onUnauthorized: redirectToLogin });
        globalEditor.load();
    }

    document.getElementById("sign-out").addEventListener("click", function () { api("api/auth/logout", { method: "POST" }).then(function () { window.location.replace("login.html"); }); });
    api("api/auth/me").then(showWorkspace).catch(function (reason) { if (reason.status !== 401) { sessionPanel.querySelector("h2").textContent = "Global RBAC could not be opened"; sessionPanel.querySelector("p").textContent = reason.message; } });
}());
