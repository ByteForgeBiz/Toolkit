(function (window, $) {
    'use strict';

    /**
     * Creates a reusable preview-then-apply bulk processor controller.
     * @param {object} options Bulk processing configuration.
     * @param {string} options.previewUrl Server endpoint used for non-mutating previews.
     * @param {string} options.applyUrl Server endpoint used for applying the last previewed import.
     * @param {string} options.antiForgeryToken ASP.NET anti-forgery token value.
     * @param {string} options.actionButtonSelector Selector for the preview/apply button.
     * @param {string} options.errorSelector Selector for the inline error element.
     * @param {string} options.summarySelector Selector for the preview summary element.
     * @param {string} options.previewText Button text used before previewing.
     * @param {string} options.previewingText Button text used while previewing.
     * @param {string} options.applyText Button text used when the last preview can be applied.
     * @param {string} options.applyingText Button text used while applying.
     * @param {string} options.noChangesText Button text used when preview has no changes.
     * @param {Function} options.getPayload Callback that returns the POST payload for preview/apply.
     * @param {Function} options.getSignature Callback that returns a stable signature for current input.
     * @param {Function} options.hasChanges Callback that returns whether the preview has applyable changes.
     * @param {Function} options.renderPreview Callback that renders the preview payload.
     * @param {Function} options.renderAppliedSummary Callback that renders the apply result summary.
     * @param {Function} options.onApplied Callback invoked after a successful apply.
     * @param {object} options.summaryClasses Optional CSS classes for the default applied summary.
     * @returns {object} Public controller methods.
     */
    function create(options) {
        options = options || {};

        var lastPreview = null;
        var lastPreviewSignature = '';

        /**
         * Gets the configured action button.
         * @returns {jQuery} The action button.
         */
        function getButton() {
            return $(options.actionButtonSelector || '#bulkApplyBtn');
        }

        /**
         * Gets the configured error element.
         * @returns {jQuery} The error element.
         */
        function getError() {
            return $(options.errorSelector || '#bulkModalError');
        }

        /**
         * Gets the configured summary element.
         * @returns {jQuery} The summary element.
         */
        function getSummary() {
            return $(options.summarySelector || '#bulkSummary');
        }

        /**
         * Builds a POST payload with the anti-forgery token included.
         * @returns {object} The POST payload.
         */
        function buildPayload() {
            var payload = {};
            if (typeof options.getPayload === 'function') {
                payload = options.getPayload() || {};
            }

            payload.__RequestVerificationToken = options.antiForgeryToken || '';
            return payload;
        }

        /**
         * Gets a stable signature for the current import inputs.
         * @returns {string} Input signature.
         */
        function getSignature() {
            if (typeof options.getSignature === 'function') {
                return String(options.getSignature() || '');
            }

            return JSON.stringify(buildPayload());
        }

        /**
         * Resets the controller to preview mode after import input changes.
         * @returns {void}
         */
        function reset() {
            lastPreview = null;
            lastPreviewSignature = '';
            getButton().prop('disabled', false).text(options.previewText || 'Preview');
        }

        /**
         * Determines whether a preview contains rows that can be applied.
         * @param {object} preview Preview payload from the server.
         * @returns {boolean} True when the preview has inserts or updates.
         */
        function hasChanges(preview) {
            if (typeof options.hasChanges === 'function') {
                return !!options.hasChanges(preview);
            }

            preview = preview || {};
            return (parseInt(preview.inserts, 10) || 0) > 0 || (parseInt(preview.updates, 10) || 0) > 0;
        }

        /**
         * Requests a server-side preview for the current import text.
         * @returns {void}
         */
        function preview() {
            var $btn = getButton();
            $btn.prop('disabled', true).text(options.previewingText || 'Previewing...');
            getError().text('');

            $.ajax({
                url: options.previewUrl,
                type: 'POST',
                dataType: 'json',
                data: buildPayload(),
                success: function (result) {
                    if (!result.success) {
                        $btn.prop('disabled', false).text(options.previewText || 'Preview');
                        getError().text(result.error || 'Preview failed.');
                        return;
                    }

                    lastPreview = result.preview;
                    lastPreviewSignature = getSignature();
                    if (typeof options.renderPreview === 'function') {
                        options.renderPreview(result.preview);
                    }

                    if (hasChanges(result.preview)) {
                        $btn.prop('disabled', false).text(options.applyText || 'Apply Upsert');
                    } else {
                        $btn.prop('disabled', true).text(options.noChangesText || 'No Changes');
                    }
                },
                error: function (xhr) {
                    $btn.prop('disabled', false).text(options.previewText || 'Preview');
                    getError().text('Request failed (' + xhr.status + ').');
                }
            });
        }

        /**
         * Applies the last previewed import when the input signature still matches.
         * @returns {void}
         */
        function apply() {
            if (!lastPreview || lastPreviewSignature !== getSignature()) {
                preview();
                return;
            }

            var $btn = getButton();
            $btn.prop('disabled', true).text(options.applyingText || 'Applying...');
            getError().text('');

            $.ajax({
                url: options.applyUrl,
                type: 'POST',
                dataType: 'json',
                data: buildPayload(),
                success: function (result) {
                    $btn.prop('disabled', false).text(options.applyText || 'Apply Upsert');
                    if (!result.success) {
                        getError().text(result.error || 'Bulk apply failed.');
                        return;
                    }

                    renderAppliedSummary(result.result || {});
                    if (typeof options.onApplied === 'function') {
                        options.onApplied(result.result || {});
                    }
                },
                error: function (xhr) {
                    $btn.prop('disabled', false).text(options.applyText || 'Apply Upsert');
                    getError().text('Request failed (' + xhr.status + ').');
                }
            });
        }

        /**
         * Renders the standard applied summary line.
         * @param {object} applyResult Apply result counters.
         * @returns {void}
         */
        function renderAppliedSummary(applyResult) {
            if (typeof options.renderAppliedSummary === 'function') {
                options.renderAppliedSummary(applyResult, getSummary());
                return;
            }

            var classes = options.summaryClasses || {};
            getSummary().html(
                '<strong>Applied.</strong> ' +
                '<span class="' + (classes.inserts || 'bf-bulk-processor-count bf-bulk-processor-count-inserts') + '">' + (applyResult.inserts || 0) + ' inserts</span> ' +
                '<span class="' + (classes.updates || 'bf-bulk-processor-count bf-bulk-processor-count-updates') + '">' + (applyResult.updates || 0) + ' updates</span> ' +
                '<span class="' + (classes.skipped || 'bf-bulk-processor-count bf-bulk-processor-count-skipped') + '">' + (applyResult.skipped || 0) + ' skipped</span>'
            );
        }

        /**
         * Performs preview first, then apply when the current input matches the preview.
         * @returns {void}
         */
        function handleAction() {
            if (!lastPreview || lastPreviewSignature !== getSignature()) {
                preview();
                return;
            }

            apply();
        }

        return {
            reset: reset,
            preview: preview,
            apply: apply,
            handleAction: handleAction,
            renderAppliedSummary: renderAppliedSummary
        };
    }

    window.ByteForgeBulkProcessor = {
        create: create
    };
}(window, window.jQuery));
