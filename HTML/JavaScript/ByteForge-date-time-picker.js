(function (global) {
    'use strict';

    function pad(value) {
        return String(value).padStart(2, '0');
    }

    function validDate(year, month, day) {
        var value = new Date(year, month - 1, day);
        return value.getFullYear() === year && value.getMonth() === month - 1 && value.getDate() === day;
    }

    function parseValue(value) {
        var match = String(value || '').trim().match(/^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})/);
        if (!match) {
            return null;
        }
        var parsed = {
            year: Number(match[1]), month: Number(match[2]), day: Number(match[3]),
            hour: Number(match[4]), minute: Number(match[5])
        };
        return validDate(parsed.year, parsed.month, parsed.day) && parsed.hour < 24 && parsed.minute < 60
            ? parsed : null;
    }

    function parseDisplayValue(value) {
        var match = String(value || '').trim().match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})\s+(\d{1,2}):(\d{2})$/);
        if (!match) {
            return null;
        }
        var parsed = {
            year: Number(match[3]), month: Number(match[1]), day: Number(match[2]),
            hour: Number(match[4]), minute: Number(match[5])
        };
        return validDate(parsed.year, parsed.month, parsed.day) && parsed.hour < 24 && parsed.minute < 60
            ? parsed : null;
    }

    function formatValue(value) {
        return value.year + '-' + pad(value.month) + '-' + pad(value.day) + 'T' + pad(value.hour) + ':' + pad(value.minute);
    }

    function formatDisplayValue(value) {
        return pad(value.month) + '/' + pad(value.day) + '/' + value.year + ' ' + pad(value.hour) + ':' + pad(value.minute);
    }

    function todayValue() {
        var now = new Date();
        return {
            year: now.getFullYear(), month: now.getMonth() + 1, day: now.getDate(),
            hour: now.getHours(), minute: Math.floor(now.getMinutes() / 5) * 5
        };
    }

    function textElement(tag, className, text) {
        var element = document.createElement(tag);
        element.className = className || '';
        element.textContent = text || '';
        return element;
    }

    function button(className, label, text) {
        var element = textElement('button', className, text);
        element.type = 'button';
        element.setAttribute('aria-label', label);
        return element;
    }

    function createPicker(options) {
        if (!options || !options.root || !options.input) {
            throw new Error('ByteForgeDateTimePicker requires root and input elements.');
        }

        var root = options.root;
        var input = options.input;
        var label = options.label || 'Date and time';
        var selected = parseValue(input.value) || todayValue();
        var visibleMonth = selected.month - 1;
        var visibleYear = selected.year;
        var mode = 'hour';
        var dragPointerId = null;

        root.textContent = '';
        root.classList.add('bf-date-time-picker');
        input.type = 'hidden';

        var trigger = textElement('div', 'bf-date-time-trigger');
        var display = document.createElement('input');
        display.type = 'text';
        display.className = 'bf-date-time-display';
        display.placeholder = 'MM/DD/YYYY HH:mm';
        display.inputMode = 'numeric';
        display.autocomplete = 'off';
        display.setAttribute('aria-label', label);
        display.value = parseValue(input.value) ? formatDisplayValue(selected) : '';
        var toggle = button('bf-date-time-toggle', 'Open ' + label.toLowerCase() + ' picker', '\u25f7');
        toggle.setAttribute('aria-expanded', 'false');
        trigger.appendChild(display);
        trigger.appendChild(toggle);

        var popup = textElement('div', 'bf-date-time-popup');
        popup.hidden = true;
        popup.setAttribute('role', 'dialog');
        popup.setAttribute('aria-label', label + ' picker');

        var split = textElement('div', 'bf-date-time-split');
        var calendarPane = textElement('div', 'bf-date-time-pane');
        var calendarHeader = textElement('div', 'bf-date-time-calendar-header');
        var previous = button('bf-date-time-nav', 'Previous month', '\u2039');
        var monthTitle = textElement('span', 'bf-date-time-month');
        var next = button('bf-date-time-nav', 'Next month', '\u203a');
        calendarHeader.appendChild(previous);
        calendarHeader.appendChild(monthTitle);
        calendarHeader.appendChild(next);
        var weekdays = textElement('div', 'bf-date-time-calendar bf-date-time-weekdays');
        ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa'].forEach(function (day) {
            weekdays.appendChild(textElement('span', '', day));
        });
        var days = textElement('div', 'bf-date-time-calendar bf-date-time-days');
        days.setAttribute('role', 'grid');
        calendarPane.appendChild(calendarHeader);
        calendarPane.appendChild(weekdays);
        calendarPane.appendChild(days);

        var timePane = textElement('div', 'bf-date-time-pane bf-date-time-clock-pane');
        var readout = textElement('div', 'bf-date-time-readout');
        var hour = document.createElement('input');
        hour.type = 'text';
        hour.inputMode = 'numeric';
        hour.maxLength = 2;
        hour.className = 'bf-date-time-part';
        hour.setAttribute('aria-label', 'Hour, 00 through 23');
        var separator = textElement('span', 'bf-date-time-separator', ':');
        var minute = document.createElement('input');
        minute.type = 'text';
        minute.inputMode = 'numeric';
        minute.maxLength = 2;
        minute.className = 'bf-date-time-part';
        minute.setAttribute('aria-label', 'Minute, 00 through 59');
        readout.appendChild(hour);
        readout.appendChild(separator);
        readout.appendChild(minute);
        var hint = textElement('p', 'bf-date-time-hint');
        var clock = textElement('div', 'bf-date-time-clock');
        clock.setAttribute('role', 'group');
        var hand = textElement('span', 'bf-date-time-hand');
        hand.setAttribute('aria-hidden', 'true');
        var hub = textElement('span', 'bf-date-time-hub');
        hub.setAttribute('aria-hidden', 'true');
        var clockOptions = textElement('div', 'bf-date-time-clock-options');
        clock.appendChild(hand);
        clock.appendChild(hub);
        clock.appendChild(clockOptions);
        timePane.appendChild(readout);
        timePane.appendChild(hint);
        timePane.appendChild(clock);
        split.appendChild(calendarPane);
        split.appendChild(timePane);

        var actions = textElement('div', 'bf-date-time-actions');
        var clear = button('bf-date-time-action', 'Clear ' + label.toLowerCase(), 'Clear');
        var cancel = button('bf-date-time-action', 'Cancel date and time selection', 'Cancel');
        var apply = button('bf-date-time-action bf-date-time-action-primary', 'Apply date and time selection', 'Apply');
        actions.appendChild(clear);
        actions.appendChild(cancel);
        actions.appendChild(apply);
        popup.appendChild(split);
        popup.appendChild(actions);
        root.appendChild(trigger);
        root.appendChild(popup);

        function setReadout() {
            hour.value = pad(selected.hour);
            minute.value = pad(selected.minute);
            hour.setAttribute('data-active', String(mode === 'hour'));
            minute.setAttribute('data-active', String(mode === 'minute'));
        }

        function positionClockNumber(element, value, inner) {
            var index = mode === 'hour' ? value % 12 : value / 5;
            var radians = (index * 30 - 90) * Math.PI / 180;
            var radius = inner ? 25 : 40;
            element.style.left = (50 + Math.cos(radians) * radius) + '%';
            element.style.top = (50 + Math.sin(radians) * radius) + '%';
        }

        function renderClock() {
            clockOptions.textContent = '';
            var values = mode === 'hour'
                ? [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 0]
                : [0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55];
            values.forEach(function (value) {
                var inner = mode === 'hour' && (value === 0 || value > 12);
                var option = button('bf-date-time-number', mode === 'hour' ? pad(value) + ' hours' : pad(value) + ' minutes', pad(value));
                var isSelected = mode === 'hour' ? value === selected.hour : value === selected.minute;
                option.classList.toggle('is-selected', isSelected);
                positionClockNumber(option, value, inner);
                option.addEventListener('click', function (event) {
                    event.stopPropagation();
                    if (mode === 'hour') {
                        selected.hour = value;
                        setMode('minute');
                    } else {
                        selected.minute = value;
                        setReadout();
                        renderClock();
                    }
                });
                clockOptions.appendChild(option);
            });
            var angle = mode === 'hour' ? (selected.hour % 12) * 30 : selected.minute * 6;
            var innerHour = mode === 'hour' && (selected.hour === 0 || selected.hour > 12);
            hand.style.height = (innerHour ? 26 : 41) + '%';
            hand.style.transform = 'rotate(' + angle + 'deg)';
        }

        function setMode(nextMode) {
            mode = nextMode;
            hint.textContent = mode === 'hour' ? 'Outer ring 01\u201312 \u00b7 inner ring 13\u201300' : 'Select minutes';
            clock.setAttribute('aria-label', mode === 'hour' ? 'Select hour on a 24-hour clock' : 'Select minutes on an analog clock');
            setReadout();
            renderClock();
        }

        function renderCalendar() {
            var monthDate = new Date(visibleYear, visibleMonth, 1);
            monthTitle.textContent = monthDate.toLocaleDateString('en-US', { month: 'long', year: 'numeric' });
            days.setAttribute('aria-label', monthTitle.textContent);
            days.textContent = '';
            var firstVisible = new Date(visibleYear, visibleMonth, 1 - monthDate.getDay());
            var today = new Date();
            for (var index = 0; index < 42; index += 1) {
                var date = new Date(firstVisible.getFullYear(), firstVisible.getMonth(), firstVisible.getDate() + index);
                var dayButton = button('bf-date-time-day', date.toLocaleDateString('en-US', {
                    weekday: 'long', month: 'long', day: 'numeric', year: 'numeric'
                }), String(date.getDate()));
                dayButton.setAttribute('role', 'gridcell');
                dayButton.classList.toggle('is-outside', date.getMonth() !== visibleMonth);
                dayButton.classList.toggle('is-selected', date.getFullYear() === selected.year && date.getMonth() + 1 === selected.month && date.getDate() === selected.day);
                dayButton.classList.toggle('is-today', date.toDateString() === today.toDateString());
                (function (picked) {
                    dayButton.addEventListener('click', function () {
                        selected.year = picked.getFullYear();
                        selected.month = picked.getMonth() + 1;
                        selected.day = picked.getDate();
                        visibleYear = selected.year;
                        visibleMonth = selected.month - 1;
                        renderCalendar();
                    });
                }(date));
                days.appendChild(dayButton);
            }
        }

        function selectClockPoint(event) {
            var bounds = clock.getBoundingClientRect();
            var x = event.clientX - bounds.left - bounds.width / 2;
            var y = event.clientY - bounds.top - bounds.height / 2;
            var degrees = (Math.atan2(y, x) * 180 / Math.PI + 450) % 360;
            if (mode === 'hour') {
                var twelveHour = Math.round(degrees / 30) % 12;
                var outer = Math.hypot(x, y) > bounds.width * .34;
                selected.hour = outer ? (twelveHour || 12) : (twelveHour === 0 ? 0 : twelveHour + 12);
            } else {
                selected.minute = Math.round(degrees / 6) % 60;
            }
            setReadout();
            renderClock();
        }

        function openPicker() {
            selected = parseValue(input.value) || parseDisplayValue(display.value) || todayValue();
            visibleYear = selected.year;
            visibleMonth = selected.month - 1;
            popup.hidden = false;
            toggle.setAttribute('aria-expanded', 'true');
            renderCalendar();
            setMode('hour');
            popup.scrollIntoView({ block: 'nearest', inline: 'nearest' });
        }

        function closePicker(restoreDisplay) {
            stopClockDrag();
            popup.hidden = true;
            toggle.setAttribute('aria-expanded', 'false');
            if (restoreDisplay) {
                var stored = parseValue(input.value);
                display.value = stored ? formatDisplayValue(stored) : '';
                display.setCustomValidity('');
                display.removeAttribute('aria-invalid');
            }
        }

        function applyTypedValue(reportError) {
            if (!display.value.trim()) {
                display.setCustomValidity('');
                display.removeAttribute('aria-invalid');
                return true;
            }
            var parsed = parseDisplayValue(display.value);
            if (!parsed) {
                display.setCustomValidity('Use MM/DD/YYYY HH:mm with a 24-hour time.');
                display.setAttribute('aria-invalid', 'true');
                if (reportError) display.reportValidity();
                return false;
            }
            selected = parsed;
            visibleYear = selected.year;
            visibleMonth = selected.month - 1;
            display.setCustomValidity('');
            display.removeAttribute('aria-invalid');
            return true;
        }

        function notifyInputChanged() {
            input.dispatchEvent(new Event('input', { bubbles: true }));
            input.dispatchEvent(new Event('change', { bubbles: true }));
        }

        toggle.addEventListener('click', function () {
            if (popup.hidden) openPicker(); else closePicker(true);
        });
        // Update validity while typing: a stale custom error can prevent the
        // browser from submitting before change/blur gets a chance to clear it.
        display.addEventListener('input', function () {
            if (applyTypedValue(false)) {
                input.value = display.value.trim() ? formatValue(selected) : '';
                notifyInputChanged();
            }
        });
        display.addEventListener('change', function () {
            if (applyTypedValue(true)) {
                display.value = display.value.trim() ? formatDisplayValue(selected) : '';
                input.value = display.value.trim() ? formatValue(selected) : '';
                notifyInputChanged();
            }
        });
        previous.addEventListener('click', function () {
            visibleMonth -= 1;
            if (visibleMonth < 0) { visibleMonth = 11; visibleYear -= 1; }
            renderCalendar();
        });
        next.addEventListener('click', function () {
            visibleMonth += 1;
            if (visibleMonth > 11) { visibleMonth = 0; visibleYear += 1; }
            renderCalendar();
        });
        hour.addEventListener('focus', function () { setMode('hour'); });
        minute.addEventListener('focus', function () { setMode('minute'); });
        hour.addEventListener('change', function () {
            var value = Number(hour.value);
            if (Number.isInteger(value) && value >= 0 && value <= 23) selected.hour = value;
            setMode('hour');
        });
        minute.addEventListener('change', function () {
            var value = Number(minute.value);
            if (Number.isInteger(value) && value >= 0 && value <= 59) selected.minute = value;
            setMode('minute');
        });
        function stopClockDrag() {
            var pointerId = dragPointerId;
            dragPointerId = null;
            if (pointerId !== null && clock.hasPointerCapture(pointerId)) clock.releasePointerCapture(pointerId);
        }
        clock.addEventListener('pointerdown', function (event) {
            if (event.button !== 0 || dragPointerId !== null) return;
            event.preventDefault();
            dragPointerId = event.pointerId;
            clock.setPointerCapture(event.pointerId);
            selectClockPoint(event);
        });
        clock.addEventListener('pointermove', function (event) {
            if (event.pointerId === dragPointerId) selectClockPoint(event);
        });
        clock.addEventListener('pointerup', function (event) {
            if (event.pointerId !== dragPointerId) return;
            selectClockPoint(event);
            stopClockDrag();
            if (mode === 'hour') setMode('minute');
        });
        clock.addEventListener('pointercancel', function (event) {
            if (event.pointerId === dragPointerId) stopClockDrag();
        });
        clock.addEventListener('lostpointercapture', function (event) {
            if (event.pointerId === dragPointerId) dragPointerId = null;
        });
        clear.addEventListener('click', function () {
            input.value = '';
            display.value = '';
            display.setCustomValidity('');
            display.removeAttribute('aria-invalid');
            notifyInputChanged();
            closePicker(false);
        });
        cancel.addEventListener('click', function () { closePicker(true); });
        apply.addEventListener('click', function () {
            input.value = formatValue(selected);
            display.value = formatDisplayValue(selected);
            display.setCustomValidity('');
            display.removeAttribute('aria-invalid');
            notifyInputChanged();
            closePicker(false);
            display.focus();
        });
        function handleOutsidePointerDown(event) {
            if (!document.body.contains(root)) {
                document.removeEventListener('pointerdown', handleOutsidePointerDown);
                return;
            }
            // Clock rendering replaces number buttons during pointerdown. The
            // original event path still identifies that gesture as inside.
            if (!popup.hidden && event.composedPath().indexOf(root) === -1) closePicker(true);
        }
        document.addEventListener('pointerdown', handleOutsidePointerDown);
        root.addEventListener('keydown', function (event) {
            if (event.key === 'Escape' && !popup.hidden) {
                event.preventDefault();
                closePicker(true);
                toggle.focus();
            }
        });

        setReadout();
        return {
            open: openPicker,
            close: function () { closePicker(true); },
            refresh: function () {
                var stored = parseValue(input.value);
                display.value = stored ? formatDisplayValue(stored) : '';
                display.setCustomValidity('');
                display.removeAttribute('aria-invalid');
            }
        };
    }

    global.ByteForgeDateTimePicker = { createPicker: createPicker };
}(window));
