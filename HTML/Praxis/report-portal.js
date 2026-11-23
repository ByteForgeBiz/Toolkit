(function () {
    "use strict";

    var sessionTokenKey = "praxis.reportPortal.accessToken";
    var persistentTokenKey = "praxis.reportPortal.persistentAccessToken";
    var state = {
        token: "",
        configuration: null,
        history: null,
        metrics: null,
        calls: null,
        appointments: null,
        progress: null,
        agentRows: [],
        agentPage: 1,
        agentPageSize: 10,
        agentSort: "dials",
        agentDirection: "desc",
        callPage: 1,
        callSort: "started",
        callDirection: "desc",
        appointmentPage: 1,
        appointmentSort: "appointmentDate",
        appointmentDirection: "asc",
        canEditAppointments: false,
        canOpenReportScript: false,
        appointmentEditorRequest: 0,
        appointmentEditorModel: null,
        appointmentEditorReturnFocus: null,
        historyMode: "per-hour",
        agentHistoryRows: [],
        agentHistoryRequest: 0,
        agentModalReturnFocus: null,
        historySourceReturnFocus: null,
        rbac: null,
        rbacAccess: null,
        loadingCounts: {}
    };

    var filterForm = document.getElementById("report-filter-form");
    var reportContent = document.getElementById("report-content");
    var message = document.getElementById("report-message");
    var serviceMessage = document.getElementById("report-service-message");
    var agentHistoryModal = document.getElementById("agent-history-modal");
    var appointmentEditorModal = document.getElementById("appointment-editor-modal");
    var historySourceModal = document.getElementById("history-source-modal");

    // The visible account control requests its profile immediately when attached.
    state.token = readSessionToken();
    if (window.ByteForgeAccount) window.ByteForgeAccount.attach(document.getElementById("session-status"), { getToken: function () { return state.token; }, onPasswordChanged: signOut });
    filterForm.addEventListener("submit", function (event) {
        event.preventDefault();
        if (!validateRange()) return;
        state.agentPage = 1;
        state.callPage = 1;
        state.appointmentPage = 1;
        refreshReport(true);
    });
    document.getElementById("date-range").addEventListener("change", function (event) {
        applyDateRange(event.target.value);
    });
    document.getElementById("from-date").addEventListener("change", selectCustomDateRange);
    document.getElementById("to-date").addEventListener("change", selectCustomDateRange);
    document.getElementById("sign-out-button").addEventListener("click", signOut);
    window.addEventListener("hashchange", function () {
        if (!state.token) return;
            if (window.location.hash === "#security" && state.rbacAccess) openSecurity();
        else openReports();
    });
    document.getElementById("agent-search").addEventListener("input", debounce(function () { state.agentPage = 1; renderAgents(); }, 250));
    document.getElementById("agent-previous").addEventListener("click", function () { state.agentPage -= 1; renderAgents(); });
    document.getElementById("agent-next").addEventListener("click", function () { state.agentPage += 1; renderAgents(); });
    document.getElementById("appointment-previous").addEventListener("click", function () { state.appointmentPage -= 1; loadAppointments(); });
    document.getElementById("appointment-next").addEventListener("click", function () { state.appointmentPage += 1; loadAppointments(); });
    document.getElementById("appointment-search").addEventListener("input", debounce(refreshAppointmentSearch, 250));
    document.getElementById("appointment-advisor").addEventListener("input", debounce(refreshAppointmentSearch, 250));
    document.getElementById("call-previous").addEventListener("click", function () { state.callPage -= 1; loadCalls(); });
    document.getElementById("call-next").addEventListener("click", function () { state.callPage += 1; loadCalls(); });
    document.getElementById("call-search").addEventListener("input", debounce(refreshCallSearch, 250));
    document.getElementById("call-agent").addEventListener("input", debounce(refreshCallSearch, 250));
    document.getElementById("call-disposition").addEventListener("change", refreshCallSearch);
    document.getElementById("call-contact").addEventListener("change", refreshCallSearch);
    document.getElementById("agent-history-close").addEventListener("click", closeAgentHistory);
    document.getElementById("appointment-editor-close").addEventListener("click", closeAppointmentEditor);
    document.getElementById("history-source-close").addEventListener("click", closeHistorySource);
    Array.prototype.forEach.call(document.querySelectorAll("[data-history-mode]"), function (button) {
        button.addEventListener("click", function () {
            state.historyMode = button.getAttribute("data-history-mode");
            updateHistoryModeButtons();
            renderHistory();
            renderAgentHistory();
        });
    });
    agentHistoryModal.addEventListener("click", function (event) { if (event.target === agentHistoryModal) closeAgentHistory(); });
    appointmentEditorModal.addEventListener("click", function (event) { if (event.target === appointmentEditorModal) closeAppointmentEditor(); });
    historySourceModal.addEventListener("click", function (event) { if (event.target === historySourceModal) closeHistorySource(); });
    document.addEventListener("keydown", handleAgentHistoryKeydown);
    document.addEventListener("keydown", handleAppointmentEditorKeydown);
    document.addEventListener("keydown", handleHistorySourceKeydown);
    Array.prototype.forEach.call(document.querySelectorAll("[data-agent-sort]"), function (button) {
        button.addEventListener("click", function () {
            var nextSort = button.getAttribute("data-agent-sort");
            state.agentDirection = state.agentSort === nextSort && state.agentDirection === "desc" ? "asc" : "desc";
            state.agentSort = nextSort;
            state.agentPage = 1;
            updateSortIndicators();
            renderAgents();
        });
    });
    Array.prototype.forEach.call(document.querySelectorAll("[data-appointment-sort]"), function (button) {
        button.addEventListener("click", function () {
            var nextSort = button.getAttribute("data-appointment-sort");
            state.appointmentDirection = state.appointmentSort === nextSort && state.appointmentDirection === "asc" ? "desc" : "asc";
            state.appointmentSort = nextSort;
            state.appointmentPage = 1;
            updateSortIndicators();
            loadAppointments();
        });
    });
    Array.prototype.forEach.call(document.querySelectorAll("[data-call-sort]"), function (button) {
        button.addEventListener("click", function () {
            var nextSort = button.getAttribute("data-call-sort");
            state.callDirection = state.callSort === nextSort && state.callDirection === "asc" ? "desc" : "asc";
            state.callSort = nextSort;
            state.callPage = 1;
            updateSortIndicators();
            loadCalls();
        });
    });
    Array.prototype.forEach.call(document.querySelectorAll("[data-kpi-dispositions]"), function (button) {
        button.addEventListener("click", function () { openKpiDispositions(button.getAttribute("data-kpi-dispositions"), button); });
    });
    updateSortIndicators();
    updateHistoryModeButtons();
    restoreSession();

    function refreshAppointmentSearch() {
        state.appointmentPage = 1;
        loadAppointments();
    }

    function refreshCallSearch() {
        state.callPage = 1;
        loadCalls();
    }

    function updateHistoryModeButtons() {
        Array.prototype.forEach.call(document.querySelectorAll("[data-history-mode]"), function (button) {
            button.setAttribute("aria-pressed", button.getAttribute("data-history-mode") === state.historyMode ? "true" : "false");
        });
    }

    function updateSortIndicators() {
        ByteForgeSortIndicator.update("[data-agent-sort]", "data-agent-sort", { key: state.agentSort, direction: state.agentDirection });
        ByteForgeSortIndicator.update("[data-appointment-sort]", "data-appointment-sort", { key: state.appointmentSort, direction: state.appointmentDirection });
        ByteForgeSortIndicator.update("[data-call-sort]", "data-call-sort", { key: state.callSort, direction: state.callDirection });
    }

    function restoreSession() {
        var savedToken = readSessionToken();
        if (!savedToken) {
            ByteForgePraxisAuth.redirect();
            return;
        }

        state.token = savedToken;
        document.getElementById("sign-out-button").hidden = false;
        document.getElementById("session-status").textContent = "Restoring secure session";
        var configurationFailure = null;
        serviceMessage.hidden = true;
        Promise.all([
            apiGet("api/configuration").catch(function (failure) { configurationFailure = failure; return null; }),
            apiGet("api/rbac/me").catch(function (failure) {
                // A normal non-admin cannot access this endpoint; service failures must remain visible.
                if (failure.httpStatus !== 403) {
                    serviceMessage.textContent = "User administration could not be loaded. " + failure.message;
                    serviceMessage.className = "report-message is-error";
                    serviceMessage.hidden = false;
                }
                return null;
            }),
            ByteForgePraxisAdmin.load(state.token)
        ]).then(function (access) {
            state.configuration = access[0];
            openPortal(access[1], access[2], configurationFailure);
        }).catch(function (failure) {
            reportContent.hidden = true;
            document.getElementById("rbac-content").hidden = true;
            document.getElementById("sign-out-button").hidden = false;
            document.getElementById("session-status").textContent = "Report unavailable";
            showMessage("The Praxis report could not be loaded. " + failure.message + " Refresh the page to retry, or use Sign out to sign in again.", true);
        });
    }

    function readSessionToken() {
        try {
            return window.sessionStorage.getItem(sessionTokenKey)
                || window.localStorage.getItem(persistentTokenKey)
                || "";
        } catch (ignored) { return ""; }
    }

    function clearSessionToken() {
        state.token = "";
        ByteForgePraxisAdmin.clear();
        clearStoredTokens();
    }

    function clearStoredTokens() {
        fetch("api/config-session", { method: "DELETE", credentials: "same-origin", keepalive: true }).catch(function () { });
        try { window.sessionStorage.removeItem(sessionTokenKey); } catch (ignored) { }
        try { window.localStorage.removeItem(persistentTokenKey); } catch (ignored) { }
    }

    function openPortal(rbacAccess, pages, configurationFailure) {
        state.rbacAccess = rbacAccess;
        var requestedKey = window.location.hash === "#security" ? "PraxisRbacAdmin" : "PraxisReports";
        var page = pages.filter(function (item) { return item.key === requestedKey; })[0] || pages[0];
        if (page.key !== "PraxisReports" && page.key !== "PraxisRbacAdmin") {
            window.location.replace(page.route);
            return;
        }
        document.getElementById("sign-out-button").hidden = false;
        var status = document.getElementById("session-status");
        status.textContent = "Secure session";
        status.classList.add("is-ready");
        if (state.configuration) {
            populateScope();
            setDefaultDates();
            Promise.all([loadAppointmentEditorAccess(), loadReportScriptAccess()]).then(loadDispositionOptions).then(refreshReport).catch(function (failure) {
                showMessage(failure.message, true);
            });
        }
        if (page.key === "PraxisRbacAdmin" && rbacAccess) openSecurity();
        else if (state.configuration) openReports();
        else throw configurationFailure || new Error("The Praxis page could not be loaded. Please try again.");
    }

    function openSecurity() {
        if (!state.rbacAccess) return;
        reportContent.hidden = true;
        document.getElementById("rbac-content").hidden = false;
        ByteForgePraxisAdmin.select("PraxisRbacAdmin");
        window.history.replaceState(null, "", "#security");
        if (!state.rbac) {
            state.rbac = new ByteForgeRbacAdmin({
                root: document.getElementById("rbac-admin"),
                apiBase: "api/rbac",
                inviteWithoutPassword: true,
                getToken: function () { return state.token; }
            });
        }
        state.rbac.load();
    }

    function openReports() {
        if (!state.configuration) return;
        document.getElementById("rbac-content").hidden = true;
        reportContent.hidden = false;
        ByteForgePraxisAdmin.select("PraxisReports");
        window.history.replaceState(null, "", "#reports");
    }

    function signOut(event) {
        if (event && event.preventDefault) event.preventDefault();
        clearSessionToken();
        state.configuration = null;
        state.rbacAccess = null;
        state.canEditAppointments = false;
        state.canOpenReportScript = false;
        state.appointmentEditorModel = null;
        closeAppointmentEditor();
        reportContent.hidden = true;
        document.getElementById("rbac-content").hidden = true;
        ByteForgePraxisAuth.redirect();
        document.getElementById("sign-out-button").hidden = true;
        var status = document.getElementById("session-status");
        status.textContent = "Signed out";
        status.classList.remove("is-ready");
        hideMessage();
    }

    function populateScope() {
        replaceOptions("service-filter", state.configuration.services || []);
        replaceOptions("team-filter", state.configuration.teams || []);
        document.getElementById("report-context").textContent =
            "Calendar dates in " + state.configuration.reportTimeZone +
            ", using the configured Praxis services and teams. Today reflects activity processed so far.";
    }

    function replaceOptions(id, values) {
        var select = document.getElementById(id);
        while (select.options.length > 1) select.remove(1);
        values.forEach(function (value) {
            var option = document.createElement("option");
            option.value = value;
            option.textContent = value;
            select.appendChild(option);
        });
    }

    function loadDispositionOptions() {
        return apiGet("api/dispositions").then(function (catalog) {
            replaceOptions("call-disposition", catalog.callDispositions || []);
        });
    }

    function setDefaultDates() {
        document.getElementById("date-range").value = "last-7-days";
        applyDateRange("last-7-days");
    }

    function loadAppointmentEditorAccess() {
        return apiGet("api/appointments/editor-access").then(function () {
            state.canEditAppointments = true;
        }).catch(function () {
            state.canEditAppointments = false;
        }).then(function () {
            document.getElementById("appointment-edit-heading").hidden = !state.canEditAppointments;
        });
    }

    function loadReportScriptAccess() {
        state.canOpenReportScript = false;
        return apiGet("api/appointments/script-access").then(function (access) {
            if (access.canOpenScript !== true) return;
            // A new noopener tab does not inherit the report's sessionStorage token.
            return fetch("api/config-session/workspace", {
                method: "POST", credentials: "same-origin",
                headers: { Authorization: "Bearer " + state.token }
            }).then(function (response) {
                if (!response.ok) throw new Error("Could not establish the workspace user session.");
                state.canOpenReportScript = true;
            });
        }).catch(function () {
            state.canOpenReportScript = false;
        });
    }

    function applyDateRange(range) {
        var today = dateInZone(new Date(), state.configuration.reportIanaTimeZone);
        var through = today;
        var from;

        if (range === "custom") return;
        if (range === "month-to-date") {
            from = today.slice(0, 7) + "-01";
        } else if (range === "last-month") {
            through = addDays(today.slice(0, 7) + "-01", -1);
            from = through.slice(0, 7) + "-01";
        } else if (range === "last-7-days") {
            from = addDays(through, -6);
        } else if (range === "last-30-days") {
            from = addDays(through, -29);
        } else if (range === "last-60-days") {
            from = addDays(through, -59);
        } else if (range === "last-90-days") {
            from = addDays(through, -89);
        } else {
            document.getElementById("date-range").value = "custom";
            return;
        }

        document.getElementById("to-date").value = through;
        document.getElementById("from-date").value = from;
    }

    function selectCustomDateRange() {
        document.getElementById("date-range").value = "custom";
    }

    function validateRange() {
        var from = document.getElementById("from-date").value;
        var to = document.getElementById("to-date").value;
        if (!from || !to || from > to) {
            showMessage("Choose a valid From and Through date range.", true);
            return false;
        }
        return true;
    }

    function refreshReport(forceRefresh) {
        if (!validateRange()) return;
        setBusy(filterForm, true);
        showMessage("Refreshing Praxis report data...", false);
        var tasks = [loadHistory(), loadMetrics(), loadProgress(), loadCalls(), loadAppointments(forceRefresh === true)];
        Promise.allSettled(tasks).then(function (results) {
            var failures = results.filter(function (result) { return result.status === "rejected"; });
            if (failures.length) {
                showMessage("Some report sections could not be refreshed. " + failures.map(function (item) { return item.reason.message; }).join(" "), true);
            } else {
                hideMessage();
            }
            setBusy(filterForm, false);
        });
    }

    function loadMetrics() {
        var bounds = getBounds();
        var requestId = state.metricsRequest = (state.metricsRequest || 0) + 1;
        state.metricsLoading = true;
        state.metricsError = "";
        state.metrics = null;
        renderMetricCompleteness({});
        buildAgentRows();
        renderAgents();
        beginCardLoading("metrics");
        var request = apiGet("api/metrics/agents?" + queryString(metricParameters(bounds))).then(function (data) {
            if (requestId !== state.metricsRequest) return;
            state.metricsLoading = false;
            state.metrics = data;
            renderMetricCompleteness(data);
            buildAgentRows();
            renderAgents();
        }).catch(function (failure) {
            if (requestId !== state.metricsRequest) return;
            state.metricsLoading = false;
            state.metricsError = "Agent metrics could not be loaded. Refresh to retry. If this continues, check the DataImport results.";
            renderAgents();
            throw failure;
        });
        return completeCardLoading("metrics", request);
    }

    function renderMetricCompleteness(data) {
        var notice = document.getElementById("agent-metrics-completeness");
        if (!notice) return;
        var missing = data.missingTeamEvidence || [];
        notice.hidden = !data.isPartial && !missing.length;
        notice.textContent = notice.hidden ? "" : "Agent metrics are incomplete. Activity awaiting team verification is excluded from these totals: " +
            missing.map(function (item) {
                return (item.agent || "Unknown agent") + " on " + item.date + " (" + formatNumber(item.excludedCallCount) + " calls)";
            }).join("; ") + ". Refresh after the source data updates.";
    }

    function loadHistory() {
        var bounds = getBounds();
        var requestId = state.historyRequest = (state.historyRequest || 0) + 1;
        state.history = null;
        renderHistorySummary();
        renderHistory();
        beginCardLoading("history");
        var request = apiGet("api/metrics/history?" + queryString(metricParameters(bounds))).then(function (data) {
            if (requestId !== state.historyRequest) return;
            state.history = data;
            renderHistorySummary();
            renderHistory();
        });
        return completeCardLoading("history", request);
    }

    function metricParameters(bounds, agent) {
        return { from: bounds.fromIso, to: bounds.toIso,
            service: document.getElementById("service-filter").value,
            team: document.getElementById("team-filter").value,
            agent: agent || undefined };
    }

    function loadProgress() {
        var requestId = state.progressRequestId = (state.progressRequestId || 0) + 1;
        state.progress = null;
        document.getElementById("list-progress").textContent = "";
        beginCardLoading("progress");
        var request = apiGet("api/lists/progress").then(function (data) {
            if (requestId !== state.progressRequestId) return;
            state.progress = data;
            renderProgress();
        }).catch(function (failure) {
            if (requestId !== state.progressRequestId) return;
            document.getElementById("list-progress").textContent = "Calling list breakdown is unavailable. Refresh to retry.";
            throw failure;
        });
        return completeCardLoading("progress", request);
    }

    function loadCalls() {
        var bounds = getBounds();
        var parameters = {
            from: bounds.fromIso,
            to: bounds.toIso,
            service: document.getElementById("service-filter").value,
            team: document.getElementById("team-filter").value,
            agent: document.getElementById("call-agent").value.trim(),
            search: document.getElementById("call-search").value.trim(),
            disposition: document.getElementById("call-disposition").value,
            humanContact: document.getElementById("call-contact").value,
            page: state.callPage,
            pageSize: 25,
            sort: state.callSort,
            direction: state.callDirection
        };
        beginCardLoading("calls");
        var request = apiGet("api/calls?" + queryString(parameters)).then(function (data) {
            state.calls = data;
            renderCalls();

        }).catch(function (failure) {
            setTableError("call-table-body", 7, failure.message);
            throw failure;
        });
        return completeCardLoading("calls", request);
    }

    function loadAppointments(forceRefresh) {
        var bounds = getBounds();
        var requestId = state.appointmentRequestId = (state.appointmentRequestId || 0) + 1;
        var parameters = {
            from: bounds.fromDate + "T00:00:00",
            to: bounds.toExclusiveDate + "T00:00:00",
            advisorId: document.getElementById("appointment-advisor").value.trim(),
            search: document.getElementById("appointment-search").value.trim(),
            service: document.getElementById("service-filter").value,
            team: document.getElementById("team-filter").value,
            includeQa: true,
            page: state.appointmentPage,
            pageSize: 25,
            sort: state.appointmentSort,
            direction: state.appointmentDirection
        };
        if (forceRefresh === true) parameters.refresh = true;
        state.appointments = null;
        state.appointmentError = false;
        renderAppointmentSummary();
        beginCardLoading("appointments");
        var request = apiGet("api/appointments?" + queryString(parameters)).then(function (data) {
            if (requestId !== state.appointmentRequestId) return;
            state.appointments = data;
            renderAppointments();
            renderAppointmentSummary();
        }).catch(function (failure) {
            if (requestId !== state.appointmentRequestId) throw failure;
            state.appointmentError = true;
            renderAppointmentSummary();
            setTableError("appointment-table-body", state.canEditAppointments ? 9 : 8, failure.message);
            throw failure;
        });
        return completeCardLoading("appointments", request);
    }

    function getBounds() {
        var fromDate = document.getElementById("from-date").value;
        var throughDate = document.getElementById("to-date").value;
        var toExclusiveDate = addDays(throughDate, 1);
        return {
            fromDate: fromDate,
            toExclusiveDate: toExclusiveDate,
            fromIso: zonedMidnightIso(fromDate, state.configuration.reportIanaTimeZone),
            toIso: zonedMidnightIso(toExclusiveDate, state.configuration.reportIanaTimeZone)
        };
    }

    function renderHistorySummary() {
        if (!state.history) {
            document.getElementById("calls-total").textContent = "\u2014";
            document.getElementById("contacts-total").textContent = "\u2014";
            document.getElementById("contact-rate").textContent = "Awaiting selected campaign";
            document.getElementById("dedicated-hours").textContent = "\u2014";
            renderAppointmentSummary();
            return;
        }
        var totals = (state.history.items || []).reduce(function (sum, item) {
            sum.dials += item.dials || 0;
            sum.contacts += item.humanContacts || 0;
            sum.appointments += item.appointments || 0;
            sum.aodSeconds += item.aodSeconds || 0;
            return sum;
        }, { dials: 0, contacts: 0, appointments: 0, aodSeconds: 0 });
        document.getElementById("calls-total").textContent = formatNumber(totals.dials);
        document.getElementById("contacts-total").textContent = formatNumber(totals.contacts);
        document.getElementById("contact-rate").textContent = totals.dials ? formatPercent(totals.contacts / totals.dials) + " of dials" : "No dials in range";
        renderAppointmentSummary();
        document.getElementById("dedicated-hours").textContent = formatNumber(totals.aodSeconds / 3600, 1);
        document.getElementById("dedicated-scope").textContent = "Selected teams: login minus lunch";
    }

    function renderAppointmentSummary() {
        var totals = state.history ? summarizeHistoryRows(state.history.items || []) : null;
        document.getElementById("appointments-total").textContent = totals ? formatNumber(totals.appointments) : "\u2014";
        document.getElementById("appointment-rate").textContent = !totals ? "Awaiting selected campaign" :
            totals.contacts ? formatPercent(totals.appointments / totals.contacts) + " of human contacts" : "No human contacts in range";
    }
    function renderHistory() {
        var grid = document.getElementById("history-grid");
        grid.textContent = "";
        if (!state.history) {
            document.getElementById("history-period").textContent = "Loading selected campaign...";
            return;
        }
        var dailyRows = (state.history.items || []).slice().sort(function (left, right) {
            return left.date.localeCompare(right.date);
        });
        if (!dailyRows.length) {
            grid.appendChild(emptyMessage("No metric history is available in this range and campaign."));
            document.getElementById("history-period").textContent = "No history in the selected period";
            return;
        }

        document.getElementById("history-period").textContent =
            formatReportDate(dailyRows[0].date) + " through " + formatReportDate(dailyRows[dailyRows.length - 1].date) +
            " · Daily trends";

        historySeries(state.historyMode).forEach(function (series) {
            grid.appendChild(renderHistoryCard(series, dailyRows, "team"));
        });
    }

    function historySeries(mode) {
        var perHour = mode === "per-hour";
        return [
            historyCountSeries("dials", "Dials", "calls", "var(--report-primary)", "dials", perHour, 1),
            historyCountSeries("contacts", "Contacts", "contacts", "var(--report-success)", "humanContacts", perHour, 1),
            historyCountSeries("appointments", "Appointments", "appointments", "var(--report-accent)", "appointments", perHour, 2),
            {
                id: "conversion",
                title: "Conversion",
                unit: "%",
                color: "var(--report-purple)",
                aggregate: "conversion",
                decimals: 1,
                value: function (row) { return row.humanContacts ? row.appointments / row.humanContacts * 100 : null; }
            }
        ];
    }

    function historyCountSeries(id, title, unit, color, field, perHour, decimals) {
        return {
            id: id,
            title: title + (perHour ? " / hr" : ""),
            unit: perHour ? unit + " / hr" : unit,
            color: color,
            field: field,
            aggregate: perHour ? "perHour" : "sum",
            decimals: perHour ? decimals : 0,
            value: function (row) {
                return perHour ? (row.aodSeconds ? row[field] / (row.aodSeconds / 3600) : null) : row[field];
            }
        };
    }

    function renderHistoryCard(series, rows, tooltipScope) {
        var values = rows.map(series.value);
        var totals = summarizeHistoryRows(rows);
        var displayValue = values.reduce(function (sum, value) { return sum + (value || 0); }, 0);
        if (series.aggregate === "perHour") {
            displayValue = totals.aodSeconds ? totals[series.field === "humanContacts" ? "contacts" : series.field] / (totals.aodSeconds / 3600) : null;
        } else if (series.aggregate === "conversion") {
            displayValue = totals.contacts ? totals.appointments / totals.contacts * 100 : null;
        }
        var context = "Total in selected period";
        if (series.aggregate === "perHour") context = totals.aodSeconds ? "Per Praxis AOD hour" : "No AOD hours in range";
        if (series.aggregate === "conversion") context = totals.contacts ? "Appointments / contacts" : "No contacts in range";

        var card = document.createElement("article");
        card.className = "report-history-card";
        var heading = document.createElement("div");
        heading.className = "report-history-card-head";
        var summary = document.createElement("div");
        appendTextElement(summary, "h3", series.title);
        var value = appendTextElement(summary, "div", displayValue === null ? "—" : formatNumber(displayValue, series.decimals));
        value.className = "report-history-value";
        appendTextElement(value, "span", " " + series.unit);
        appendTextElement(summary, "p", context).className = "report-history-context";
        heading.appendChild(summary);
        var actions = document.createElement("div");
        actions.className = "report-history-card-actions";
        var sourceButton = document.createElement("button");
        sourceButton.type = "button";
        sourceButton.className = "report-history-source-button";
        sourceButton.textContent = "Source";
        sourceButton.setAttribute("aria-label", "View source values for " + getHistoryMetricTitle(series.id));
        sourceButton.addEventListener("click", function () { openHistorySource(series, rows, tooltipScope, sourceButton); });
        actions.appendChild(sourceButton);
        actions.appendChild(renderHistoryDelta(series, values, rows, tooltipScope));
        heading.appendChild(actions);
        card.appendChild(heading);
        card.appendChild(renderHistoryChart(series, rows, values, tooltipScope));
        return card;
    }

    function summarizeHistoryRows(rows) {
        return rows.reduce(function (sum, row) {
            sum.dials += row.dials || 0;
            sum.contacts += row.humanContacts || 0;
            sum.appointments += row.appointments || 0;
            sum.aodSeconds += row.aodSeconds || 0;
            sum.callDuration += row.callDurationSeconds || 0;
            sum.acw += row.afterCallWorkSeconds || 0;
            return sum;
        }, { dials: 0, contacts: 0, appointments: 0, aodSeconds: 0, callDuration: 0, acw: 0 });
    }

    function getHistoryMetricTitle(id) {
        if (id === "contacts") return "Contacts";
        if (id === "appointments") return "Appointments";
        if (id === "conversion") return "Conversion";
        return "Dials";
    }

    function openHistorySource(series, rows, tooltipScope, trigger) {
        var details = buildHistorySourceDetails(series.id, rows);
        state.historySourceReturnFocus = trigger;
        document.body.classList.add("has-report-modal");
        historySourceModal.hidden = false;
        document.getElementById("history-source-title").textContent = details.title + " source";
        document.getElementById("history-source-context").textContent = getHistorySourceContext(tooltipScope, rows);
        document.getElementById("history-source-total").textContent = details.totalKpi;
        document.getElementById("history-source-per-hour").textContent = details.perHourKpi;
        document.getElementById("history-source-aod").textContent = details.aodKpi;
        document.getElementById("history-source-formula").textContent = details.formula;
        var body = document.getElementById("history-source-values");
        body.textContent = "";
        details.points.forEach(function (point) {
            var row = document.createElement("tr");
            appendCell(row, point.date);
            appendCell(row, point.total);
            appendCell(row, point.perHour);
            body.appendChild(row);
        });
        document.getElementById("history-source-close").focus();
    }

    function getHistorySourceContext(tooltipScope, rows) {
        var period = formatReportDate(rows[0].date) + " through " + formatReportDate(rows[rows.length - 1].date);
        if (tooltipScope !== "agent") return "Campaign totals · " + period;
        var agentName = document.getElementById("agent-history-title").textContent;
        return agentName + " · " + period;
    }

    function buildHistorySourceDetails(id, rows) {
        var totals = summarizeHistoryRows(rows);
        var hours = totals.aodSeconds / 3600;
        var title = getHistoryMetricTitle(id);
        if (id === "conversion") {
            var conversion = totals.contacts ? totals.appointments / totals.contacts * 100 : null;
            return {
                title: title,
                totalKpi: conversion === null ? "—" : formatNumber(conversion, 1) + "%",
                perHourKpi: conversion === null ? "—" : formatNumber(conversion, 1) + "%",
                aodKpi: formatNumber(hours, 2) + " hours",
                points: rows.map(function (row) { return buildHistorySourcePoint(id, row); }),
                formula: "Conversion is appointments ÷ human contacts. The same daily conversion point appears in both graph modes because dividing both inputs by the same AOD hours does not change their ratio."
            };
        }

        var field = id === "contacts" ? "contacts" : id;
        var unit = id === "dials" ? "calls" : id;
        var decimals = id === "appointments" ? 2 : 1;
        var perHour = hours ? totals[field] / hours : null;
        return {
            title: title,
            totalKpi: formatNumber(totals[field]) + " " + unit,
            perHourKpi: formatSourceRate(perHour, unit, decimals),
            aodKpi: formatNumber(hours, 2) + " hours",
            points: rows.map(function (row) { return buildHistorySourcePoint(id, row); }),
            formula: "Per-hour KPI: " + formatNumber(totals[field]) + " " + unit + " ÷ " + formatNumber(hours, 2) + " Praxis AOD hours" + (perHour === null ? ". No per-hour value is available because the selected range has no AOD hours." : " = " + formatSourceRate(perHour, unit, decimals) + ".")
        };
    }

    function buildHistorySourcePoint(id, row) {
        var hours = (row.aodSeconds || 0) / 3600;
        if (id === "conversion") {
            var totalConversion = row.humanContacts ? row.appointments / row.humanContacts * 100 : null;
            return {
                date: formatReportDate(row.date),
                total: totalConversion === null ? "—" : formatNumber(totalConversion, 1) + "%",
                perHour: totalConversion === null ? "—" : formatNumber(totalConversion, 1) + "%"
            };
        }

        var field = id === "contacts" ? "humanContacts" : id;
        var unit = id === "dials" ? "calls" : id;
        var decimals = id === "appointments" ? 2 : 1;
        return {
            date: formatReportDate(row.date),
            total: formatNumber(row[field]) + " " + unit,
            perHour: formatSourceRate(hours ? row[field] / hours : null, unit, decimals)
        };
    }

    function formatSourceRate(value, unit, decimals) {
        return value === null ? "—" : formatNumber(value, decimals) + " " + unit + " / hr";
    }

    function closeHistorySource() {
        if (!historySourceModal || historySourceModal.hidden) return;
        historySourceModal.hidden = true;
        if (agentHistoryModal.hidden && appointmentEditorModal.hidden) document.body.classList.remove("has-report-modal");
        if (state.historySourceReturnFocus && document.body.contains(state.historySourceReturnFocus)) state.historySourceReturnFocus.focus();
        state.historySourceReturnFocus = null;
    }

    function handleHistorySourceKeydown(event) {
        if (!historySourceModal || historySourceModal.hidden) return;
        if (event.key === "Escape") {
            event.preventDefault();
            closeHistorySource();
            return;
        }
        if (event.key !== "Tab") return;
        var focusable = historySourceModal.querySelectorAll("button:not([disabled]), [tabindex='0']");
        if (!focusable.length) return;
        var first = focusable[0], last = focusable[focusable.length - 1];
        if (event.shiftKey && document.activeElement === first) {
            event.preventDefault();
            last.focus();
        } else if (!event.shiftKey && document.activeElement === last) {
            event.preventDefault();
            first.focus();
        }
    }

    function renderHistoryDelta(series, values, rows, tooltipScope) {
        var wrapper = document.createElement("div");
        wrapper.className = "bf-history-comparison";
        var delta = document.createElement("span");
        delta.className = "report-history-delta is-neutral";
        delta.tabIndex = 0;
        var dialingDays = state.configuration.reportDialingDays;
        if (!Array.isArray(dialingDays) || !dialingDays.length || dialingDays.some(function (day) {
            return !Number.isInteger(day) || day < 0 || day > 6;
        })) throw new Error("Report dialing days are missing or invalid in the API configuration.");
        var points = values.map(function (value, index) { return { value: value, date: rows[index].date }; })
            .filter(function (point) {
                // The API date is already a report-zone calendar date; avoid browser-zone shifts.
                var weekday = new Date(point.date.slice(0, 10) + "T00:00:00Z").getUTCDay();
                return point.value !== null && dialingDays.indexOf(weekday) >= 0;
            });
        var first = points[0], last = points[points.length - 1];
        var explanation;
        if (points.length < 2 || !first.value) {
            delta.textContent = "No comparison";
            explanation = points.length < 2 ? "At least two chart points on configured dialing days with values are needed for a comparison." :
                "A percentage change cannot be calculated because the first chart value is zero.";
        } else {
            var percent = (last.value - first.value) / first.value;
            var favorable = series.betterDown ? percent <= 0 : percent >= 0;
            delta.className = "report-history-delta " + (favorable ? "is-positive" : "is-negative");
            delta.textContent = (percent >= 0 ? "+" : "") + formatPercent(percent);
            explanation = percent === 0 ? "There was no change" :
                "There was a " + formatPercent(Math.abs(percent)) + (percent > 0 ? " increase" : " decrease");
            explanation += " between " + formatReportDate(first.date) + " and " + formatReportDate(last.date) + ".";
        }
        var tooltip = document.createElement("div");
        tooltip.id = "comparison-tooltip-" + tooltipScope + "-" + series.id;
        tooltip.className = "report-history-tooltip bf-history-comparison-tooltip";
        tooltip.setAttribute("role", "tooltip");
        tooltip.hidden = true;
        delta.setAttribute("aria-describedby", tooltip.id);
        delta.setAttribute("aria-label", series.title + ": " + delta.textContent);
        appendTextElement(tooltip, "strong", series.title).className = "report-history-tooltip-date";
        appendTextElement(tooltip, "p", explanation);
        if (points.length >= 2) {
            [first, last].forEach(function (point) {
                var detail = document.createElement("div");
                detail.className = "report-history-tooltip-row";
                appendTextElement(detail, "span", formatReportDate(point.date));
                appendTextElement(detail, "strong", formatNumber(point.value, series.decimals) + " " + series.unit);
                tooltip.appendChild(detail);
            });
        }
        appendTextElement(tooltip, "p", "Compares the first and last available chart points on configured dialing days within the selected date range. Non-dialing days and days without a value are skipped; zero values on dialing days are included.").className = "report-history-tooltip-detail";
        wrapper.appendChild(delta);
        wrapper.appendChild(tooltip);
        wrapper.addEventListener("mouseenter", function () { tooltip.hidden = false; });
        wrapper.addEventListener("mouseleave", function () { tooltip.hidden = true; });
        delta.addEventListener("focus", function () { tooltip.hidden = false; });
        delta.addEventListener("blur", function () { tooltip.hidden = true; });
        delta.addEventListener("keydown", function (event) {
            if (event.key === "Escape") { tooltip.hidden = true; event.stopPropagation(); }
        });
        return wrapper;
    }

    function renderHistoryChart(series, rows, values, tooltipScope) {
        var container = document.createElement("div");
        container.className = "report-chart";
        var tooltip = document.createElement("div");
        tooltip.id = "history-tooltip-" + tooltipScope + "-" + series.id;
        tooltip.className = "report-history-tooltip";
        tooltip.setAttribute("role", "tooltip");
        tooltip.hidden = true;
        var svg = svgElement("svg", {
            viewBox: "0 0 600 165",
            role: "img",
            "aria-label": series.title + " trend from " + formatReportDate(rows[0].date) + " through " + formatReportDate(rows[rows.length - 1].date),
            style: "--series-color:" + series.color
        });
        var left = 8, right = 12, top = 12, bottom = 28;
        var width = 600 - left - right, height = 165 - top - bottom;
        var numericValues = values.filter(function (value) { return value !== null; });
        if (!numericValues.length) {
            container.appendChild(emptyMessage(series.aggregate === "perHour" ? "No AOD hours are available for this period." : "No contacts are available for conversion."));
            return container;
        }
        var min = Math.min.apply(null, numericValues), max = Math.max.apply(null, numericValues);
        var spread = max - min || 1;
        for (var line = 0; line <= 2; line += 1) {
            var gridY = top + height * line / 2;
            svg.appendChild(svgElement("line", { x1: left, x2: left + width, y1: gridY, y2: gridY, "class": "chart-grid" }));
        }
        var points = values.map(function (item, index) {
            return {
                x: left + (values.length === 1 ? width / 2 : width * index / (values.length - 1)),
                y: item === null ? null : (values.length === 1 ? top + height / 2 : top + (max - item) * height / spread),
                value: item
            };
        });
        var line = points.reduce(function (path, point, index) {
            if (point.y === null) return path;
            var previousIsMissing = index === 0 || points[index - 1].y === null;
            return path + (previousIsMissing ? " M " : " L ") + point.x.toFixed(1) + " " + point.y.toFixed(1);
        }, "");
        svg.appendChild(svgElement("path", { d: line, "class": "chart-history-line" }));
        var pointRadius = rows.length > 60 ? 2 : rows.length > 31 ? 3 : 4;
        points.filter(function (point) { return point.y !== null; }).forEach(function (point) {
            svg.appendChild(svgElement("circle", { cx: point.x, cy: point.y, r: pointRadius, "class": "chart-history-point" }));
        });
        [0, Math.floor((rows.length - 1) / 2), rows.length - 1].filter(function (index, position, indexes) {
            return indexes.indexOf(index) === position;
        }).forEach(function (index) {
            var label = svgElement("text", {
                x: points[index].x,
                y: 160,
                "text-anchor": index === 0 ? "start" : (index === rows.length - 1 ? "end" : "middle"),
                "class": "chart-date-label"
            });
            label.textContent = formatShortReportDate(rows[index].date);
            svg.appendChild(label);
        });
        points.forEach(function (point, index) {
            var step = rows.length === 1 ? width : width / (rows.length - 1);
            var hitStart = rows.length === 1 ? left : Math.max(left, point.x - Math.max(5, step / 2));
            var hitEnd = rows.length === 1 ? left + width : Math.min(left + width, point.x + Math.max(5, step / 2));
            var hitArea = svgElement("rect", {
                x: hitStart,
                y: top,
                width: hitEnd - hitStart,
                height: height,
                tabindex: "0",
                role: "img",
                "aria-describedby": tooltip.id,
                "aria-label": formatReportDate(rows[index].date) + ": " + (point.value === null ? "No value" : formatNumber(point.value, series.decimals) + " " + series.unit),
                "class": "chart-history-hit-area"
            });
            hitArea.addEventListener("mouseenter", function () { showHistoryTooltip(container, tooltip, hitArea, series, rows[index], point.value); });
            hitArea.addEventListener("mouseleave", function () { tooltip.hidden = true; });
            hitArea.addEventListener("focus", function () { showHistoryTooltip(container, tooltip, hitArea, series, rows[index], point.value); });
            hitArea.addEventListener("blur", function () { tooltip.hidden = true; });
            svg.appendChild(hitArea);
        });
        container.appendChild(svg);
        container.appendChild(tooltip);
        return container;
    }

    function openAgentHistory(agent, trigger) {
        var bounds = getBounds();
        var requestId = ++state.agentHistoryRequest;
        state.agentModalReturnFocus = trigger;
        document.body.classList.add("has-report-modal");
        agentHistoryModal.hidden = false;
        document.getElementById("agent-history-title").textContent = agent.agentName || agent.agent;
        document.getElementById("agent-history-context").textContent =
            agent.agent + " · " + formatReportDate(bounds.fromDate) + " through " + formatReportDate(addDays(bounds.toExclusiveDate, -1));
        document.getElementById("agent-history-grid").textContent = "";
        state.agentHistoryRows = [];
        document.getElementById("agent-history-message").hidden = true;
        document.getElementById("agent-history-loading").hidden = false;
        document.getElementById("agent-history-close").focus();

        apiGet("api/metrics/history?" + queryString(metricParameters(bounds, agent.agent))).then(function (data) {
            if (requestId !== state.agentHistoryRequest) return;
            document.getElementById("agent-history-loading").hidden = true;
            var rows = (data.items || []).slice().sort(function (left, right) { return left.date.localeCompare(right.date); });
            if (!rows.length) {
                showAgentHistoryMessage("No history is available for this agent in the selected range and campaign.", false);
                return;
            }
            state.agentHistoryRows = rows;
            renderAgentHistory();
        }).catch(function (failure) {
            if (requestId !== state.agentHistoryRequest) return;
            document.getElementById("agent-history-loading").hidden = true;
            showAgentHistoryMessage(failure.message, true);
        });
    }

    function renderAgentHistory() {
        var grid = document.getElementById("agent-history-grid");
        if (!grid || !state.agentHistoryRows.length) return;
        grid.textContent = "";
        historySeries(state.historyMode).forEach(function (series) {
            grid.appendChild(renderHistoryCard(series, state.agentHistoryRows, "agent"));
        });
    }

    function closeAgentHistory() {
        if (agentHistoryModal.hidden) return;
        state.agentHistoryRequest += 1;
        agentHistoryModal.hidden = true;
        document.body.classList.remove("has-report-modal");
        if (state.agentModalReturnFocus && document.body.contains(state.agentModalReturnFocus)) {
            state.agentModalReturnFocus.focus();
        }
        state.agentModalReturnFocus = null;
    }

    function showAgentHistoryMessage(text, isError) {
        var modalMessage = document.getElementById("agent-history-message");
        modalMessage.textContent = text;
        modalMessage.className = "report-message" + (isError ? " is-error" : "");
        modalMessage.hidden = false;
    }

    function handleAgentHistoryKeydown(event) {
        if (agentHistoryModal.hidden || !historySourceModal.hidden) return;
        if (event.key === "Escape") {
            event.preventDefault();
            closeAgentHistory();
            return;
        }
        if (event.key !== "Tab") return;
        var focusable = agentHistoryModal.querySelectorAll("button:not([disabled]), [tabindex='0']");
        if (!focusable.length) return;
        var first = focusable[0], last = focusable[focusable.length - 1];
        if (event.shiftKey && document.activeElement === first) {
            event.preventDefault();
            last.focus();
        } else if (!event.shiftKey && document.activeElement === last) {
            event.preventDefault();
            first.focus();
        }
    }

    function openAppointmentEditor(appointment, trigger) {
        var requestId = ++state.appointmentEditorRequest;
        state.appointmentEditorReturnFocus = trigger;
        state.appointmentEditorModel = null;
        document.body.classList.add("has-report-modal");
        appointmentEditorModal.hidden = false;
        document.getElementById("appointment-editor-title").textContent = appointment.contact || "Appointment record";
        document.getElementById("appointment-editor-context").textContent = "Record #" + appointment.id;
        document.getElementById("appointment-editor-fields").textContent = "";
        document.getElementById("appointment-editor-message").hidden = true;
        document.getElementById("appointment-editor-loading").hidden = false;
        document.getElementById("appointment-editor-close").focus();
        apiGet("api/appointments/" + encodeURIComponent(appointment.id) + "/editor").then(function (model) {
            if (requestId !== state.appointmentEditorRequest) return;
            document.getElementById("appointment-editor-loading").hidden = true;
            state.appointmentEditorModel = model;
            renderAppointmentEditor();
        }).catch(function (failure) {
            if (requestId !== state.appointmentEditorRequest) return;
            document.getElementById("appointment-editor-loading").hidden = true;
            showAppointmentEditorMessage(failure.message, true);
        });
    }

    function closeAppointmentEditor() {
        if (!appointmentEditorModal || appointmentEditorModal.hidden) return;
        state.appointmentEditorRequest += 1;
        appointmentEditorModal.hidden = true;
        document.body.classList.remove("has-report-modal");
        if (state.appointmentEditorReturnFocus && document.body.contains(state.appointmentEditorReturnFocus)) {
            state.appointmentEditorReturnFocus.focus();
        } else if (state.appointmentEditorModel && state.appointmentEditorModel.record) {
            var replacement = document.querySelector('[data-appointment-edit-id="' + state.appointmentEditorModel.record.id + '"]');
            if (replacement) replacement.focus();
        }
        state.appointmentEditorReturnFocus = null;
        state.appointmentEditorModel = null;
    }

    function handleAppointmentEditorKeydown(event) {
        if (!appointmentEditorModal || appointmentEditorModal.hidden || !historySourceModal.hidden) return;
        if (event.key === "Escape") {
            event.preventDefault();
            closeAppointmentEditor();
            return;
        }
        if (event.key !== "Tab") return;
        var focusable = appointmentEditorModal.querySelectorAll("button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex='0']");
        if (!focusable.length) return;
        var first = focusable[0], last = focusable[focusable.length - 1];
        if (event.shiftKey && document.activeElement === first) {
            event.preventDefault();
            last.focus();
        } else if (!event.shiftKey && document.activeElement === last) {
            event.preventDefault();
            first.focus();
        }
    }

    function showAppointmentEditorMessage(text, isError) {
        var modalMessage = document.getElementById("appointment-editor-message");
        modalMessage.textContent = text;
        modalMessage.className = "report-message" + (isError ? " is-error" : "");
        modalMessage.hidden = false;
    }

    function appointmentEditorField(label, property, value, editor, options) {
        return { label: label, property: property, rawValue: value, editor: editor || "text", options: options || [] };
    }

    function appointmentEditorSections(model) {
        var record = model.record || {};
        var advisors = (model.advisors || []).map(function (advisor) {
            return { value: String(advisor.id), label: advisor.name + (advisor.firm ? " — " + advisor.firm : "") };
        });
        return [
            {
                title: "Appointment",
                fields: [
                    appointmentEditorField("Date and time", "AppointmentDateTime", record.appointmentDateTime, "datetime-local"),
                    appointmentEditorField("Time zone", "AppointmentTimeZone", record.appointmentTimeZone, "timezone"),
                    appointmentEditorField("Confirmed", "AppointmentConfirmed", record.appointmentConfirmed, "boolean", [{ value: "true", label: "Yes" }, { value: "false", label: "No" }]),
                    appointmentEditorField("Advisor", "FinalAdvisorId", model.finalAdvisorId, "select", advisors)
                ]
            },
            {
                title: "Contact",
                fields: [
                    appointmentEditorField("Name", "Contact", record.contact),
                    appointmentEditorField("Phone", "KeyPhone", record.keyPhone, "tel"),
                    appointmentEditorField("Preferred email", "EmailAddress", record.emailAddress, "email"),
                    appointmentEditorField("Work email", "WorkEmail", record.workEmail, "email"),
                    appointmentEditorField("Personal email", "PersonalEmail", record.personalEmail, "email"),
                    appointmentEditorField("LinkedIn", "LinkedInUrl", record.linkedInUrl, "url"),
                    appointmentEditorField("Location", "Location", record.location),
                    appointmentEditorField("Title", "Title", record.title),
                    appointmentEditorField("Industry", "Industry", record.industry),
                    appointmentEditorField("Company", "CompanyName", record.companyName),
                    appointmentEditorField("University", "University", record.university)
                ]
            },
            {
                title: "Event and research",
                fields: [
                    appointmentEditorField("Event type", "EventType", record.eventType),
                    appointmentEditorField("Event date", "EventDate", record.eventDate),
                    appointmentEditorField("Event accurate", "EventAccurate", record.eventAccurate, "select", ["True", "False", "Unable to Confirm"]),
                    appointmentEditorField("AI data", "AiData", record.aiData, "textarea"),
                    appointmentEditorField("BP notes", "BpNotes", record.bpNotes, "textarea")
                ]
            },
            {
                title: "Additional contact data",
                fields: [
                    appointmentEditorField("Rated A emails", "RrAEmails", record.rrAEmails, "textarea"),
                    appointmentEditorField("Rated A-minus emails", "RrAminusEmails", record.rrAminusEmails, "textarea"),
                    appointmentEditorField("Rated B emails", "RrBEmails", record.rrBEmails, "textarea"),
                    appointmentEditorField("Mobile phones", "RrMobilePhone", record.rrMobilePhone, "textarea"),
                    appointmentEditorField("Office phones", "RrOfficePhone", record.rrOfficePhone, "textarea"),
                    appointmentEditorField("Other phones", "RrOtherPhones", record.rrOtherPhones, "textarea")
                ]
            },
            {
                title: "Read-only context",
                fields: [
                    appointmentEditorField("Record ID", null, record.id),
                    appointmentEditorField("List", null, record.listName || record.listId),
                    appointmentEditorField("Contact date", null, formatWallClock(record.callDateTime)),
                    appointmentEditorField("Agent", null, record.agentName || record.agent),
                    appointmentEditorField("Services", null, (record.serviceNames || []).join(", ")),
                    appointmentEditorField("Teams", null, (record.teamNames || []).join(", "))
                ]
            }
        ];
    }

    function renderAppointmentEditor() {
        var model = state.appointmentEditorModel;
        if (!model || !model.record) return;
        var record = model.record;
        document.getElementById("appointment-editor-title").textContent = record.contact || "Appointment record";
        document.getElementById("appointment-editor-context").textContent =
            "Record #" + record.id + (record.companyName ? " · " + record.companyName : "");
        var fieldsRoot = document.getElementById("appointment-editor-fields");
        fieldsRoot.textContent = "";
        appointmentEditorSections(model).forEach(function (section) {
            var sectionElement = document.createElement("section");
            sectionElement.className = "appointment-editor-section";
            appendTextElement(sectionElement, "h3", section.title);
            var list = document.createElement("dl");
            list.className = "appointment-editor-field-list";
            section.fields.forEach(function (item) {
                var wrapper = document.createElement("div");
                var term = appendTextElement(wrapper, "dt", item.label);
                var description = document.createElement("dd");
                var value = appendTextElement(description, "span", appointmentEditorDisplayValue(item));
                value.className = "appointment-editor-value";
                if (item.property) {
                    var editButton = document.createElement("button");
                    editButton.type = "button";
                    editButton.className = "appointment-field-edit-button";
                    editButton.innerHTML = "&#9999;&#65039;";
                    editButton.setAttribute("aria-label", "Edit " + item.label);
                    editButton.title = "Edit " + item.label;
                    editButton.addEventListener("click", function () { beginAppointmentFieldEdit(description, item); });
                    description.appendChild(editButton);
                } else {
                    term.classList.add("is-read-only");
                }
                wrapper.appendChild(description);
                list.appendChild(wrapper);
            });
            sectionElement.appendChild(list);
            fieldsRoot.appendChild(sectionElement);
        });
        appendAppointmentInvalidation(fieldsRoot, model);
    }

    function appendAppointmentInvalidation(fieldsRoot, model) {
        var section = document.createElement("section");
        section.className = "appointment-invalidation-panel appointment-field-editor";
        appendTextElement(section, "h3", "Report status");
        appendTextElement(section, "p", "Select the call and replacement disposition. Invalidating the appointment updates the linked call and report summary while keeping an audit of the original values. Historical totals update with the next metrics refresh.");
        var calls = model.invalidationCalls || [];
        var dispositions = model.invalidationDispositions || [];
        var controls = new ByteForgeDispositionEditor(section, {
            dispositions: dispositions,
            calls: calls.map(function (call) {
                return { value: call.primaryKey, label: "Call " + call.primaryKey + " - " + formatWallClock(call.callDateTime) + " - " + (call.agentId || "Unknown agent") };
            }),
            onError: function (message) { showAppointmentEditorMessage(message, true); }
        });
        var button = document.createElement("button");
        button.type = "button";
        button.className = "appointment-invalidate-button";
        button.innerHTML = "&#9940; Invalidate appointment";
        button.disabled = !calls.length || !dispositions.length;
        if (button.disabled) appendTextElement(section, "p", "Invalidation is unavailable because no verified appointment call or configured replacement disposition is available.");
        button.addEventListener("click", function () {
            var decision = controls.read();
            if (!decision) return;
            decision.isInvalidated = true;
            button.disabled = true;
            controls.setDisabled(true);
            apiPost("api/appointments/" + encodeURIComponent(model.record.id) + "/invalidation", decision)
                .then(function () {
                    closeAppointmentEditor();
                    state.appointmentPage = 1;
                    return Promise.all([loadAppointments(), loadMetrics(), loadCalls()]).then(function () {
                        showMessage("Appointment invalidated and linked dispositions updated. Historical totals will update with the next metrics refresh.", false);
                    }).catch(function (failure) {
                        showMessage("Appointment saved, but the report could not refresh: " + failure.message + ". Refresh the page to reload the report.", true);
                    });
                }).catch(function (failure) {
                    button.disabled = false;
                    controls.setDisabled(false);
                    showAppointmentEditorMessage(failure.message, true);
                });
        });
        section.appendChild(button);
        fieldsRoot.appendChild(section);
    }
    function appointmentEditorDisplayValue(item) {
        if (item.editor === "boolean") return item.rawValue === true ? "Yes" : item.rawValue === false ? "No" : "Not set";
        if (item.editor === "datetime-local") return formatWallClock(item.rawValue);
        if (item.editor === "select") {
            var match = item.options.filter(function (option) {
                return String(typeof option === "string" ? option : option.value) === String(item.rawValue == null ? "" : item.rawValue);
            })[0];
            if (match) return typeof match === "string" ? match : match.label;
        }
        return item.rawValue === null || item.rawValue === undefined || String(item.rawValue).trim() === "" ? "—" : String(item.rawValue);
    }

    function createAppointmentFieldEditor(item) {
        if (item.editor === "textarea") {
            var textarea = document.createElement("textarea");
            textarea.rows = 4;
            textarea.maxLength = item.property.indexOf("Rr") === 0 ? 1000 : 2500;
            textarea.value = item.rawValue || "";
            return textarea;
        }
        if (item.editor === "timezone") {
            var timeZoneEditor = document.createElement("div");
            timeZoneEditor.className = "appointment-time-zone-editor";
            var timeZoneInput = document.createElement("input");
            timeZoneInput.type = "text";
            timeZoneInput.maxLength = 50;
            timeZoneInput.value = item.rawValue || "";
            timeZoneInput.setAttribute("data-appointment-editor-value", "");
            var picker = document.createElement("div");
            timeZoneEditor.appendChild(timeZoneInput);
            timeZoneEditor.appendChild(picker);
            ByteForgeTimeZones.createPicker({
                root: picker,
                input: timeZoneInput,
                zones: state.appointmentEditorModel.appointmentTimeZones || [],
                label: "Appointment time zone"
            });
            return timeZoneEditor;
        }
        if (item.editor === "select" || item.editor === "boolean") {
            var select = document.createElement("select");
            var empty = document.createElement("option");
            empty.value = "";
            empty.textContent = "Select";
            select.appendChild(empty);
            item.options.forEach(function (option) {
                var value = typeof option === "string" ? option : option.value;
                var label = typeof option === "string" ? option : option.label;
                var element = document.createElement("option");
                element.value = value;
                element.textContent = label;
                element.selected = String(item.rawValue == null ? "" : item.rawValue).toLowerCase() === String(value).toLowerCase();
                select.appendChild(element);
            });
            return select;
        }
        if (item.editor === "datetime-local") {
            var dateTimeEditor = document.createElement("div");
            dateTimeEditor.className = "appointment-date-time-editor";
            var dateTimeInput = document.createElement("input");
            dateTimeInput.type = "hidden";
            dateTimeInput.value = toDateTimeLocalValue(item.rawValue);
            dateTimeInput.setAttribute("data-appointment-editor-value", "");
            dateTimeEditor.appendChild(dateTimeInput);
            var dateTimePicker = document.createElement("div");
            dateTimeEditor.appendChild(dateTimePicker);
            ByteForgeDateTimePicker.createPicker({ root: dateTimePicker, input: dateTimeInput, label: "Appointment date and time" });
            return dateTimeEditor;
        }
        var input = document.createElement("input");
        input.type = item.editor;
        input.value = item.rawValue || "";
        return input;
    }

    function toDateTimeLocalValue(value) {
        var match = String(value || "").match(/^(\d{4}-\d{2}-\d{2}T\d{2}:\d{2})/);
        return match ? match[1] : "";
    }

    function appointmentEditorValue(editor, item) {
        if (item.editor === "timezone" || item.editor === "datetime-local") return editor.querySelector("[data-appointment-editor-value]").value;
        return editor.value;
    }

    function beginAppointmentFieldEdit(description, item) {
        var active = document.querySelector(".appointment-editor-field-list dd.is-editing");
        if (active) renderAppointmentEditor();
        description.textContent = "";
        description.classList.add("is-editing");
        var editor = createAppointmentFieldEditor(item);
        editor.classList.add("appointment-field-editor");
        var actions = document.createElement("span");
        actions.className = "appointment-field-actions";
        var saveButton = document.createElement("button");
        saveButton.type = "button";
        saveButton.className = "appointment-field-save";
        saveButton.innerHTML = "&#9989;";
        saveButton.setAttribute("aria-label", "Save " + item.label);
        saveButton.title = "Save " + item.label;
        var cancelButton = document.createElement("button");
        cancelButton.type = "button";
        cancelButton.className = "appointment-field-cancel";
        cancelButton.innerHTML = "&#10006;&#65039;";
        cancelButton.setAttribute("aria-label", "Cancel editing " + item.label);
        cancelButton.title = "Cancel editing " + item.label;
        cancelButton.addEventListener("click", renderAppointmentEditor);
        saveButton.addEventListener("click", function () {
            saveButton.disabled = true;
            cancelButton.disabled = true;
            document.getElementById("appointment-editor-message").hidden = true;
            apiPost(
                "api/appointments/" + encodeURIComponent(state.appointmentEditorModel.record.id) + "/fields",
                { field: item.property, value: appointmentEditorValue(editor, item) }
            ).then(function (model) {
                state.appointmentEditorModel = model;
                var index = (state.appointments.items || []).findIndex(function (record) { return record.id === model.record.id; });
                if (index >= 0) state.appointments.items[index] = model.record;
                renderAppointments();
                renderAppointmentEditor();
                showAppointmentEditorMessage(item.label + " saved.", false);
            }).catch(function (failure) {
                saveButton.disabled = false;
                cancelButton.disabled = false;
                showAppointmentEditorMessage(failure.message, true);
            });
        });
        actions.appendChild(saveButton);
        actions.appendChild(cancelButton);
        description.appendChild(editor);
        description.appendChild(actions);
        var focusTarget = editor.matches("input, select, textarea") ? editor : editor.querySelector("button, input, select, textarea");
        if (focusTarget) focusTarget.focus();
    }

    function showHistoryTooltip(container, tooltip, hitArea, series, row, value) {
        tooltip.textContent = "";
        appendTextElement(tooltip, "strong", formatReportDate(row.date)).className = "report-history-tooltip-date";
        var valueRow = document.createElement("div");
        valueRow.className = "report-history-tooltip-row";
        appendTextElement(valueRow, "span", series.title);
        appendTextElement(valueRow, "strong", value === null ? "No value" : formatNumber(value, series.decimals) + " " + series.unit);
        tooltip.appendChild(valueRow);

        var detail = getHistoryTooltipDetail(series, row);
        if (detail) appendTextElement(tooltip, "p", detail).className = "report-history-tooltip-detail";
        tooltip.hidden = false;

        var containerBounds = container.getBoundingClientRect();
        var hitBounds = hitArea.getBoundingClientRect();
        var desiredLeft = hitBounds.left - containerBounds.left + hitBounds.width / 2;
        var halfWidth = tooltip.offsetWidth / 2;
        tooltip.style.left = Math.max(halfWidth + 6, Math.min(container.clientWidth - halfWidth - 6, desiredLeft)) + "px";
    }

    function getHistoryTooltipDetail(series, row) {
        if (series.aggregate === "conversion") {
            return row.humanContacts ? formatNumber(row.appointments) + " appointments / " + formatNumber(row.humanContacts) + " contacts" : "No contacts recorded for the day";
        }
        if (series.aggregate === "perHour") {
            return row.aodSeconds ? "Across " + formatNumber(row.aodSeconds / 3600, 1) + " Praxis AOD hours" : "No AOD hours recorded for the day";
        }
        return "Daily total";
    }

    function renderTrend() {
        var grouped = {};
        ((state.metrics && state.metrics.items) || []).forEach(function (item) {
            if (!grouped[item.date]) grouped[item.date] = { date: item.date, dials: 0, contacts: 0 };
            grouped[item.date].dials += item.dials || 0;
            grouped[item.date].contacts += item.humanContacts || 0;
        });
        var rows = Object.keys(grouped).sort().map(function (key) { return grouped[key]; });
        var container = document.getElementById("trend-chart");
        container.textContent = "";
        if (!rows.length) {
            container.appendChild(emptyMessage("No metric activity in this range."));
            return;
        }
        var svg = svgElement("svg", {
            viewBox: "0 0 800 280",
            role: "img",
            "aria-label": "Daily dials on the left scale and human contacts on the right scale"
        });
        var left = 56, right = 62, top = 30, bottom = 38;
        var width = 800 - left - right, height = 280 - top - bottom;
        var dialMax = Math.max.apply(null, rows.map(function (row) { return row.dials; }).concat([1]));
        var contactMax = Math.max.apply(null, rows.map(function (row) { return row.contacts; }).concat([1]));
        var dialTitle = svgElement("text", { x: left, y: 15, "text-anchor": "start", "class": "chart-axis-title chart-axis-dials" });
        dialTitle.textContent = "Dials";
        svg.appendChild(dialTitle);
        var contactTitle = svgElement("text", { x: left + width, y: 15, "text-anchor": "end", "class": "chart-axis-title chart-axis-contacts" });
        contactTitle.textContent = "Human contacts";
        svg.appendChild(contactTitle);
        var tooltip = document.createElement("div");
        tooltip.id = "trend-chart-tooltip";
        tooltip.className = "report-chart-tooltip";
        tooltip.setAttribute("role", "tooltip");
        tooltip.hidden = true;
        for (var line = 0; line <= 4; line += 1) {
            var y = top + (height * line / 4);
            svg.appendChild(svgElement("line", { x1: left, x2: left + width, y1: y, y2: y, "class": "chart-grid" }));
            var dialLabel = svgElement("text", { x: left - 8, y: y + 4, "text-anchor": "end", "class": "chart-axis-label chart-axis-dials" });
            dialLabel.textContent = Math.round(dialMax * (4 - line) / 4);
            svg.appendChild(dialLabel);
            var contactLabel = svgElement("text", { x: left + width + 8, y: y + 4, "text-anchor": "start", "class": "chart-axis-label chart-axis-contacts" });
            contactLabel.textContent = Math.round(contactMax * (4 - line) / 4);
            svg.appendChild(contactLabel);
        }
        var dialPoints = [], contactPoints = [];
        var dateLabelIndexes = getTrendDateLabelIndexes(rows.length, width);
        rows.forEach(function (row, index) {
            var x = left + (rows.length === 1 ? width / 2 : width * index / (rows.length - 1));
            var dialY = top + height - (row.dials / dialMax * height);
            var contactY = top + height - (row.contacts / contactMax * height);
            dialPoints.push(x + "," + dialY);
            contactPoints.push(x + "," + contactY);
            if (dateLabelIndexes[index]) {
                var dateLabel = svgElement("text", { x: x, y: 270, "text-anchor": "middle", "class": "chart-date-label" });
                dateLabel.textContent = row.date.slice(5);
                svg.appendChild(dateLabel);
            }
        });
        svg.appendChild(svgElement("polyline", { points: dialPoints.join(" "), "class": "chart-dials-line" }));
        svg.appendChild(svgElement("polyline", { points: contactPoints.join(" "), "class": "chart-contacts-line" }));
        var pointRadius = rows.length > 120 ? 1.5 : rows.length > 60 ? 2 : rows.length > 31 ? 3 : 4;
        rows.forEach(function (row, index) {
            var x = left + (rows.length === 1 ? width / 2 : width * index / (rows.length - 1));
            var dialY = top + height - (row.dials / dialMax * height);
            var contactY = top + height - (row.contacts / contactMax * height);
            svg.appendChild(svgElement("circle", { cx: x, cy: dialY, r: pointRadius, "class": "chart-dials-point" }));
            svg.appendChild(svgElement("circle", { cx: x, cy: contactY, r: pointRadius, "class": "chart-contacts-point" }));
        });
        rows.forEach(function (row, index) {
            var step = rows.length === 1 ? width : width / (rows.length - 1);
            var x = left + (rows.length === 1 ? width / 2 : step * index);
            var hitStart = rows.length === 1 ? left : Math.max(left, x - step / 2);
            var hitEnd = rows.length === 1 ? left + width : Math.min(left + width, x + step / 2);
            var hitArea = svgElement("rect", {
                x: hitStart,
                y: top,
                width: hitEnd - hitStart,
                height: height,
                tabindex: "0",
                role: "img",
                "aria-describedby": tooltip.id,
                "aria-label": formatReportDate(row.date) + ": " +
                    formatNumber(row.dials) + " dials and " +
                    formatNumber(row.contacts) + " human contacts",
                "class": "chart-hit-area"
            });
            hitArea.addEventListener("mouseenter", function () {
                showTrendTooltip(container, tooltip, hitArea, row);
            });
            hitArea.addEventListener("mouseleave", function () { tooltip.hidden = true; });
            hitArea.addEventListener("focus", function () {
                showTrendTooltip(container, tooltip, hitArea, row);
            });
            hitArea.addEventListener("blur", function () { tooltip.hidden = true; });
            svg.appendChild(hitArea);
        });
        container.appendChild(svg);
        container.appendChild(tooltip);
    }

    function getTrendDateLabelIndexes(rowCount, plotWidth) {
        var indexes = {};
        if (!rowCount) return indexes;
        var labelCount = Math.min(rowCount, Math.max(2, Math.floor(plotWidth / 64)));
        if (labelCount === 1) {
            indexes[0] = true;
            return indexes;
        }
        for (var labelIndex = 0; labelIndex < labelCount; labelIndex += 1) {
            indexes[Math.round(labelIndex * (rowCount - 1) / (labelCount - 1))] = true;
        }
        return indexes;
    }

    function showTrendTooltip(container, tooltip, hitArea, row) {
        tooltip.textContent = "";
        appendTextElement(tooltip, "strong", formatReportDate(row.date)).className = "report-chart-tooltip-date";
        var dialRow = document.createElement("div");
        dialRow.className = "report-chart-tooltip-row";
        var dialLabel = appendTextElement(dialRow, "span", "Dials");
        dialLabel.className = "report-chart-tooltip-label is-dials";
        appendTextElement(dialRow, "strong", formatNumber(row.dials));
        tooltip.appendChild(dialRow);
        var contactRow = document.createElement("div");
        contactRow.className = "report-chart-tooltip-row";
        var contactLabel = appendTextElement(contactRow, "span", "Human contacts");
        contactLabel.className = "report-chart-tooltip-label is-contacts";
        appendTextElement(contactRow, "strong", formatNumber(row.contacts));
        tooltip.appendChild(contactRow);
        tooltip.hidden = false;

        var containerBounds = container.getBoundingClientRect();
        var hitBounds = hitArea.getBoundingClientRect();
        var desiredLeft = hitBounds.left - containerBounds.left + hitBounds.width / 2;
        var halfWidth = tooltip.offsetWidth / 2;
        tooltip.style.left = Math.max(halfWidth + 8, Math.min(container.clientWidth - halfWidth - 8, desiredLeft)) + "px";
    }

    function renderProgress() {
        var container = document.getElementById("list-progress");
        container.textContent = "";
        document.getElementById("progress-snapshot").textContent = state.progress.snapshotUtc ? "Current snapshot · As of " + formatDateTime(state.progress.snapshotUtc) : "Current snapshot";
        (state.progress.items || []).forEach(function (item) {
            var total = item.totalRecords || 0;
            var completed = item.completedRecords || 0;
            var percent = total ? completed / total * 100 : 0;
            var row = document.createElement("div");
            row.className = "report-progress-item bf-list-card";
            var heading = document.createElement("div");
            heading.className = "report-progress-heading";
            appendTextElement(heading, "strong", item.listName);
            appendTextElement(heading, "span", formatPercent(percent / 100));
            row.appendChild(heading);
            appendTextElement(row, "p", "Recorded attempts per primary phone").className = "bf-list-caption";
            var buckets = item.attemptBuckets;
            if (Array.isArray(buckets) && buckets.length === 6) {
                var graph = document.createElement("div");
                graph.className = "bf-list-attempts";
                graph.setAttribute("role", "list");
                graph.setAttribute("aria-label", "Phone numbers by recorded attempts");
                var maximum = Math.max.apply(null, buckets.map(function (bucket) { return bucket.phoneCount; }).concat([1]));
                buckets.forEach(function (bucket) {
                    var label = bucket.attempts === 5 ? "5+" : String(bucket.attempts);
                    var column = document.createElement("div");
                    column.className = "bf-list-attempt-column" + (bucket.attempts === 5 ? " is-high" : "");
                    column.setAttribute("role", "listitem");
                    column.setAttribute("aria-label", label + " attempts: " + formatNumber(bucket.phoneCount) + " phone numbers");
                    appendTextElement(column, "span", formatNumber(bucket.phoneCount)).className = "bf-list-attempt-count";
                    var well = document.createElement("div");
                    well.className = "bf-list-attempt-well";
                    var bar = document.createElement("div");
                    bar.className = "bf-list-attempt-bar";
                    bar.style.height = (bucket.phoneCount / maximum * 100) + "%";
                    well.appendChild(bar);
                    column.appendChild(well);
                    appendTextElement(column, "span", label);
                    graph.appendChild(column);
                });
                row.appendChild(graph);
                if (item.missingPhoneRecords) appendTextElement(row, "p", formatNumber(item.missingPhoneRecords) + " records without a primary phone are excluded from the graph.").className = "bf-list-caption";
            } else appendTextElement(row, "p", "Attempt breakdown unavailable.").className = "bf-list-caption";
            appendTextElement(row, "p", "Record completion").className = "bf-list-caption";
            var track = document.createElement("div");
            track.className = "report-progress-track";
            track.setAttribute("role", "progressbar");
            track.setAttribute("aria-label", item.listName + " record completion");
            track.setAttribute("aria-valuemin", "0");
            track.setAttribute("aria-valuemax", "100");
            track.setAttribute("aria-valuenow", String(Math.round(percent)));
            var fill = document.createElement("div");
            fill.className = "report-progress-fill";
            fill.style.width = Math.max(0, Math.min(100, percent)) + "%";
            track.appendChild(fill);
            row.appendChild(track);
            var detail = document.createElement("div");
            detail.className = "report-progress-detail";
            appendTextElement(detail, "span", formatNumber(completed) + " completed");
            appendTextElement(detail, "span", formatNumber(item.remainingRecords || 0) + " not completed");
            row.appendChild(detail);
            if (item.dispositions && item.dispositions.length) {
                row.appendChild(buildDispositionChart(item));
            }
            container.appendChild(row);
        });
        (state.progress.missingConfiguredLists || []).forEach(function (name) {
            container.appendChild(emptyMessage(name + ": no mapped source records found."));
        });
        if (!(state.progress.items || []).length) container.appendChild(emptyMessage("No configured list progress was returned."));
    }

    function buildDispositionChart(item) {
        var all = (item.dispositions || [])
            .slice().sort(function (a, b) { return b.records - a.records || a.disposition.localeCompare(b.disposition); });
        var total = all.reduce(function (sum, entry) { return sum + entry.records; }, 0);
        var positive = all.filter(function (entry) { return entry.records > 0; });
        var slices = positive.slice(0, 10);
        var others = positive.slice(10).reduce(function (sum, entry) { return sum + entry.records; }, 0);
        if (others) slices.push({ disposition: "Others", records: others });
        var panel = document.createElement("details"); panel.className = "bf-list-dispositions";
        appendTextElement(panel, "summary", "Current dispositions · " + formatNumber(total) + " records");
        var heading = document.createElement("div"); heading.className = "report-progress-heading";
        appendTextElement(heading, "strong", "Current dispositions");
        var source = document.createElement("button"); source.type = "button";
        source.className = "report-history-source-button"; source.textContent = "Source";
        source.setAttribute("aria-label", "Full disposition breakdown for " + item.listName);
        source.addEventListener("click", function () { openDispositionSource(item, all, total, source); });
        heading.appendChild(source); panel.appendChild(heading);
        appendTextElement(panel, "p", formatNumber(total) + " records · Current list snapshot").className = "bf-list-caption";
        var pie = document.createElement("div"); pie.className = "bf-list-disposition-pie";
        pie.setAttribute("role", "img"); pie.setAttribute("aria-label", "Disposition shares: top 10 plus Others. Full values in Source.");
        var legend = document.createElement("ul"); legend.className = "bf-list-disposition-legend";
        var start = 0;
        var stops = slices.map(function (slice, index) {
            var end = start + slice.records / total * 100;
            var color = slice.disposition === "Others" && others ? "#818991" : dispositionColor(slice.disposition);
            var stop = color + " " + start + "% " + end + "%"; start = end;
            var entry = document.createElement("li");
            var swatch = document.createElement("span"); swatch.className = "bf-list-disposition-swatch";
            swatch.style.backgroundColor = color; swatch.setAttribute("aria-hidden", "true"); entry.appendChild(swatch);
            appendTextElement(entry, "span", slice.disposition);
            appendTextElement(entry, "span", formatNumber(slice.records) + " · " + formatNumber(slice.records / total * 100, 1) + "%");
            legend.appendChild(entry); return stop;
        });
        if (total) { pie.style.background = "conic-gradient(" + stops.join(",") + ")"; panel.appendChild(pie); }
        else appendTextElement(panel, "p", "No dispositions available.");
        panel.appendChild(legend); return panel;
    }

    function dispositionColor(name) {
        // Derive identity from the label, never its rank or list membership.
        var key = (name || "").trim().toLowerCase();
        var hash = 2166136261;
        for (var i = 0; i < key.length; i++) hash = Math.imul(hash ^ key.charCodeAt(i), 16777619) >>> 0;
        return "hsl(" + (hash % 360) + ", " + (45 + (hash >>> 9) % 20) + "%, " + (48 + (hash >>> 17) % 12) + "%)";
    }

    function openKpiDispositions(kind, trigger) {
        var history = state.history;
        var contacts = kind === "contacts";
        var title = contacts ? "Human contacts" : "Dials";
        var available = history && history.dispositionCountsAvailable;
        var all = available ? history.dispositions.filter(function (row) { return !contacts || row.humanContact; })
            .map(function (row) { return { disposition: row.disposition || "(No disposition)", records: row.calls }; }) : [];
        // Group again because configuration membership can change between publication dates.
        var grouped = Object.create(null);
        all.forEach(function (row) { var key = row.disposition.toLowerCase();
            if (!Object.prototype.hasOwnProperty.call(grouped, key)) grouped[key] = { disposition: row.disposition, records: 0 };
            grouped[key].records += row.records; });
        all = Object.keys(grouped).map(function (key) { return grouped[key]; });
        var total = all.reduce(function (sum, row) { return sum + row.records; }, 0);
        openDispositionSource({ listName: title, callCounts: true,
            sourceContext: !history ? "Report data is loading or unavailable. Close this window and retry after refreshing the report." :
                !available ? "Disposition counts are unavailable for this range. Reprocess the selected dates with DataImport, then refresh the report." :
                formatReportDate((history.from || "").slice(0, 10)) + " through " + formatReportDate((history.to || "").slice(0, 10)) + " (exclusive), " + history.reportTimeZone +
                " · Selected service and team · " + formatNumber(total) + " calls. " +
                (contacts ? "Wrong Number is excluded from Human Contacts and conversion. Wrong Numbers: " + formatNumber(history.dispositions.filter(function (row) { return (row.disposition || "").trim().toLowerCase() === "wrong number"; }).reduce(function (sum, row) { return sum + row.calls; }, 0)) + ". See Dials for the separate disposition breakdown." : "Each dial contributes one source disposition.") }, all, total, trigger);
    }

    function openDispositionSource(item, all, total, trigger) {
        var dialog = document.createElement("dialog"); dialog.className = "report-modal bf-list-source-dialog";
        dialog.setAttribute("aria-label", item.listName + " disposition source");
        var header = document.createElement("div"); header.className = "report-progress-heading";
        appendTextElement(header, "h2", item.listName + " dispositions");
        var close = document.createElement("button"); close.type = "button"; close.className = "report-modal-close";
        close.innerHTML = "&#10006;&#65039;"; close.setAttribute("aria-label", "Close disposition source");
        close.addEventListener("click", function () { dialog.close(); }); header.appendChild(close); dialog.appendChild(header);
        appendTextElement(dialog, "p", item.sourceContext || "Current list snapshot · " + formatNumber(total) + " records. Each record contributes its latest disposition; report dates do not filter this snapshot.").className = "report-meta";
        var search = document.createElement("input"); search.type = "search"; search.placeholder = "Search dispositions";
        search.setAttribute("aria-label", "Search dispositions"); dialog.appendChild(search);
        var table = document.createElement("table"); var head = document.createElement("thead"); var headings = document.createElement("tr");
        var sort = "records", direction = -1, page = 1;
        [["Disposition", "disposition"], [item.callCounts ? "Calls" : "Records", "records"], ["Share", "records"]].forEach(function (column) {
            var th = document.createElement("th"); var button = document.createElement("button"); button.type = "button";
            button.textContent = column[0]; button.addEventListener("click", function () {
                direction = sort === column[1] ? -direction : column[1] === "records" ? -1 : 1; sort = column[1]; page = 1; render();
            }); th.appendChild(button); headings.appendChild(th);
        }); head.appendChild(headings); table.appendChild(head);
        var body = document.createElement("tbody"); table.appendChild(body); dialog.appendChild(table);
        var pager = document.createElement("div"); pager.className = "report-progress-heading";
        // List snapshots retain their full scrollable source; call breakdowns use fixed-size pages.
        var summary = document.createElement("span"); pager.appendChild(summary); dialog.appendChild(pager);
        var previous, next;
        if (item.callCounts) {
            previous = appendTextElement(pager, "button", "Previous"); previous.type = "button";
            next = appendTextElement(pager, "button", "Next"); next.type = "button";
            previous.addEventListener("click", function () { page--; render(); });
            next.addEventListener("click", function () { page++; render(); });
        }
        search.addEventListener("input", function () { page = 1; render(); });
        function render() {
            var rows = all.filter(function (entry) { return entry.disposition.toLowerCase().indexOf(search.value.trim().toLowerCase()) >= 0; })
                .slice().sort(function (a, b) { return direction * (sort === "records" ? a.records - b.records : a.disposition.localeCompare(b.disposition)); });
            body.textContent = "";
            (item.callCounts ? rows.slice((page - 1) * 10, page * 10) : rows).forEach(function (entry) {
                var tr = document.createElement("tr"); appendCell(tr, entry.disposition); appendCell(tr, formatNumber(entry.records));
                appendCell(tr, formatNumber(total ? entry.records / total * 100 : 0, 1) + "%"); body.appendChild(tr);
            });
            if (!rows.length) appendEmptyRow(body, 3, "No dispositions match.");
            Array.prototype.forEach.call(headings.children, function (th, index) { th.setAttribute("aria-sort", (index === 0 ? "disposition" : "records") === sort ? direction === 1 ? "ascending" : "descending" : "none"); });
            summary.textContent = rows.length + " of " + all.length + " dispositions";
            if (item.callCounts) {
                previous.disabled = page <= 1; next.disabled = page * 10 >= rows.length;
                summary.textContent += " · Page " + page + " of " + Math.max(1, Math.ceil(rows.length / 10));
            }
        }
        dialog.addEventListener("click", function (event) { if (event.target === dialog && (event.clientX < dialog.getBoundingClientRect().left || event.clientX > dialog.getBoundingClientRect().right || event.clientY < dialog.getBoundingClientRect().top || event.clientY > dialog.getBoundingClientRect().bottom)) dialog.close(); });
        dialog.addEventListener("close", function () { dialog.remove(); trigger.focus(); });
        document.body.appendChild(dialog); render(); dialog.showModal(); search.focus();
    }

    function buildAgentRows() {
        var agents = {};
        ((state.metrics && state.metrics.items) || []).forEach(function (item) {
            var key = (item.agent || "").toLowerCase();
            if (!agents[key]) agents[key] = {
                agent: item.agent,
                agentName: item.agentName || item.agent,
                dials: 0,
                humanContacts: 0,
                aodSeconds: 0,
                afterCallWorkSeconds: 0,
                callDurationSeconds: 0,
                appointments: 0
            };
            agents[key].dials += item.dials || 0;
            agents[key].humanContacts += item.humanContacts || 0;
            agents[key].aodSeconds += item.aodSeconds || 0;
            agents[key].afterCallWorkSeconds += item.afterCallWorkSeconds || 0;
            agents[key].callDurationSeconds += item.callDurationSeconds || 0;
            agents[key].appointments += item.appointments || 0;
        });
        state.agentRows = Object.keys(agents).map(function (key) {
            var row = agents[key];
            row.aodHours = row.aodSeconds / 3600;
            row.dialsPerAodHour = row.aodHours ? row.dials / row.aodHours : null;
            row.contactsPerAodHour = row.aodHours ? row.humanContacts / row.aodHours : null;
            row.averageAfterCallWorkSeconds = row.dials ? row.afterCallWorkSeconds / row.dials : null;
            row.averageHandleTimeSeconds = row.dials ? row.callDurationSeconds / row.dials : null;
            row.appointmentsPerAodHour = row.aodHours ? row.appointments / row.aodHours : null;
            row.appointmentsPerCall = row.dials ? row.appointments / row.dials : null;
            row.conversionRate = row.humanContacts ? row.appointments / row.humanContacts : null;
            return row;
        });
    }

    function renderAgents() {
        var sourceNote = document.getElementById("agent-metrics-source");
        if (sourceNote) {
            sourceNote.hidden = !state.metrics || state.metrics.source !== "DataImport.scoped";
            sourceNote.textContent = sourceNote.hidden ? "" : "Preprocessed results" +
                (state.metrics.activityAvailableToDate ? " \u00b7 Latest stored day: " + state.metrics.activityAvailableToDate : "");
        }
        if (state.metricsLoading || state.metricsError) {
            var statusBody = document.getElementById("agent-table-body");
            statusBody.textContent = "";
            appendEmptyRow(statusBody, 11, state.metricsError || "Loading agent metrics...");
            document.getElementById("agent-summary").textContent = state.metricsError ? "Unavailable" : "Loading...";
            updatePager("agent", 1, 1);
            return;
        }
        var search = document.getElementById("agent-search").value.trim().toLowerCase();
        var rows = state.agentRows.filter(function (row) { return !search || getAgentSearchText(row).indexOf(search) >= 0; });
        rows.sort(function (left, right) {
            var a = left[state.agentSort], b = right[state.agentSort];
            if (typeof a === "string") return a.localeCompare(b) * (state.agentDirection === "asc" ? 1 : -1);
            return ((a || 0) - (b || 0)) * (state.agentDirection === "asc" ? 1 : -1);
        });
        var totalPages = Math.max(1, Math.ceil(rows.length / state.agentPageSize));
        state.agentPage = Math.min(Math.max(1, state.agentPage), totalPages);
        var pageRows = rows.slice((state.agentPage - 1) * state.agentPageSize, state.agentPage * state.agentPageSize);
        var body = document.getElementById("agent-table-body");
        body.textContent = "";
        pageRows.forEach(function (row) {
            var tr = document.createElement("tr");
            tr.className = "report-agent-row";
            tr.addEventListener("click", function () { openAgentHistory(row, tr); });
            appendAgentHistoryCell(tr, row, search);
            appendCell(tr, formatNumber(row.aodHours, 1), search);
            appendCell(tr, formatNumber(row.dials), search);
            appendCell(tr, formatNumber(row.humanContacts), search);
            appendCell(tr, row.dialsPerAodHour === null ? "—" : formatNumber(row.dialsPerAodHour, 1), search);
            appendCell(tr, row.contactsPerAodHour === null ? "—" : formatNumber(row.contactsPerAodHour, 1), search);
            appendCell(tr, row.averageAfterCallWorkSeconds === null ? "—" : formatDuration(row.averageAfterCallWorkSeconds), search);
            appendCell(tr, row.averageHandleTimeSeconds === null ? "—" : formatDuration(row.averageHandleTimeSeconds), search);
            appendCell(tr, row.appointmentsPerAodHour === null ? "—" : formatNumber(row.appointmentsPerAodHour, 2), search);
            appendCell(tr, row.conversionRate === null ? "—" : formatPercent(row.conversionRate), search);
            appendCell(tr, formatNumber(row.appointments), search);
            body.appendChild(tr);
        });
        if (!pageRows.length) appendEmptyRow(body, 11, "No agents match this report.");
        document.getElementById("agent-summary").textContent = formatNumber(rows.length) + " agents";
        updatePager("agent", state.agentPage, totalPages);
    }

    function getAgentSearchText(row) {
        return [
            row.agentName, row.agent, formatNumber(row.aodHours, 1), formatNumber(row.dials),
            formatNumber(row.humanContacts), row.dialsPerAodHour === null ? "—" : formatNumber(row.dialsPerAodHour, 1),
            row.contactsPerAodHour === null ? "—" : formatNumber(row.contactsPerAodHour, 1),
            row.averageAfterCallWorkSeconds === null ? "—" : formatDuration(row.averageAfterCallWorkSeconds),
            row.averageHandleTimeSeconds === null ? "—" : formatDuration(row.averageHandleTimeSeconds),
            row.appointmentsPerAodHour === null ? "—" : formatNumber(row.appointmentsPerAodHour, 2),
            row.conversionRate === null ? "—" : formatPercent(row.conversionRate), formatNumber(row.appointments)
        ].join(" ").toLowerCase();
    }

    function renderAppointments() {
        var search = document.getElementById("appointment-search").value.trim();
        var body = document.getElementById("appointment-table-body");
        body.textContent = "";
        var rejectedSectionStarted = false;
        (state.appointments.items || []).forEach(function (item) {
            var rejected = item.qaStatus === "Rejected";
            if (rejected && !rejectedSectionStarted) {
                var separator = document.createElement("tr");
                separator.className = "bf-report-qa-separator";
                var heading = document.createElement("th");
                heading.colSpan = state.canEditAppointments ? 9 : 8;
                heading.scope = "rowgroup";
                heading.textContent = "QA Rejected appointments \u2014 excluded from totals";
                separator.appendChild(heading);
                body.appendChild(separator);
                rejectedSectionStarted = true;
            }
            var tr = document.createElement("tr");
            if (rejected) tr.className = "bf-report-qa-rejected";
            if (state.canEditAppointments) {
                var actionCell = document.createElement("td");
                actionCell.className = "report-edit-cell";
                var editButton = document.createElement("button");
                editButton.type = "button";
                editButton.className = "report-edit-button";
                editButton.setAttribute("data-appointment-edit-id", item.id);
                editButton.innerHTML = "&#9999;&#65039;";
                editButton.setAttribute("aria-label", "Edit appointment for " + (item.contact || "record " + item.id));
                editButton.title = "Edit appointment record";
                editButton.addEventListener("click", function () { openAppointmentEditor(item, editButton); });
                if (item.qaStatus !== "Rejected") actionCell.appendChild(editButton);
                tr.appendChild(actionCell);
            }
            var appointmentDate = formatWallClock(item.appointmentDateTime);
            if (item.appointmentDateTime && item.appointmentTimeZone) {
                appointmentDate += " " + item.appointmentTimeZone;
            }
            appendRichCell(tr, appointmentDate, item.eventType || item.listName, search);
            appendCell(tr, formatWallClock(item.callDateTime), search);
            appendRichCell(tr, item.contact || "—", item.emailAddress || item.keyPhone, search);
            if (state.canOpenReportScript) {
                var scriptLink = document.createElement("a");
                scriptLink.href = "workspace?RecordID=" + encodeURIComponent(item.id) + "&FromReport=true&AgentID=" + encodeURIComponent(item.agent || "");
                scriptLink.target = "_blank";
                scriptLink.rel = "noopener";
                scriptLink.className = "report-edit-button";
                scriptLink.innerHTML = "&#128065;&#65039; Script";
                scriptLink.title = "Open script for " + (item.contact || "record " + item.id) + " in a new tab";
                scriptLink.setAttribute("aria-label", scriptLink.title);
                tr.lastElementChild.appendChild(scriptLink);
            }
            appendCell(tr, item.companyName || "—", search);
            appendRichCell(tr, item.agentName || item.agent || "—", item.agentName && item.agent ? item.agent : "", search);
            appendRichCell(tr, item.finalAdvisorName || "—", item.finalAdvisorFirm || "", search);
            appendPillCell(tr, item.appointmentConfirmed, search);
            var qaCell = document.createElement("td");
            var qaPill = document.createElement("span");
            var qaStatus = item.qaStatus || "Pending QA";
            qaPill.className = "report-pill " + (qaStatus === "Approved" ? "bf-report-qa-approved" : rejected ? "bf-report-qa-reject" : "is-unknown");
            qaPill.textContent = rejected ? "Reject" : qaStatus;
            if (rejected) qaPill.title = "QA rejected; excluded from totals";
            qaCell.appendChild(qaPill);
            tr.appendChild(qaCell);
            body.appendChild(tr);
        });
        if (!(state.appointments.items || []).length) appendEmptyRow(body, state.canEditAppointments ? 9 : 8, "No appointments match these filters.");
        document.getElementById("appointment-summary").textContent = formatNumber(state.appointments.totalRows) + " records";
        updatePager("appointment", state.appointments.page, state.appointments.totalPages || 1);
    }

    function renderCalls() {
        var search = document.getElementById("call-search").value.trim();
        var body = document.getElementById("call-table-body");
        body.textContent = "";
        (state.calls.items || []).forEach(function (item) {
            var tr = document.createElement("tr");
            var dialedPhone = item.dialedPhone || "";
            var recordPhone = item.record && item.record.keyPhone ? item.record.keyPhone : "";
            var formattedDialedPhone = formatPhoneNumber(dialedPhone);
            var formattedRecordPhone = formatPhoneNumber(recordPhone);
            appendRichCell(tr, formatDateTime(item.startedAt), item.callId || "", search);
            appendRichCell(tr, item.record && item.record.contact ? item.record.contact : "—", item.record && item.record.companyName ? item.record.companyName : item.praxisRecordId, search);
            appendRichCell(tr, formattedDialedPhone || formattedRecordPhone || "—", formattedDialedPhone && formattedRecordPhone && formattedDialedPhone !== formattedRecordPhone ? "Record: " + formattedRecordPhone : "", search);
            appendRichCell(tr, item.agentName || item.agent || "—", item.agentName && item.agent ? item.agent : "", search);
            appendRichCell(tr, item.disposition || "—", item.humanContact ? "Human contact" : "No human contact", search);
            appendCell(tr, formatDuration(item.totalDurationSeconds), search);
            appendCell(tr, item.listName || "—", search);
            body.appendChild(tr);
        });
        if (!(state.calls.items || []).length) appendEmptyRow(body, 7, "No calls match these filters.");
        document.getElementById("call-summary").textContent = formatNumber(state.calls.totalRows) + " calls";
        updatePager("call", state.calls.page, state.calls.totalPages || 1);
    }

    function apiGet(path) {
        return fetch(path, { headers: { "Accept": "application/json", "Authorization": "Bearer " + state.token } }).then(readResponse).then(function (result) {
            if (!result.response.ok) throw reportApiError(result, path, "Unable to load report data.");
            return result.data;
        });
    }

    function readResponse(response) {
        return response.text().then(function (text) {
            var data = null;
            if (text) {
                try { data = JSON.parse(text); } catch (ignored) { data = { message: text }; }
            }
            return { response: response, data: data };
        });
    }

    function reportApiError(result, path, fallback) {
        var detail = getErrorMessage(result.data, fallback);
        var guidance = result.response.status === 401
            ? " The session may have expired, or the UI and API authentication settings may not match. Sign out and sign in again; contact support if this continues."
            : "";
        var reference = result.data && (result.data.requestId || result.data.RequestId);
        if (/^[0-9a-f]{12}$/i.test(reference || "")) detail += " Support reference: " + reference + ".";
        var failure = new Error("Praxis API request failed (HTTP " + result.response.status + "; " + path.split("?")[0] + "). " + detail + guidance);
        failure.httpStatus = result.response.status;
        return failure;
    }

    function getErrorMessage(data, fallback) {
        return data && (data.message || data.Message || data.error_description || data.error) ? (data.message || data.Message || data.error_description || data.error) : fallback;
    }

    function queryString(values) {
        var parameters = new URLSearchParams();
        Object.keys(values).forEach(function (key) {
            if (values[key] !== "" && values[key] !== null && values[key] !== undefined) parameters.append(key, values[key]);
        });
        return parameters.toString();
    }

    function dateInZone(date, timeZone) {
        var parts = new Intl.DateTimeFormat("en-CA", { timeZone: timeZone, year: "numeric", month: "2-digit", day: "2-digit" }).formatToParts(date);
        var values = {};
        parts.forEach(function (part) { values[part.type] = part.value; });
        return values.year + "-" + values.month + "-" + values.day;
    }

    function zonedMidnightIso(dateValue, timeZone) {
        var pieces = dateValue.split("-").map(Number);
        var desired = Date.UTC(pieces[0], pieces[1] - 1, pieces[2], 0, 0, 0);
        var guess = desired;
        for (var attempt = 0; attempt < 3; attempt += 1) {
            var parts = new Intl.DateTimeFormat("en-US", {
                timeZone: timeZone, year: "numeric", month: "2-digit", day: "2-digit",
                hour: "2-digit", minute: "2-digit", second: "2-digit", hourCycle: "h23"
            }).formatToParts(new Date(guess));
            var values = {};
            parts.forEach(function (part) { values[part.type] = part.value; });
            var represented = Date.UTC(Number(values.year), Number(values.month) - 1, Number(values.day), Number(values.hour), Number(values.minute), Number(values.second));
            guess += desired - represented;
        }
        return new Date(guess).toISOString();
    }

    function addDays(value, days) {
        var date = new Date(value + "T00:00:00Z");
        date.setUTCDate(date.getUTCDate() + days);
        return date.toISOString().slice(0, 10);
    }

    function formatDateTime(value) {
        if (!value) return "—";
        try {
            return new Intl.DateTimeFormat(undefined, {
                timeZone: state.configuration.reportIanaTimeZone,
                month: "short", day: "numeric", year: "numeric", hour: "numeric", minute: "2-digit"
            }).format(new Date(value));
        } catch (ignored) { return value; }
    }

    function formatWallClock(value) {
        if (!value) return "—";
        var match = String(value).match(/^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})/);
        if (!match) return value;
        var hour = Number(match[4]);
        return match[2] + "/" + match[3] + "/" + match[1] + " " + ((hour + 11) % 12 + 1) + ":" + match[5] + " " + (hour >= 12 ? "PM" : "AM");
    }

    function formatReportDate(value) {
        if (!value) return "—";
        try {
            return new Intl.DateTimeFormat(undefined, {
                timeZone: "UTC", month: "short", day: "numeric", year: "numeric"
            }).format(new Date(value + "T00:00:00Z"));
        } catch (ignored) { return value; }
    }

    function formatNumber(value, decimals) {
        if (value === null || value === undefined || isNaN(value)) return "—";
        return Number(value).toLocaleString(undefined, { minimumFractionDigits: decimals || 0, maximumFractionDigits: decimals || 0 });
    }

    function formatPercent(value) {
        if (value === null || value === undefined || isNaN(value)) return "—";
        return (value * 100).toLocaleString(undefined, { minimumFractionDigits: 1, maximumFractionDigits: 1 }) + "%";
    }

    function formatDuration(seconds) {
        seconds = Math.max(0, Number(seconds) || 0);
        var hours = Math.floor(seconds / 3600), minutes = Math.floor((seconds % 3600) / 60), remainder = Math.floor(seconds % 60);
        return (hours ? hours + "h " : "") + (minutes ? minutes + "m " : "") + remainder + "s";
    }

    function formatShortReportDate(value) {
        if (!value) return "—";
        try {
            return new Intl.DateTimeFormat(undefined, {
                timeZone: "UTC", month: "short", day: "numeric"
            }).format(new Date(value + "T00:00:00Z"));
        } catch (ignored) { return value; }
    }

    function formatPhoneNumber(phoneNumber) {
        if (!phoneNumber || /^\s*$/.test(phoneNumber)) return phoneNumber;
        var digits = String(phoneNumber).replace(/\D/g, "");
        if (digits.length === 11 && digits.charAt(0) === "1") digits = digits.substring(1);
        if (digits.length < 10) return digits;
        if (digits.length === 10) {
            return "(" + digits.substring(0, 3) + ") " + digits.substring(3, 6) + "-" + digits.substring(6);
        }
        var countryCode = digits.substring(0, digits.length - 10);
        var localNumber = digits.substring(digits.length - 10);
        return "+" + countryCode + " (" + localNumber.substring(0, 3) + ") " +
            localNumber.substring(3, 6) + "-" + localNumber.substring(6);
    }

    function updatePager(prefix, page, totalPages) {
        document.getElementById(prefix + "-page").textContent = "Page " + page + " of " + totalPages;
        document.getElementById(prefix + "-previous").disabled = page <= 1;
        document.getElementById(prefix + "-next").disabled = page >= totalPages;
    }

    function appendCell(row, value, search) {
        var cell = document.createElement("td");
        appendHighlightedText(cell, value, search);
        row.appendChild(cell);
    }

    function appendRichCell(row, primary, secondary, search) {
        var cell = document.createElement("td");
        var primaryElement = document.createElement("strong");
        appendHighlightedText(primaryElement, primary || "—", search);
        cell.appendChild(primaryElement);
        if (secondary) {
            var secondaryElement = document.createElement("small");
            appendHighlightedText(secondaryElement, secondary, search);
            cell.appendChild(secondaryElement);
        }
        row.appendChild(cell);
    }

    function appendAgentHistoryCell(tableRow, agent, search) {
        var cell = document.createElement("td");
        var button = document.createElement("button");
        button.type = "button";
        button.className = "report-agent-history-button";
        appendHighlightedText(button, agent.agentName || agent.agent, search);
        button.setAttribute("aria-label", "View historical graphs for " + (agent.agentName || agent.agent));
        button.addEventListener("click", function (event) {
            event.stopPropagation();
            openAgentHistory(agent, button);
        });
        cell.appendChild(button);
        if (agent.agentName !== agent.agent) {
            var login = document.createElement("small");
            appendHighlightedText(login, agent.agent, search);
            cell.appendChild(login);
        }
        tableRow.appendChild(cell);
    }

    function appendPillCell(row, value, search) {
        var cell = document.createElement("td");
        var pill = document.createElement("span");
        pill.className = "report-pill " + (value === true ? "is-positive" : value === false ? "is-negative" : "is-unknown");
        appendHighlightedText(pill, value === true ? "Confirmed" : value === false ? "Not confirmed" : "Not attempted", search);
        cell.appendChild(pill);
        row.appendChild(cell);
    }

    function appendHighlightedText(parent, value, search) {
        var text = value === null || value === undefined ? "" : String(value);
        var query = String(search || "").trim();
        if (!query) {
            parent.textContent = text;
            return;
        }

        var lowerText = text.toLocaleLowerCase();
        var lowerQuery = query.toLocaleLowerCase();
        var cursor = 0;
        var index = lowerText.indexOf(lowerQuery);
        if (index < 0) {
            parent.textContent = text;
            return;
        }

        while (index >= 0) {
            if (index > cursor) parent.appendChild(document.createTextNode(text.substring(cursor, index)));
            var mark = document.createElement("mark");
            mark.className = "report-search-highlight";
            mark.textContent = text.substring(index, index + query.length);
            parent.appendChild(mark);
            cursor = index + query.length;
            index = lowerText.indexOf(lowerQuery, cursor);
        }
        if (cursor < text.length) parent.appendChild(document.createTextNode(text.substring(cursor)));
    }

    function debounce(callback, delay) {
        var timer;
        return function () {
            var context = this;
            var args = arguments;
            clearTimeout(timer);
            timer = setTimeout(function () { callback.apply(context, args); }, delay);
        };
    }

    function appendTextElement(parent, tag, value) {
        var element = document.createElement(tag);
        element.textContent = value;
        parent.appendChild(element);
        return element;
    }

    function appendEmptyRow(body, columns, text) {
        var row = document.createElement("tr"), cell = document.createElement("td");
        cell.colSpan = columns;
        cell.className = "report-empty";
        cell.textContent = text;
        row.appendChild(cell);
        body.appendChild(row);
    }

    function setTableError(id, columns, text) {
        var body = document.getElementById(id);
        body.textContent = "";
        appendEmptyRow(body, columns, text);
    }

    function emptyMessage(text) {
        var element = document.createElement("p");
        element.className = "report-empty";
        element.textContent = text;
        return element;
    }

    function svgElement(name, attributes) {
        var element = document.createElementNS("http://www.w3.org/2000/svg", name);
        Object.keys(attributes).forEach(function (key) { element.setAttribute(key, attributes[key]); });
        return element;
    }

    function setBusy(form, busy) {
        Array.prototype.forEach.call(form.querySelectorAll("button, input, select"), function (control) { control.disabled = busy; });
    }

    function beginCardLoading(source) {
        state.loadingCounts[source] = (state.loadingCounts[source] || 0) + 1;
        if (state.loadingCounts[source] > 1) return;
        Array.prototype.forEach.call(document.querySelectorAll('[data-loading-source="' + source + '"]'), function (card) {
            card.setAttribute("aria-busy", "true");
            var indicator = document.createElement("div");
            indicator.className = "report-loading-indicator";
            indicator.setAttribute("role", "status");
            var spinner = document.createElement("span");
            spinner.className = "report-loading-spinner";
            spinner.setAttribute("aria-hidden", "true");
            indicator.appendChild(spinner);
            appendTextElement(indicator, "span", "Loading data...");
            card.appendChild(indicator);
        });
    }

    function apiPost(path, data) {
        return fetch(path, {
            method: "POST",
            headers: {
                "Accept": "application/json",
                "Content-Type": "application/json",
                "Authorization": "Bearer " + state.token
            },
            body: JSON.stringify(data)
        }).then(readResponse).then(function (result) {
            if (!result.response.ok) throw reportApiError(result, path, "The field could not be saved.");
            return result.data;
        });
    }

    function endCardLoading(source) {
        state.loadingCounts[source] = Math.max(0, (state.loadingCounts[source] || 1) - 1);
        if (state.loadingCounts[source]) return;
        Array.prototype.forEach.call(document.querySelectorAll('[data-loading-source="' + source + '"]'), function (card) {
            card.removeAttribute("aria-busy");
            var indicator = card.querySelector(".report-loading-indicator");
            if (indicator) card.removeChild(indicator);
        });
    }

    function completeCardLoading(source, request) {
        return request.then(function (result) {
            endCardLoading(source);
            return result;
        }, function (failure) {
            endCardLoading(source);
            throw failure;
        });
    }

    function showMessage(text, isError) {
        message.textContent = text;
        message.className = "report-message" + (isError ? " is-error" : "");
        message.hidden = false;
    }

    function hideMessage() { message.hidden = true; }
}());
