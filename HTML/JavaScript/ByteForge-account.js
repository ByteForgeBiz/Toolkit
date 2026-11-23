(function (global) {
    "use strict";
    function attach(button, options) {
        if (!button) return null;
        if (button.bfAccount) { Object.assign(button.bfAccount.options, options || {}); return button.bfAccount; }
        var account = { options: options || {} }, profile, dialog;
        button.bfAccount = account; button.classList.add("bf-account-button");
        button.setAttribute("aria-haspopup", "dialog"); button.title = "View and edit my profile";
        function request(body) {
            var headers = { Accept: "application/json", "X-TELCO-Profile": "1" };
            var token = account.options.getToken && account.options.getToken();
            if (token) headers.Authorization = "Bearer " + token;
            var init = { method: body ? "POST" : "GET", headers: headers, credentials: token ? "omit" : "same-origin", cache: "no-store" };
            if (body && button.dataset.formEncoded === "true") {
                var fields = new URLSearchParams(body), csrf = document.querySelector('input[name="__RequestVerificationToken"]');
                if (csrf) fields.append("__RequestVerificationToken", csrf.value);
                headers["Content-Type"] = "application/x-www-form-urlencoded; charset=UTF-8"; init.body = fields.toString();
            } else if (body) { headers["Content-Type"] = "application/json"; init.body = JSON.stringify(body); }
            return fetch(button.dataset.api, init).then(function (response) {
                return response.text().then(function (text) {
                    var data; try { data = text ? JSON.parse(text) : {}; } catch (ignored) { data = {}; }
                    if (!response.ok || response.redirected) throw new Error(data.message || data.Message || "Your profile could not be loaded or saved. Please sign in again.");
                    return data;
                });
            });
        }
        function normalize(value) { return { displayName: value.displayName || value.DisplayName, username: value.username || value.Username, email: value.email || value.Email }; }
        function update(value) { profile = normalize(value); button.textContent = profile.displayName || profile.username || "My profile"; button.setAttribute("aria-label", "My profile: " + button.textContent); }
        function refresh() { return request().then(update).catch(function () { button.textContent = "My profile"; }); }
        function close() { if (dialog) { dialog.close(); dialog.remove(); dialog = null; } button.focus(); }
        function open() {
            if (dialog) return;
            dialog = document.createElement("dialog"); dialog.className = "bf-account-dialog";
            dialog.setAttribute("aria-label", "My TELCO profile");
            dialog.innerHTML = '<form method="dialog" autocomplete="off" data-lpignore="true" data-1p-ignore="true"><div class="bf-account-heading"><h2>My profile</h2><button type="button" data-close aria-label="Close profile" title="Close">&#10006;&#65039;</button></div><p>Your TELCO credentials are shared across the projects you can access. Changing your password changes it for all of them.</p><p data-status role="status" aria-live="polite">Loading your profile...</p><fieldset disabled><dl><dt>Username</dt><dd data-identity></dd><dt>Email</dt><dd data-contact></dd></dl><p>Ask an administrator to change your username or email.</p><label>Display name<input data-field="displayName" maxlength="200" required autocomplete="off" data-lpignore="true"></label><details><summary>Change password</summary><label>Current password<input data-field="currentPassword" type="password" autocomplete="current-password" data-lpignore="true"></label><label>New password<input data-field="newPassword" type="password" minlength="8" autocomplete="new-password" data-lpignore="true"></label><label>Confirm new password<input data-field="confirmation" type="password" autocomplete="new-password" data-lpignore="true"></label><p>You will sign in again only if you change your password.</p></details><div class="bf-account-actions"><button type="button" data-cancel>Cancel</button><button type="submit" data-save>&#128190; Save changes</button></div></fieldset></form>';
            document.body.appendChild(dialog); var current = dialog, form = dialog.querySelector("form"), status = dialog.querySelector("[data-status]"), fields = {};
            var random = new Uint32Array(2); global.crypto.getRandomValues(random);
            Array.prototype.forEach.call(form.querySelectorAll("[data-field]"), function (input, index) {
                fields[input.dataset.field] = input; input.name = "bf-self-" + random[0].toString(36) + random[1].toString(36) + "-" + index;
            });
            dialog.querySelector("[data-close]").onclick = close; dialog.querySelector("[data-cancel]").onclick = close;
            dialog.addEventListener("cancel", function (event) { event.preventDefault(); close(); });
            dialog.showModal();
            request().then(function (data) {
                if (dialog !== current) return; update(data);
                dialog.querySelector("[data-identity]").textContent = profile.username;
                dialog.querySelector("[data-contact]").textContent = profile.email;
                fields.displayName.value = profile.displayName; status.textContent = "";
                form.querySelector("fieldset").disabled = false; fields.displayName.focus();
            }).catch(function (error) { status.textContent = error.message; });
            fields.newPassword.addEventListener("input", function () { fields.currentPassword.required = fields.confirmation.required = !!fields.newPassword.value; });
            form.addEventListener("submit", function (event) {
                event.preventDefault();
                if (fields.newPassword.value !== fields.confirmation.value) { status.textContent = "The new passwords do not match."; return; }
                if (!form.reportValidity()) return;
                var save = form.querySelector("[data-save]"); save.disabled = true; button.disabled = true; form.setAttribute("aria-busy", "true"); status.textContent = "Saving...";
                request({ displayName: fields.displayName.value, currentPassword: fields.currentPassword.value, newPassword: fields.newPassword.value }).then(function (data) {
                    if (data.passwordChanged) {
                        close();
                        if (account.options.onPasswordChanged) account.options.onPasswordChanged();
                        else global.location.assign(button.dataset.login);
                    } else { update(data.profile); fields.currentPassword.value = fields.newPassword.value = fields.confirmation.value = ""; fields.currentPassword.required = fields.confirmation.required = false; status.textContent = "Your profile has been saved."; }
                }).catch(function (error) { status.textContent = error.message; }).finally(function () { save.disabled = false; button.disabled = false; form.removeAttribute("aria-busy"); });
            });
        }
        button.addEventListener("click", open);
        var session = document.getElementById(button.dataset.sessionControl);
        function sync() { button.hidden = session ? session.hidden : false; if (!button.hidden) refresh(); else if (dialog) close(); }
        if (session) new MutationObserver(sync).observe(session, { attributes: true, attributeFilter: ["hidden"] });
        sync(); account.refresh = refresh; return account;
    }
    global.ByteForgeAccount = { attach: attach };
    function start() { document.querySelectorAll("[data-bf-account]").forEach(function (button) { attach(button); }); }
    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", start); else start();
}(window));
