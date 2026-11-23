/* Configured disposition/reason controls. Callers own authorization and persistence. */
(function (root) {
    "use strict";

    function ByteForgeDispositionEditor(container, options) {
        var self = this;
        var settings = options || {};
        var choices = settings.dispositions || [];
        var calls = settings.calls || [];
        var error = typeof settings.onError === "function" ? settings.onError : function () {};
        function field(caption, tag) {
            var label = document.createElement("label");
            label.textContent = caption;
            var control = document.createElement(tag);
            control.required = true;
            control.style.fontWeight = "400";
            label.appendChild(control);
            container.appendChild(label);
            return control;
        }
        function option(select, value, caption) {
            var item = document.createElement("option");
            item.value = String(value);
            item.textContent = caption;
            select.appendChild(item);
        }
        var target = field("Appointment call", "select");
        option(target, "", "Select the call to correct");
        calls.forEach(function (call) { option(target, call.value, call.label); });
        if (calls.length === 1) target.value = String(calls[0].value);
        var disposition = field("Replacement disposition", "select");
        option(disposition, "", "Select a disposition");
        choices.forEach(function (choice) { option(disposition, choice, choice); });
        var reason = field("Reason", "textarea");
        reason.rows = 3;
        reason.maxLength = 500;
        reason.placeholder = "Explain why the appointment is being invalidated.";
        self.setDisabled = function (disabled) {
            target.disabled = disabled;
            disposition.disabled = disabled;
            reason.disabled = disabled;
        };
        self.read = function () {
            if (!calls.some(function (call) { return String(call.value) === target.value; })) {
                error("Select the exact appointment call to correct."); target.focus(); return null;
            }
            if (choices.indexOf(disposition.value) < 0) {
                error("Select a replacement disposition."); disposition.focus(); return null;
            }
            var explanation = reason.value.trim();
            if (!explanation || explanation.length > 500) {
                error("Enter an invalidation reason of 1 to 500 characters."); reason.focus(); return null;
            }
            return { primaryKey: Number(target.value), disposition: disposition.value, reason: explanation };
        };
    }
    root.ByteForgeDispositionEditor = ByteForgeDispositionEditor;
}(window));
