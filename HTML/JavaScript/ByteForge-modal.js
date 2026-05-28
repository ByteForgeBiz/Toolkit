/*
 *  ___      _       ___                                       _      _    _
 * | _ )_  _| |_ ___| __|__ _ _ __ _ ___   ___   _ __  ___  __| |__ _| |  (_)___
 * | _ \ || |  _/ -_) _/ _ \ '_/ _` / -_) |___| | '  \/ _ \/ _` / _` | |_ | (_-<
 * |___/\_, |\__\___|_|\___/_| \__, \___|       |_|_|_\___/\__,_\__,_|_(_)/ /__/
 *      |__/                   |___/                                    |__/
 */

/**
 * Modal notification system for modern user notifications.
 * Replaces the old JavaScript alert() functionality with a styled modal.
 *
 * Features:
 * - Multiple modal types: info, success, warning, error
 * - Custom titles and messages
 * - Customizable buttons with callbacks
 * - Keyboard navigation (Enter, Escape)
 * - Accessibility support (ARIA attributes, focus management)
 * - Dark mode support
 * - Smooth animations
 * - Click-to-close overlay
 *
 * Usage:
 * ByteForgeModal.info('This is an info message');
 * ByteForgeModal.error('Something went wrong!', 'Error Title');
 * ByteForgeModal.confirm('Are you sure?', () => console.log('Confirmed'));
 *
 * @class ByteForgeModal
 */
class ByteForgeModal {
    /**
     * Shows a modal notification.
     * @param {string} message - The message to display.
     * @param {Object} options - Configuration options.
     * @param {string} options.type - Modal type: 'none', 'info', 'success', 'warning', 'error'. Default: 'info'.
     * @param {string} options.title - Custom title. Default: based on type.
     * @param {boolean} options.showClose - Show close button. Default: true.
     * @param {Array} options.buttons - Custom buttons. Default: single OK button.
     * @param {Function} options.onClose - Callback when modal is closed.
     * @param {boolean} options.closeOnOverlay - Close when clicking overlay. Default: true.
     * @param {boolean} options.closeExisting - Close existing ByteForge modals before opening. Default: false.
     * @param {string} options.footerHtml - Optional custom footer HTML rendered before buttons.
     * @param {string} options.footerClass - Optional CSS class applied to the generated footer.
     * @param {string} options.contentSelector - Optional selector for existing content to move into the modal body.
     * @param {HTMLElement} options.contentElement - Optional existing element to move into the modal body.
     * @param {string} options.initialFocusSelector - Optional selector inside the modal to focus after open.
     * @returns {HTMLElement} The modal element.
     */
    static show(message, options = {}) {
        const defaults = {
            type: 'info',
            title: null,
            showClose: true,
            buttons: null,
            onClose: null,
            closeOnOverlay: true,
            allowHtml: false,
            modalClass: '',
            bodyClass: '',
            footerClass: '',
            footerHtml: '',
            hideFooter: false,
            maxWidth: null,
            closeExisting: false,
            contentSelector: '',
            contentElement: null,
            initialFocusSelector: ''
        };

        const config = ByteForgeModal.normalizeConfig(defaults, options);

        if (config.closeExisting) {
            ByteForgeModal.closeAll();
        }

        // Create modal HTML
        const modal = ByteForgeModal.createModal(message, config);
        document.body.appendChild(modal);

        // Show modal with animation
        requestAnimationFrame(() => {
            modal.style.display = 'flex';
            requestAnimationFrame(() => {
                modal.classList.add('byte-forge-modal-open');
            });
        });

        // Focus first button for accessibility
        setTimeout(() => {
            const focusTarget = ByteForgeModal.getInitialFocusTarget(modal, config);
            if (focusTarget) focusTarget.focus();
        }, 100);

        return modal;
    }

    /**
     * Creates the modal DOM structure.
     * @private
     */
    static createModal(message, config) {
        const overlay = document.createElement('div');
        overlay.className = 'byte-forge-modal-overlay';
        overlay.setAttribute('role', 'dialog');
        overlay.setAttribute('aria-modal', 'true');
        const modalId = ByteForgeModal.nextModalId();
        overlay.setAttribute('aria-labelledby', `${modalId}-title`);
        overlay.dataset.byteForgeModalId = modalId;

        const modal = document.createElement('div');
        modal.className = 'byte-forge-modal';
        if (config.modalClass) {
            modal.className += ` ${config.modalClass}`;
        }
        if (config.maxWidth) {
            modal.style.maxWidth = typeof config.maxWidth === 'number' ? `${config.maxWidth}px` : config.maxWidth;
        }

        // Header
        const header = document.createElement('div');
        header.className = 'byte-forge-modal-header';

        const title = document.createElement('h3');
        title.className = 'byte-forge-modal-title';
        title.id = `${modalId}-title`;
        ByteForgeModal.appendTitleContent(title, config.title, config.type);

        header.appendChild(title);

        if (config.showClose) {
            const closeBtn = document.createElement('button');
            closeBtn.className = 'byte-forge-modal-close';
            closeBtn.innerHTML = '&#10060;';
            closeBtn.setAttribute('aria-label', 'Close');
            closeBtn.onclick = () => ByteForgeModal.close(overlay, config.onClose);
            header.appendChild(closeBtn);
        }

        // Body
        const body = document.createElement('div');
        body.className = 'byte-forge-modal-body';
        if (config.bodyClass) {
            body.className += ` ${config.bodyClass}`;
        }
        const contentElement = ByteForgeModal.resolveContentElement(config);
        if (contentElement) {
            ByteForgeModal.attachContentElement(overlay, body, contentElement);
        } else {
            body.innerHTML = config.allowHtml ? String(message ?? '') : ByteForgeModal.escapeHtml(message);
        }

        // Footer
        const footer = document.createElement('div');
        footer.className = 'byte-forge-modal-footer';
        if (config.footerClass) {
            footer.className += ` ${config.footerClass}`;
        }

        if (config.footerHtml) {
            const footerContent = document.createElement('div');
            footerContent.className = 'byte-forge-modal-footer-content';
            footerContent.innerHTML = config.footerHtml;
            footer.appendChild(footerContent);
        }

        const buttons = ByteForgeModal.normalizeButtons(config.buttons);
        buttons.forEach((btn, index) => {
            const button = document.createElement('button');
            button.className = `byte-forge-modal-button ${btn.primary ? 'primary' : 'secondary'}`;
            if (btn.className) button.className += ` ${btn.className}`;
            button.type = btn.type;
            button.textContent = btn.text;
            button.onclick = () => {
                const result = typeof btn.onClick === 'function'
                    ? btn.onClick(overlay, button)
                    : null;
                ByteForgeModal.closeAfterButtonResult(overlay, button, config, btn, result);
            };
            if (index === 0) button.setAttribute('data-default', 'true');
            footer.appendChild(button);
        });

        // Assemble modal
        modal.appendChild(header);
        modal.appendChild(body);
        if (!config.hideFooter) {
            modal.appendChild(footer);
        }
        overlay.appendChild(modal);

        // Event handlers
        if (config.closeOnOverlay) {
            overlay.onclick = (e) => {
                if (e.target === overlay) {
                    ByteForgeModal.close(overlay, config.onClose);
                }
            };
        }

        // Keyboard handling
        overlay.onkeydown = (e) => {
            if (e.key === 'Escape') {
                ByteForgeModal.close(overlay, config.onClose);
            } else if (e.key === 'Enter' && !ByteForgeModal.shouldIgnoreDefaultEnter(e)) {
                const defaultBtn = overlay.querySelector('[data-default="true"]');
                if (defaultBtn) defaultBtn.click();
            }
        };

        return overlay;
    }

    /**
     * Normalizes modal configuration and filters unsafe option shapes.
     * @private
     */
    static normalizeConfig(defaults, options) {
        const suppliedOptions = options && typeof options === 'object' && !Array.isArray(options)
            ? options
            : {};
        const config = { ...defaults, ...suppliedOptions };

        config.type = ['none', 'info', 'success', 'warning', 'error'].indexOf(config.type) >= 0
            ? config.type
            : defaults.type;
        config.showClose = typeof config.showClose === 'boolean' ? config.showClose : defaults.showClose;
        config.closeOnOverlay = typeof config.closeOnOverlay === 'boolean' ? config.closeOnOverlay : defaults.closeOnOverlay;
        config.allowHtml = typeof config.allowHtml === 'boolean' ? config.allowHtml : defaults.allowHtml;
        config.title = config.title == null ? null : String(config.title);
        config.modalClass = typeof config.modalClass === 'string' ? config.modalClass : '';
        config.bodyClass = typeof config.bodyClass === 'string' ? config.bodyClass : '';
        config.footerClass = typeof config.footerClass === 'string' ? config.footerClass : '';
        config.footerHtml = typeof config.footerHtml === 'string' ? config.footerHtml : '';
        config.hideFooter = typeof config.hideFooter === 'boolean' ? config.hideFooter : defaults.hideFooter;
        config.closeExisting = typeof config.closeExisting === 'boolean' ? config.closeExisting : defaults.closeExisting;
        config.contentSelector = typeof config.contentSelector === 'string' ? config.contentSelector : '';
        config.contentElement = config.contentElement instanceof HTMLElement ? config.contentElement : null;
        config.initialFocusSelector = typeof config.initialFocusSelector === 'string' ? config.initialFocusSelector : '';
        config.onClose = typeof config.onClose === 'function' ? config.onClose : null;

        return config;
    }

    /**
     * Normalizes modal button configuration.
     * @private
     */
    static normalizeButtons(buttons) {
        const configuredButtons = Array.isArray(buttons) && buttons.length > 0
            ? buttons
            : [{ text: 'Close', primary: true }];

        const normalizedButtons = configuredButtons
            .filter(btn => btn && typeof btn === 'object' && !Array.isArray(btn))
            .map(btn => ({
                text: btn.text == null || String(btn.text).trim() === '' ? 'Close' : String(btn.text),
                primary: btn.primary === true,
                onClick: typeof btn.onClick === 'function' ? btn.onClick : null,
                closeOnClick: btn.closeOnClick !== false,
                className: typeof btn.className === 'string' ? btn.className : '',
                type: typeof btn.type === 'string' ? btn.type : 'button'
            }));

        return normalizedButtons.length > 0
            ? normalizedButtons
            : [{ text: 'Close', primary: true, onClick: null, closeOnClick: true, className: '', type: 'button' }];
    }

    /**
     * Gets the element that should receive focus after the modal opens.
     * @param {HTMLElement} modal The modal overlay.
     * @param {Object} config The normalized modal configuration.
     * @returns {HTMLElement|null} The element to focus.
     */
    static getInitialFocusTarget(modal, config) {
        if (config.initialFocusSelector) {
            const selected = modal.querySelector(config.initialFocusSelector);
            if (selected) return selected;
        }

        return modal.querySelector('.byte-forge-modal-button');
    }

    /**
     * Checks whether Enter should be left to the focused editor control.
     * @param {KeyboardEvent} event - The keyboard event raised inside the modal.
     * @returns {boolean} True when the modal should not click the default button.
     */
    static shouldIgnoreDefaultEnter(event) {
        const target = event ? event.target : null;
        if (!target) return false;

        if (target.tagName === 'TEXTAREA') return true;
        if (target.isContentEditable) return true;

        return false;
    }

    /**
     * Creates a unique identifier for one modal instance.
     * @returns {string} The modal identifier.
     */
    static nextModalId() {
        ByteForgeModal._counter = (ByteForgeModal._counter || 0) + 1;
        return `byte-forge-modal-${ByteForgeModal._counter}`;
    }

    /**
     * Resolves an existing content element supplied through modal options.
     * @param {Object} config The normalized modal configuration.
     * @returns {HTMLElement|null} The content element to move into the modal.
     */
    static resolveContentElement(config) {
        if (config.contentElement) return config.contentElement;
        if (!config.contentSelector) return null;
        return document.querySelector(config.contentSelector);
    }

    /**
     * Moves an existing content element into the modal body and stores restoration metadata.
     * @param {HTMLElement} overlay The modal overlay.
     * @param {HTMLElement} body The modal body.
     * @param {HTMLElement} contentElement The element to move.
     */
    static attachContentElement(overlay, body, contentElement) {
        const placeholder = document.createComment('byte-forge-modal-content-placeholder');
        const parent = contentElement.parentNode;
        if (parent) {
            parent.insertBefore(placeholder, contentElement);
        }

        overlay._byteForgeContent = { element: contentElement, placeholder, wasHidden: contentElement.hidden };
        contentElement.hidden = false;
        body.appendChild(contentElement);
    }

    /**
     * Restores a moved content element to its original DOM location.
     * @param {HTMLElement} modal The modal overlay.
     */
    static restoreContentElement(modal) {
        const state = modal ? modal._byteForgeContent : null;
        if (!state || !state.element || !state.placeholder) return;

        const parent = state.placeholder.parentNode;
        if (parent) {
            parent.insertBefore(state.element, state.placeholder);
            parent.removeChild(state.placeholder);
        }
        state.element.hidden = state.wasHidden;

        delete modal._byteForgeContent;
    }

    /**
     * Closes a modal after a button callback when the callback and button allow it.
     * @param {HTMLElement} overlay The modal overlay.
     * @param {HTMLElement} button The clicked button.
     * @param {Object} config The normalized modal configuration.
     * @param {Object} btn The normalized button configuration.
     * @param {*} result The button callback return value.
     */
    static closeAfterButtonResult(overlay, button, config, btn, result) {
        if (!btn.closeOnClick || result === false) return;

        if (result && typeof result.then === 'function') {
            button.disabled = true;
            result
                .then(value => {
                    if (value !== false) ByteForgeModal.close(overlay, config.onClose);
                })
                .catch(() => {
                    button.disabled = false;
                });
            return;
        }

        ByteForgeModal.close(overlay, config.onClose);
    }

    /**
     * Gets title content with icon based on type.
     * @private
     */
    static getTitleContent(customTitle, type) {
        const wrapper = document.createElement('span');
        ByteForgeModal.appendTitleContent(wrapper, customTitle, type);
        return wrapper.innerHTML;
    }

    /**
     * Appends title content with an icon based on type.
     * @param {HTMLElement} titleElement The title element to populate.
     * @param {string|null} customTitle Optional caller-provided title.
     * @param {string} type Modal type.
     * @returns {void}
     */
    static appendTitleContent(titleElement, customTitle, type) {
        const icons = {
            none: '',
            info: 'ℹ',
            success: '✓',
            warning: '⚠',
            error: '✖'
        };

        const defaultTitles = {
            none: 'Information',
            info: 'Information',
            success: 'Success',
            warning: 'Warning',
            error: 'Error'
        };

        const icon = icons[type] || icons.info;
        const title = customTitle || defaultTitles[type] || defaultTitles.info;

        if (type !== 'none') {
            const iconSpan = document.createElement('span');
            iconSpan.className = `byte-forge-modal-icon ${type}`;
            iconSpan.textContent = icon;
            titleElement.appendChild(iconSpan);
        }

        titleElement.appendChild(document.createTextNode(title));
    }

    /**
     * Escapes HTML to prevent XSS.
     * @private
     */
    static escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    /**
     * Closes a specific modal.
     * @param {HTMLElement} modal - The overlay element to close.
     * @param {Function} onClose - Optional callback after the modal is removed.
     */
    static close(modal, onClose) {
        if (modal && modal.parentNode && modal.dataset.byteForgeClosing !== 'true') {
            modal.dataset.byteForgeClosing = 'true';
            modal.classList.remove('byte-forge-modal-open');
            modal.classList.add('byte-forge-modal-closing');

            setTimeout(() => {
                if (modal.parentNode) {
                    ByteForgeModal.restoreContentElement(modal);
                    modal.parentNode.removeChild(modal);
                }
                if (typeof onClose === 'function') onClose();
            }, 220);
        }
    }

    /**
     * Closes all open modals.
     */
    static closeAll() {
        const modals = document.querySelectorAll('.byte-forge-modal-overlay');
        modals.forEach(modal => ByteForgeModal.close(modal));
    }

    /**
     * Convenience method for info modal.
     * @param {string} message - The message to display
     * @param {string} title - Optional custom title
     * @param {Function} onClose - Optional callback when modal is closed
     * @returns {HTMLElement} The modal element
     */
    static info(message, title = null, onClose = null) {
        return ByteForgeModal.show(message, { type: 'info', title, onClose });
    }

    /**
     * Convenience method for success modal.
     * @param {string} message - The message to display
     * @param {string} title - Optional custom title
     * @param {Function} onClose - Optional callback when modal is closed
     * @returns {HTMLElement} The modal element
     */
    static success(message, title = null, onClose = null) {
        return ByteForgeModal.show(message, { type: 'success', title, onClose });
    }

    /**
     * Convenience method for warning modal.
     * @param {string} message - The message to display
     * @param {string} title - Optional custom title
     * @param {Function} onClose - Optional callback when modal is closed
     * @returns {HTMLElement} The modal element
     */
    static warning(message, title = null, onClose = null) {
        return ByteForgeModal.show(message, { type: 'warning', title, onClose });
    }

    /**
     * Convenience method for error modal.
     * @param {string} message - The message to display
     * @param {string} title - Optional custom title
     * @param {Function} onClose - Optional callback when modal is closed
     * @returns {HTMLElement} The modal element
     */
    static error(message, title = null, onClose = null) {
        return ByteForgeModal.show(message, { type: 'error', title, onClose });
    }

    /**
     * Shows a confirmation modal with Yes/No buttons.
     * @param {string} message - The confirmation message.
     * @param {Function} onConfirm - Callback when Yes is clicked.
     * @param {Function} onCancel - Callback when No is clicked.
     * @param {string} title - Optional title.
     * @param {string} yesText - Optional text for the confirm button.
     * @param {string} noText - Optional text for the cancel button.
     * @returns {HTMLElement} The modal element
     */
    static confirm(message, onConfirm, onCancel = null, title = 'Confirm', yesText = 'Yes', noText = 'No') {
        return ByteForgeModal.show(message, {
            type: 'warning',
            title: title,
            buttons: [
                { text: yesText, primary: true, onClick: onConfirm },
                { text: noText, onClick: onCancel }
            ]
        });
    }
}

if (typeof window !== 'undefined') {
    window.ByteForgeModal = ByteForgeModal;
}

/**
 * Backward compatibility function to replace window.alert.
 * @param {string} message - The message to display.
 */
function modalAlert(message) {
    ByteForgeModal.info(message);
}

/**
 * Shows a standardized "No Record" error modal.
 * @param {string} recordIdentifier - The record identifier that wasn't found.
 */
function modalAlertNoRecord(recordIdentifier) {
    const message = `No record found${recordIdentifier ? ` for: ${recordIdentifier}` : ''}.`;
    ByteForgeModal.error(message, 'Record Not Found');
}
