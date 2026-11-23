/* Report-compatible calendar presets for GET date-range forms. */
(function (root) {
    "use strict";

    function resolve(preset, endDate) {
        if (!/^(today|yesterday|last7|last30)$/.test(preset)) return null;
        // Calendar arithmetic in UTC avoids browser-zone and DST date shifts.
        var end = new Date(endDate + "T00:00:00Z");
        if (isNaN(end.getTime())) return null;
        if (preset === "yesterday") end.setUTCDate(end.getUTCDate() - 1);
        var start = new Date(end.getTime());
        start.setUTCDate(start.getUTCDate() - (preset === "last7" ? 6 : preset === "last30" ? 29 : 0));
        return { start: start.toISOString().slice(0, 10), end: end.toISOString().slice(0, 10) };
    }

    function init(form) {
        var preset = form.querySelector("[data-range-preset]");
        var start = form.querySelector("[data-range-start]");
        var end = form.querySelector("[data-range-end-input]");
        if (!preset || !start || !end) return;
        preset.addEventListener("change", function () {
            var dates = resolve(preset.value, form.getAttribute("data-range-end"));
            if (dates) { start.value = dates.start; end.value = dates.end; }
        });
        [start, end].forEach(function (input) {
            input.addEventListener("change", function () { preset.value = "custom"; });
        });
    }

    root.ByteForgeDateRange = { resolve: resolve, init: init };
    if (root.document) {
        root.document.querySelectorAll("[data-bf-date-range]").forEach(init);
    }
}(typeof window !== "undefined" ? window : globalThis));
