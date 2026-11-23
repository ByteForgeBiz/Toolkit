/** Searchable single-choice picker that preserves the source select's form value and change events. */
(function () {
    'use strict';
    function create(select) {
        if (!select || select.byteForgePicker) return select && select.byteForgePicker;
        var root = document.createElement('div');
        root.className = 'bf-searchable-select';
        var control = document.createElement('button');
        control.type = 'button'; control.className = 'bf-searchable-select-control';
        control.id = select.id + '-control';
        var caption = document.createElement('span');
        var arrow = document.createElement('span'); arrow.innerHTML = '&#9662;'; arrow.setAttribute('aria-hidden', 'true');
        control.appendChild(caption); control.appendChild(arrow);
        var popup = document.createElement('div'); popup.className = 'bf-searchable-select-popup'; popup.hidden = true;
        var search = document.createElement('input'); search.type = 'search'; search.autocomplete = 'off';
        search.className = 'bf-searchable-select-search'; search.placeholder = select.dataset.searchPlaceholder || 'Search options';
        search.setAttribute('aria-label', search.placeholder); search.setAttribute('role', 'combobox');
        search.setAttribute('aria-autocomplete', 'list'); search.setAttribute('aria-expanded', 'false');
        var list = document.createElement('div'); list.id = select.id + '-options'; list.className = 'bf-searchable-select-list'; list.setAttribute('role', 'listbox');
        control.setAttribute('aria-haspopup', 'listbox'); control.setAttribute('aria-expanded', 'false'); control.setAttribute('aria-controls', list.id);
        search.setAttribute('aria-controls', list.id);
        var label = document.querySelector('label[for="' + select.id + '"]');
        if (label) { label.htmlFor = control.id; list.setAttribute('aria-label', label.textContent.trim()); }
        popup.appendChild(search); popup.appendChild(list); root.appendChild(control); root.appendChild(popup);
        select.insertAdjacentElement('afterend', root); select.hidden = true; select.classList.add('bf-searchable-select-source');
        var matches = [], active = -1;
        function refresh() {
            var selected = select.options[select.selectedIndex];
            caption.textContent = selected ? selected.textContent : 'Choose an option';
            control.disabled = select.disabled;
            control.setAttribute('aria-required', String(select.required));
            if (!popup.hidden) render();
        }
        function highlight(index) {
            active = matches.length ? Math.max(0, Math.min(index, matches.length - 1)) : -1;
            Array.prototype.forEach.call(list.children, function (row, i) { row.classList.toggle('is-active', i === active); });
            if (active < 0) search.removeAttribute('aria-activedescendant');
            else { search.setAttribute('aria-activedescendant', list.children[active].id); list.children[active].scrollIntoView({ block: 'nearest' }); }
        }
        function choose(option) {
            if (!option || option.disabled || select.disabled) return;
            select.value = option.value;
            select.setCustomValidity(''); control.removeAttribute('aria-invalid');
            close(); refresh();
            select.dispatchEvent(new Event('input', { bubbles: true }));
            select.dispatchEvent(new Event('change', { bubbles: true }));
            control.focus();
        }
        function render() {
            var query = search.value.trim().toLowerCase();
            matches = Array.prototype.filter.call(select.options, function (option) {
                return !option.disabled && option.value && (option.textContent + ' ' + option.value).toLowerCase().indexOf(query) >= 0;
            });
            list.textContent = '';
            matches.forEach(function (option, index) {
                var row = document.createElement('button'); row.type = 'button'; row.tabIndex = -1;
                row.className = 'bf-searchable-select-option'; row.id = list.id + '-' + index;
                row.setAttribute('role', 'option'); row.setAttribute('aria-selected', String(option.selected)); row.textContent = option.textContent;
                row.addEventListener('mousedown', function (event) { event.preventDefault(); });
                row.addEventListener('click', function () { choose(option); }); list.appendChild(row);
            });
            if (!matches.length) { var empty = document.createElement('div'); empty.className = 'bf-searchable-select-empty'; empty.setAttribute('role', 'status'); empty.textContent = 'No matching options.'; list.appendChild(empty); }
            highlight(matches.findIndex(function (option) { return option.selected; }));
        }
        function open() {
            if (select.disabled) return;
            popup.hidden = false; root.classList.add('is-open'); control.setAttribute('aria-expanded', 'true'); search.setAttribute('aria-expanded', 'true');
            search.value = ''; render(); search.focus();
        }
        function close() {
            popup.hidden = true; root.classList.remove('is-open'); control.setAttribute('aria-expanded', 'false'); search.setAttribute('aria-expanded', 'false');
            search.removeAttribute('aria-activedescendant');
        }
        control.addEventListener('click', function () { if (popup.hidden) open(); else close(); });
        control.addEventListener('keydown', function (event) { if (event.key === 'ArrowDown' || event.key === 'ArrowUp') { event.preventDefault(); open(); } });
        search.addEventListener('input', function (event) { event.stopPropagation(); render(); });
        search.addEventListener('change', function (event) { event.stopPropagation(); });
        search.addEventListener('keydown', function (event) {
            if (event.key === 'ArrowDown' || event.key === 'ArrowUp') { event.preventDefault(); highlight(active + (event.key === 'ArrowDown' ? 1 : -1)); }
            if (event.key === 'Enter') { event.preventDefault(); choose(matches[active]); }
            if (event.key === 'Escape') { event.preventDefault(); close(); control.focus(); }
        });
        root.addEventListener('focusout', function (event) { if (!root.contains(event.relatedTarget)) close(); });
        document.addEventListener('mousedown', function (event) { if (!root.contains(event.target)) close(); });
        select.addEventListener('change', refresh);
        select.addEventListener('invalid', function (event) { event.preventDefault(); control.setAttribute('aria-invalid', 'true'); open(); });
        if (select.form) select.form.addEventListener('reset', function () { setTimeout(function () { close(); refresh(); }, 0); });
        new MutationObserver(refresh).observe(select, { childList: true, subtree: true, attributes: true, attributeFilter: ['disabled', 'selected', 'required'] });
        select.byteForgePicker = { open: open, close: close, refresh: refresh }; refresh(); return select.byteForgePicker;
    }
    window.ByteForgeSearchableSelect = { create: create };
    document.querySelectorAll('select[data-bf-searchable-select]').forEach(create);
}());
