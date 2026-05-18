# ByteForge UI Framework

A comprehensive HTML/CSS/JavaScript framework for building professional web applications, extracted from the NPD (Newspaper Printing Division) project and preserved in ByteForge.Toolkit as reusable ByteForge assets.

## Overview

This UI framework provides a complete set of components for building modern, accessible web applications with support for both light and dark themes. The framework emphasizes professional appearance, user experience, and code maintainability.

## Components

### 📱 Modal System (`ByteForge-modal.js`)
Professional modal dialogs that replace browser alerts with styled, accessible notifications.

**Features:**
- Multiple types: info, success, warning, error
- Custom titles and messages
- Keyboard navigation (Enter, Escape)
- Accessibility support (ARIA attributes)
- Dark/light mode support
- Smooth animations
- Click-outside-to-close

**Usage:**
```javascript
// Simple notifications
ByteForgeModal.info('Information message');
ByteForgeModal.success('Operation completed!');
ByteForgeModal.warning('Please check your input');
ByteForgeModal.error('Something went wrong');

// Custom modal with callback
ByteForgeModal.error('Database connection failed', 'Connection Error', () => {
    console.log('Modal closed');
});

// Confirmation dialog
ByteForgeModal.confirm('Are you sure?', () => {
    console.log('User confirmed');
}, () => {
    console.log('User cancelled');
}, 'Confirm Action', 'Proceed', 'Stay Here');
```

### 📅 Calendar Widget (`ByteForge-calendar.js`)
Custom date picker that provides better styling control than HTML5 date inputs.

**Features:**
- Month/year navigation
- Today highlighting
- Keyboard accessible
- Cross-browser compatible
- Automatic positioning
- Focus management

**Usage:**
```html
<!-- Basic usage -->
<input type="text" id="dateInput" readonly>
<button onclick="showDatePicker('dateInput')">📅</button>

<!-- Auto-initialize with data attribute -->
<input type="text" data-calendar="true" id="autoDate">
```

```javascript
// Manual initialization
showDatePicker('dateInput');

// Auto-initialize all date inputs
initializeDatePickers();
```

### 🛠️ Utilities (`ByteForge-utilities.js`)
Collection of common utility functions for form handling, validation, and user interaction.

**Key Functions:**
- `CheckNumeric(event)` - Numeric input filtering
- `validateField(element)` - Field validation
- `copyToClipboard(text)` - Cross-browser clipboard operations
- `ScrollTo(elementId)` - Smooth scrolling
- `IsDarkMode()` - Theme detection
- `initializeFormSecurity()` - Security hardening

**Usage:**
```javascript
// Numeric input validation
<input onkeypress="return CheckNumeric(event)">

// Copy to clipboard
await copyToClipboard('Text to copy');

// Scroll to element
ScrollTo('targetElement');
```

### Bulk Processor (`ByteForge-bulk-processor.js`)
Preview-then-apply controller for bulk UI workflows that need a non-mutating preview before sending the final apply request.

**Features:**
- Shared preview/apply state machine
- Input signature check before apply
- Configurable button, error, and summary selectors
- ASP.NET anti-forgery token support
- Page-specific payload, preview rendering, and post-apply callbacks

**Usage:**
```javascript
var processor = ByteForgeBulkProcessor.create({
    previewUrl: previewUrl,
    applyUrl: applyUrl,
    antiForgeryToken: antiForgeryToken,
    getPayload: function () {
        return { BulkText: $('#bulkText').val() };
    },
    getSignature: function () {
        return $('#bulkText').val();
    },
    summaryClasses: {
        inserts: 'bf-bulk-processor-count bf-bulk-processor-count-inserts',
        updates: 'bf-bulk-processor-count bf-bulk-processor-count-updates',
        skipped: 'bf-bulk-processor-count bf-bulk-processor-count-skipped'
    },
    renderPreview: renderPreview,
    onApplied: reloadRows
});

$('#bulkText').on('input change', processor.reset);
$('#bulkApplyBtn').on('click', processor.handleAction);
```

### Multi Selector (`ByteForge-multi-selector.js`)
Reusable two-list selector mechanics for custom available/selected pickers.

**Features:**
- Safe option HTML formatting
- Keyboard and click selection toggling
- Selected-value extraction
- Available/selected list rendering from item data
- Configurable CSS class names for project-specific styling

**Usage:**
```javascript
var selectorOptions = {
    classes: {
        option: 'bf-multi-selector-option',
        name: 'bf-multi-selector-option-name',
        description: 'bf-multi-selector-option-description'
    }
};

var lists = ByteForgeMultiSelector.renderLists({
    items: roles,
    selected: selectedRoleIds,
    getValue: function (role) { return role.id; },
    formatOption: function (role) {
        return ByteForgeMultiSelector.formatOption(
            role.id,
            role.name,
            role.description,
            role.description,
            'data-role-id',
            selectorOptions);
    }
});
```

### Boolean / Tri-State Toggle (`ByteForge-tri-state-toggle.js`)
Two-position boolean control that can opt into a configuration-editor default state.

**Features:**
- Cycles through false and true by default
- Can opt into inherited defaults with `data-allow-default="true"`
- Posts a configurable default marker for server-side removal of persisted overrides when default is enabled
- Shows default captions such as `On (default)` from reflected property defaults
- Keyboard accessible with Enter and Space
- Supports `bf-tri-state-toggle-compact` for table rows and textbox-height editors
- Uses `ByteForge-ui.css` for light/dark theme styling

**Usage:**
```html
<input id="enabledValue" type="hidden" name="Enabled" value="__AMCH_CONFIG_DEFAULT__">
<div data-bf-tri-state-toggle="true"
     data-input-id="enabledValue"
     data-allow-default="true"
     data-default-value="true"></div>

<script src="JavaScript/ByteForge-tri-state-toggle.js"></script>
<script>
    ByteForgeTriStateToggle.renderAll(document);
</script>
```

### 🎨 Styling (`ByteForge-ui.css`)
Comprehensive CSS framework with professional styling and full dark mode support.

**Features:**
- Complete form styling system
- Modal and notification styling
- Loading indicators and spinners
- Professional button designs
- Validation state styling
- Responsive grid layouts
- Dark/light mode support

### Configuration Editor Pattern
Applications that expose Toolkit configuration through an admin UI should use a compact master/detail table instead of a marketing-style page or nested card layout.

**Recommended behavior:**
- Show section names, section descriptions, property counts, encrypted counts, and row actions in the master table.
- Use banded master rows so adjacent configuration sections are easy to scan.
- Put item descriptions in the last column of the expanded detail table.
- Render existing property names as plain text with hidden form fields, and reserve text inputs for new property names.
- Use the lock HTML entity (`&#x1F512;`) for encrypted values to avoid source encoding problems.
- Keep encrypted values masked; blank replacement input means "keep the stored encrypted value".
- Use theme variables for row banding, detail headers, lock indicators, and encrypted-count pills so light and dark modes both remain legible.

**Documentation integration:**
Descriptions should come from `ByteForge.Toolkit.Configuration` metadata:

```csharp
var sectionDescription = Configuration.GetSectionDescription(sectionName);
var itemDescription = Configuration.GetItemDescription(sectionName, key);
```

For dynamic rows added by JavaScript, keep the generated markup structurally identical to the server-rendered markup. In particular, make sure generated master rows include the description column, the same `colspan`, and the same banding class calculation as initial rows.

## Integration Guide

### 1. Basic Setup
```html
<!DOCTYPE html>
<html>
<head>
    <link rel="stylesheet" href="CSS/ByteForge-ui.css">
</head>
<body>
    <!-- Your content -->
    
    <script src="JavaScript/ByteForge-modal.js"></script>
    <script src="JavaScript/ByteForge-calendar.js"></script>
    <script src="JavaScript/ByteForge-utilities.js"></script>
    <script src="JavaScript/ByteForge-bulk-processor.js"></script>
    <script src="JavaScript/ByteForge-multi-selector.js"></script>
    <script src="JavaScript/ByteForge-tri-state-toggle.js"></script>
</body>
</html>
```

### 2. Theme Support
```javascript
// Auto-detect and apply theme
document.body.classList.add(IsDarkMode() ? 'dark-mode' : 'light-mode');

// Update themed images
updateThemedImages();
```

### 3. Form Security
```javascript
// Initialize security features
initializeFormSecurity();
```

## Examples

### Error Handling with Database Operations
```javascript
try {
    const result = await databaseOperation();
    ByteForgeModal.success('Data saved successfully!');
} catch (error) {
    ByteForgeModal.error(`Database error: ${error.message}`, 'Database Error');
}
```

### Form Validation
```html
<form>
    <div class="form-group">
        <label class="form-label required">Name</label>
        <input type="text" class="form-input required" 
               onblur="validateField(this)" 
               onfocus="removeInvalidClass(this)">
    </div>
    
    <div class="form-group">
        <label class="form-label">Start Date</label>
        <div class="date-input-group">
            <input type="text" data-calendar="true" class="form-input" readonly>
        </div>
    </div>
</form>
```

### Professional Buttons
```html
<button class="save-button" onclick="saveData()">
    💾 Save Changes
</button>

<div class="validate-sale">
    <input type="checkbox" id="validate">
    <label for="validate">Validate before saving</label>
</div>
```

## Architecture

### Component Structure
```
HTML/
├── JavaScript/
│   ├── ByteForge-modal.js             # Modal notification system
│   ├── ByteForge-calendar.js          # Custom date picker
│   ├── ByteForge-utilities.js         # Utility functions
│   ├── ByteForge-bulk-processor.js    # Preview/apply bulk processor
│   ├── ByteForge-multi-selector.js    # Two-list selector helpers
│   ├── ByteForge-tri-state-toggle.js  # Default/true/false boolean toggle
│   └── README.md                      # JavaScript component notes
├── CSS/
│   └── ByteForge-ui.css               # Complete styling framework
├── Examples/
│   └── (usage examples)
└── README.md           # This documentation
```

### Design Principles

1. **Accessibility First**: All components include ARIA attributes and keyboard navigation
2. **Progressive Enhancement**: Works without JavaScript, enhanced with it
3. **Theme Consistency**: Dark/light mode support throughout
4. **Security Minded**: Input sanitization and XSS prevention
5. **Performance Optimized**: Minimal DOM manipulation, efficient event handling
6. **Data-Dense Admin UX**: Operational tools should favor scan-friendly tables, stable dimensions, and restrained framing

## Browser Support

- **Modern Browsers**: Full support (Chrome 60+, Firefox 55+, Safari 12+, Edge 79+)
- **Legacy Support**: Graceful degradation with fallbacks
- **Mobile**: Touch-friendly interactions and responsive design

## Migration Notes

When migrating from browser alerts to ByteForgeModal:

```javascript
// Old way
alert('Error message');

// New way
ByteForgeModal.error('Error message');

// Old way
if (confirm('Are you sure?')) {
    // action
}

// New way
ByteForgeModal.confirm('Are you sure?', () => {
    // action
}, null, 'Confirm Action', 'Proceed', 'Cancel');
```

## Security Features

- **Input Sanitization**: HTML escaping in modal content
- **Autocomplete Disabled**: Prevents credential leakage
- **Right-click Protection**: Context menu disabled on non-text elements
- **XSS Prevention**: Safe DOM manipulation practices

## Performance Considerations

- **Event Delegation**: Efficient event handling
- **DOM Reuse**: Minimal element creation/destruction
- **CSS Animations**: Hardware-accelerated transitions
- **Memory Management**: Proper cleanup of modal instances

## Customization

### Theme Variables
The CSS uses consistent color schemes that can be customized:

```css
/* Light mode primary colors */
--primary-color: #3498db;
--success-color: #27ae60;
--warning-color: #f39c12;
--error-color: #e74c3c;

/* Dark mode adaptations */
body.dark-mode {
    --primary-color: #5dade2;
    /* ... */
}
```

### Modal Customization
```javascript
ByteForgeModal.show('Custom message', {
    type: 'warning',
    title: 'Custom Title',
    buttons: [
        { text: 'Save', primary: true, onClick: () => save() },
        { text: 'Cancel', onClick: () => cancel() }
    ]
});
```

## Contributing

When adding new components:

1. Follow the established naming conventions
2. Include comprehensive JSDoc comments
3. Add dark mode support
4. Ensure accessibility compliance
5. Update this documentation

## License

This framework is part of the ByteForge.Toolkit project and follows the same licensing terms.

## 📚 Related Modules

| Module                                        | Description                                                                     |
|-----------------------------------------------|---------------------------------------------------------------------------------|
| [🏠 Home](../readme.md)                       | ByteForge.Toolkit main documentation                                            |
| [CommandLine](../CLI/readme.md)               | Attribute-based CLI parsing with aliasing, typo correction, and plugin support  |
| [Configuration](../Configuration/readme.md)   | INI configuration with typed sections, validation, encryption, and doc metadata |
| [Data](../Data/readme.md)                     | Comprehensive data processing with CSV, Database, Audio, and Exception handling |
| [DataStructures](../DataStructures/readme.md) | AVL tree and URL utility classes                                                |
| [Logging](../Logging/readme.md)               | Thread-safe logging system with async file/console output                       |
| [Mail](../Mail/readme.md)                     | Email utility with HTML support and attachment handling                         |
| [Net](../Net/readme.md)                       | FTP/FTPS/SFTP high-level transfer client                                        |
| [Security](../Security/readme.md)             | AES-based string encryption with key generation and Galois Field logic          |
| [Utils](../Utils/readme.md)                   | Miscellaneous helpers: timing, path utilities, progress bar                     |
| [Core](../Core/readme.md)                     | Embedded resource deployment (WinSCP)                                           |
| [HTML](../HTML/README.md)                     | NPD UI framework components                                                     |
