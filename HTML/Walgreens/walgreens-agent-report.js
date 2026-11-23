(function () {
    "use strict";

    const report = window.walgreensAgentReportData || {};
    const agentRows = Array.isArray(report.agentRows) ? report.agentRows : [];
    const pageSize = 25;
    let currentPage = 1;
    let sort = { key: "optIns", direction: "desc" };
    const formatNumber = new Intl.NumberFormat("en-US");

    function safeDivide(numerator, denominator) {
        return denominator ? numerator / denominator : 0;
    }

    function escapeHtml(value) {
        return String(value ?? "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    function formatDurationSeconds(value) {
        const totalSeconds = Math.max(0, Math.round(Number(value) || 0));
        return `${Math.floor(totalSeconds / 60)}:${String(totalSeconds % 60).padStart(2, "0")}`;
    }

    function getContactsReached(row) {
        return (Number(row.optIns) || 0)
            + (Number(row.declines) || 0)
            + (Number(row.callbacks) || 0)
            + (Number(row.sendInfo) || 0)
            + (Number(row.sendVoucher) || 0);
    }

    function getAgentMetrics(row) {
        const hours = Number(row.hours) || 0;
        const contacts = getContactsReached(row);
        return {
            ...row,
            hours,
            contacts,
            optInsPerHour: safeDivide(row.optIns, hours),
            contactsPerHour: safeDivide(contacts, hours),
            attemptsPerHour: safeDivide(row.dials, hours),
            conversion: Number(row.conversion) || 0
        };
    }

    function compareRows(left, right, key) {
        const a = left[key];
        const b = right[key];
        if (typeof a === "number" || typeof b === "number") {
            return (Number(a) || 0) - (Number(b) || 0);
        }
        return String(a ?? "").localeCompare(String(b ?? ""), undefined, { sensitivity: "base", numeric: true });
    }

    function getVisibleRows() {
        const search = document.getElementById("agentSearch").value.trim().toLowerCase();
        return agentRows
            .map(getAgentMetrics)
            .filter(row => !search || Object.values(row).some(value => String(value).toLowerCase().includes(search)))
            .sort((left, right) => compareRows(left, right, sort.key) * (sort.direction === "asc" ? 1 : -1));
    }

    function niceMaximum(value, minimum, step) {
        return Math.max(minimum, Math.ceil(value / step) * step);
    }

    function formatAxisValue(value) {
        return Number(value).toFixed(value < 10 ? 1 : 0);
    }

    function renderPerformanceMatrix() {
        const chart = document.getElementById("agentPerformanceMatrix");
        const empty = document.getElementById("agentMatrixEmpty");
        const rows = agentRows.map(getAgentMetrics)
            .filter(row => row.hours > 0 && Number.isFinite(row.contactsPerHour) && Number.isFinite(row.conversion));

        if (!rows.length) {
            chart.innerHTML = "";
            chart.style.display = "none";
            empty.style.display = "block";
            return;
        }

        chart.style.display = "block";
        empty.style.display = "none";

        const width = 1000;
        const height = 430;
        const margin = { top: 30, right: 38, bottom: 58, left: 72 };
        const plotWidth = width - margin.left - margin.right;
        const plotHeight = height - margin.top - margin.bottom;
        const totalHours = rows.reduce((sum, row) => sum + row.hours, 0);
        const totalContacts = rows.reduce((sum, row) => sum + row.contacts, 0);
        const totalOptIns = rows.reduce((sum, row) => sum + (Number(row.optIns) || 0), 0);
        const averageContactsPerHour = safeDivide(totalContacts, totalHours);
        const averageConversion = safeDivide(totalOptIns * 100, totalContacts);
        const maxContactsPerHour = niceMaximum(Math.max(...rows.map(row => row.contactsPerHour), averageContactsPerHour) * 1.08, 1, 1);
        const maxConversion = niceMaximum(Math.max(...rows.map(row => row.conversion), averageConversion) * 1.05, 20, 10);
        const yMaximum = Math.min(100, maxConversion);
        const maxHours = Math.max(...rows.map(row => row.hours), 1);
        const x = value => margin.left + Math.min(value, maxContactsPerHour) / maxContactsPerHour * plotWidth;
        const y = value => margin.top + plotHeight - Math.min(value, yMaximum) / yMaximum * plotHeight;
        const bubbleRadius = hours => 7 + Math.sqrt(hours / maxHours) * 15;
        const xTicks = 5;
        const yTicks = 5;
        const grid = [];

        for (let index = 0; index <= xTicks; index++) {
            const value = maxContactsPerHour * index / xTicks;
            const position = x(value);
            grid.push(`<line class="agent-matrix-gridline" x1="${position}" y1="${margin.top}" x2="${position}" y2="${margin.top + plotHeight}"></line>`);
            grid.push(`<text class="agent-matrix-tick" x="${position}" y="${height - 31}" text-anchor="middle">${formatAxisValue(value)}</text>`);
        }
        for (let index = 0; index <= yTicks; index++) {
            const value = yMaximum * index / yTicks;
            const position = y(value);
            grid.push(`<line class="agent-matrix-gridline" x1="${margin.left}" y1="${position}" x2="${margin.left + plotWidth}" y2="${position}"></line>`);
            grid.push(`<text class="agent-matrix-tick" x="${margin.left - 12}" y="${position + 4}" text-anchor="end">${formatAxisValue(value)}%</text>`);
        }

        const points = rows.map((row, index) => {
            const cx = x(row.contactsPerHour);
            const cy = y(row.conversion);
            const radius = bubbleRadius(row.hours);
            const lowVolume = row.contacts < 5;
            const labelY = Math.max(margin.top + 11, cy - radius - 6);
            return `<g class="agent-matrix-point${lowVolume ? " is-low-volume" : ""}">
                <circle tabindex="0" role="img" aria-label="${escapeHtml(row.agent)}, ${row.contactsPerHour.toFixed(2)} contacts per hour, ${row.conversion.toFixed(1)} percent conversion, ${row.hours.toFixed(1)} hours"
                    data-agent-matrix-index="${index}" cx="${cx}" cy="${cy}" r="${radius}"></circle>
                <text class="agent-matrix-agent-label" x="${cx}" y="${labelY}" text-anchor="middle">${escapeHtml(row.agent)}</text>
            </g>`;
        }).join("");

        const averageX = x(averageContactsPerHour);
        const averageY = y(averageConversion);
        chart.innerHTML = `<svg class="agent-matrix-svg" viewBox="0 0 ${width} ${height}" preserveAspectRatio="xMidYMid meet" role="group" aria-labelledby="agentPerformanceTitle agentPerformanceNote">
            <g>${grid.join("")}</g>
            <line class="agent-matrix-average-line" x1="${averageX}" y1="${margin.top}" x2="${averageX}" y2="${margin.top + plotHeight}"></line>
            <line class="agent-matrix-average-line" x1="${margin.left}" y1="${averageY}" x2="${margin.left + plotWidth}" y2="${averageY}"></line>
            <text class="agent-matrix-average-label" x="${averageX + 8}" y="${margin.top + 15}">Team avg ${averageContactsPerHour.toFixed(2)} contacts/hr</text>
            <text class="agent-matrix-average-label" x="${margin.left + plotWidth - 8}" y="${averageY - 8}" text-anchor="end">Team avg ${averageConversion.toFixed(1)}% conversion</text>
            <text class="agent-matrix-quadrant-label" x="${margin.left + 12}" y="${margin.top + 20}">Effective / lower activity</text>
            <text class="agent-matrix-quadrant-label" x="${margin.left + plotWidth - 12}" y="${margin.top + 20}" text-anchor="end">High performance</text>
            <text class="agent-matrix-quadrant-label" x="${margin.left + 12}" y="${margin.top + plotHeight - 12}">Needs investigation</text>
            <text class="agent-matrix-quadrant-label" x="${margin.left + plotWidth - 12}" y="${margin.top + plotHeight - 12}" text-anchor="end">Productive / coaching opportunity</text>
            ${points}
            <text class="agent-matrix-axis-title" x="${margin.left + plotWidth / 2}" y="${height - 6}" text-anchor="middle">Contacts per hour</text>
            <text class="agent-matrix-axis-title" transform="translate(17 ${margin.top + plotHeight / 2}) rotate(-90)" text-anchor="middle">Conversion</text>
        </svg>`;

        const tooltip = document.getElementById("agentMatrixTooltip");
        const positionTooltip = (clientX, clientY) => {
            tooltip.style.left = `${Math.max(8, Math.min(clientX + 14, window.innerWidth - tooltip.offsetWidth - 12))}px`;
            tooltip.style.top = `${Math.max(8, Math.min(clientY + 14, window.innerHeight - tooltip.offsetHeight - 12))}px`;
        };
        const showTooltip = (element, event) => {
            const row = rows[Number(element.getAttribute("data-agent-matrix-index"))];
            if (!row) return;
            tooltip.innerHTML = `<div class="chart-tooltip-title">${escapeHtml(row.agent)}</div>
                <div class="chart-tooltip-row"><span>Contacts per hour</span><strong>${row.contactsPerHour.toFixed(2)}</strong></div>
                <div class="chart-tooltip-row"><span>Conversion</span><strong>${row.conversion.toFixed(1)}%</strong></div>
                <div class="chart-tooltip-row"><span>Hours worked</span><strong>${row.hours.toFixed(1)}</strong></div>
                <div class="chart-tooltip-divider"></div>
                <div class="chart-tooltip-row"><span>Reached contacts</span><strong>${formatNumber.format(row.contacts)}</strong></div>
                <div class="chart-tooltip-row"><span>Opt-ins</span><strong>${formatNumber.format(row.optIns)}</strong></div>
                <div class="chart-tooltip-row"><span>Dial attempts</span><strong>${formatNumber.format(row.dials)}</strong></div>
                ${row.contacts < 5 ? `<div class="chart-tooltip-warning">Low volume: interpret this conversion cautiously.</div>` : ""}`;
            tooltip.style.display = "block";
            if (event && Number.isFinite(event.clientX)) {
                positionTooltip(event.clientX, event.clientY);
            } else {
                const bounds = element.getBoundingClientRect();
                positionTooltip(bounds.left + bounds.width / 2, bounds.top + bounds.height / 2);
            }
        };
        const hideTooltip = () => { tooltip.style.display = "none"; };
        chart.querySelectorAll("[data-agent-matrix-index]").forEach(element => {
            element.addEventListener("mouseenter", event => showTooltip(element, event));
            element.addEventListener("mousemove", event => positionTooltip(event.clientX, event.clientY));
            element.addEventListener("mouseleave", hideTooltip);
            element.addEventListener("focus", () => showTooltip(element));
            element.addEventListener("blur", hideTooltip);
        });
    }

    function render() {
        window.ByteForgeSortIndicator.update("[data-agent-sort]", "data-agent-sort", sort);
        const rows = getVisibleRows();
        currentPage = Math.min(currentPage, Math.max(1, Math.ceil(rows.length / pageSize)));
        const startIndex = (currentPage - 1) * pageSize;
        document.getElementById("agentTable").innerHTML = rows.slice(startIndex, startIndex + pageSize).map(row => `
            <tr>
                <td>${escapeHtml(row.agent)}</td>
                <td class="numeric">${formatNumber.format(row.optIns)}</td>
                <td class="numeric">${formatNumber.format(row.declines)}</td>
                <td class="numeric">${formatNumber.format(row.callbacks)}</td>
                <td class="numeric">${formatNumber.format(row.unworkable)}</td>
                <td class="numeric">${formatNumber.format(row.dials)}</td>
                <td class="numeric">${Number(row.hours).toFixed(1)}</td>
                <td class="numeric">${row.optInsPerHour.toFixed(2)}</td>
                <td class="numeric">${row.contactsPerHour.toFixed(2)}</td>
                <td class="numeric">${row.attemptsPerHour.toFixed(2)}</td>
                <td class="numeric">${Number(row.conversion).toFixed(1)}%</td>
                <td class="numeric" title="${Number(row.averageAfterCallWorkSeconds).toFixed(1)} seconds">${formatDurationSeconds(row.averageAfterCallWorkSeconds)}</td>
            </tr>`).join("");
        document.getElementById("agentEmpty").style.display = rows.length ? "none" : "block";
        currentPage = WalgreensAdminListTools.renderPagination({
            summaryElement: document.getElementById("agentPageMeta"),
            buttonsElement: document.getElementById("agentPagination"),
            totalRows: rows.length,
            pageSize,
            currentPage,
            onPageChange: page => {
                currentPage = page;
                render();
            }
        });
    }

    function applyDateRange() {
        const startDate = document.getElementById("startDate").value;
        const endDate = document.getElementById("endDate").value;
        if (!startDate || !endDate || endDate < startDate) {
            window.alert("Choose a valid report date range.");
            return;
        }
        const url = new URL(window.location.href);
        url.searchParams.set("startDate", startDate);
        url.searchParams.set("endDate", endDate);
        if (document.getElementById("reportTeam")) url.searchParams.set("team", document.getElementById("reportTeam").value);
        window.location.assign(url.toString());
    }

    document.getElementById("reportRange").addEventListener("change", event => {
        const value = event.target.value;
        if (value === "custom") return;
        const today = new Date();
        const end = new Date(today.getFullYear(), today.getMonth(), today.getDate());
        const start = new Date(end);
        if (value === "yesterday") {
            start.setDate(start.getDate() - 1);
            end.setDate(end.getDate() - 1);
        } else if (value === "last7") {
            start.setDate(start.getDate() - 6);
        } else if (value === "last30") {
            start.setDate(start.getDate() - 29);
        }
        const toIsoDate = date => `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
        document.getElementById("startDate").value = toIsoDate(start);
        document.getElementById("endDate").value = toIsoDate(end);
    });
    document.getElementById("applyFiltersButton").addEventListener("click", applyDateRange);
    document.getElementById("agentSearch").addEventListener("input", () => {
        currentPage = 1;
        render();
    });
    document.querySelectorAll("[data-agent-sort]").forEach(button => {
        button.addEventListener("click", () => {
            const key = button.getAttribute("data-agent-sort");
            sort = { key, direction: sort.key === key && sort.direction === "asc" ? "desc" : "asc" };
            currentPage = 1;
            render();
        });
    });
    document.getElementById("exportAgentWorkbookButton").addEventListener("click", event => {
        const url = new URL(event.currentTarget.dataset.exportUrl, window.location.origin);
        url.searchParams.set("startDate", document.getElementById("startDate").value);
        url.searchParams.set("endDate", document.getElementById("endDate").value);
        if (document.getElementById("reportTeam")) url.searchParams.set("team", document.getElementById("reportTeam").value);
        window.location.assign(url.toString());
    });
    document.getElementById("printAgentReportButton").addEventListener("click", () => window.print());
    renderPerformanceMatrix();
    render();
}());
