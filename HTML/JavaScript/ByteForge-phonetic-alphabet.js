/* Shared agent reference. Keep the CSS and supplied PNG beside this script. */
(function () {
    'use strict';
    var script = document.currentScript;
    if (!script) return;
    var assetRoot = new URL('.', script.src);
    var imageUrl = new URL('ByteForge-phonetic-alphabet.png', assetRoot).href;
    var referenceWindow;
    var storageKey = script.getAttribute('data-state-key') || 'ByteForge.PhoneticAlphabet.Collapsed';
    var position = script.getAttribute('data-position');

    function initialize() {
        if (document.getElementById('bf-phonetic-alphabet')) return;
        var stylesheet = document.createElement('link');
        stylesheet.rel = 'stylesheet';
        stylesheet.href = new URL('ByteForge-phonetic-alphabet.css?v=20261012-1', assetRoot).href;
        document.head.appendChild(stylesheet);

        var link = document.createElement('a');
        var dock = document.createElement('div');
        dock.id = 'bf-phonetic-alphabet';
        dock.className = 'bf-phonetic-alphabet';
        if (position === 'top-right') dock.className += ' bf-phonetic-alphabet-top-right';
        link.id = 'bf-phonetic-alphabet-link';
        link.className = 'bf-phonetic-alphabet-link';
        link.href = imageUrl;
        link.target = 'ByteForgePhoneticAlphabet';
        link.title = 'Open phonetic alphabet in a separate window';
        link.innerHTML = '&#128065;&#65039; Phonetic Alphabet';
        var toggle = document.createElement('button');
        toggle.type = 'button';
        toggle.className = 'bf-phonetic-alphabet-toggle';
        toggle.setAttribute('aria-controls', link.id);
        function setCollapsed(collapsed) {
            link.hidden = collapsed;
            toggle.setAttribute('aria-expanded', String(!collapsed));
            toggle.setAttribute('aria-label', collapsed ? 'Show phonetic alphabet control' : 'Hide phonetic alphabet control');
            toggle.title = toggle.getAttribute('aria-label');
            toggle.innerHTML = collapsed ? '&lt;' : '&gt;';
        }
        toggle.addEventListener('click', function () {
            setCollapsed(!link.hidden);
            if (storageKey) {
                try { window.localStorage.setItem(storageKey, String(link.hidden)); }
                catch (error) { /* Keep the control usable when storage is blocked. */ }
            }
        });
        var initiallyCollapsed = false;
        if (storageKey) {
            try { initiallyCollapsed = window.localStorage.getItem(storageKey) === 'true'; }
            catch (error) { /* Default to expanded when storage is blocked. */ }
        }
        setCollapsed(initiallyCollapsed);
        link.addEventListener('click', function (event) {
            // Open synchronously from the click so popup blockers permit it.
            if (event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
            try {
                if (referenceWindow && !referenceWindow.closed) {
                    referenceWindow.focus();
                    event.preventDefault();
                    return;
                }
                referenceWindow = window.open(imageUrl, link.target,
                    'popup=yes,width=580,height=780,resizable=yes,scrollbars=yes');
                if (referenceWindow) {
                    referenceWindow.focus();
                    event.preventDefault();
                }
            } catch (error) {
                // Retain the native link when an iframe/browser disallows popups.
            }
        });
        dock.appendChild(link);
        dock.appendChild(toggle);
        document.body.appendChild(dock);
    }
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initialize);
    } else {
        initialize();
    }
}());
