/* Row navigation delegates to the real phone link; actions retain their own behavior. */
(function (document, window) {
    'use strict';
    document.addEventListener('click', function (event) {
        if (event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
        var row = event.target.closest('.bf-abandoned-callback-row');
        if (!row || event.target.closest('a, button, input, select, textarea, label, summary, details, form')) return;
        if (window.getSelection && window.getSelection().toString()) return;
        var link = row.querySelector('.bf-abandoned-callback-sale-link');
        if (link) link.click();
    });
})(document, window);
