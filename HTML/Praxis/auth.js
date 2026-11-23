(function () {
    "use strict";
    // Renew remembered proof before page startup requests share the saved bearer token.
    // Keep component-held tokens working after rotation without replaying writes.
    if (window.fetch) {
        var transport = window.fetch;
        var renewal = null, nextRenewal = 0, replacedTokens = Object.create(null), redirecting = false;
        var rememberedKey = "praxis.reportPortal.persistentAccessToken";
        function rememberedToken() {
            try {
                if (sessionStorage.getItem("praxis.reportPortal.accessToken")) return "";
                return localStorage.getItem(rememberedKey) || "";
            } catch (ignored) { return ""; }
        }
        function sameSession(token, current) {
            while (replacedTokens[token]) token = replacedTokens[token];
            return token === current;
        }
        function renew(token) {
            if (renewal) return renewal;
            if (Date.now() < nextRenewal) return Promise.resolve();
            renewal = transport("api/auth/refresh", { method: "POST", cache: "no-store", credentials: "same-origin",
                headers: { Authorization: "Bearer " + token, Accept: "application/json" } })
                .then(function (response) {
                    if (response.status === 401) {
                        // A revoked/expired proof cannot be renewed. The login page handles reauthentication.
                        if (!document.getElementById("login-form") && !redirecting && rememberedToken() === token) {
                            redirecting = true;
                            redirect();
                        }
                        return;
                    }
                    if (!response.ok) return;
                    return response.json().then(function (data) {
                        // Never resurrect a sign-out or overwrite a different login from another tab.
                        if (!data.accessToken || rememberedToken() !== token) return;
                        localStorage.setItem(rememberedKey, data.accessToken);
                        if (data.accessToken !== token) replacedTokens[token] = data.accessToken;
                        nextRenewal = Date.now() + 15 * 60 * 1000;
                    });
                }).catch(function () { /* Keep saved authentication during transport/storage failures. */ })
                .then(function () { nextRenewal = Math.max(nextRenewal, Date.now() + 30000); renewal = null; });
            return renewal;
        }
        function send(input, options, url, bearer) {
            return transport(input, options).then(function (response) {
                if (response.status === 401 && url.origin === location.origin && /\/api\//.test(url.pathname) &&
                    !/\/auth\/(portal-token|portal-handoff|refresh)$/.test(url.pathname) && bearer &&
                    !document.getElementById("login-form")) {
                    var current = "";
                    try { current = sessionStorage.getItem("praxis.reportPortal.accessToken") || localStorage.getItem(rememberedKey) || ""; } catch (ignored) { }
                    if (current && sameSession(bearer, current) && !redirecting) {
                        redirecting = true;
                        redirect();
                    }
                }
                return response;
            });
        }
        window.fetch = function (input, options) {
            var url = new URL(typeof input === "string" || input instanceof URL ? input : input.url, location.href);
            var headers = new Headers(options && options.headers || (input && input.headers) || {});
            var bearer = (headers.get("Authorization") || "").replace(/^Bearer\s+/i, "");
            var saved = rememberedToken();
            if (url.origin !== location.origin || !/\/api\//.test(url.pathname) || !saved ||
                !sameSession(bearer, saved) || /\/auth\/(portal-token|portal-handoff|refresh)$/.test(url.pathname))
                return send(input, options, url, bearer);
            return renew(saved).then(function () {
                var current = rememberedToken();
                if (!current || !sameSession(saved, current)) throw new Error("Your Praxis session changed. Please reload the page.");
                headers.set("Authorization", "Bearer " + current);
                var updated = Object.assign({}, options || {}, { headers: headers });
                return send(input, updated, url, current);
            });
        };
    }
    // Keep request failures visible even when an optional feature catches its own rejection.
    if (!window.ByteForgePraxisRequestErrors && window.fetch) {
        window.ByteForgePraxisRequestErrors = true;
        var originalFetch = window.fetch;
        function showRequestFailure(method, endpoint, status, data) {
            var area = document.getElementById("praxis-request-errors");
            if (!area) {
                area = document.createElement("section");
                area.id = "praxis-request-errors";
                area.className = "report-shell";
                area.setAttribute("aria-label", "Request errors");
                var main = document.querySelector("main") || document.body;
                main.insertBefore(area, main.firstChild);
            }
            var item = document.createElement("div");
            item.className = "report-message is-error";
            item.setAttribute("role", "alert");
            var detail = data && (data.message || data.Message || data.error_description);
            var reference = data && (data.requestId || data.RequestId);
            var label = document.createElement("span");
            label.textContent = method + " " + endpoint + " failed (" + status + "). " +
                (typeof detail === "string" ? detail : "Please retry or contact support.") +
                (/^[0-9a-f]{12}$/i.test(reference || "") ? " Support reference: " + reference + "." : "");
            item.appendChild(label);
            var close = document.createElement("button");
            close.type = "button";
            close.className = "report-link-button";
            close.innerHTML = " &#10006;&#65039; Dismiss";
            close.setAttribute("aria-label", "Dismiss request error");
            close.addEventListener("click", function () { item.remove(); });
            item.appendChild(close);
            area.appendChild(item);
        }
        window.fetch = function (input, options) {
            var url = new URL(typeof input === "string" || input instanceof URL ? input : input.url, location.href);
            var tracked = url.origin === location.origin && /\/api\//.test(url.pathname);
            var method = (options && options.method) || (input && input.method) || "GET";
            return originalFetch.apply(this, arguments).then(function (response) {
                if (tracked && !response.ok) {
                    response.clone().json().catch(function () { return null; }).then(function (data) {
                        showRequestFailure(method, url.pathname, "HTTP " + response.status, data);
                    });
                }
                return response;
            }, function (error) {
                if (tracked) showRequestFailure(method, url.pathname, "network error", null);
                throw error;
            });
        };
        window.addEventListener("error", function (event) {
            // Browsers emit these layout-cycle notifications without a script exception.
            // Leave them in developer tools; they do not indicate a failed save or startup.
            if (!event.error && (event.message === "ResizeObserver loop completed with undelivered notifications." ||
                event.message === "ResizeObserver loop limit exceeded")) return;
            if (event.filename) {
                var script = new URL(event.filename, location.href);
                if (script.origin === location.origin) showRequestFailure("Portal startup", script.pathname,
                    "JavaScript error", { message: event.message || "A portal component could not start. Please reload or contact support." });
            }
        });
    }
    var sessionKey = "praxis.reportPortal.accessToken";
    var persistentKey = "praxis.reportPortal.persistentAccessToken";
    function clear() {
        fetch("api/config-session", { method: "DELETE", credentials: "same-origin", keepalive: true }).catch(function () { });
        try { sessionStorage.removeItem(sessionKey); } catch (ignored) { }
        try { localStorage.removeItem(persistentKey); } catch (ignored) { }
    }
    function redirect() {
        var target = location.pathname.split("/").pop() + location.hash;
        location.replace("login.html?returnUrl=" + encodeURIComponent(target));
    }
    window.ByteForgePraxisAuth = { redirect: redirect };
    var form = document.getElementById("login-form");
    if (!form) return;
    var panel = document.getElementById("login-panel");
    var error = document.getElementById("login-error");
    var loading = document.getElementById("login-loading");
    function showLogin(message) {
        loading.hidden = true;
        panel.hidden = false;
        error.textContent = message || "";
        error.hidden = !message;
    }
    // Only return to a known portal page that the server grants this user.
    function destination(pages) {
        var requested = new URLSearchParams(location.search).get("returnUrl") || "report.html";
        var known = { "confirmations.html": "PraxisConfirmations", "appointment-review.html": "PraxisAppointmentReview", "report.html": "PraxisReports", "report.html#reports": "PraxisReports", "report.html#security": "PraxisRbacAdmin", "scripts.html": "PraxisScriptEditor", "configuration.html": "PraxisConfig", "logs.html": "PraxisLogs", "advisors.html": "PraxisAdvisorEditor", "manual-sale.html": "PraxisManualSale" };
        if (known[requested] && pages.some(function (page) { return page.key === known[requested]; })) return requested;
        var fallback = Object.keys(known).filter(function (route) {
            return pages.some(function (page) { return page.key === known[route]; });
        })[0];
        if (!fallback) throw new Error("Your Praxis roles do not grant access to any portal pages.");
        return fallback;
    }
    function access(token) {
        return fetch("api/auth/pages", { headers: { Authorization: "Bearer " + token, Accept: "application/json" }, cache: "no-store" })
            .then(function (response) {
                if (!response.ok) throw new Error("Your session is unavailable. Please sign in again.");
                return response.json();
            }).then(function (data) { return destination(data.pages || []); });
    }
    form.addEventListener("submit", function (event) {
        event.preventDefault();
        var button = form.querySelector("button[type=submit]");
        button.disabled = true;
        error.hidden = true;
        var persistent = document.getElementById("keep-logged-in").checked;
        fetch("api/auth/portal-token", {
            method: "POST",
            headers: { "Content-Type": "application/json", Accept: "application/json" },
            body: JSON.stringify({ identifier: document.getElementById("account-identifier").value.trim(), password: document.getElementById("account-password").value, keepLoggedIn: persistent })
        }).then(function (response) {
            return response.json().catch(function () { return {}; }).then(function (data) {
                if (!response.ok || !data.accessToken) throw new Error(data.message || data.Message || "Unable to sign in. Check your credentials and try again.");
                document.getElementById("account-password").value = "";
                return access(data.accessToken).then(function (route) {
                    clear();
                    try { (persistent ? localStorage : sessionStorage).setItem(persistent ? persistentKey : sessionKey, data.accessToken); }
                    catch (failure) { throw new Error("Allow browser storage for Praxis to stay signed in between pages."); }
                    location.replace(route);
                });
            });
        }).catch(function (failure) { clear(); showLogin(failure.message); })
            .then(function () { button.disabled = false; });
    });
    if (new URLSearchParams(location.search).get("signOut") === "1") {
        clear();
        history.replaceState(null, "", location.pathname);
        showLogin();
        return;
    }
    var handoff = new URLSearchParams(location.hash.slice(1)).get("portalHandoff");
    if (handoff) {
        history.replaceState(null, "", location.pathname);
        fetch("api/auth/portal-handoff", { method: "POST", credentials: "same-origin", cache: "no-store",
            headers: { "Content-Type": "application/json", Accept: "application/json", "X-TELCO-Handoff": "1" },
            body: JSON.stringify({ token: handoff }) })
            .then(function (response) { return response.json().then(function (data) {
                if (!response.ok || !data.accessToken) throw new Error(data.message || "This sign-in link is invalid or has expired. Return to sign in.");
                return access(data.accessToken).then(function (route) {
                    clear();
                    try { (data.keepLoggedIn ? localStorage : sessionStorage).setItem(data.keepLoggedIn ? persistentKey : sessionKey, data.accessToken); }
                    catch (failure) { throw new Error("Allow browser storage for Praxis to stay signed in between pages."); }
                    location.replace(route);
                });
            }); }).catch(function (failure) { clear(); showLogin(failure.message); });
        return;
    }
    var token = "";
    try { token = sessionStorage.getItem(sessionKey) || localStorage.getItem(persistentKey) || ""; } catch (ignored) { }
    if (!token) showLogin();
    else access(token).then(function (route) { location.replace(route); })
        .catch(function (failure) { clear(); showLogin(failure.message); });
}());
