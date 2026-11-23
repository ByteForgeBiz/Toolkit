/** Shows navigation feedback without replacing normal link or form behavior. */
(function () {
    "use strict";
    if (window.ByteForgePageLoading) return;
    var overlay, timer;
    var style = document.createElement("style");
    style.textContent = `
        .bf-page-loading { --bf-loading-bg:#fff; --bf-loading-fg:#17272d; --bf-loading-border:#697e86;
            --bf-loading-accent:#105193; position:fixed; inset:0; z-index:100000; display:grid;
            place-items:center; padding:24px; background:rgba(0,0,0,.35); pointer-events:none; }
        .bf-page-loading[hidden] { display:none !important; }
        @media(prefers-color-scheme:dark) { .bf-page-loading {
            --bf-loading-bg:#142329; --bf-loading-fg:#edf5f6; --bf-loading-border:#859ba4; --bf-loading-accent:#8bc2ff; } }
        [data-theme=light] .bf-page-loading { --bf-loading-bg:#fff; --bf-loading-fg:#17272d;
            --bf-loading-border:#697e86; --bf-loading-accent:#105193; }
        [data-theme=dark] .bf-page-loading { --bf-loading-bg:#142329; --bf-loading-fg:#edf5f6;
            --bf-loading-border:#859ba4; --bf-loading-accent:#8bc2ff; }
        .bf-page-loading-panel { box-sizing:border-box; width:340px; max-width:100%; padding:28px;
            border:1px solid var(--bf-loading-border); border-radius:12px; text-align:center;
            background:var(--bf-loading-bg); color:var(--bf-loading-fg); box-shadow:0 16px 48px #0004; }
        .bf-page-loading-spinner { display:block; width:32px; height:32px; margin:0 auto 16px;
            border:3px solid var(--bf-loading-border); border-top-color:var(--bf-loading-accent);
            border-radius:50%; animation:bf-page-loading-spin .8s linear infinite; }
        .bf-page-loading-title { display:block; font-size:1.15rem; font-weight:600; }
        .bf-page-loading-panel p { font-size:.95rem; font-weight:400; margin:12px 0; }
        .bf-page-loading-dismiss { pointer-events:auto; font:inherit; font-weight:400; cursor:pointer;
            border:1px solid var(--bf-loading-border); border-radius:6px; padding:7px 12px;
            background:var(--bf-loading-bg); color:var(--bf-loading-fg); }
        .bf-page-loading-dismiss:hover { text-decoration:underline; }
        .bf-page-loading-dismiss:focus-visible { outline:3px solid var(--bf-loading-accent); outline-offset:3px; }
        @keyframes bf-page-loading-spin { to { transform:rotate(360deg); } }
        @media(prefers-reduced-motion:reduce) { .bf-page-loading-spinner { animation:none; } }
    `;
    document.head.appendChild(style);

    /** Removes stale feedback after navigation, cancellation, or browser history restoration. */
    function hide() {
        window.clearTimeout(timer);
        if (overlay) overlay.hidden = true;
    }

    /** Displays the same centered spinner/message pattern as AMCH admin navigation. */
    function show() {
        if (!document.body) return;
        if (!overlay) {
            overlay = document.createElement("div");
            overlay.className = "bf-page-loading";
            overlay.hidden = true;
            overlay.innerHTML = '<div class="bf-page-loading-panel">' +
                '<div role="status" aria-live="polite"><span class="bf-page-loading-spinner" aria-hidden="true"></span>' +
                '<strong class="bf-page-loading-title">Loading...</strong><p>Please wait while the page loads.</p></div>' +
                '<button type="button" class="bf-page-loading-dismiss" title="Hide this message without cancelling navigation">&#10006;&#65039; Dismiss</button></div>';
            overlay.querySelector("button").addEventListener("click", hide);
            document.body.appendChild(overlay);
        }
        overlay.hidden = false;
        window.clearTimeout(timer);
        // Navigation can be cancelled by an unsaved-changes prompt or become a download.
        // The panel is non-blocking and dismissible; this final fallback never cancels the request.
        timer = window.setTimeout(hide, 120000);
    }

    /** Limits feedback to same-frame HTTP navigation, excluding known download endpoints. */
    function navigates(href, target) {
        if (target && target.toLowerCase() !== "_self") return false;
        if (!href || href.charAt(0) === "#") return false;
        var url;
        try { url = new URL(href, document.baseURI); } catch (_) { return false; }
        if (!/^https?:$/.test(url.protocol) || url.origin !== window.location.origin) return false;
        if (url.pathname === window.location.pathname && url.search === window.location.search && url.hash) return false;
        return !/(?:download|export)/i.test(url.pathname) && !/\.(?:pdf|csv|xlsx?|zip|docx?|pptx?|mp3|mp4)$/i.test(url.pathname);
    }

    function baseTarget() {
        var base = document.querySelector("base[target]");
        return base ? base.target : "";
    }

    // Window bubbling runs after document-level AJAX handlers have prevented navigation.
    window.addEventListener("click", function (event) {
        if (event.defaultPrevented || event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
        var link = event.target.closest && event.target.closest("a[href]");
        if (!link || link.hasAttribute("download") || link.closest('[data-bf-page-loading="off"]')) return;
        if (navigates(link.getAttribute("href"), link.getAttribute("target") || baseTarget())) show();
    });
    window.addEventListener("submit", function (event) {
        var form = event.target, button = event.submitter;
        if (event.defaultPrevented || form.tagName !== "FORM" || form.closest('[data-bf-page-loading="off"]')) return;
        if (button && button.closest('[data-bf-page-loading="off"]')) return;
        if ((button && button.getAttribute("formmethod") || form.method).toLowerCase() === "dialog") return;
        var action = button && button.getAttribute("formaction") || form.action;
        var target = button && button.getAttribute("formtarget") || form.getAttribute("target") || baseTarget();
        if (navigates(action, target)) show();
    });
    window.addEventListener("pageshow", hide);
    window.addEventListener("pagehide", hide);
    window.addEventListener("hashchange", hide);
    window.addEventListener("keydown", function (event) { if (event.key === "Escape") hide(); });
    window.ByteForgePageLoading = { show: show, hide: hide };
}());
