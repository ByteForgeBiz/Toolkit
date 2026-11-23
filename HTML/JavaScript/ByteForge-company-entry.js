(function (root) {
    "use strict";

    /** Shared company-entry validation for searchable fields that optionally allow free text. */
    var ByteForgeCompanyEntry = {
        validate: function (input, selectedId, allowTyped, allowBlank, showMessage, setStatus) {
            if (!input || !selectedId) return true;
            var company = input.value.trim();
            var valid = (allowBlank && !company)
                || (Boolean(company) && (Boolean(selectedId.value) || allowTyped));
            var message = allowTyped
                ? "Enter a company name before saving."
                : "Select a company from the existing-company search results.";
            input.setCustomValidity(valid ? "" : message);
            if (!valid && showMessage) {
                input.reportValidity();
                input.focus();
                setStatus(message, true);
            }
            return valid;
        },

        /** Keeps lookup guidance consistent with the server-authorized entry mode. */
        setStatus: function (element, message, isError, allowTyped) {
            if (!element) return;
            if (allowTyped && message === "No matching existing company was found. This company cannot be saved.") {
                message = "No matching company was found. You can use the name you typed for this inbound call.";
                isError = false;
            } else if (allowTyped && message === "Type at least 2 characters, then select an existing company.") {
                message = "Search for an existing company or enter a new company name for this inbound call.";
            }
            element.textContent = message || "";
            element.classList.toggle("is-error", Boolean(isError));
        }
    };
    root.ByteForgeCompanyEntry = ByteForgeCompanyEntry;
    if (typeof module !== "undefined" && module.exports) module.exports = ByteForgeCompanyEntry;
}(typeof window !== "undefined" ? window : globalThis));
