# NPD UI Framework

A comprehensive HTML/CSS/JavaScript framework for building professional web applications, extracted from the NPD (Newspaper Printing Division) project and preserved in ByteForge.Toolkit.

## Overview

This UI framework provides a complete set of components for building modern, accessible web applications with support for both light and dark themes. The framework emphasizes professional appearance, user experience, and code maintainability.

## Components

### 📱 Modal System (`npdModal.js`)
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
NPDModal.info('Information message');
NPDModal.success('Operation completed!');
NPDModal.warning('Please check your input');
NPDModal.error('Something went wrong');

// Custom modal with callback
NPDModal.error('Database connection failed', 'Connection Error', () => {
    console.log('Modal closed');
});

// Confirmation dialog
NPDModal.confirm('Are you sure?', () => {
    console.log('User confirmed');
}, () => {
    console.log('User cancelled');
});
```

### 📅 Calendar Widget (`calendar.js`)
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

### 🛠️ Utilities (`utilities.js`)
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

### 🎨 Styling (`npd-ui.css`)
Comprehensive CSS framework with professional styling and full dark mode support.

**Features:**
- Complete form styling system
- Modal and notification styling
- Loading indicators and spinners
- Professional button designs
- Validation state styling
- Responsive grid layouts
- Dark/light mode support

## Integration Guide

### 1. Basic Setup
```html
<!DOCTYPE html>
<html>
<head>
    <link rel="stylesheet" href="CSS/npd-ui.css">
</head>
<body>
    <!-- Your content -->
    
    <script src="JavaScript/npdModal.js"></script>
    <script src="JavaScript/calendar.js"></script>
    <script src="JavaScript/utilities.js"></script>
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
    NPDModal.success('Data saved successfully!');
} catch (error) {
    NPDModal.error(`Database error: ${error.message}`, 'Database Error');
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
│   ├── npdModal.js      # Modal notification system
│   ├── calendar.js      # Custom date picker
│   └── utilities.js     # Utility functions
├── CSS/
│   └── npd-ui.css      # Complete styling framework
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

## Browser Support

- **Modern Browsers**: Full support (Chrome 60+, Firefox 55+, Safari 12+, Edge 79+)
- **Legacy Support**: Graceful degradation with fallbacks
- **Mobile**: Touch-friendly interactions and responsive design

## Migration Notes

When migrating from browser alerts to NPDModal:

```javascript
// Old way
alert('Error message');

// New way
NPDModal.error('Error message');

// Old way
if (confirm('Are you sure?')) {
    // action
}

// New way
NPDModal.confirm('Are you sure?', () => {
    // action
});
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
NPDModal.show('Custom message', {
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

| Module                                | Description                                                                     |
|---------------------------------------|---------------------------------------------------------------------------------|
| [🏠 Home](./Home.md)                  | ByteForge.Toolkit main documentation                                            |
| [CommandLine](./CLI.md)               | Attribute-based CLI parsing with aliasing, typo correction, and plugin support  |
| [Configuration](./Configuration.md)   | INI-based configuration system with typed section support                       |
| [Data](./Data.md)                     | Comprehensive data processing with CSV, Database, Audio, and Exception handling |
| [DataStructures](./DataStructures.md) | AVL tree and URL utility classes                                                |
| [Logging](./Logging.md)               | Thread-safe logging system with async file/console output                       |
| [Mail](./Mail.md)                     | Email utility with HTML support and attachment handling                         |
| [Net](./Net.md)                       | FTP/FTPS/SFTP high-level transfer client                                        |
| [Security](./Security.md)             | AES-based string encryption with key generation and Galois Field logic          |
| [Utils](./Utils.md)                   | Miscellaneous helpers: timing, path utilities, progress bar                     |
| [Core](./Core.md)                     | Embedded resource deployment (WinSCP)                                           |
| [HTML](./HTML.md)                     | NPD UI framework components                                                     |

