(function () {
    'use strict';

    var form = document.getElementById('workspace-form');
    var phone = document.getElementById('KeyPhone');
    var rawPhone = document.getElementById('KeyPhoneRaw');
    if (phone && rawPhone && typeof formatPhoneNumber === 'function') {
        function syncPhone() {
            var digits = phone.value.replace(/\D/g, '');
            rawPhone.value = digits.length === 11 && digits.charAt(0) === '1' ? digits.substring(1) : digits;
            phone.setCustomValidity(rawPhone.value && !/^\d{10}$/.test(rawPhone.value) ? 'Enter a 10-digit phone number.' : '');
        }
        syncPhone();
        phone.value = formatPhoneNumber(phone.value);
        phone.addEventListener('input', syncPhone);
        phone.addEventListener('blur', function () {
            if (!rawPhone.value || /^\d{10}$/.test(rawPhone.value)) phone.value = formatPhoneNumber(rawPhone.value);
        });
    }
    var phone2 = document.getElementById('Phone2');
    if (phone2 && typeof formatPhoneNumber === 'function') phone2.value = formatPhoneNumber(phone2.value);
    var agentCheck = document.getElementById('bf-workspace-agent-check');
    if (form) form.addEventListener('bf-workspace-saved', function () {
        if (agentCheck) {
            if (agentCheck.open) agentCheck.close();
            agentCheck.remove();
            agentCheck = null;
        }
    });
    if (agentCheck && typeof agentCheck.showModal === 'function') {
        agentCheck.close();
        agentCheck.showModal();
        var cancelAgentCheck = agentCheck.querySelector('[data-agent-check-cancel]');
        cancelAgentCheck.hidden = false;
        cancelAgentCheck.addEventListener('click', function () { agentCheck.close(); });
        agentCheck.addEventListener('close', function () {
            var save = form.querySelector('footer button[type="submit"]');
            if (save) save.focus();
        });
    }
    var attributionWarning = document.getElementById('bf-workspace-attribution-warning');
    if (attributionWarning && typeof attributionWarning.showModal === 'function') {
        attributionWarning.addEventListener('close', function () {
            var saveButton = form && form.querySelector('button[type="submit"]');
            if (saveButton) saveButton.focus();
        });
        attributionWarning.showModal();
    }
    var notes = document.getElementById('notes');
    var counter = document.getElementById('notes-counter');
    var state = document.getElementById('save-state');
    var toggle = document.querySelector('[data-toggle="script"]');
    var script = document.getElementById('script');
    var advisorSelect = document.getElementById('advisor-select');
    var appointmentDateTime = document.getElementById('AppointmentDateTime');
    var appointmentDateTimePicker = document.getElementById('appointment-date-time-picker');
    var appointmentTimeZone = document.getElementById('AppointmentTimeZone');
    var appointmentTimeZonePicker = document.getElementById('appointment-time-zone-picker');
    var appointmentTimeZoneOptions = document.getElementById('appointment-time-zone-options');

    function fieldValue(id) {
        var field = document.getElementById(id);
        return field ? field.value.trim() : '';
    }

    function contactFirstName(value) {
        return (value || '').split(/\s+/).filter(Boolean)[0] || '';
    }

    function contactLocation() {
        return [fieldValue('City'), fieldValue('State'), fieldValue('Country')].filter(Boolean).join(', ');
    }

    function refreshContact() {
        var contact = fieldValue('Contact');
        var title = fieldValue('Title');
        var company = fieldValue('CompanyName');
        var location = contactLocation();
        var city = document.getElementById('City');
        if (city) city.setCustomValidity(location.length > 255 ? 'Combined location cannot exceed 255 characters.' : '');

        setText('[data-current-contact="name"]', contact || 'New inbound contact');
        setText('[data-current-contact="summary"]', [title, company, location].filter(Boolean).join(' \u00b7 '));

        setScriptField('CustomerFirst', contactFirstName(contact));
        setScriptField('Contact', contact);
        setScriptField('CompanyName', company);
        setScriptField('Title', title);
        setScriptField('Industry', fieldValue('Industry'));
        setScriptField('University', fieldValue('University'));
        setScriptField('City', fieldValue('City'));
        setScriptField('State', fieldValue('State'));
        setScriptField('Country', fieldValue('Country'));
        setScriptField('EmailAddress', fieldValue('EmailAddress'));
        setScriptField('Phone', typeof formatPhoneNumber === 'function' ? formatPhoneNumber(fieldValue('KeyPhone')) : fieldValue('KeyPhone'));
        setScriptField('LinkedInUrl', fieldValue('LinkedInUrl'));
    }

    function initializeDateTimePicker() {
        if (!appointmentDateTime || !appointmentDateTimePicker || !window.ByteForgeDateTimePicker) {
            return;
        }

        window.ByteForgeDateTimePicker.createPicker({
            root: appointmentDateTimePicker,
            input: appointmentDateTime,
            label: 'Appointment date and time'
        });
    }

    function initializeTimeZones() {
        if (!appointmentTimeZone || !appointmentTimeZonePicker || !window.ByteForgeTimeZones) {
            return;
        }

        var currentValue = appointmentTimeZone.value.trim();
        var currentZone = window.ByteForgeTimeZones.current();
        var configuredZones = appointmentTimeZoneOptions ? Array.prototype.map.call(
            appointmentTimeZoneOptions.options,
            function (option) { return { id: option.value, displayName: option.getAttribute('data-display-name') || option.value }; }
        ) : [];
        var currentZoneIsAllowed = configuredZones.some(function (option) { return option.id === currentZone; });
        if (currentZone && currentZoneIsAllowed && !currentValue) {
            appointmentTimeZone.value = currentZone;
        }

        window.ByteForgeTimeZones.createPicker({
            root: appointmentTimeZonePicker,
            input: appointmentTimeZone,
            zones: configuredZones,
            placeholder: 'Search time zones',
            label: 'Appointment time zone'
        });
    }

    function count() {
        if (counter && notes) {
            counter.textContent = notes.value.length.toLocaleString() + ' / 2,500';
        }
    }

    function setText(selector, value) {
        Array.prototype.forEach.call(document.querySelectorAll(selector), function (element) {
            element.textContent = value || '';
        });
    }

    function setScriptField(field, value) {
        setText('[data-praxis-field="' + field + '"]', value);
    }

    function safeExternalUrl(value) {
        return /^https?:\/\//i.test(value || '') ? value : '';
    }

    function setAdvisorLink(name, value) {
        var links = document.querySelectorAll('[data-advisor-link="' + name + '"]');
        var url = safeExternalUrl(value);
        Array.prototype.forEach.call(links, function (link) {
            link.hidden = !url;
            if (url) {
                link.href = url;
            } else {
                link.removeAttribute('href');
            }
        });
    }

    function refreshAdvisor() {
        if (!advisorSelect || advisorSelect.selectedIndex < 0) {
            return;
        }

        var option = advisorSelect.options[advisorSelect.selectedIndex];
        var advisor = {
            name: option.getAttribute('data-name') || '',
            first: option.getAttribute('data-first') || '',
            firm: option.getAttribute('data-firm') || '',
            email: option.getAttribute('data-email') || '',
            phone: typeof formatPhoneNumber === 'function' ? formatPhoneNumber(option.getAttribute('data-phone') || '') : option.getAttribute('data-phone') || '',
            website: option.getAttribute('data-website') || '',
            zoomLink: option.getAttribute('data-zoom-link') || '',
            phoneLink: option.getAttribute('data-phone-link') || '',
            timeZone: option.getAttribute('data-time-zone') || ''
        };

        setScriptField('Advisor', advisor.name);
        setScriptField('AdvisorName', advisor.name);
        setScriptField('AdvisorFirst', advisor.first);
        setScriptField('AdvisorFirm', advisor.firm);
        setScriptField('AdvisorEmail', advisor.email);
        setScriptField('AdvisorPhone', advisor.phone);
        setScriptField('OfficePhone', advisor.phone);
        setScriptField('AdvisorWebsite', advisor.website);
        setScriptField('AdvisorZoomLink', advisor.zoomLink);
        setScriptField('AdvisorPhoneLink', advisor.phoneLink);
        setScriptField('AdvisorTimeZone', advisor.timeZone);

        setText('[data-advisor-summary="firm"]', advisor.firm);
        setText('[data-advisor-summary="contact"]', [advisor.phone, advisor.email].filter(Boolean).join(' \u00b7 '));
        setText('[data-advisor-summary="time-zone"], [data-advisor-time-zone]', advisor.timeZone);
        setAdvisorLink('schedule', advisor.phoneLink);
        setAdvisorLink('website', advisor.website);
        setAdvisorLink('calendar', advisor.zoomLink);
    }

    initializeDateTimePicker();
    initializeTimeZones();
    count();
    refreshContact();
    if (form) {
        form.addEventListener('input', function (event) {
            if (event.target && event.target.classList.contains('bf-time-zone-picker-search')) {
                return;
            }
            if (state) {
                state.textContent = 'Unsaved changes';
            }
            count();
            refreshContact();
        });
        form.addEventListener('change', refreshContact);
        var identityReady = false;
        var identityPending = false;
        form.addEventListener('submit', function (event) {
            var submitter = event.submitter;
            if (agentCheck && !agentCheck.open) {
                event.preventDefault();
                agentCheck.showModal();
                return;
            }
            var selection = document.getElementById('SelectedAgentId');
            if (selection && !selection.value) {
                event.preventDefault();
                selection.focus();
                if (state) state.textContent = 'Select an agent before continuing.';
                agentCheck.querySelector('[data-agent-check-status]').textContent = 'Select an agent before continuing.';
                return;
            }
            var fromReport = document.getElementById('Launch_FromReport');
            var token = '';
            if (fromReport && fromReport.value.toLowerCase() === 'true') {
                try { token = sessionStorage.getItem('praxis.reportPortal.accessToken') || localStorage.getItem('praxis.reportPortal.persistentAccessToken') || ''; } catch (ignored) { }
            }
            if (token && !identityReady) {
                event.preventDefault();
                if (identityPending) return;
                identityPending = true;
                if (state) state.textContent = 'Checking signed-in user\u2026';
                fetch('api/config-session/workspace', { method: 'POST', credentials: 'same-origin', headers: { Authorization: 'Bearer ' + token } })
                    .then(function (response) {
                        if (!response.ok) throw new Error('Your portal session could not be checked. Sign in again or sign out of the portal, then retry. Your edits are still here.');
                        identityReady = true;
                        identityPending = false;
                        window.setTimeout(function () { form.requestSubmit(submitter || undefined); }, 0);
                    }).catch(function (error) {
                        identityPending = false;
                        if (state) state.textContent = error.message;
                        if (agentCheck) agentCheck.querySelector('[data-agent-check-status]').textContent = error.message;
                    });
                return;
            }
            if (state) {
                state.textContent = 'Saving\u2026';
            }
        });
    }
    if (advisorSelect) {
        advisorSelect.addEventListener('change', refreshAdvisor);
        refreshAdvisor();
    }
    if (toggle && script) {
        toggle.addEventListener('click', function () {
            var open = !script.hidden;
            script.hidden = open;
            toggle.textContent = open ? 'Expand' : 'Collapse';
            toggle.setAttribute('aria-expanded', String(!open));
        });
    }
}());
