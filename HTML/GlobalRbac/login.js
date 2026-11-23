(function () {
    "use strict";
    var form = document.getElementById("login-form"), status = document.getElementById("login-status"), error = document.getElementById("login-error");

    function returnUrl() {
        var candidate = new URLSearchParams(window.location.search).get("returnUrl") || "./";
        try {
            var target = new URL(candidate, window.location.href);
            if (target.origin !== window.location.origin || !target.pathname.startsWith(new URL("./", window.location.href).pathname) || /\/login\.html$/i.test(target.pathname)) return "./";
            return target.pathname + target.search + target.hash;
        } catch (ignored) { return "./"; }
    }

    function navigateAfterLogin(data) {
        form.elements.password.value = "";
        if (!data.portals) { window.location.replace(returnUrl()); return; }
        if (!data.portals.length) { showLogin("Your account has no available portals. Contact your administrator."); return; }
        function enter(portal) {
            var target = new URL(portal.signInUrl);
            if (target.protocol !== "https:" && !(target.protocol === "http:" && /^(localhost|127\.0\.0\.1|\[::1\])$/.test(target.hostname))) throw new Error("The portal sign-in URL is invalid.");
            target.hash = "portalHandoff=" + encodeURIComponent(portal.handoffToken);
            window.location.replace(target.href);
        }
        if (data.portals.length === 1) { enter(data.portals[0]); return; }
        form.hidden = true; status.hidden = true;
        var selector = document.getElementById("portal-selector"), options = document.getElementById("portal-options");
        options.textContent = "";
        data.portals.forEach(function (portal) {
            var button = document.createElement("button"); button.type = "button"; button.textContent = portal.displayName;
            button.addEventListener("click", function () { enter(portal); }); var item = document.createElement("p"); item.appendChild(button); options.appendChild(item);
        });
        selector.hidden = false; document.getElementById("portal-heading").focus();
    }

    function api(path, init) {
        init = init || {}; init.headers = init.headers || {}; init.headers.Accept = "application/json";
        if (init.body) init.headers["Content-Type"] = "application/json";
        return fetch(path, init).then(function (response) { return response.text().then(function (text) {
            var data = {}; try { data = text ? JSON.parse(text) : {}; } catch (ignored) { data = { message: text }; }
            if (!response.ok) { var reason = new Error(data.message || data.Message || data.exceptionMessage || data.ExceptionMessage || data.error || "Sign-in failed with HTTP " + response.status + "."); reason.status = response.status; throw reason; }
            return data;
        }); });
    }

    function showLogin(message) {
        document.getElementById("portal-selector").hidden = true;
        form.querySelector("button[type=submit], button").disabled = false;
        status.hidden = true; form.hidden = false; error.textContent = message || ""; error.hidden = !message;
        form.elements.identifier.focus();
    }

    form.addEventListener("submit", function (event) {
        event.preventDefault(); var button = form.querySelector("button"); button.disabled = true; error.hidden = true;
        api("api/auth/login", { method: "POST", body: JSON.stringify({ identifier: form.elements.identifier.value, password: form.elements.password.value, keepLoggedIn: form.elements.keepLoggedIn.checked }) })
            .then(navigateAfterLogin)
            .catch(function (reason) { button.disabled = false; showLogin(reason.message); });
    });

    document.getElementById("portal-back").addEventListener("click", function () { document.getElementById("portal-options").textContent = ""; form.reset(); showLogin(); });

    api("api/auth/me").then(navigateAfterLogin).catch(function (reason) { showLogin(reason.status === 401 ? "" : reason.message); });
}());
