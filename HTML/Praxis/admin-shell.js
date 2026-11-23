(function () {
    "use strict";
    var generation = 0;
    var pages = [];
    var sidebar = document.createElement("aside");
    sidebar.className = "bf-praxis-sidebar";
    sidebar.hidden = true;
    sidebar.innerHTML = '<div class="bf-praxis-brand"><span>Telecom Inc.</span><strong>Praxis</strong><span>Administration</span></div><nav aria-label="Praxis navigation"></nav>';
    document.body.insertBefore(sidebar, document.body.firstChild);
    var nav = sidebar.querySelector("nav");

    function current() {
        var file = window.location.pathname.split("/").pop();
        return file === "confirmations.html" ? "PraxisConfirmations" : file === "appointment-review.html" ? "PraxisAppointmentReview" : file === "manual-sale.html" ? "PraxisManualSale" : file === "advisors.html" ? "PraxisAdvisorEditor" : file === "scripts.html" ? "PraxisScriptEditor" : window.location.hash === "#security" ? "PraxisRbacAdmin" : "PraxisReports";
    }
    function select(key) {
        Array.prototype.forEach.call(nav.querySelectorAll("a"), function (link) {
            if (link.dataset.page === key) link.setAttribute("aria-current", "page");
            else link.removeAttribute("aria-current");
        });
        var page = pages.filter(function (item) { return item.key === key; })[0];
        var title = document.querySelector(".report-brand-detail");
        if (title) title.textContent = page ? "Praxis / " + page.label : "Praxis Portal";
    }
    function clear() {
        generation++;
        pages = [];
        nav.textContent = "";
        sidebar.hidden = true;
        document.body.classList.remove("bf-praxis-admin");
    }
    function load(token) {
        clear();
        var request = generation;
        return fetch("api/auth/pages", { headers: { Authorization: "Bearer " + token, Accept: "application/json" }, cache: "no-store" })
            .then(function (response) {
                if (!response.ok) throw new Error("Unable to load your Praxis page access. Sign in again.");
                return response.json();
            }).then(function (data) {
                if (request !== generation) throw new Error("Your Praxis session changed. Sign in again.");
                pages = data.pages || [];
                if (!pages.length) throw new Error("Your Praxis roles do not grant access to any portal pages.");
                pages.forEach(function (page) {
                    var link = document.createElement("a");
                    link.href = page.route;
                    link.textContent = page.label;
                    link.dataset.page = page.key;
                    nav.appendChild(link);
                });
                sidebar.hidden = false;
                document.body.classList.add("bf-praxis-admin");
                select(current());
                return pages;
            });
    }
    window.addEventListener("hashchange", function () { select(current()); });
    window.ByteForgePraxisAdmin = { load: load, clear: clear, select: select };
}());
