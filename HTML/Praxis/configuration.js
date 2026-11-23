(function () {
    "use strict";
    var token = "";
    try { token = sessionStorage.getItem("praxis.reportPortal.accessToken") || localStorage.getItem("praxis.reportPortal.persistentAccessToken") || ""; } catch (ignored) { }
    function login() { location.replace("login.html?returnUrl=configuration.html"); }
    if (!token) { login(); return; }
    fetch("api/config-session", {
        method: "POST", credentials: "same-origin", cache: "no-store",
        headers: { Authorization: "Bearer " + token, Accept: "application/json" }
    }).then(function (response) {
        if (response.status === 401) { login(); return; }
        if (!response.ok) throw new Error("Your Praxis role does not grant configuration access, or the service is unavailable.");
        location.replace("Admin/Configuration");
    }).catch(function (error) { document.getElementById("config-status").textContent = error.message; });
}());
