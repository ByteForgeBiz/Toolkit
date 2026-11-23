(function (window, document) {
    "use strict";

    /**
     * Applies accessible active-sort state to one group of header controls.
     * @param {string|NodeList|Array} controls A selector or collection of sortable controls.
     * @param {string} keyAttribute The data attribute containing each control's sort key.
     * @param {{key: string, direction: string}} state The active sort state.
     * @returns {void}
     */
    function update(controls, keyAttribute, state) {
        var items = typeof controls === "string"
            ? document.querySelectorAll(controls)
            : controls || [];
        var activeKey = state && String(state.key || "");
        var activeDirection = state && state.direction === "desc" ? "desc" : "asc";

        Array.prototype.forEach.call(items, function (control) {
            var isActive = control.getAttribute(keyAttribute) === activeKey;
            var direction = isActive ? activeDirection : "";
            var header = control.closest ? control.closest("th") : null;

            control.classList.add("byte-forge-sort-control");
            control.setAttribute("data-sort-direction", direction);
            control.setAttribute("aria-pressed", isActive ? "true" : "false");
            if (header) {
                header.setAttribute(
                    "aria-sort",
                    isActive
                        ? (activeDirection === "asc" ? "ascending" : "descending")
                        : "none");
            }
        });
    }

    window.ByteForgeSortIndicator = {
        update: update
    };
}(window, document));
