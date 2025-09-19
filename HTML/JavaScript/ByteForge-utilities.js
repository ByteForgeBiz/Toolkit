/*
 *  ___      _       ___                              _   _ _ _ _   _           _    
 * | _ )_  _| |_ ___| __|__ _ _ __ _ ___   ___   _  _| |_(_) (_) |_(_)___ ___  (_)___
 * | _ \ || |  _/ -_) _/ _ \ '_/ _` / -_) |___| | || |  _| | | |  _| / -_)_-<_ | (_-<
 * |___/\_, |\__\___|_|\___/_| \__, \___|        \_,_|\__|_|_|_|\__|_\___/__(_)/ /__/
 *      |__/                   |___/                                         |__/    
 */

/**
 * Utility Functions Collection
 * 
 * A comprehensive collection of utility functions for web applications,
 * including form validation, clipboard operations, input filtering,
 * scrolling, and focus management.
 * 
 * @module Utilities
 */

// =============================================================================
// FORM VALIDATION AND INPUT HANDLING
// =============================================================================

/**
 * Checks if the pressed key is numeric.
 * Allows numbers, numpad numbers, and navigation keys (backspace, tab, arrows, delete).
 * @param {KeyboardEvent} event - The keyboard event.
 * @returns {boolean} True if the key is numeric, otherwise false.
 */
function CheckNumeric(event) {
    var key = event.which || event.keyCode;
    var isNumeric = (
        (key >= 48 && key <= 57) || // Numbers 0-9
        (key >= 96 && key <= 105) || // Numpad 0-9
        [8, 9, 37, 39, 46].includes(key) // Backspace, Tab, Left Arrow, Right Arrow, Delete
    );

    if (!isNumeric)
        event.preventDefault();

    return isNumeric;
}

/**
 * Removes the 'invalid' class from the specified input element.
 * @param {HTMLInputElement} element - The input element to update.
 */
function removeInvalidClass(element) {
    element.classList.remove('invalid');
}

/**
 * Validates the specified input field without preventing focus change.
 * Checks for required fields and pattern validation.
 * @param {HTMLInputElement} element - The input element to validate.
 * @returns {boolean} True if the field is valid, otherwise false.
 */
function validateField(element) {
    // Remove existing visual indicators
    removeInvalidClass(element);

    // Basic validation without preventing focus change
    if (!element.value && element.classList.contains('required')) {
        element.classList.add('invalid');
        return true;
    }

    // All other validations require the field to have a value
    if (!element.value) return true;

    // Validate regex patterns
    var re = new RegExp(element.pattern);
    if (element.pattern && !re.test(element.value)) {
        element.patternError = element.patternError ?? element.getAttribute('patternError') ?? 'Invalid input';
        element.classList.add('invalid');
        element.oldTitle = element.title;
        element.oldTitle = element.oldTitle == "Please match the requested format." ? "" : element.oldTitle;
        element.title = element.patternError ?? element.pattern;
        return false;
    }
    else if (element.title == element.patterError)
        element.title = element.oldTitle ?? '';

    // Return validation result without blocking
    return true;
}

/**
 * Toggles the required state of an element and updates its attributes accordingly.
 * @param {string} id - The ID of the element to update.
 * @param {boolean} isRequired - Whether the element should be required.
 */
function RequireValue(id, isRequired) {
    var element = document.getElementById(id);
    if (!element)
        return;

    /*
     * Store the original blur and focus events if they are not already stored.
     */
    var blur = element.getAttribute('onblur');
    var focus = element.getAttribute('onfocus');

    if (blur !== null && element.oldBlur === null)
        element.oldBlur = element.blur ?? validateField;

    if (focus !== null && element.oldFocus === null)
        element.oldFocus = element.focus ?? removeInvalidClass;

    /*
     * Toggle the required class on the element and any associated elements.
     */
    element.classList.toggle('required', isRequired);
    var assocElements = document.querySelectorAll(`[for="${id}"]`);
    if (assocElements && assocElements.length > 0)
        assocElements.forEach((el) => { el.classList.toggle('required', isRequired); });

    if (isRequired === true) {
        /* 
         * Restore the original blur and focus events if they are stored.
         */
        element.blur = element.oldBlur;
        element.focus = element.oldFocus;
    }
    else {
        /*
         * Remove the required attribute and any blur and focus events.
         */
        element.removeAttribute('required');
        element.removeAttribute('onblur');
        element.removeAttribute('onfocus');

        element.blur = null;
        element.focus = null;
    }
}

// =============================================================================
// CLIPBOARD OPERATIONS
// =============================================================================

/**
 * Copies the specified text to the clipboard using the modern Clipboard API.
 * @async
 * @param {string} text - The text to copy.
 * @returns {Promise<boolean>} True if the text was copied, otherwise false.
 */
async function copyToClipboardModern(text) {
    try {
        await navigator.clipboard.writeText(text);
        console.log('Text copied to clipboard');
        return true;
    } catch (err) {
        console.error('Failed to copy: ', err);
        return false;
    }
}

/**
 * Copies the specified text to the clipboard using a fallback method for older browsers.
 * @param {string} text - The text to copy.
 * @returns {boolean} True if the text was copied, otherwise false.
 */
function copyToClipboardFallback(text) {
    const textArea = document.createElement("textarea");
    textArea.value = text;

    // Make the textarea invisible but accessible
    textArea.style.position = "fixed";
    textArea.style.left = "-999999px";
    textArea.style.top = "-999999px";

    document.body.appendChild(textArea);
    textArea.focus();
    textArea.select();

    try {
        const successful = document.execCommand('copy');

        if (successful) {
            console.log('Text copied to clipboard');
            return true;
        } else {
            console.error('Failed to copy text');
            return false;
        }
    } catch (err) {
        console.error('Failed to copy: ', err);
        return false;
    } finally {
        document.body.removeChild(textArea);
    }
}

/**
 * Copies the specified text to the clipboard using the modern API if available,
 * otherwise falls back to a legacy method.
 * @async
 * @param {string} text - The text to copy.
 * @returns {Promise<boolean>} True if the text was copied, otherwise false.
 */
async function copyToClipboard(text) {
    // Check if the modern API is available and we're in a secure context
    if (navigator.clipboard && window.isSecureContext) {
        return await copyToClipboardModern(text);
    } else {
        return copyToClipboardFallback(text);
    }
}

/**
 * Copies the record information to the clipboard and shows a success message.
 * @returns {Promise<void>}
 */
async function copyRecordInfo() {
    // Get the name of the current page
    let href = window.location.href;
    let page = href.substring(href.lastIndexOf('/') + 1);
    page = page.indexOf('?') !== -1 ? page.substring(0, page.indexOf('?')) : page;

    let text = 'Page: ' + page + '\n';
    const recordId = document.getElementById('lblRecordID');
    const phoneNumber = document.getElementById('lblPhoneNumber');

    if (recordId)
        text += 'Record ID: ' + recordId.textContent.trim() + '\n';
    if (phoneNumber)
        text += 'Phone Number: ' + phoneNumber.textContent.trim() + '\n';

    // Copy to clipboard
    const success = await copyToClipboard(text);

    if (success)
        showCopyMessage();

    return false;
}

/**
 * Shows a fading success message when record info is copied.
 * @returns {void}
 */
function showCopyMessage() {
    // Remove any existing message
    const existingMessage = document.querySelector('.bf-copy-notification');
    if (existingMessage)
        existingMessage.remove();

    // Create the success message element
    const message = document.createElement('div');
    message.className = 'bf-copy-notification';
    message.textContent = 'Record info copied to clipboard!';

    // Add to the page
    document.body.appendChild(message);

    // Show notification
    requestAnimationFrame(() => {
        message.classList.add('visible');
    });

    // Fade out and remove after 3 seconds
    setTimeout(() => {
        message.classList.remove('visible');
        setTimeout(() => {
            if (message.parentNode)
                message.parentNode.removeChild(message);
        }, 300);
    }, 3000);
}

// =============================================================================
// NAVIGATION AND FOCUS MANAGEMENT
// =============================================================================

/**
 * Scrolls the page to the specified element.
 * @param {string} elementId - The ID of the element to scroll to.
 * @returns {void}
 */
function ScrollTo(elementId) {
    var element = document.getElementById(elementId);
    if (element)
        element.scrollIntoView({ behavior: 'smooth', block: 'center' });
}

/**
 * Sets focus to the first name input field.
 * @returns {void}
 */
function focusFirstElement() {
    var firstNameInput = document.getElementById("txtFirstName");
    if (firstNameInput)
        firstNameInput.focus();
}

/**
 * Sets focus to the specified element by ID.
 * @param {string} elementId - The ID of the element to focus.
 * @returns {boolean} True if the element was found and focused, otherwise false.
 */
function focusElement(elementId) {
    var element = document.getElementById(elementId);
    if (element) {
        element.focus();
        return true;
    }
    return false;
}

/**
 * Moves focus to the next focusable element in the tab order.
 * @param {HTMLElement} currentElement - The current focused element.
 * @returns {boolean} True if focus was moved, otherwise false.
 */
function focusNext(currentElement) {
    var focusableElements = Array.from(document.querySelectorAll(
        'input:not([disabled]), select:not([disabled]), textarea:not([disabled]), button:not([disabled]), [tabindex]:not([tabindex="-1"])'
    ));
    
    var currentIndex = focusableElements.indexOf(currentElement);
    if (currentIndex >= 0 && currentIndex < focusableElements.length - 1) {
        focusableElements[currentIndex + 1].focus();
        return true;
    }
    return false;
}

// =============================================================================
// THEME AND DISPLAY UTILITIES
// =============================================================================

/**
 * Checks if the user prefers a dark color scheme.
 * @returns {boolean} True if the user prefers dark mode, otherwise false.
 */
function IsDarkMode() {
    return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
}

/**
 * Updates all images with the class "themedImage" to be theme aware.
 * Changes the image source from "light" to "dark" and vice-versa.
 */
function updateThemedImages() {
    var isDarkMode = IsDarkMode();
    var images = document.querySelectorAll('.themedImage');

    images.forEach(function (image) {
        var src = image.getAttribute('src');
        if (isDarkMode) {
            image.setAttribute('src', src.replace('light', 'dark'));
        }
        else {
            image.setAttribute('src', src.replace('dark', 'light'));
        }
    });
}

// =============================================================================
// SECURITY AND INPUT PROTECTION
// =============================================================================

/**
 * Prevents the default context menu from appearing on non-text input elements.
 * Allows right-click context menu on text inputs, textareas, and content editable elements.
 * @param {MouseEvent} e - The contextmenu event object.
 * @returns {boolean|void} Returns true to allow context menu on text inputs, otherwise prevents default behavior.
 */
function preventRightClickOnNonText(e) {
    if (e.target.type === 'text' || e.target.type === 'textarea' || e.target.isContentEditable)
        return true;
    e.preventDefault();
}

// =============================================================================
// INITIALIZATION AND SETUP
// =============================================================================

/**
 * Initializes common form controls and security settings.
 * Should be called when the DOM is ready.
 */
function initializeFormSecurity() {
    // Disable HTML5 validation to prevent browser balloons
    var forms = document.getElementsByTagName('form');
    for (var i = 0; i < forms.length; i++)
        forms[i].setAttribute('novalidate', '');

    // Disable autocomplete on all inputs to prevent credential leakage
    var controls = document.getElementsByTagName('input');
    for (var i = 0; i < controls.length; i++) {
        var ctrl = controls[i];
        ctrl.setAttribute('autocomplete', 'new-password');
        ctrl.setAttribute('autocompletetype', 'Disabled');
        ctrl.setAttribute('autocorrect', 'off');
        ctrl.setAttribute('autocapitalize', 'off');
    }

    var selects = document.getElementsByTagName('select');
    for (var i = 0; i < selects.length; i++)
        selects[i].setAttribute('autocomplete', 'new-password');

    // Special handling for credit card fields
    var ccField = document.getElementById('txtCC');
    if (ccField) {
        ccField.setAttribute('autocomplete', 'off');
        ccField.setAttribute('data-lpignore', 'true'); // Helps block LastPass
        ccField.setAttribute('data-1pw-field', 'none'); // Helps block 1Password
    }
}

/**
 * Sets up event listeners for common functionality.
 */
function setupEventListeners() {
    // Copy button functionality
    const copyButton = document.querySelector('.copy-info');
    if (copyButton)
        copyButton.addEventListener('click', copyRecordInfo);

    // Right-click prevention
    document.addEventListener('contextmenu', preventRightClickOnNonText);

    // Theme detection
    updateThemedImages();
    document.body.classList.add(IsDarkMode() ? 'dark-mode' : 'light-mode');
}

// =============================================================================
// AUTO-INITIALIZATION
// =============================================================================

/**
 * Automatically initializes utilities when the DOM is ready.
 */
function initializeUtilities() {
    initializeFormSecurity();
    setupEventListeners();
}

// Auto-initialize when DOM is loaded
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initializeUtilities);
} else {
    initializeUtilities();
}