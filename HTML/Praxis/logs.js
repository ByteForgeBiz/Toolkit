(function () {
    "use strict";
    var status = document.getElementById("logs-status");
    if (status) {
        var token = "";
        try { token = sessionStorage.getItem("praxis.reportPortal.accessToken") || localStorage.getItem("praxis.reportPortal.persistentAccessToken") || ""; } catch (ignored) { }
        function login() { location.replace("login.html?returnUrl=logs.html"); }
        if (!token) { login(); return; }
        fetch("api/config-session/logs", {
            method: "POST", credentials: "same-origin", cache: "no-store",
            headers: { Authorization: "Bearer " + token, Accept: "application/json" }
        }).then(function (response) {
            if (response.status === 401) { login(); return; }
            if (!response.ok) throw new Error("Your Praxis role does not grant Application Logs access, or the service is unavailable.");
            location.replace("Admin/Logs");
        }).catch(function (error) { status.textContent = error.message; });
        return;
    }
    var preview = document.getElementById("bf-log-preview");
    var older = document.getElementById("bf-log-older");
    var message = document.getElementById("bf-log-message");
    var dialog = document.getElementById("bf-log-dialog");
    var generation = 0;
    if (dialog) {
        document.getElementById("bf-log-close").addEventListener("click", function () { dialog.close(); });
        dialog.addEventListener("close", function () { generation++; });
        document.querySelectorAll(".bf-log-open").forEach(function (link) {
            link.addEventListener("click", function (event) {
                event.preventDefault();
                var request = ++generation;
                document.getElementById("bf-log-title").textContent = link.dataset.name;
                var download = document.getElementById("bf-log-download");
                download.href = link.dataset.download;
                download.hidden = false;
                preview.textContent = "";
                older.hidden = true;
                older.disabled = false;
                older.dataset.url = link.dataset.chunk;
                message.textContent = "Loading log...";
                dialog.showModal();
                fetch(link.dataset.chunk, { credentials: "same-origin", cache: "no-store" })
                    .then(function (response) {
                        if (!response.ok) throw new Error("Log could not be loaded. Close and reopen the preview to retry.");
                        return response.json();
                    }).then(function (chunk) {
                        if (request !== generation) return;
                        preview.textContent = chunk.Content;
                        older.dataset.before = chunk.Before;
                        older.hidden = chunk.Before <= 0;
                        preview.scrollTop = preview.scrollHeight;
                        message.textContent = "Newest entries shown. Use your browser's Find to search loaded text.";
                    }).catch(function (error) { if (request === generation) message.textContent = error.message; });
            });
        });
    }
    if (preview) preview.scrollTop = preview.scrollHeight;
    if (older) older.addEventListener("click", function () {
        var request = generation;
        older.disabled = true;
        message.textContent = "Loading older entries...";
        var previousHeight = preview.scrollHeight;
        var previousTop = preview.scrollTop;
        fetch(older.dataset.url + "&before=" + encodeURIComponent(older.dataset.before), { credentials: "same-origin", cache: "no-store" })
            .then(function (response) {
                if (!response.ok) throw new Error("Older entries could not be loaded. Reopen Application Logs to check your session, then retry.");
                return response.json();
            }).then(function (chunk) {
                if (request !== generation) return;
                preview.textContent = chunk.Content + preview.textContent;
                older.dataset.before = chunk.Before;
                older.hidden = chunk.Before <= 0;
                preview.scrollTop = previousTop + preview.scrollHeight - previousHeight;
                message.textContent = chunk.Before > 0 ? "Older entries loaded." : "Start of file loaded.";
            }).catch(function (error) { if (request === generation) message.textContent = error.message; })
            .finally(function () { if (request === generation) older.disabled = false; });
    });
}());
