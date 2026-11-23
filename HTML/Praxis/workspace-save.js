(function () {
    'use strict';
    var form = document.getElementById('workspace-form');
    var action = document.getElementById('workspace-save-action');
    var state = document.getElementById('save-state');
    var copyAgain = document.getElementById('workspace-copy-again');
    if (!form || !action || !copyAgain) return;
    var saving = false;
    var lastSavedText = null;

    function status(message) { if (state) state.textContent = message; }

    function validate(event) {
        var submitter = event.submitter;
        if (submitter && submitter.name === 'workspaceAction') action.value = submitter.value;
        if (action.value !== 'Save' && action.value !== 'SetAppointment' && action.value !== 'SendInfo') {
            status('Choose Save, Set Appointment or Send Info.');
            return false;
        }
        ['Contact', 'KeyPhone', 'EmailAddress', 'EventAccurate'].forEach(function (id) {
            var field = document.getElementById(id);
            if (field) field.required = action.value !== 'Save';
        });
        if (!form.reportValidity()) return false;
        if (action.value === 'SetAppointment') {
            var date = document.getElementById('AppointmentDateTime');
            var zone = document.getElementById('AppointmentTimeZone');
            if (!date || !date.value.trim() || !zone || !zone.value.trim()) {
                status('Select the appointment date, time and time zone before setting an appointment.');
                var focus = !date || !date.value.trim()
                    ? document.querySelector('#appointment-date-time-picker input, #appointment-date-time-picker button') : zone;
                if (focus) focus.focus();
                return false;
            }
        }
        return true;
    }

    // Capture runs before the existing attribution/session handler. Its asynchronous
    // identity check can still cancel submission; the bubble handler below respects it.
    form.addEventListener('submit', function (event) {
        if (saving || !validate(event)) {
            event.preventDefault();
            event.stopImmediatePropagation();
        }
    }, true);

    function legacyCopy(text) {
        var previous = document.activeElement;
        var field = document.createElement('textarea');
        field.value = text;
        field.setAttribute('readonly', '');
        field.style.position = 'fixed';
        field.style.left = '-10000px';
        document.body.appendChild(field);
        var copied = false;
        try { field.select(); copied = document.execCommand('copy'); }
        finally { field.remove(); if (previous && previous.focus) previous.focus(); }
        if (!copied) throw new Error('Clipboard access is unavailable.');
    }

    function copySaved() {
        if (lastSavedText === null) return Promise.resolve();
        var write;
        try {
            write = navigator.clipboard && navigator.clipboard.writeText
                ? navigator.clipboard.writeText(lastSavedText).catch(function () { legacyCopy(lastSavedText); })
                : Promise.resolve().then(function () { legacyCopy(lastSavedText); });
        } catch (error) { write = Promise.reject(error); }
        return write.then(function () {
            copyAgain.hidden = true;
            status('Saved and copied.');
        }, function () {
            copyAgain.hidden = false;
            status('Saved successfully, but clipboard copying failed. Use Copy again; do not save again.');
        });
    }

    copyAgain.addEventListener('click', function () { copySaved(); });

    form.addEventListener('submit', function (event) {
        if (event.defaultPrevented) return;
        event.preventDefault();
        var body = new FormData(form);
        if (event.submitter && event.submitter.name) body.set(event.submitter.name, event.submitter.value);
        var buttons = Array.prototype.slice.call(form.querySelectorAll('button[type="submit"]'));
        saving = true;
        copyAgain.hidden = true;
        buttons.forEach(function (button) { button.disabled = true; });
        status('Saving...');
        fetch(form.action || window.location.href, { method: 'POST', credentials: 'same-origin',
            headers: { 'X-Requested-With': 'XMLHttpRequest', Accept: 'application/json' }, body: body })
            .then(function (response) {
                if ((response.headers.get('Content-Type') || '').indexOf('application/json') >= 0) {
                    return response.json().then(function (result) {
                        if (!response.ok || !result.ok || (result.action !== 'Save' && typeof result.clipboardText !== 'string'))
                            throw new Error(result.message || 'We could not confirm that your changes were saved. Keep this page open and contact your supervisor.');
                        return result;
                    });
                }
                // The existing validation and agent-confirmation views retain all entered
                // values. They contain no success payload, so they must never copy text.
                return response.text().then(function (html) {
                    var page = new DOMParser().parseFromString(html, 'text/html');
                    if (!page.getElementById('workspace-form'))
                        throw new Error('We could not confirm that your changes were saved. Keep this page open and contact your supervisor.');
                    document.open(); document.write(html); document.close();
                    return null;
                });
            })
            .then(function (result) {
                if (!result) return;
                lastSavedText = result.action === 'Save' ? null : result.clipboardText;
                ['agentId', 'agentLogin'].forEach(function (key) {
                    var field = document.getElementById(key === 'agentId' ? 'Launch_AgentId' : 'Launch_AgentLogin');
                    if (field) field.value = result[key] || '';
                });
                Array.prototype.forEach.call(document.querySelectorAll('[data-praxis-field="AgentFirst"]'), function (field) {
                    field.textContent = result.agentFirstName || '';
                });
                var warning = document.getElementById('workspace-save-warning');
                if (warning) { warning.textContent = result.warning || ''; warning.hidden = !result.warning; }
                form.dispatchEvent(new CustomEvent('bf-workspace-saved'));
                if (result.action === 'Save') {
                    status(result.message || 'Changes saved.');
                    if (result.warning) status(state.textContent + ' ' + result.warning);
                    return;
                }
                return copySaved().then(function () {
                    if (result.warning) status(state.textContent + ' ' + result.warning);
                });
            })
            .catch(function (error) { status(error.message || 'Save could not be confirmed. Keep this page open and contact your supervisor.'); })
            .then(function () { saving = false; buttons.forEach(function (button) { button.disabled = false; }); });
    });
}());
