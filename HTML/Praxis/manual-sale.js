(function () {
    "use strict";
    var form = document.getElementById("manual-sale-form");
    var status = document.getElementById("manual-status");
    var button = document.getElementById("manual-start");
    var agentInput = document.getElementById("manual-agent");
    var phoneInput = document.getElementById("manual-phone");
    var openLink = document.getElementById("manual-open");
    var customerQuery = document.getElementById("manual-customer-query");
    var customerResults = document.getElementById("manual-customer-results");
    var customerRows = document.getElementById("manual-customer-rows");
    var customerStatus = document.getElementById("manual-customer-status");
    var customerDetail = document.getElementById("manual-customer-detail");
    var selectedCustomer = null, searchVersion = 0, searchTimer = null, matches = [], matchOptions = [], activeMatch = -1;
    var token = "", options = null, pending = null, busy = false;
    try { token = sessionStorage.getItem("praxis.reportPortal.accessToken") || localStorage.getItem("praxis.reportPortal.persistentAccessToken") || ""; } catch (ignored) { }
    if (!token) { ByteForgePraxisAuth.redirect(); return; }
    if (window.ByteForgeAccount) window.ByteForgeAccount.attach(document.getElementById("session-status"), { getToken: function () { return token; }, onPasswordChanged: signOut });
    function api(path, body) {
        return fetch(path, {
            method: body ? "POST" : "GET", credentials: "same-origin", cache: "no-store",
            headers: { Authorization: "Bearer " + token, Accept: "application/json", "Content-Type": "application/json" },
            body: body ? JSON.stringify(body) : undefined
        }).then(function (response) {
            if (response.status === 401) { ByteForgePraxisAuth.redirect(); throw new Error("Sign in again to continue."); }
            return response.json().catch(function () { return {}; }).then(function (data) {
                if (!response.ok) {
                    var message = response.status === 403 ? "Your role does not grant Manual Sale access." : data.message || data.Message || data.error || "The request could not be completed. Retry with this page open.";
                    if (data.requestId) message += " Reference: " + data.requestId + ".";
                    var error = new Error(message);
                    error.status = response.status;
                    throw error;
                }
                return data;
            });
        });
    }
    // Build text nodes with mark elements; customer data never becomes HTML.
    function highlightMatch(cell, value, query, phone) {
        var lower = value.toLowerCase(), term = query.toLowerCase(), ranges = [], at = 0, found;
        while (term && (found = lower.indexOf(term, at)) >= 0) { ranges.push([found, found + term.length]); at = found + term.length; }
        if (!ranges.length && phone && /^[0-9+(). -]+$/.test(query)) {
            var digits = query.replace(/\D/g, ""), positions = [];
            if (digits.length === 11 && digits.charAt(0) === "1") digits = digits.substring(1);
            for (var i = 0; i < value.length; i++) if (/[0-9]/.test(value.charAt(i))) positions.push(i);
            found = value.replace(/\D/g, "").indexOf(digits);
            if (digits && found >= 0) ranges.push([positions[found], positions[found + digits.length - 1] + 1]);
        }
        at = 0;
        ranges.forEach(function (range) {
            var before = document.createElement("span"), mark = document.createElement("mark");
            before.textContent = value.substring(at, range[0]); mark.textContent = value.substring(range[0], range[1]);
            cell.appendChild(before); cell.appendChild(mark); at = range[1];
        });
        var rest = document.createElement("span"); rest.textContent = value.substring(at); cell.appendChild(rest);
    }
    function dateText(value) { return value ? String(value).replace("T", " ").substring(0, 16) : "None"; }
    // Render the selected customer's values as text within a structured summary.
    function renderCustomerCard(customer) {
        customerDetail.textContent = "";
        function add(parent, tag, className, text) {
            var element = document.createElement(tag);
            element.className = className || "";
            if (text !== undefined) element.textContent = text;
            parent.appendChild(element);
            return element;
        }
        function field(parent, label, value, className) {
            var group = add(parent, "div", className);
            add(group, "dt", "", label);
            add(group, "dd", "", value || "Not available");
        }
        var header = add(customerDetail, "div", "bf-customer-card-header");
        var identity = add(header, "div", "bf-customer-card-identity");
        add(identity, "h2", "bf-customer-card-name", customer.contact || "Unnamed customer");
        add(identity, "p", "bf-customer-card-company", customer.companyName || "Company not available");
        add(header, "span", "bf-customer-card-record", "Record " + customer.recordId);
        var contacts = add(customerDetail, "dl", "bf-customer-card-contacts");
        field(contacts, "Phone", formatPhoneNumber(customer.keyPhone || ""));
        field(contacts, "Email", customer.emailAddress || "No email provided");
        var sections = add(customerDetail, "div", "bf-customer-card-sections");
        var source = add(sections, "section", "bf-customer-card-section");
        add(source, "h3", "", "Source list");
        var sourceFields = add(source, "dl", "bf-customer-card-fields");
        field(sourceFields, "List", customer.listName);
        field(sourceFields, "Imported", customer.importDate ? String(customer.importDate).substring(0, 10) : "");
        field(sourceFields, "List ID", customer.listId, "bf-customer-card-list-id");
        var event = add(sections, "section", "bf-customer-card-section");
        add(event, "h3", "", "Event details");
        var eventFields = add(event, "dl", "bf-customer-card-fields");
        field(eventFields, "Event", customer.eventType);
        field(eventFields, "Event date", customer.eventDate ? String(customer.eventDate).substring(0, 10) : "");
        field(eventFields, "Event accurate", customer.eventAccurate);
        var booking = add(customerDetail, "div", "bf-customer-card-booking" + (customer.apptDateTime ? " has-booking" : ""));
        add(booking, "strong", "", customer.apptDateTime ? "Existing appointment" : "No appointment on this record");
        if (customer.apptDateTime) {
            add(booking, "p", "", dateText(customer.apptDateTime) + " " + (customer.timeZone || "") + " | " + (customer.advisorName || "No advisor"));
            add(booking, "p", "", "This record already has a booking. Check whether another appointment is needed before starting a new sale.");
        }
        var notes = add(customerDetail, "section", "bf-customer-card-notes");
        add(notes, "h3", "", "Previous notes (history only)");
        add(notes, "div", "bf-customer-card-note-text", customer.agentNotes || "No previous notes");
    }
    function closeCustomerLookup() {
        window.clearTimeout(searchTimer);
        searchVersion++;
        customerResults.hidden = true;
        customerResults.removeAttribute("aria-busy");
        customerQuery.setAttribute("aria-expanded", "false");
        customerQuery.removeAttribute("aria-activedescendant");
        activeMatch = -1;
    }
    function activateCustomer(index) {
        if (!matches.length) return;
        activeMatch = (index + matches.length) % matches.length;
        matchOptions.forEach(function (option, i) {
            option.setAttribute("aria-selected", i === activeMatch ? "true" : "false");
        });
        customerQuery.setAttribute("aria-activedescendant", matchOptions[activeMatch].id);
        matchOptions[activeMatch].scrollIntoView({ block: "nearest" });
    }
    function clearCustomer() {
        selectedCustomer = null;
        phoneInput.readOnly = false;
        customerDetail.hidden = true;
        customerDetail.textContent = "";
    }
    function chooseCustomer(customer) {
        if (busy || pending) return;
        closeCustomerLookup();
        selectedCustomer = customer;
        customerQuery.value = customer.contact || "Unnamed customer";
        customerQuery.setCustomValidity("");
        phoneInput.value = customer.keyPhone || "";
        formatCustomerPhone();
        phoneInput.readOnly = true;
        renderCustomerCard(customer);
        customerDetail.hidden = false;
        customerStatus.textContent = "Selected record " + customer.recordId + ". Customer and list/event details will be copied. Missing source fields remain blank.";
    }
    document.getElementById("manual-customer-new").addEventListener("click", function () {
        if (busy || pending) return;
        closeCustomerLookup();
        clearCustomer();
        customerQuery.value = "";
        customerQuery.setCustomValidity("");
        phoneInput.value = "";
        phoneInput.focus();
        customerStatus.textContent = "New customer: enter a phone number and complete the details in the entry form.";
    });
    function searchCustomers() {
        if (busy || pending) return;
        var query = customerQuery.value.trim(), version = ++searchVersion;
        customerRows.textContent = "";
        matches = []; matchOptions = []; activeMatch = -1;
        customerResults.removeAttribute("aria-busy");
        if (query.length < 2) { customerStatus.textContent = "Enter at least two characters to search."; return; }
        customerStatus.textContent = "Searching customers...";
        customerResults.setAttribute("aria-busy", "true");
        api("api/manual-sales/customers?query=" + encodeURIComponent(query)).then(function (rows) {
            if (version !== searchVersion) return;
            matches = rows;
            rows.forEach(function (customer, index) {
                var item = document.createElement("tr");
                item.className = "bf-manual-customer-result";
                item.id = "manual-customer-option-" + index;
                item.setAttribute("role", "option");
                item.setAttribute("aria-selected", "false");
                [String(customer.recordId), customer.contact || "Unnamed customer", customer.companyName || "", formatPhoneNumber(customer.keyPhone || ""), customer.emailAddress || ""].forEach(function (value, column) {
                    var cell = document.createElement("td");
                    highlightMatch(cell, value, query, column === 3);
                    item.appendChild(cell);
                });
                item.addEventListener("mousedown", function (event) { event.preventDefault(); });
                matchOptions.push(item);
                item.addEventListener("click", function () { chooseCustomer(customer); });
                customerRows.appendChild(item);
            });
            customerResults.hidden = !rows.length;
            customerQuery.setAttribute("aria-expanded", rows.length ? "true" : "false");
            customerStatus.textContent = rows.length ? "Choose a record below. " + (rows.length === 50 ? "Showing the first 50 matches; refine your search if needed." : rows.length + " matching records.") : "No matching customers. Try another search or choose New customer.";
        }).catch(function (error) { if (version === searchVersion) customerStatus.textContent = error.message; })
            .finally(function () { if (version === searchVersion) customerResults.removeAttribute("aria-busy"); });
    }
    customerQuery.addEventListener("input", function () {
        if (busy || pending) return;
        closeCustomerLookup();
        if (selectedCustomer) { clearCustomer(); phoneInput.value = ""; }
        var query = customerQuery.value.trim();
        customerQuery.setCustomValidity(query ? "Choose a customer from the results, or choose New customer." : "");
        if (query.length < 2) { customerStatus.textContent = "Type at least two characters to search customers."; return; }
        customerStatus.textContent = "Searching customers...";
        searchTimer = window.setTimeout(searchCustomers, 250);
    });
    customerQuery.addEventListener("focus", function () {
        if (!selectedCustomer && customerQuery.value.trim().length >= 2 && !pending && !busy) searchCustomers();
    });
    customerQuery.addEventListener("keydown", function (event) {
        if (event.key === "Escape") { event.preventDefault(); closeCustomerLookup(); return; }
        if (event.key === "Enter") {
            event.preventDefault();
            if (!customerResults.hidden && activeMatch >= 0) chooseCustomer(matches[activeMatch]);
            return;
        }
        if (!customerResults.hidden && matches.length && (event.key === "ArrowDown" || event.key === "ArrowUp")) {
            event.preventDefault();
            activateCustomer(event.key === "ArrowDown" ? activeMatch + 1 : (activeMatch < 0 ? matches.length - 1 : activeMatch - 1));
        }
    });
    customerQuery.addEventListener("blur", closeCustomerLookup);
    function label(agent) { return (agent.name || agent.agentId) + " (" + agent.agentId + ")"; }
    function loadOptions() {
        return api("api/manual-sales/options").then(function (data) {
            options = data;
            var list = agentInput;
            list.textContent = "";
            var agentPlaceholder = document.createElement("option"); agentPlaceholder.value = ""; agentPlaceholder.textContent = "Choose a booking agent"; list.appendChild(agentPlaceholder);
            data.agents.forEach(function (agent) { var item = document.createElement("option"); item.value = agent.agentId; item.textContent = label(agent); list.appendChild(item); });
            var select = document.getElementById("manual-service");
            select.textContent = "";
            var placeholder = document.createElement("option"); placeholder.value = ""; placeholder.textContent = "Choose a service"; select.appendChild(placeholder);
            data.services.forEach(function (service) { var item = document.createElement("option"); item.value = service; item.textContent = service; select.appendChild(item); });
            form.hidden = false;
            document.getElementById("session-status").hidden = false;
            status.textContent = "Choose the agent who should receive booking credit.";
        });
    }
    agentInput.addEventListener("input", function () { agentInput.setCustomValidity(""); });
    function formatCustomerPhone() {
        // Leave invalid/incomplete input intact so server validation cannot silently
        // turn letters, extensions, or an extra digit into a different phone number.
        var value = phoneInput.value.trim();
        var digits = value.replace(/\D/g, "");
        if (/^[0-9+().\s-]+$/.test(value) && /^(1)?[0-9]{10}$/.test(digits)) {
            phoneInput.value = formatPhoneNumber(value);
        }
    }
    phoneInput.addEventListener("blur", formatCustomerPhone);
    phoneInput.addEventListener("change", formatCustomerPhone);
    form.addEventListener("submit", function (event) {
        event.preventDefault();
        if (busy || !options) return;
        if (!pending) {
            var agent = options.agents.filter(function (item) { return item.agentId === agentInput.value; })[0];
            if (!agent) { agentInput.setCustomValidity("Choose an agent from the directory."); agentInput.reportValidity(); return; }
            if (!form.reportValidity()) return;
            formatCustomerPhone();
            pending = { requestId: options.requestId, agentId: agent.agentId, service: document.getElementById("manual-service").value, phone: phoneInput.value, sourceRecordId: selectedCustomer ? selectedCustomer.recordId : null };
        }
        busy = true;
        closeCustomerLookup();
        customerQuery.readOnly = true;
        button.disabled = true;
        form.setAttribute("aria-busy", "true");
        status.textContent = "Starting Manual Sale...";
        // Open synchronously to avoid popup blocking after the authenticated request.
        var popup = window.open("about:blank", "_blank");
        if (popup) popup.opener = null;
        api("api/manual-sales", pending).then(function (data) {
            openLink.href = data.workspaceUrl;
            openLink.hidden = false;
            if (popup) popup.location.replace(data.workspaceUrl);
            pending = null;
            form.reset();
            clearCustomer();
            customerQuery.setCustomValidity("");
            closeCustomerLookup();
            customerRows.textContent = "";
            customerResults.removeAttribute("aria-busy");
            customerStatus.textContent = "Type to search across all lists. The most recent record is shown for each name, or choose New customer.";
            options = null;
            return loadOptions().then(function () {
                status.textContent = "Manual Sale " + data.recordId + " created. Complete and save the appointment in the entry form.";
            });
        }).catch(function (error) {
            if (popup && !openLink.href) popup.close();
            else if (popup && popup.location.href === "about:blank") popup.close();
            // A definite validation rejection permits correction. An uncertain result retains
            // the exact request, so retry cannot create a second record.
            if (error.status === 400) pending = null;
            status.textContent = error.message + (pending ? " Retry will use the same launch details." : "");
        }).finally(function () { busy = false; customerQuery.readOnly = !!pending; button.disabled = false; form.removeAttribute("aria-busy"); });
    });
    function signOut() {
        token = "";
        try { sessionStorage.removeItem("praxis.reportPortal.accessToken"); localStorage.removeItem("praxis.reportPortal.persistentAccessToken"); } catch (ignored) { }
        fetch("api/config-session", { method: "DELETE", credentials: "same-origin" }).finally(function () { location.replace("login.html"); });
    }
    document.getElementById("manual-sign-out").addEventListener("click", signOut);
    Promise.all([ByteForgePraxisAdmin.load(token), loadOptions()]).catch(function (error) { form.hidden = true; status.textContent = error.message; });
}());
