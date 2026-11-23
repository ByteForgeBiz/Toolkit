/** Enhances the server-paged Walgreens case list without downloading off-page history. */
(function () {
    "use strict";
    var form = document.getElementById("submissionListForm");
    if (!form) return;

    form.querySelectorAll("[data-submit-on-change]").forEach(function (input) {
        input.addEventListener("change", function () { form.requestSubmit(); });
    });

    form.addEventListener("click", function (event) {
        var toggle = event.target.closest("[data-case-toggle]");
        if (toggle) {
            event.preventDefault();
            var history = document.getElementById(toggle.getAttribute("aria-controls"));
            if (!history) return;
            history.hidden = !history.hidden;
            toggle.setAttribute("aria-expanded", history.hidden ? "false" : "true");
            toggle.setAttribute("aria-label", history.hidden ? "View history" : "Hide history");
            toggle.setAttribute("title", history.hidden ? "View history" : "Hide history");
            toggle.innerHTML = history.hidden ? "&#10133;" : "&#10134;";
            return;
        }
        var row = event.target.closest("[data-case-open-url]");
        if (row && !event.target.closest("a, button, input, select, textarea")) openRecord(row);
    });
    form.addEventListener("keydown", function (event) {
        var row = event.target.closest("[data-case-open-url]");
        if (row && event.target === row && (event.key === "Enter" || event.key === " ")) {
            event.preventDefault();
            openRecord(row);
        }
    });
    function openRecord(row) {
        var url = row.getAttribute("data-case-open-url");
        if (url) window.open(url, "_blank", "noopener");
    }

    // Read-only timing markers let browser checks distinguish usable controls from full load.
    form.setAttribute("data-ready-ms", Math.round(performance.now()));
    window.addEventListener("load", function () {
        form.setAttribute("data-load-ms", Math.round(performance.now()));
    }, { once: true });
}());
