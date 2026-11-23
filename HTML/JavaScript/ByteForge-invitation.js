(function () {
    "use strict";
    var root = document.getElementById("bf-invitation"), form = document.getElementById("bf-invitation-form"), status = document.getElementById("bf-invitation-status");
    var token = new URLSearchParams(window.location.hash.slice(1)).get("token"), api = root.getAttribute("data-api");
    // Fragments never reach server logs. Remove the token from browser history before making any requests.
    window.history.replaceState(null, "", window.location.pathname);
    function message(text) { status.textContent = text; }
    function request(path, body) {
        return fetch(api + path, { method: "POST", credentials: "omit", cache: "no-store", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) })
            .then(function (response) { return response.json().then(function (data) { if (!response.ok) throw new Error(data.message || data.Message || "The invitation could not be processed. Please try again."); return data; }); });
    }
    if (root.getAttribute("data-mode") === "request") {
        var resetForm = document.getElementById("bf-reset-request-form");
        document.title = "Forgot your password?";
        document.getElementById("bf-invitation-title").textContent = "Forgot your password?";
        document.getElementById("bf-invitation-help").textContent = "Enter your username or email to request a password reset link.";
        resetForm.hidden = false; message("");
        resetForm.addEventListener("submit", function (event) {
            event.preventDefault(); if (!resetForm.reportValidity()) return;
            var button = resetForm.querySelector("button"); button.disabled = true; resetForm.setAttribute("aria-busy", "true"); message("Requesting a reset link...");
            request("/request", { identifier: resetForm.elements.identifier.value }).then(function (data) { message(data.message); })
                .catch(function () { message("Unable to submit the request. Please try again."); })
                .finally(function () { button.disabled = false; resetForm.removeAttribute("aria-busy"); });
        });
        return;
    }
    if (!token) { message("This password setup link is missing or invalid."); return; }
    request("/validate", { token: token }).then(function (data) {
        if (!data.isValid) { message("This password setup link is invalid or has expired."); return; }
        message("Your password setup link is ready."); form.hidden = false;
    }).catch(function () { message("Unable to check the link. Please reopen the email link to try again."); });
    form.addEventListener("submit", function (event) {
        event.preventDefault();
        if (!form.reportValidity()) return;
        if (form.elements.password.value !== form.elements.confirmPassword.value) { message("The passwords do not match."); return; }
        var button = form.querySelector("button"); button.disabled = true; form.setAttribute("aria-busy", "true"); message("Setting your password...");
        request("/accept", { token: token, password: form.elements.password.value }).then(function () {
            token = null; form.reset(); form.hidden = true; message("Your password has been set. You can now sign in.");
        }).catch(function (error) { message(error.message); }).finally(function () { button.disabled = false; form.removeAttribute("aria-busy"); });
    });
}());
