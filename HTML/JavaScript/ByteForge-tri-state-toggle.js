(function (window, document) {
    'use strict';

    var defaultOptions = {
        allowDefault: false,
        defaultMarker: '__AMCH_CONFIG_DEFAULT__',
        defaultValue: 'false',
        trueLabel: 'On',
        falseLabel: 'Off',
        trueGlyph: 'I',
        falseGlyph: 'O'
    };

    /**
     * Copies default options and caller options into a new object.
     * @param {Object} options Caller-provided component options.
     * @returns {Object} The merged options.
     */
    function mergeOptions(options) {
        var merged = {};
        var key;

        for (key in defaultOptions) {
            if (Object.prototype.hasOwnProperty.call(defaultOptions, key)) {
                merged[key] = defaultOptions[key];
            }
        }

        options = options || {};
        for (key in options) {
            if (Object.prototype.hasOwnProperty.call(options, key) && options[key] != null) {
                merged[key] = options[key];
            }
        }

        return merged;
    }

    /**
     * Parses a DOM data value as a boolean.
     * @param {string} value The raw attribute value.
     * @returns {boolean} True when the attribute represents true.
     */
    function parseBoolean(value) {
        value = String(value == null ? '' : value).toLowerCase();
        return value === 'true' || value === '1' || value === 'yes';
    }

    /**
     * Normalizes a boolean-editor value into a tri-state key.
     * @param {string} value The posted form value.
     * @param {Object} options The tri-state options.
     * @returns {string} The normalized state key.
     */
    function normalizeState(value, options) {
        if (value == null || value === '' || value === options.defaultMarker) {
            return options.allowDefault ? 'default' : normalizeBooleanState(options.defaultValue);
        }

        var state = normalizeBooleanState(value);
        if (state) return state;
        return options.allowDefault ? 'default' : normalizeBooleanState(options.defaultValue);
    }

    /**
     * Normalizes true/false-like values into a boolean state key.
     * @param {string} value The raw value.
     * @returns {string} The true or false state key.
     */
    function normalizeBooleanState(value) {
        value = String(value == null ? '' : value).toLowerCase();
        if (value === 'true' || value === '1' || value === 'yes' || value === 'on') return 'true';
        if (value === 'false' || value === '0' || value === 'no' || value === 'off') return 'false';
        return 'false';
    }

    /**
     * Gets the effective boolean position for a state.
     * @param {string} state The state key.
     * @param {Object} options The tri-state options.
     * @returns {string} The true or false visual position.
     */
    function getPosition(state, options) {
        return state === 'default'
            ? normalizeBooleanState(options.defaultValue)
            : normalizeBooleanState(state);
    }

    /**
     * Gets the value that should be posted for the specified state.
     * @param {string} state The tri-state key.
     * @param {Object} options The tri-state options.
     * @returns {string} The posted value.
     */
    function getPostedValue(state, options) {
        if (state === 'true') return 'true';
        if (state === 'false') return 'false';
        return options.allowDefault ? options.defaultMarker : normalizeBooleanState(options.defaultValue);
    }

    /**
     * Gets the visual track glyph for the specified state.
     * @param {string} state The tri-state key.
     * @param {Object} options The tri-state options.
     * @returns {string} The track glyph.
     */
    function getGlyph(state, options) {
        var position = getPosition(state, options);
        if (position === 'true') return options.trueGlyph;
        if (position === 'false') return options.falseGlyph;
        return '';
    }

    /**
     * Gets the caption for the specified state.
     * @param {string} state The tri-state key.
     * @param {Object} options The tri-state options.
     * @returns {string} The visible state caption.
     */
    function getCaption(state, options) {
        var position = getPosition(state, options);
        var label = position === 'true' ? options.trueLabel : options.falseLabel;
        return state === 'default' ? label + ' (default)' : label;
    }

    /**
     * Applies a state to a rendered tri-state toggle.
     * @param {HTMLElement} host The component host element.
     * @param {string} state The state key to apply.
     * @returns {void}
     */
    function applyState(host, state) {
        var options = host._byteForgeTriStateOptions;
        var input = host._byteForgeTriStateInput;
        var button = host.querySelector('.bf-tri-state-toggle-button');
        var glyph = host.querySelector('.bf-tri-state-toggle-glyph');
        var caption = host.querySelector('.bf-tri-state-toggle-caption');

        if (!options || !input || !button) return;

        state = normalizeState(getPostedValue(state, options), options);
        var position = getPosition(state, options);
        input.value = getPostedValue(state, options);
        host.setAttribute('data-state', state);
        host.setAttribute('data-position', position);
        button.setAttribute('data-state', state);
        button.setAttribute('data-position', position);
        button.setAttribute('aria-label', getCaption(state, options));
        button.setAttribute('aria-pressed', position === 'true' ? 'true' : 'false');

        if (glyph) glyph.textContent = getGlyph(state, options);
        if (caption) caption.textContent = getCaption(state, options);
        input.dispatchEvent(new Event('change', { bubbles: true }));
    }

    /**
     * Moves a rendered tri-state toggle to its next state.
     * @param {HTMLElement} host The component host element.
     * @returns {void}
     */
    function cycleState(host) {
        var options = host._byteForgeTriStateOptions;
        var currentPosition = host.getAttribute('data-position') || getPosition(host.getAttribute('data-state'), options);
        var nextPosition = currentPosition === 'true' ? 'false' : 'true';
        var defaultPosition = getPosition('default', options);
        var nextState = options && options.allowDefault && nextPosition === defaultPosition
            ? 'default'
            : nextPosition;

        applyState(host, nextState);
    }

    /**
     * Creates the interactive markup for a tri-state toggle.
     * @param {HTMLElement} host The element that should receive the rendered control.
     * @param {Object} options The tri-state options.
     * @returns {HTMLElement} The rendered host element.
     */
    function render(host, options) {
        if (!host) {
            throw new Error('A tri-state toggle host element is required.');
        }

        options = mergeOptions(options);
        var input = options.input || document.getElementById(options.inputId || host.getAttribute('data-input-id'));
        if (!input) {
            throw new Error('A tri-state toggle hidden input is required.');
        }

        options.allowDefault = parseBoolean(host.getAttribute('data-allow-default')) || options.allowDefault === true;
        options.defaultValue = host.getAttribute('data-default-value') || options.defaultValue;
        options.trueLabel = host.getAttribute('data-true-label') || options.trueLabel;
        options.falseLabel = host.getAttribute('data-false-label') || options.falseLabel;
        options.defaultMarker = host.getAttribute('data-default-marker') || options.defaultMarker;

        host._byteForgeTriStateOptions = options;
        host._byteForgeTriStateInput = input;
        host.classList.add('bf-tri-state-toggle');
        host.innerHTML = '';

        var button = document.createElement('button');
        button.type = 'button';
        button.className = 'bf-tri-state-toggle-button';

        var glyph = document.createElement('span');
        glyph.className = 'bf-tri-state-toggle-glyph';

        var knob = document.createElement('span');
        knob.className = 'bf-tri-state-toggle-knob';

        var caption = document.createElement('span');
        caption.className = 'bf-tri-state-toggle-caption';

        button.appendChild(glyph);
        button.appendChild(knob);
        host.appendChild(button);
        host.appendChild(caption);

        button.addEventListener('click', function () {
            cycleState(host);
        });

        button.addEventListener('keydown', function (event) {
            if (event.key !== ' ' && event.key !== 'Enter') return;
            event.preventDefault();
            cycleState(host);
        });

        applyState(host, normalizeState(input.value || host.getAttribute('data-value'), options));
        return host;
    }

    /**
     * Renders all tri-state toggle hosts under a root element.
     * @param {HTMLElement} root The root element to search.
     * @param {Object} options Shared options for each toggle.
     * @returns {Array} The rendered host elements.
     */
    function renderAll(root, options) {
        root = root || document;
        var hosts = root.querySelectorAll('[data-bf-tri-state-toggle="true"]');
        var rendered = [];

        for (var i = 0; i < hosts.length; i++) {
            rendered.push(render(hosts[i], options));
        }

        return rendered;
    }

    window.ByteForgeTriStateToggle = {
        applyState: applyState,
        cycleState: cycleState,
        render: render,
        renderAll: renderAll
    };
})(window, document);
