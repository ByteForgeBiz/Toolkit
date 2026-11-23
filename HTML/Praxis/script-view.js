(function () {
    "use strict";
    var key = new URLSearchParams(window.location.search).get("scriptKey") || "";
    var status = document.getElementById("script-status");
    var content = document.getElementById("script-content");
    var refresh = document.getElementById("script-refresh");
    /** Loads saved text anonymously; never substitutes customer or agent information. */
    function load() {
        refresh.disabled = true;
        content.textContent = "";
        status.hidden = false;
        status.textContent = "Loading scripts...";
        fetch("api/public-scripts?scriptKey=" + encodeURIComponent(key), { cache: "no-store", credentials: "omit" })
            .then(function (response) {
                if (!response.ok) throw new Error(response.status === 404 ? "This script is unavailable." : "Scripts could not be loaded. Please retry.");
                return response.json();
            }).then(function (model) {
                var scripts = model.scripts || [];
                scripts.forEach(function (script) {
                    var article = document.createElement("article");
                    article.className = "bf-script-text";
                    var title = document.createElement("h2");
                    title.textContent = script.displayName;
                    article.appendChild(title);
                    var updated = document.createElement("p");
                    updated.className = "bf-script-updated";
                    updated.textContent = "Last saved " + new Date(/Z$|[+-]\d\d:\d\d$/.test(script.updatedAtUtc) ? script.updatedAtUtc : script.updatedAtUtc + "Z").toLocaleString();
                    article.appendChild(updated);
                    var body = document.createElement("div");
                    body.innerHTML = ByteForgeScriptHtml.sanitize(script.bodyHtml || "");
                    article.appendChild(body);
                    content.appendChild(article);
                });
                if (scripts.length === 1) document.title = document.getElementById("script-title").textContent = scripts[0].displayName;
                status.textContent = scripts.length ? "" : "No active scripts are available.";
                status.hidden = scripts.length > 0;
            }).catch(function (error) { status.textContent = error.message; })
            .finally(function () { refresh.disabled = false; });
    }
    refresh.addEventListener("click", load);
    load();
}());
