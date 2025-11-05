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
     * @param {string} options.type - Modal type: 'info', 'success', 'warning', 'error'. Default: 'info'.
     * @param {string} options.title - Custom title. Default: based on type.
     * @param {boolean} options.showClose - Show close button. Default: true.
     * @param {Array} options.buttons - Custom buttons. Default: single OK button.
     * @param {Function} options.onClose - Callback when modal is closed.
     * @param {boolean} options.closeOnOverlay - Close when clicking overlay. Default: true.
     * @returns {HTMLElement} The modal element.
     */
    static show(message, options = {}) {
        const defaults = {
            type: 'info',
            title: null,
            showClose: true,
            buttons: null,
            onClose: null,
            closeOnOverlay: true
        };
        
        const config = { ...defaults, ...options };
        
        // Remove any existing modal
        ByteForgeModal.closeAll();
        
        // Create modal HTML
        const modal = ByteForgeModal.createModal(message, config);
        document.body.appendChild(modal);
        
        // Show modal with animation
        requestAnimationFrame(() => {
            modal.style.display = 'flex';
        });
        
        // Focus first button for accessibility
        setTimeout(() => {
            const firstButton = modal.querySelector('.byte-forge-modal-button');
            if (firstButton) firstButton.focus();
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
        overlay.setAttribute('aria-labelledby', 'byte-forge-modal-title');
        
        const modal = document.createElement('div');
        modal.className = 'byte-forge-modal';
        
        // Header
        const header = document.createElement('div');
        header.className = 'byte-forge-modal-header';
        
        const title = document.createElement('h3');
        title.className = 'byte-forge-modal-title';
        title.id = 'byte-forge-modal-title';
        title.innerHTML = ByteForgeModal.getTitleContent(config.title, config.type);
        
        header.appendChild(title);
        
        if (config.showClose) {
            const closeBtn = document.createElement('button');
            closeBtn.className = 'byte-forge-modal-close';
            closeBtn.innerHTML = '×';
            closeBtn.setAttribute('aria-label', 'Close');
            closeBtn.onclick = () => ByteForgeModal.close(overlay, config.onClose);
            header.appendChild(closeBtn);
        }
        
        // Body
        const body = document.createElement('div');
        body.className = 'byte-forge-modal-body';
        body.innerHTML = ByteForgeModal.escapeHtml(message);
        
        // Footer
        const footer = document.createElement('div');
        footer.className = 'byte-forge-modal-footer';
        
        const buttons = config.buttons || [{ text: 'OK', primary: true }];
        buttons.forEach((btn, index) => {
            const button = document.createElement('button');
            button.className = `byte-forge-modal-button ${btn.primary ? 'primary' : 'secondary'}`;
            button.textContent = btn.text;
            button.onclick = () => {
                if (btn.onClick) btn.onClick();
                ByteForgeModal.close(overlay, config.onClose);
            };
            if (index === 0) button.setAttribute('data-default', 'true');
            footer.appendChild(button);
        });
        
        // Assemble modal
        modal.appendChild(header);
        modal.appendChild(body);
        modal.appendChild(footer);
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
            } else if (e.key === 'Enter') {
                const defaultBtn = overlay.querySelector('[data-default="true"]');
                if (defaultBtn) defaultBtn.click();
            }
        };
        
        return overlay;
    }
    
    /**
     * Gets title content with icon based on type.
     * @private
     */
    static getTitleContent(customTitle, type) {
        const icons = {
            info: 'ℹ',
            success: '✓',
            warning: '⚠',
            error: '✖'
        };
        
        const defaultTitles = {
            info: 'Information',
            success: 'Success',
            warning: 'Warning', 
            error: 'Error'
        };
        
        const icon = icons[type] || icons.info;
        const title = customTitle || defaultTitles[type] || defaultTitles.info;
        
        return `<span class="byte-forge-modal-icon ${type}">${icon}</span>${title}`;
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
     */
    static close(modal, onClose) {
        if (modal && modal.parentNode) {
            modal.style.opacity = '0';
            setTimeout(() => {
                if (modal.parentNode) {
                    modal.parentNode.removeChild(modal);
                }
                if (onClose) onClose();
            }, 200);
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
     * @returns {HTMLElement} The modal element
     */
    static confirm(message, onConfirm, onCancel = null, title = 'Confirm') {
        return ByteForgeModal.show(message, {
            type: 'warning',
            title: title,
            buttons: [
                { text: 'Yes', primary: true, onClick: onConfirm },
                { text: 'No', onClick: onCancel }
            ]
        });
    }
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