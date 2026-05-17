# ByteForge HTML JavaScript

This folder contains reusable browser-side helpers for ByteForge.Toolkit HTML applications. The scripts are plain JavaScript modules that attach a small public API to `window`, so they can be consumed by ASP.NET Web Forms, MVC, and older pages without a bundler.

## Components

| File | Global API | Purpose |
|------|------------|---------|
| `ByteForge-modal.js` | `ByteForgeModal` | Accessible modal notifications, confirmations, custom button dialogs, and embedded form dialogs |
| `ByteForge-calendar.js` | `showDatePicker`, `initializeDatePickers` | Custom date picker for text inputs where native date styling is not enough |
| `ByteForge-utilities.js` | Utility functions | Form validation, numeric filtering, theme helpers, scrolling, clipboard, and page hardening helpers |
| `ByteForge-bulk-processor.js` | `ByteForgeBulkProcessor` | Preview-then-apply workflow controller for bulk admin actions |
| `ByteForge-multi-selector.js` | `ByteForgeMultiSelector` | Two-list available/selected picker rendering and selection helpers |
| `ByteForge-tri-state-toggle.js` | `ByteForgeTriStateToggle` | Boolean toggle with optional default/true/false configuration override mode |

## Loading Order

Load shared scripts before page-specific scripts. Page scripts should assume the shared APIs already exist and should avoid redefining them.

```html
<script src="JavaScript/ByteForge-modal.js"></script>
<script src="JavaScript/ByteForge-calendar.js"></script>
<script src="JavaScript/ByteForge-utilities.js"></script>
<script src="JavaScript/ByteForge-bulk-processor.js"></script>
<script src="JavaScript/ByteForge-multi-selector.js"></script>
<script src="JavaScript/ByteForge-tri-state-toggle.js"></script>
<script src="Scripts/Page-Specific.js"></script>
```

## Page-Specific Script Guidelines

Page-specific scripts, such as an admin configuration editor, should live in the consuming application rather than this shared folder unless the behavior is truly reusable.

Use these conventions:

* Wrap scripts in an immediately invoked function expression to avoid global variable leakage.
* Exit early when required DOM anchors are missing, so scripts can be bundled safely on shared layouts.
* Use event delegation for dynamic rows, buttons, and table entries.
* Keep generated markup identical to server-rendered markup for the same component.
* HTML-encode dynamic text before inserting string-built markup.
* Keep form field `name` attributes aligned with server model binding.
* Preserve hidden fields that carry immutable identifiers, existing-state flags, encrypted flags, or section/key names.
* Prefer CSS classes and theme variables over inline styles.
* Use HTML entities for special symbols that may be edited in non-UTF-aware tooling.

## Configuration Editor Scripts

Configuration editor pages typically combine server-rendered configuration data with dynamic add-section and add-property behavior. Keep the script responsible for client-side structure only; descriptions and encrypted metadata should come from the server view model, which should read them from `ByteForge.Toolkit.Configuration`.

Recommended markup behavior:

* Master rows include section name, section description, property count, encrypted count, and row actions.
* Expanded detail rows use a direct detail table, not nested cards or extra wrapper panels.
* Existing keys render as text plus hidden inputs; new keys render as editable text inputs.
* Item descriptions render in the final detail-table column.
* Encrypted indicators use `&#x1F512;` and theme-aware CSS.
* New dynamic rows include the same column count, `colspan`, and banding classes as initial rows.

Minimal dynamic-row shape:

```javascript
function htmlEncode(value) {
    return (value || '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
}

function createEntryMarkup(sectionIndex, entryIndex, sectionName) {
    var encodedSectionName = htmlEncode(sectionName);
    return '' +
        '<div class="admin-config-entry" data-entry-index="' + entryIndex + '">' +
            '<input type="hidden" name="Sections[' + sectionIndex + '].Entries[' + entryIndex + '].Section" value="' + encodedSectionName + '" />' +
            '<input type="hidden" name="Sections[' + sectionIndex + '].Entries[' + entryIndex + '].IsEncrypted" value="false" />' +
            '<input type="hidden" name="Sections[' + sectionIndex + '].Entries[' + entryIndex + '].HasStoredValue" value="false" />' +
            '<input type="hidden" name="Sections[' + sectionIndex + '].Entries[' + entryIndex + '].IsExisting" value="false" />' +
            '<div class="admin-config-key-wrap">' +
                '<div class="admin-config-key-main">' +
                    '<input type="text" class="form-control admin-config-key-input" name="Sections[' + sectionIndex + '].Entries[' + entryIndex + '].Key" value="" required />' +
                '</div>' +
            '</div>' +
            '<input type="text" class="form-control" name="Sections[' + sectionIndex + '].Entries[' + entryIndex + '].Value" value="" />' +
            '<div class="config-description config-entry-description"></div>' +
        '</div>';
}
```

## Security Notes

* Treat all server-provided descriptions and names as data.
* Use text content or explicit HTML encoding when inserting values into the DOM.
* Do not write decrypted secrets into markup.
* For encrypted configuration values, post only replacements; an empty value should mean "keep the stored secret" when the server-side model supports that behavior.
* Preserve anti-forgery tokens for preview/apply and save workflows.

## Maintenance Checklist

When adding or changing a JavaScript component:

* Add JSDoc comments for public functions and options.
* Keep the API small and namespaced under a `ByteForge...` global.
* Verify keyboard behavior for buttons, dialogs, and dynamic controls.
* Verify light and dark theme compatibility for generated markup.
* Update `../README.md` and this file with usage notes.
