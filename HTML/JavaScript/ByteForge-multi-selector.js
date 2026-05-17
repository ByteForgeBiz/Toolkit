(function (window, $) {
    'use strict';

    var defaultClasses = {
        option: 'bf-multi-selector-option',
        name: 'bf-multi-selector-option-name',
        description: 'bf-multi-selector-option-description'
    };

    /**
     * Escapes text for safe HTML output.
     * @param {*} value Raw value.
     * @returns {string} Escaped value.
     */
    function escHtml(value) {
        return String(value == null ? '' : value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    /**
     * Escapes text for safe HTML attribute output.
     * @param {*} value Raw value.
     * @returns {string} Escaped attribute value.
     */
    function escAttr(value) {
        return escHtml(value).replace(/\r?\n/g, '&#10;');
    }

    /**
     * Escapes a CSS class name for a simple class selector.
     * @param {string} value CSS class name.
     * @returns {string} Escaped CSS class selector fragment.
     */
    function escCssClass(value) {
        return String(value || '').replace(/([ !"#$%&'()*+,./:;<=>?@[\\\]^`{|}~])/g, '\\$1');
    }

    /**
     * Merges caller-supplied class names with the default class map.
     * @param {object} options Optional selector configuration.
     * @param {object} options.classes Optional CSS class overrides.
     * @returns {object} Resolved CSS class map.
     */
    function getClasses(options) {
        var classes = options && options.classes ? options.classes : {};
        return {
            option: classes.option || defaultClasses.option,
            name: classes.name || defaultClasses.name,
            description: classes.description || defaultClasses.description
        };
    }

    /**
     * Formats one custom two-list option.
     * @param {string|number} value Stored option value.
     * @param {string} title Primary label.
     * @param {string} description Secondary label.
     * @param {string} tooltip Optional title text.
     * @param {string} valueAttribute Optional data attribute name.
     * @param {object} options Optional selector configuration.
     * @returns {string} Option HTML.
     */
    function formatOption(value, title, description, tooltip, valueAttribute, options) {
        var classes = getClasses(options);
        valueAttribute = valueAttribute || 'data-value';
        return '<div class="' + escAttr(classes.option) + '" role="option" tabindex="0" aria-selected="false" ' +
            valueAttribute + '="' + escAttr(value) + '" title="' + escAttr(tooltip || description || title) + '">' +
            '<span class="' + escAttr(classes.name) + '">' + escHtml(title) + '</span>' +
            (description ? '<span class="' + escAttr(classes.description) + '">' + escHtml(description) + '</span>' : '') +
            '</div>';
    }

    /**
     * Reads selected custom picker values from a list selector or option selector.
     * @param {string} selector List selector or option selector.
     * @param {Function} readValue Callback that reads a value from one option.
     * @param {object} options Optional selector configuration.
     * @returns {Array} Selected option values.
     */
    function getValues(selector, readValue, options) {
        var classes = getClasses(options);
        var optionSelector = '.' + escCssClass(classes.option);
        var $matched = $(selector);
        var $options = $matched.filter(optionSelector);

        if (!$options.length) {
            $options = $(selector + ' ' + optionSelector + '.is-selected');
        }

        return $options.map(function () { return readValue($(this)); })
            .get()
            .filter(function (value) { return value !== null && value !== undefined && String(value).length > 0; });
    }

    /**
     * Toggles visual selection for a custom picker option.
     * @param {HTMLElement} option Option element.
     * @returns {void}
     */
    function toggleOption(option) {
        var $option = $(option);
        if ($option.closest('[aria-disabled="true"]').length) return;

        var selected = !$option.hasClass('is-selected');
        $option.toggleClass('is-selected', selected).attr('aria-selected', selected ? 'true' : 'false');
    }

    /**
     * Handles keyboard selection for a custom picker option.
     * @param {KeyboardEvent} event Keyboard event from the option row.
     * @returns {void}
     */
    function handleOptionKeydown(event) {
        if (event.key !== ' ' && event.key !== 'Enter') return;
        event.preventDefault();
        toggleOption(this);
    }

    /**
     * Renders available and selected list HTML from item data and a selected lookup.
     * @param {object} options Render options.
     * @param {Array} options.items Items to render.
     * @param {object} options.selected Selected value lookup.
     * @param {string} options.query Optional lowercase search query.
     * @param {Function} options.getValue Callback that returns an item value.
     * @param {Function} options.formatOption Callback that returns option HTML.
     * @param {Function} options.matchesQuery Optional callback that checks the search query.
     * @returns {object} Object with availableHtml and selectedHtml.
     */
    function renderLists(options) {
        options = options || {};
        var availableHtml = '';
        var selectedHtml = '';
        var selected = options.selected || {};
        var query = String(options.query || '').toLowerCase().trim();

        (options.items || []).forEach(function (item) {
            var value = options.getValue(item);
            if (!value && value !== 0) return;
            if (query && typeof options.matchesQuery === 'function' && !options.matchesQuery(item, query)) return;

            var option = options.formatOption(item, value);
            if (selected[value]) selectedHtml += option;
            else availableHtml += option;
        });

        return {
            availableHtml: availableHtml,
            selectedHtml: selectedHtml
        };
    }

    window.ByteForgeMultiSelector = {
        escHtml: escHtml,
        escAttr: escAttr,
        formatOption: formatOption,
        getValues: getValues,
        toggleOption: toggleOption,
        handleOptionKeydown: handleOptionKeydown,
        renderLists: renderLists
    };
}(window, window.jQuery));
