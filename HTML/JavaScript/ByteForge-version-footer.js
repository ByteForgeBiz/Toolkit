(function () {
    "use strict";
    var script = document.currentScript;
    var url = script && script.getAttribute("data-version-url");
    if (!url) return;
    fetch(url, { cache: "no-store", credentials: "same-origin", headers: { Accept: "application/json" } })
        .then(function (response) {
            if (!response.ok) throw new Error("Version unavailable");
            return response.json();
        }).then(function (data) {
            if (!data || typeof data.version !== "string" || !data.version.trim()) return;
            // Reuse the page footer, never a dialog's footer or a sticky action bar.
            var footer = document.querySelector("footer[data-bf-application-footer], body > footer") || document.createElement("footer");
            footer.classList.add("bf-application-footer");
            footer.setAttribute("data-bf-application-footer", "");
            footer.setAttribute("aria-label", "Application version");
            var label = footer.querySelector("[data-bf-version]") || document.createElement("small");
            label.className = "bf-version-footer";
            label.setAttribute("data-bf-version", "");
            label.textContent = "Version " + data.version;
            footer.appendChild(label);
            if (!footer.parentNode) document.body.appendChild(footer);
        }).catch(function () { /* Diagnostics must not interrupt the page when unavailable. */ });
}());
