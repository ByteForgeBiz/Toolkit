(function (global, document) {
    'use strict';

    var fallbackZones = [
        'Pacific/Honolulu',
        'America/Anchorage',
        'America/Los_Angeles',
        'America/Denver',
        'America/Chicago',
        'America/New_York',
        'America/Halifax',
        'America/Sao_Paulo',
        'UTC',
        'Europe/London',
        'Europe/Paris',
        'Asia/Kolkata',
        'Asia/Singapore',
        'Asia/Tokyo',
        'Australia/Sydney'
    ];

    /** Returns the browser's active IANA time-zone identifier when available. */
    function current() {
        try {
            return Intl.DateTimeFormat().resolvedOptions().timeZone || '';
        } catch (error) {
            return '';
        }
    }

    /** Returns the IANA identifiers supported by the current browser. */
    function supported() {
        var zones = [];
        if (Intl.supportedValuesOf) {
            try {
                zones = Intl.supportedValuesOf('timeZone').slice();
            } catch (error) {
                zones = [];
            }
        }

        fallbackZones.forEach(function (zone) {
            if (zones.indexOf(zone) < 0) {
                zones.push(zone);
            }
        });
        return zones.sort();
    }

    /** Returns the zone's current localized UTC offset. */
    function offset(timeZone) {
        try {
            var parts = new Intl.DateTimeFormat('en-US', {
                timeZone: timeZone,
                timeZoneName: 'longOffset'
            }).formatToParts(new Date());
            var value = parts.filter(function (part) { return part.type === 'timeZoneName'; })[0];
            return value ? value.value.replace('GMT', 'UTC') : '';
        } catch (error) {
            return '';
        }
    }

    /** Returns the zone's localized generic display name. */
    function name(timeZone) {
        try {
            var parts = new Intl.DateTimeFormat('en-US', {
                timeZone: timeZone,
                timeZoneName: 'longGeneric'
            }).formatToParts(new Date());
            var value = parts.filter(function (part) { return part.type === 'timeZoneName'; })[0];
            return value ? value.value : timeZone.replace(/_/g, ' ');
        } catch (error) {
            return timeZone.replace(/_/g, ' ');
        }
    }

    /** Returns a searchable label containing the current offset and generic name. */
    function label(timeZone) {
        return [offset(timeZone), name(timeZone)].filter(Boolean).join(' \u00b7 ');
    }

    /** Returns the current clock time in a specific zone. */
    function localTime(timeZone) {
        try {
            return new Intl.DateTimeFormat([], {
                timeZone: timeZone,
                hour: '2-digit',
                minute: '2-digit'
            }).format(new Date());
        } catch (error) {
            return '';
        }
    }

    /**
     * Populates a datalist with supported IANA identifiers.
     * @param {HTMLDataListElement} element Target datalist.
     * @param {string[]} additionalZones Existing values or aliases that must remain selectable.
     * @returns {string[]} The identifiers added to the list.
     */
    function populateDatalist(element, additionalZones) {
        if (!element) {
            return [];
        }

        var zones = supported();
        (additionalZones || []).forEach(function (zone) {
            var value = (zone || '').trim();
            if (value && zones.indexOf(value) < 0) {
                zones.push(value);
            }
        });
        zones.sort();

        element.textContent = '';
        zones.forEach(function (zone) {
            var option = document.createElement('option');
            option.value = zone;
            option.label = label(zone);
            element.appendChild(option);
        });
        return zones;
    }

    /**
     * Creates a searchable, browseable IANA time-zone picker.
     * @param {{root: HTMLElement|string, input: HTMLInputElement|string, zones?: Array<string|{id:string, displayName:string}>, additionalZones?: string[], placeholder?: string, label?: string}} options Picker options.
     * @returns {{zones: string[], open: Function, close: Function, setValue: Function}} Picker API.
     */
    function createPicker(options) {
        options = options || {};
        var root = typeof options.root === 'string' ? document.querySelector(options.root) : options.root;
        var input = typeof options.input === 'string' ? document.querySelector(options.input) : options.input;
        if (!root || !input) {
            throw new Error('A time-zone picker root and input are required.');
        }

        var configuredZones = Array.isArray(options.zones);
        var displayNames = {};
        var zones = [];
        if (configuredZones) {
            options.zones.forEach(function (entry) {
                var value = typeof entry === 'string' ? entry : entry && entry.id;
                var displayName = typeof entry === 'string' ? '' : entry && entry.displayName;
                value = (value || '').trim();
                if (value && zones.indexOf(value) < 0) {
                    zones.push(value);
                    if ((displayName || '').trim()) {
                        displayNames[value] = displayName.trim();
                    }
                }
            });
        } else {
            zones = supported();
            (options.additionalZones || []).concat([input.value, current()]).forEach(function (zone) {
                var value = (zone || '').trim();
                if (value && zones.indexOf(value) < 0) {
                    zones.push(value);
                }
            });
            zones.sort();
        }

        function displayName(zone) {
            return displayNames[zone] || name(zone);
        }

        root.classList.add('bf-time-zone-picker');
        root.textContent = '';
        input.hidden = true;
        input.classList.add('bf-time-zone-picker-source');

        var control = document.createElement('button');
        control.type = 'button';
        control.id = root.id ? root.id + '-control' : '';
        control.className = 'bf-time-zone-picker-control';
        control.setAttribute('aria-haspopup', 'listbox');
        control.setAttribute('aria-expanded', 'false');
        control.setAttribute('aria-label', options.label || 'Time zone');

        var controlText = document.createElement('span');
        controlText.className = 'bf-time-zone-picker-control-text';
        var controlName = document.createElement('span');
        controlName.className = 'bf-time-zone-picker-control-name';
        var controlId = document.createElement('span');
        controlId.className = 'bf-time-zone-picker-control-id';
        var arrow = document.createElement('span');
        arrow.className = 'bf-time-zone-picker-arrow';
        arrow.setAttribute('aria-hidden', 'true');
        arrow.innerHTML = '&#9662;';
        controlText.appendChild(controlName);
        controlText.appendChild(controlId);
        control.appendChild(controlText);
        control.appendChild(arrow);

        var popup = document.createElement('div');
        popup.className = 'bf-time-zone-picker-popup';
        popup.hidden = true;
        var search = document.createElement('input');
        search.type = 'search';
        search.className = 'bf-time-zone-picker-search';
        search.placeholder = options.placeholder || 'Search time zones';
        search.setAttribute('aria-label', search.placeholder);
        var heading = document.createElement('div');
        heading.className = 'bf-time-zone-picker-heading';
        heading.innerHTML = '<span>Time zone</span><span>Local time</span>';
        var list = document.createElement('div');
        list.className = 'bf-time-zone-picker-list';
        list.setAttribute('role', 'listbox');
        popup.appendChild(search);
        popup.appendChild(heading);
        popup.appendChild(list);
        root.appendChild(control);
        root.appendChild(popup);

        function updateControl() {
            var value = (input.value || '').trim();
            controlName.textContent = value ? displayName(value) : (options.placeholder || 'Select a time zone');
            controlId.textContent = value && displayName(value) !== value ? value : '';
        }

        function matches(zone, query) {
            if (!query) {
                return true;
            }
            return [zone, displayName(zone), offset(zone)]
                .join(' ')
                .toLowerCase()
                .indexOf(query) >= 0;
        }

        function select(zone) {
            input.value = zone;
            updateControl();
            close();
            if (typeof Event === 'function') {
                input.dispatchEvent(new Event('input', { bubbles: true }));
                input.dispatchEvent(new Event('change', { bubbles: true }));
            }
            control.focus();
        }

        function render() {
            var query = search.value.trim().toLowerCase();
            var matchesFound = zones.filter(function (zone) { return matches(zone, query); });
            list.textContent = '';
            matchesFound.forEach(function (zone) {
                var option = document.createElement('button');
                option.type = 'button';
                option.className = 'bf-time-zone-picker-option';
                option.setAttribute('role', 'option');
                option.setAttribute('aria-selected', String(zone === input.value));
                if (zone === input.value) {
                    option.classList.add('is-selected');
                }

                var details = document.createElement('span');
                details.className = 'bf-time-zone-picker-option-details';
                var friendlyName = document.createElement('span');
                friendlyName.className = 'bf-time-zone-picker-option-name';
                friendlyName.textContent = displayName(zone);
                var identifier = document.createElement('span');
                identifier.className = 'bf-time-zone-picker-option-id';
                identifier.textContent = [zone, offset(zone)].filter(Boolean).join(' \u00b7 ');
                var clock = document.createElement('span');
                clock.className = 'bf-time-zone-picker-option-time';
                clock.textContent = localTime(zone);
                details.appendChild(friendlyName);
                details.appendChild(identifier);
                option.appendChild(details);
                option.appendChild(clock);
                option.addEventListener('click', function () { select(zone); });
                list.appendChild(option);
            });

            if (!matchesFound.length) {
                var empty = document.createElement('div');
                empty.className = 'bf-time-zone-picker-empty';
                empty.textContent = 'No matching time zones.';
                list.appendChild(empty);
            }
        }

        function open() {
            popup.hidden = false;
            control.setAttribute('aria-expanded', 'true');
            search.value = '';
            render();
            var selected = list.querySelector('.is-selected');
            if (selected) {
                selected.scrollIntoView({ block: 'nearest' });
            }
            search.focus();
        }

        function close() {
            popup.hidden = true;
            control.setAttribute('aria-expanded', 'false');
        }

        function setValue(value) {
            input.value = value || '';
            updateControl();
            if (!popup.hidden) {
                render();
            }
        }

        control.addEventListener('click', function () {
            if (popup.hidden) {
                open();
            } else {
                close();
            }
        });
        search.addEventListener('input', render);
        popup.addEventListener('keydown', function (event) {
            if (event.key === 'Escape') {
                close();
                control.focus();
            }
        });
        document.addEventListener('click', function (event) {
            if (!root.contains(event.target)) {
                close();
            }
        });

        updateControl();
        return { zones: zones.slice(), open: open, close: close, setValue: setValue };
    }

    global.ByteForgeTimeZones = {
        current: current,
        supported: supported,
        offset: offset,
        name: name,
        label: label,
        localTime: localTime,
        populateDatalist: populateDatalist,
        createPicker: createPicker
    };
}(window, document));
