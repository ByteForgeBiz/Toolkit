// Opt-in navigation timing: includes server wait, download, scripts and load handlers.
(function () {
    "use strict";
    var form = document.getElementById("bf-page-performance");
    if (!form || !window.performance || !window.performance.getEntriesByType) return;

    function reportLoad() {
        // loadEventEnd is populated only after all window.load handlers return.
        window.setTimeout(function () {
            try {
                var navigation = window.performance.getEntriesByType("navigation")[0];
                if (!navigation || navigation.loadEventEnd <= 0) return;
                var body = new FormData(form);
                body.append("loadMs", Math.round(navigation.loadEventEnd));
                body.append("responseMs", Math.round(navigation.responseStart));
                body.append("domReadyMs", Math.round(navigation.domContentLoadedEventEnd));
                window.fetch(form.action, { method: "POST", body: body, credentials: "same-origin", keepalive: true })
                    .catch(function () { /* Telemetry must never interrupt the report. */ });
            } catch (error) { /* Unsupported or blocked telemetry is best effort. */ }
        }, 0);
    }

    if (document.readyState === "complete") reportLoad();
    else window.addEventListener("load", reportLoad, { once: true });
}());
