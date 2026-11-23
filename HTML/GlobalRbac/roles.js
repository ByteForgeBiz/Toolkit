(function () {
    "use strict";
    var sessionPanel = document.getElementById("session-panel"), workspace = document.getElementById("workspace"), projectEditor;

    function redirectToLogin() {
        var returnUrl = window.location.pathname + window.location.search + window.location.hash;
        window.location.replace("login.html?returnUrl=" + encodeURIComponent(returnUrl));
    }

    function api(path, init) {
        init = init || {}; init.headers = init.headers || {}; init.headers.Accept = "application/json";
        return fetch(path, init).then(function (response) { return response.text().then(function (text) {
            var data = {}; try { data = text ? JSON.parse(text) : {}; } catch (ignored) { data = { message: text }; }
            if (!response.ok) { if (response.status === 401) redirectToLogin(); var error = new Error(data.message || data.Message || data.exceptionMessage || data.ExceptionMessage || data.error || "Request failed with HTTP " + response.status + "."); error.status = response.status; throw error; }
            return data;
        }); });
    }

    function openProject(projectKey) {
        var root = document.getElementById("project-editor"); root.textContent = "";
        Array.prototype.forEach.call(document.querySelectorAll(".project-tab"), function (tab) { tab.setAttribute("aria-selected", tab.getAttribute("data-project") === projectKey ? "true" : "false"); });
        projectEditor = new ByteForgeRbacAdmin({ root: root, apiBase: "api/projects/" + encodeURIComponent(projectKey), getToken: function () { return null; }, onUnauthorized: redirectToLogin, rolesOnly: true });
        projectEditor.load();
    }

    function loadProjects() {
        var tabs = document.getElementById("project-tabs"), error = document.getElementById("catalog-error");
        api("api/projects").then(function (projects) {
            if (!projects.length) { error.textContent = "No project catalogs were discovered."; error.hidden = false; return; }
            projects.forEach(function (project, index) {
                var tab = document.createElement("button"); tab.type = "button"; tab.className = "project-tab"; tab.setAttribute("role", "tab"); tab.setAttribute("data-project", project.projectKey); tab.setAttribute("aria-selected", index === 0 ? "true" : "false"); tab.textContent = project.displayName; tab.addEventListener("click", function () { openProject(project.projectKey); }); tabs.appendChild(tab);
            });
            openProject(projects[0].projectKey);
        }).catch(function (reason) { error.textContent = reason.message; error.hidden = false; });
    }

    function showWorkspace(me) {
        sessionPanel.hidden = true; workspace.hidden = false; document.getElementById("sign-out").hidden = false;
        document.getElementById("session-status").textContent = me.displayName || me.username || "Global administrator";
        if (!me.isSuperAdmin) {
            document.querySelector('a[href="projects.html"]').hidden = true;
            document.querySelector(".page-heading p").textContent = "Manage eligible project roles using only pages and permissions approved by your Super Admin.";
        }
        loadProjects();
    }

    document.getElementById("sign-out").addEventListener("click", function () { api("api/auth/logout", { method: "POST" }).then(function () { window.location.replace("login.html"); }); });
    api("api/auth/me").then(showWorkspace).catch(function (reason) { if (reason.status !== 401) { sessionPanel.querySelector("h2").textContent = "Roles could not be opened"; sessionPanel.querySelector("p").textContent = reason.message; } });
}());
