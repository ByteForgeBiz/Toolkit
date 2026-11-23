
    (function () {
        "use strict";

        var panel = document.querySelector(".opt-in-review-panel");
        var listUrl = panel.getAttribute("data-page-url");
        var listRequestNumber = 0;
        var listAbort = null;
        var rows = Array.prototype.slice.call(document.querySelectorAll("[data-opt-in-row]"));
        var tableBody = document.getElementById("optInTableBody");
        var searchInput = document.getElementById("optInSearch");
        var pageSizeInput = document.getElementById("optInPageSize");
        var pageSummary = document.getElementById("optInPageSummary");
        var visibleCount = document.getElementById("optInVisibleCount");
        var emptyState = document.getElementById("optInEmpty");
        var pagination = document.getElementById("optInPagination");
        var completedToggle = document.getElementById("optInCompletedToggle");
        var completedToggleLabel = document.getElementById("optInCompletedToggleLabel");
        var listHeading = document.getElementById("optInListHeading");
        var settingsToggle = document.getElementById("optInSettingsToggle");
        var settingsPanel = document.getElementById("optInSettings");
        var workspace = document.querySelector(".opt-in-review-workspace");
        var splitter = document.getElementById("optInSplitter");
        var detailEmpty = document.getElementById("optInDetailEmpty");
        var detailLoading = document.getElementById("optInDetailLoading");
        var detailError = document.getElementById("optInDetailError");
        var detailErrorMessage = document.getElementById("optInDetailErrorMessage");
        var detail = document.getElementById("optInDetail");
        var detailSections = document.getElementById("optInDetailSections");
        var recordTitle = document.getElementById("optInRecordTitle");
        var recordMeta = document.getElementById("optInRecordMeta");
        var recordStatus = document.getElementById("optInRecordStatus");
        var retryButton = document.getElementById("optInRetry");
        var currentPage = 1;
        var currentRow = null;
        var currentRecord = null;
        var currentRequestNumber = 0;
        var sort = { key: "created", direction: "desc" };
        var showCompleted = false;
        var antiForgeryToken = document.querySelector('input[name="__RequestVerificationToken"]');
        var submittedUrl = panel.getAttribute("data-submitted-url");
        var completedUrl = panel.getAttribute("data-completed-url");
        var fieldUpdateUrl = panel.getAttribute("data-field-url");

        function createRow(entry) {
            var row = document.createElement("tr");
            row.setAttribute("data-opt-in-row", "");
            row.setAttribute("data-record-url", entry.recordUrl);
            ["id", "company", "contact", "agent", "status", "created", "submittedtime"].forEach(function (key) {
                row.setAttribute("data-" + key, entry[key]);
            });
            row.setAttribute("data-submitted", entry.submitted ? "1" : "0");
            var selected = currentRow && currentRow.getAttribute("data-id") === String(entry.id);
            row.setAttribute("aria-selected", selected ? "true" : "false");
            row.classList.toggle("is-selected", !!selected);
            row.innerHTML = '<td class="opt-in-checkbox-cell" data-opt-in-submitted-cell><input type="checkbox" class="opt-in-submitted-checkbox" data-mark-submitted></td>' +
                '<td><code></code></td><td data-opt-in-company-cell><button type="button" class="opt-in-select-action" data-opt-in-select></button></td>' +
                '<td data-opt-in-contact-cell></td><td></td><td data-opt-in-status-cell><span class="status-pill submission-status"></span></td><td data-opt-in-submitted-time></td>';
            var checkbox = row.querySelector("input");
            checkbox.setAttribute("data-id", entry.id);
            checkbox.checked = entry.submitted;
            checkbox.disabled = entry.submitted || !entry.canMarkSubmitted;
            var label = entry.submitted ? "Submitted to Walgreens" : entry.status.toLowerCase() === "completed" ? "Completed without Walgreens submission" : "Mark Opt-In " + entry.id + " submitted to Walgreens";
            checkbox.setAttribute("aria-label", label);
            checkbox.title = label;
            row.querySelector("code").textContent = entry.id;
            row.querySelector("button").textContent = entry.company || "Company unavailable";
            row.querySelector("[data-opt-in-contact-cell]").textContent = entry.contact || "(blank)";
            row.children[4].textContent = entry.createdLabel || formatTableDate(entry.created);
            var status = row.querySelector("[data-opt-in-status-cell] span");
            status.textContent = entry.status;
            status.className += " submission-status-" + entry.status.toLowerCase().replace(/ /g, "-");
            row.querySelector("[data-opt-in-submitted-time]").textContent = entry.submittedLabel || (entry.submittedtime ? formatTableDate(entry.submittedtime) : "\u2014");
            bindRow(row);
            return row;
        }

        function renderTable() {
            var requestNumber = ++listRequestNumber;
            if (listAbort) listAbort.abort();
            listAbort = new AbortController();
            var query = new URLSearchParams({ search: searchInput.value.trim(), showCompleted: String(showCompleted),
                sort: sort.key, direction: sort.direction, page: String(currentPage), pageSize: pageSizeInput.value });
            tableBody.setAttribute("aria-busy", "true");
            tableBody.textContent = "";
            rows = [];
            emptyState.hidden = false;
            emptyState.textContent = "Loading Opt-Ins...";
            return fetch(listUrl + "?" + query.toString(), { credentials: "same-origin", signal: listAbort.signal,
                headers: { "Accept": "application/json" } }).then(function (response) {
                if (!response.ok) throw new Error("The Opt-In list could not be loaded.");
                return response.json();
            }).then(function (payload) {
                if (requestNumber !== listRequestNumber) return;
                if (!payload.ok) throw new Error("The Opt-In list could not be loaded.");
                currentPage = payload.page;
                pageSizeInput.value = String(payload.pageSize);
                tableBody.textContent = "";
                rows = payload.rows.map(createRow);
                rows.forEach(function (row) { tableBody.appendChild(row); });
                visibleCount.textContent = payload.totalRows;
                emptyState.hidden = payload.totalRows !== 0;
                emptyState.textContent = showCompleted ? "No Opt-Ins match the current search." : "No open Opt-Ins match the current search.";
                currentPage = WalgreensAdminListTools.renderPagination({ summaryElement: pageSummary, buttonsElement: pagination,
                    totalRows: payload.totalRows, pageSize: payload.pageSize, currentPage: currentPage,
                    onPageChange: function (target) { currentPage = target; renderTable(); } });
                updateSortIndicators();
            }).catch(function (error) {
                if (requestNumber !== listRequestNumber || error.name === "AbortError") return;
                emptyState.hidden = false;
                emptyState.textContent = "The Opt-In list could not be loaded. Change a filter or search to retry.";
            }).finally(function () {
                if (requestNumber === listRequestNumber) tableBody.setAttribute("aria-busy", "false");
            });
        }

        function updateSortIndicators() {
            window.ByteForgeSortIndicator.update("[data-opt-in-sort]", "data-opt-in-sort", sort);
        }

        function parseDate(value) {
            if (!value) return null;
            var match = /\/Date\((-?\d+)/.exec(String(value));
            var date = match ? new Date(Number(match[1])) : new Date(value);
            return Number.isNaN(date.getTime()) ? null : date;
        }

        function formatDate(value) {
            var date = parseDate(value);
            return date ? date.toLocaleDateString(undefined, {
                year: "numeric",
                month: "short",
                day: "numeric"
            }) : "";
        }

        function formatDateTime(value) {
            var date = parseDate(value);
            return date ? date.toLocaleString(undefined, {
                year: "numeric",
                month: "short",
                day: "numeric",
                hour: "numeric",
                minute: "2-digit"
            }) : "";
        }

        function formatTableDate(value) {
            var date = parseDate(value);
            if (!date) return "\u2014";
            return String(date.getMonth() + 1).padStart(2, "0") + "/" +
                String(date.getDate()).padStart(2, "0") + "/" + date.getFullYear();
        }

        function displayValue(value, type) {
            if (type === "boolean") {
                return value === true ? "Yes" : value === false ? "No" : "(blank)";
            }
            if (type === "date") {
                return formatDate(value) || "(blank)";
            }
            if (Array.isArray(value)) {
                return value.length ? value.join(", ") : "(blank)";
            }
            if (value === null || value === undefined || String(value).trim() === "") {
                return "(blank)";
            }
            return String(value);
        }

        function phoneValue(number, extension) {
            var parts = [];
            if (number) parts.push(number);
            if (extension) parts.push("ext. " + extension);
            return parts.join(" ");
        }

        function formatPhone(value) {
            var digits = (value || "").replace(/\D/g, "").slice(0, 10);
            if (digits.length <= 3) {
                return digits;
            }
            if (digits.length <= 6) {
                return "(" + digits.slice(0, 3) + ") " + digits.slice(3);
            }
            return "(" + digits.slice(0, 3) + ") " + digits.slice(3, 6) + "-" + digits.slice(6);
        }

        function bindPhoneFormatter(field) {
            if (!field || field.getAttribute("data-phone-formatter") === "bound") return;
            field.setAttribute("data-phone-formatter", "bound");
            field.addEventListener("input", function () {
                field.value = formatPhone(field.value);
            });
            field.value = formatPhone(field.value);
        }

        function vaccinationValue(record) {
            return (record.Vaccinations || []).map(function (value) {
                return value === "Other" && record.OtherVaccination
                    ? "Other: " + record.OtherVaccination
                    : value;
            });
        }

        var programOptions = ["Voucher Program Only", "Clinic", "Clinic + Vouchers"];
        var industryOptions = [
            "Agriculture, Forestry, Fishing and Hunting", "Construction", "Education Services",
            "Finance, Insurance and Information", "Food Services, Accommodation and Entertainment",
            "Health Care and Social Assistance", "Manufacturing", "Mining, Quarrying, Oil and Gas Extraction",
            "Professional, Scientific and Technical Services", "Public Administration",
            "Religious Organizations and Community Groups", "Retail and Wholesale Trade",
            "Transportation and Warehousing", "Other"
        ];
        var stateOptions = [
            "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "DC", "FL", "GA", "HI", "ID", "IL",
            "IN", "IA", "KS", "KY", "LA", "ME", "MD", "MA", "MI", "MN", "MS", "MO", "MT", "NE",
            "NV", "NH", "NJ", "NM", "NY", "NC", "ND", "OH", "OK", "OR", "PA", "RI", "SC", "SD",
            "TN", "TX", "UT", "VT", "VA", "WA", "WV", "WI", "WY"
        ];
        var vaccinationOptions = [
            "Flu Trivalent", "Flu HD (65+)", "COVID19", "Pneumonia",
            "Tdap (Whooping Cough)", "Shingles", "RSV", "Other"
        ];
        var paymentOptions = ["Insurance", "Cash", "Direct Bill"];
        var yesNoOptions = [
            { value: "true", label: "Yes" },
            { value: "false", label: "No" }
        ];

        function field(label, property, value, editor, options, locationOrder, secondaryValue) {
            return {
                label: label,
                property: property,
                rawValue: value,
                value: editor === "phone"
                    ? displayValue(phoneValue(value, secondaryValue))
                    : displayValue(value, editor === "date" ? "date" : editor === "boolean" ? "boolean" : null),
                editor: editor,
                options: options || [],
                locationOrder: locationOrder || null,
                secondaryValue: secondaryValue
            };
        }

        function buildSections(record) {
            var sections = [
                {
                    title: "1. Program and organization",
                    fields: [
                        field("Program Type", "ProgramInterest", record.ProgramInterest, "select", programOptions),
                        field("Source Employee Count", "PaydayPerxLeadEmployeeCount", record.PaydayPerxLeadEmployeeCount, "text"),
                        field("Estimated Number of Participants", "EstimatedParticipants", record.EstimatedParticipants, "number"),
                        field("Company", "Company", record.Company, "text"),
                        field("Primary Employee Medical Insurance", "PrimaryEmployeeMedicalInsurance", record.PrimaryEmployeeMedicalInsurance, "text"),
                        field("Employee Pharmacy Insurance", "EmployeePharmacyInsurance", record.EmployeePharmacyInsurance, "text"),
                        field("Current/Past Walgreens Contact", "CurrentPastWalgreensContact", record.CurrentPastWalgreensContact, "text"),
                        field("Industry", "Industry", record.Industry, "select", industryOptions)
                    ]
                },
                {
                    title: "2. Primary location and contact",
                    fields: [
                        field("Address 1", "Address1", record.Address1, "text"),
                        field("Address 2", "Address2", record.Address2, "text"),
                        field("City", "City", record.City, "text"),
                        field("State", "State", record.State, "select", stateOptions),
                        field("ZIP", "ZipCode", record.ZipCode, "text"),
                        field("First Name", "FirstName", record.FirstName, "text"),
                        field("Last Name", "LastName", record.LastName, "text"),
                        field("Title", "MainContactJobTitle", record.MainContactJobTitle, "text"),
                        field("Email", "Email", record.Email, "email"),
                        field("Phone", "Phone", record.PhoneNumber, "phone", null, null, record.Extension),
                        field("Preferred Clinic Date", "PreferredClinicDate", record.PreferredClinicDate, "date"),
                        field("Alternate Clinic Date", "AlternateClinicDate", record.AlternateClinicDate, "date"),
                        field("Estimated Number of Clinics", "EstimatedClinics", record.EstimatedClinics, "number")
                    ]
                },
                {
                    title: "3. Alternate contact",
                    fields: [
                        field("Alternate First Name", "AlternateFirstName", record.AlternateFirstName, "text"),
                        field("Alternate Last Name", "AlternateLastName", record.AlternateLastName, "text"),
                        field("Alternate Title", "AlternateContactJobTitle", record.AlternateContactJobTitle, "text"),
                        field("Alternate Email", "AlternateContactEmail", record.AlternateContactEmail, "email"),
                        field("Alternate Phone", "AlternatePhone", record.AlternateContactPhoneNumber, "phone", null, null, record.AlternateContactExtension)
                    ]
                }
            ];

            (record.AdditionalLocations || []).forEach(function (location, index) {
                sections.push({
                    title: (index + 4) + ". Additional Location " + (index + 1),
                    fields: [
                        field("Address 1", "AdditionalLocation.Address1", location.Address1, "text", null, location.LocationOrder),
                        field("Address 2", "AdditionalLocation.Address2", location.Address2, "text", null, location.LocationOrder),
                        field("City", "AdditionalLocation.City", location.City, "text", null, location.LocationOrder),
                        field("State", "AdditionalLocation.State", location.State, "select", stateOptions, location.LocationOrder),
                        field("ZIP", "AdditionalLocation.ZipCode", location.ZipCode, "text", null, location.LocationOrder),
                        field("First Name", "AdditionalLocation.FirstName", location.FirstName, "text", null, location.LocationOrder),
                        field("Last Name", "AdditionalLocation.LastName", location.LastName, "text", null, location.LocationOrder),
                        field("Title", "AdditionalLocation.MainContactJobTitle", location.MainContactJobTitle, "text", null, location.LocationOrder),
                        field("Email", "AdditionalLocation.Email", location.Email, "email", null, location.LocationOrder),
                        field("Phone", "AdditionalLocation.Phone", location.PhoneNumber, "phone", null, location.LocationOrder, location.Extension),
                        field("Preferred Clinic Date", "AdditionalLocation.PreferredClinicDate", location.PreferredClinicDate, "date", null, location.LocationOrder),
                        field("Alternate Clinic Date", "AdditionalLocation.AlternateClinicDate", location.AlternateClinicDate, "date", null, location.LocationOrder)
                    ]
                });
            });

            sections.push({
                title: (sections.length + 1) + ". Clinic details",
                fields: [
                    field("Vaccinations Requested", "Vaccinations", record.Vaccinations, "multiselect", vaccinationOptions),
                    field("Other Vaccination", "OtherVaccination", record.OtherVaccination, "text"),
                    field("Employees in Multiple States", "MultipleStates", record.MultipleStates, "boolean", yesNoOptions),
                    field("Previous Walgreens Clinic", "PreviousWalgreensClinic", record.PreviousWalgreensClinic, "boolean", yesNoOptions),
                    field("Payment Types", "PaymentTypes", record.PaymentTypes, "multiselect", paymentOptions),
                    field("Notes", "Notes", record.Notes, "textarea")
                ]
            });
            sections.push({
                title: (sections.length + 1) + ". Portal record",
                fields: [
                    field("Public Tracking ID", null, record.PublicTrackingId),
                    field("Submitted to Walgreens", null, formatDateTime(record.AutomationCompletedAtUtc)),
                    field("Walgreens Confirmation", null, record.WalgreensConfirmation)
                ]
            });
            return sections;
        }

        function formatDateInput(value) {
            var date = parseDate(value);
            if (!date) return "";
            return date.getFullYear() + "-" + String(date.getMonth() + 1).padStart(2, "0") + "-" +
                String(date.getDate()).padStart(2, "0");
        }

        function createEditor(item) {
            if (item.editor === "textarea") {
                var textarea = document.createElement("textarea");
                textarea.rows = 4;
                textarea.maxLength = 4000;
                textarea.value = item.rawValue || "";
                return textarea;
            }
            if (item.editor === "multiselect") {
                var choices = document.createElement("fieldset");
                choices.className = "opt-in-inline-choices";
                var selected = Array.isArray(item.rawValue) ? item.rawValue : [];
                item.options.forEach(function (option) {
                    var label = document.createElement("label");
                    var checkbox = document.createElement("input");
                    checkbox.type = "checkbox";
                    checkbox.value = option;
                    checkbox.checked = selected.indexOf(option) >= 0;
                    label.appendChild(checkbox);
                    label.appendChild(document.createTextNode(" " + option));
                    choices.appendChild(label);
                });
                return choices;
            }
            if (item.editor === "phone") {
                var phoneEditor = document.createElement("div");
                phoneEditor.className = "opt-in-inline-phone";
                var phone = document.createElement("input");
                phone.type = "tel";
                phone.maxLength = 14;
                phone.placeholder = "Phone";
                phone.value = item.rawValue || "";
                phone.setAttribute("data-primary-value", "");
                bindPhoneFormatter(phone);
                var extension = document.createElement("input");
                extension.maxLength = 5;
                extension.placeholder = "Ext";
                extension.value = item.secondaryValue || "";
                extension.setAttribute("data-secondary-value", "");
                phoneEditor.appendChild(phone);
                phoneEditor.appendChild(extension);
                return phoneEditor;
            }
            if (item.editor === "select" || item.editor === "boolean") {
                var select = document.createElement("select");
                var emptyOption = document.createElement("option");
                emptyOption.value = "";
                emptyOption.textContent = "Select";
                select.appendChild(emptyOption);
                item.options.forEach(function (option) {
                    var optionValue = typeof option === "string" ? option : option.value;
                    var optionLabel = typeof option === "string" ? option : option.label;
                    var element = document.createElement("option");
                    element.value = optionValue;
                    element.textContent = optionLabel;
                    element.selected = String(item.rawValue).toLowerCase() === String(optionValue).toLowerCase();
                    select.appendChild(element);
                });
                return select;
            }
            var input = document.createElement("input");
            input.type = item.editor === "date" ? "date" : item.editor === "number" ? "number" : item.editor === "email" ? "email" : "text";
            input.value = item.editor === "date" ? formatDateInput(item.rawValue) : item.rawValue || "";
            if (item.editor === "number") {
                input.min = "0";
                input.step = "1";
            }
            return input;
        }

        function editorValues(editor, item) {
            if (item.editor === "multiselect") {
                return {
                    value: Array.prototype.filter.call(editor.querySelectorAll("input:checked"), function () { return true; })
                        .map(function (input) { return input.value; }).join("|")
                };
            }
            if (item.editor === "phone") {
                return {
                    value: editor.querySelector("[data-primary-value]").value,
                    secondaryValue: editor.querySelector("[data-secondary-value]").value
                };
            }
            return { value: editor.value };
        }

        function showEditWarning(actions, title, message) {
            var priorWarning = actions.querySelector("[data-edit-warning]");
            if (priorWarning) priorWarning.remove();
            var warningId = "optInEditWarning" + Date.now();
            var warning = document.createElement("span");
            warning.className = "opt-in-edit-warning-wrap";
            warning.setAttribute("data-edit-warning", "");
            var button = document.createElement("button");
            button.type = "button";
            button.className = "opt-in-edit-icon opt-in-edit-warning";
            button.innerHTML = "&#9888;&#65039;";
            button.setAttribute("aria-label", "Why this field was not saved");
            button.setAttribute("aria-expanded", "false");
            button.setAttribute("aria-controls", warningId);
            var tooltip = document.createElement("span");
            tooltip.id = warningId;
            tooltip.className = "opt-in-edit-warning-tooltip";
            tooltip.setAttribute("role", "tooltip");
            tooltip.hidden = true;
            var heading = document.createElement("strong");
            heading.textContent = title || "Field wasn't saved";
            var explanation = document.createElement("span");
            explanation.textContent = message || "Review the value and try again.";
            tooltip.appendChild(heading);
            tooltip.appendChild(explanation);
            function setWarningOpen(open) {
                tooltip.hidden = !open;
                button.setAttribute("aria-expanded", open ? "true" : "false");
            }
            button.addEventListener("click", function () { setWarningOpen(true); });
            button.addEventListener("focus", function () { setWarningOpen(true); });
            warning.addEventListener("focusout", function (event) {
                if (!warning.contains(event.relatedTarget)) setWarningOpen(false);
            });
            warning.appendChild(button);
            warning.appendChild(tooltip);
            actions.appendChild(warning);
            button.focus();
        }

        function updateRowSummary(row, record) {
            var company = record.Company || "Company unavailable";
            var contact = [record.FirstName, record.LastName].filter(Boolean).join(" ") || "(blank)";
            row.setAttribute("data-company", record.Company || "");
            row.setAttribute("data-contact", contact === "(blank)" ? "" : contact);
            row.querySelector("[data-opt-in-company-cell] [data-opt-in-select]").textContent = company;
            row.querySelector("[data-opt-in-contact-cell]").textContent = contact;
            renderTable();
        }

        function renderRecord(record, row) {
            currentRecord = record;
            detailSections.textContent = "";
            buildSections(record).forEach(function (section) {
                var sectionElement = document.createElement("section");
                sectionElement.className = "opt-in-detail-section";

                var heading = document.createElement("h4");
                heading.textContent = section.title;
                sectionElement.appendChild(heading);

                var list = document.createElement("dl");
                list.className = "opt-in-field-list";
                section.fields.forEach(function (item) {
                    var wrapper = document.createElement("div");
                    var fieldKey = item.property ? item.property + ":" + (item.locationOrder || 0) : "";
                    if (fieldKey) wrapper.setAttribute("data-field-key", fieldKey);
                    var term = document.createElement("dt");
                    var description = document.createElement("dd");
                    var value = document.createElement("span");
                    value.className = "opt-in-field-value";
                    term.textContent = item.label;
                    value.textContent = item.value;
                    description.appendChild(value);
                    if (item.property && row.getAttribute("data-submitted") !== "1"
                        && row.getAttribute("data-status").toLowerCase() !== "invalidated") {
                        var editButton = document.createElement("button");
                        editButton.type = "button";
                        editButton.className = "opt-in-edit-icon opt-in-edit-start";
                        editButton.innerHTML = "&#9999;&#65039;";
                        editButton.setAttribute("aria-label", "Edit " + item.label);
                        editButton.title = "Edit " + item.label;
                        editButton.addEventListener("click", function () {
                            if (detailSections.querySelector(".is-editing")) renderRecord(currentRecord, row);
                            var targetDescription = detailSections.querySelector('[data-field-key="' + fieldKey + '"] dd');
                            targetDescription.textContent = "";
                            targetDescription.classList.add("is-editing");
                            targetDescription.classList.add("is-editing-" + item.editor);
                            targetDescription.closest(".opt-in-detail-section").classList.add("has-inline-editor");
                            var editor = createEditor(item);
                            editor.classList.add("opt-in-inline-editor");
                            var actions = document.createElement("span");
                            actions.className = "opt-in-edit-actions";
                            var saveButton = document.createElement("button");
                            saveButton.type = "button";
                            saveButton.className = "opt-in-edit-icon opt-in-edit-save";
                            saveButton.innerHTML = "&#10004;&#65039;";
                            saveButton.setAttribute("aria-label", "Save " + item.label);
                            saveButton.title = "Save " + item.label;
                            var cancelButton = document.createElement("button");
                            cancelButton.type = "button";
                            cancelButton.className = "opt-in-edit-icon opt-in-edit-cancel";
                            cancelButton.innerHTML = "&#10006;&#65039;";
                            cancelButton.setAttribute("aria-label", "Cancel editing " + item.label);
                            cancelButton.title = "Cancel editing " + item.label;
                            var reloadOnCancel = false;
                            cancelButton.addEventListener("click", function () {
                                if (reloadOnCancel) selectRow(row);
                                else renderRecord(currentRecord, row);
                            });
                            saveButton.addEventListener("click", function () {
                                var warning = actions.querySelector("[data-edit-warning]");
                                if (warning) warning.remove();
                                var values = editorValues(editor, item);
                                var body = new URLSearchParams();
                                body.set("Id", record.Id);
                                body.set("Field", item.property);
                                body.set("LocationOrder", item.locationOrder || "");
                                body.set("Value", values.value || "");
                                body.set("SecondaryValue", values.secondaryValue || "");
                                body.set("ExpectedUpdatedAtUtc", row.getAttribute("data-updated"));
                                body.set("__RequestVerificationToken", antiForgeryToken ? antiForgeryToken.value : "");
                                saveButton.disabled = true;
                                cancelButton.disabled = true;
                                fetch(fieldUpdateUrl, {
                                    method: "POST",
                                    credentials: "same-origin",
                                    headers: { "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8", "Accept": "application/json" },
                                    body: body.toString()
                                }).then(function (response) {
                                    return response.json().catch(function () { return {}; }).then(function (payload) {
                                        if (!response.ok || !payload.ok) {
                                            var failure = new Error(payload.message || "Review the value and try again.");
                                            failure.title = payload.title || item.label + " wasn't saved";
                                            failure.status = response.status;
                                            throw failure;
                                        }
                                        return payload;
                                    });
                                }).then(function (payload) {
                                    row.setAttribute("data-updated", payload.updatedAtUtc);
                                    updateRowSummary(row, payload.record);
                                    renderRecord(payload.record, row);
                                }).catch(function (error) {
                                    saveButton.disabled = false;
                                    cancelButton.disabled = false;
                                    reloadOnCancel = error.status === 409;
                                    showEditWarning(actions, error.title, error.message);
                                });
                            });
                            actions.appendChild(saveButton);
                            actions.appendChild(cancelButton);
                            targetDescription.appendChild(editor);
                            targetDescription.appendChild(actions);
                            var focusTarget = editor.matches("input, select, textarea")
                                ? editor
                                : editor.querySelector("input, select, textarea");
                            if (focusTarget) focusTarget.focus();
                        });
                        description.appendChild(editButton);
                    }
                    wrapper.appendChild(term);
                    wrapper.appendChild(description);
                    list.appendChild(wrapper);
                });
                sectionElement.appendChild(list);
                detailSections.appendChild(sectionElement);
            });

            var company = record.Company || "Company unavailable";
            var status = row.getAttribute("data-status") || "Pending";
            recordTitle.textContent = company + " - Opt-In #" + record.Id;
            recordMeta.textContent = "Created " + formatDateTime(row.getAttribute("data-created")) +
                " | Updated " + formatDateTime(row.getAttribute("data-updated"));
            recordStatus.textContent = status;
            recordStatus.className = "status-pill submission-status submission-status-" +
                status.toLowerCase().replace(/\s+/g, "-");
        }

        function showState(state) {
            detailEmpty.hidden = state !== "empty";
            detailLoading.hidden = state !== "loading";
            detailError.hidden = state !== "error";
            detail.hidden = state !== "detail";
            detail.setAttribute("aria-busy", state === "loading" ? "true" : "false");
        }

        function selectRow(row) {
            currentRow = row;
            currentRecord = null;
            currentRequestNumber += 1;
            var requestNumber = currentRequestNumber;

            rows.forEach(function (candidate) {
                var selected = candidate === row;
                candidate.classList.toggle("is-selected", selected);
                candidate.setAttribute("aria-selected", selected ? "true" : "false");
            });

            showState("loading");
            fetch(row.getAttribute("data-record-url"), {
                credentials: "same-origin",
                headers: { "Accept": "application/json" }
            })
                .then(function (response) {
                    if (!response.ok) {
                        throw new Error(response.status === 404
                            ? "The selected Opt-In no longer exists."
                            : "The selected Opt-In could not be loaded.");
                    }
                    return response.json();
                })
                .then(function (result) {
                    if (requestNumber !== currentRequestNumber) return;
                    if (!result || !result.ok || !result.record) {
                        throw new Error("The selected Opt-In did not return complete data.");
                    }
                    if (result.updatedAtUtc) row.setAttribute("data-updated", result.updatedAtUtc);
                    renderRecord(result.record, row);
                    showState("detail");
                })
                .catch(function (error) {
                    if (requestNumber !== currentRequestNumber) return;
                    detailErrorMessage.textContent = error.message || "The selected Opt-In could not be loaded.";
                    showState("error");
                });
        }

        function markSubmitted(checkbox, row) {
            checkbox.checked = false;
            window.ByteForgeModal.show(
                "Was Opt-In #" + checkbox.getAttribute("data-id") + " submitted to Walgreens?",
                {
                    type: "warning",
                    title: "Confirm Walgreens Submission",
                    buttons: [
                        { text: "Yes, mark submitted", primary: true, onClick: function () { submitOptIn(checkbox, row); } },
                        { text: "No, but mark completed", onClick: function () { completeOptIn(checkbox, row); } },
                        { text: "Cancel" }
                    ]
                });
        }

        function submitOptIn(checkbox, row) {
            checkbox.checked = true;
            checkbox.disabled = true;
            var body = new URLSearchParams();
            body.set("id", checkbox.getAttribute("data-id"));
            body.set("__RequestVerificationToken", antiForgeryToken ? antiForgeryToken.value : "");
            fetch(submittedUrl, {
                method: "POST",
                credentials: "same-origin",
                headers: { "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8", "Accept": "application/json" },
                body: body.toString()
            }).then(function (response) {
                return response.json().then(function (payload) {
                    if (!response.ok || !payload.ok) throw new Error(payload.message || "The Opt-In could not be marked submitted.");
                    return payload;
                });
            }).then(function (payload) {
                row.setAttribute("data-submitted", "1");
                row.setAttribute("data-submittedtime", payload.submittedAtUtc);
                row.setAttribute("data-status", "Completed");
                checkbox.setAttribute("aria-label", "Submitted to Walgreens");
                checkbox.setAttribute("title", "Submitted to Walgreens");
                row.querySelector("[data-opt-in-submitted-time]").textContent = formatTableDate(payload.submittedAtUtc);
                row.querySelector("[data-opt-in-status-cell]").innerHTML = '<span class="status-pill submission-status submission-status-completed">Completed</span>';
                renderTable();
                if (currentRow === row) selectRow(row);
            }).catch(function (error) {
                checkbox.checked = false;
                checkbox.disabled = false;
                window.ByteForgeModal.error(error.message || "The Opt-In could not be marked submitted.", "Submission Error");
            });
        }

        function completeOptIn(checkbox, row) {
            checkbox.disabled = true;
            var body = new URLSearchParams();
            body.set("id", checkbox.getAttribute("data-id"));
            body.set("__RequestVerificationToken", antiForgeryToken ? antiForgeryToken.value : "");
            fetch(completedUrl, {
                method: "POST",
                credentials: "same-origin",
                headers: { "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8", "Accept": "application/json" },
                body: body.toString()
            }).then(function (response) {
                return response.json().then(function (payload) {
                    if (!response.ok || !payload.ok) throw new Error(payload.message || "The Opt-In could not be marked completed.");
                    return payload;
                });
            }).then(function () {
                row.setAttribute("data-status", "Completed");
                checkbox.setAttribute("aria-label", "Completed without Walgreens submission");
                checkbox.setAttribute("title", "Completed without Walgreens submission");
                row.querySelector("[data-opt-in-status-cell]").innerHTML = '<span class="status-pill submission-status submission-status-completed">Completed</span>';
                renderTable();
                if (currentRow === row) selectRow(row);
            }).catch(function (error) {
                checkbox.disabled = false;
                window.ByteForgeModal.error(error.message || "The Opt-In could not be marked completed.", "Completion Error");
            });
        }

        function bindRow(row) {
            row.addEventListener("click", function () { selectRow(row); });
            var submittedCheckbox = row.querySelector("[data-mark-submitted]");
            if (submittedCheckbox && !submittedCheckbox.disabled) {
                submittedCheckbox.addEventListener("click", function (event) {
                    event.stopPropagation();
                    markSubmitted(submittedCheckbox, row);
                });
            }
        }

        function setSplitterPosition(percent) {
            var nextPercent = Math.max(32, Math.min(60, percent));
            workspace.style.setProperty("--opt-in-list-width", nextPercent + "%");
            splitter.setAttribute("aria-valuenow", String(Math.round(nextPercent)));
        }

        splitter.addEventListener("pointerdown", function (event) {
            if (window.matchMedia("(max-width: 1100px)").matches) return;
            event.preventDefault();
            splitter.setPointerCapture(event.pointerId);
            document.body.classList.add("is-resizing-opt-in-panes");
        });
        splitter.addEventListener("pointermove", function (event) {
            if (!splitter.hasPointerCapture(event.pointerId)) return;
            var bounds = workspace.getBoundingClientRect();
            setSplitterPosition(((event.clientX - bounds.left) / bounds.width) * 100);
        });
        splitter.addEventListener("pointerup", function (event) {
            if (splitter.hasPointerCapture(event.pointerId)) splitter.releasePointerCapture(event.pointerId);
            document.body.classList.remove("is-resizing-opt-in-panes");
        });
        splitter.addEventListener("keydown", function (event) {
            if (event.key !== "ArrowLeft" && event.key !== "ArrowRight") return;
            event.preventDefault();
            var current = Number(splitter.getAttribute("aria-valuenow")) || 40;
            setSplitterPosition(current + (event.key === "ArrowRight" ? 2 : -2));
        });
        splitter.addEventListener("dblclick", function () { setSplitterPosition(40); });

        Array.prototype.forEach.call(document.querySelectorAll("[data-opt-in-sort]"), function (button) {
            button.addEventListener("click", function () {
                var key = button.getAttribute("data-opt-in-sort");
                sort.direction = sort.key === key && sort.direction === "asc" ? "desc" : "asc";
                sort.key = key;
                currentPage = 1;
                renderTable();
            });
        });

        var searchTimer;
        searchInput.addEventListener("input", function () {
            currentPage = 1;
            listRequestNumber++;
            if (listAbort) listAbort.abort();
            window.clearTimeout(searchTimer);
            searchTimer = window.setTimeout(renderTable, 250);
        });
        pageSizeInput.addEventListener("change", function () {
            currentPage = 1;
            renderTable();
        });
        completedToggle.addEventListener("click", function () {
            showCompleted = !showCompleted;
            completedToggle.setAttribute("aria-pressed", showCompleted ? "true" : "false");
            completedToggle.setAttribute("title", showCompleted
                ? "Hide completed Opt-Ins from the list"
                : "Include completed Opt-Ins in the list");
            completedToggleLabel.textContent = showCompleted ? "Hide completed" : "Show completed";
            listHeading.textContent = showCompleted ? "All Opt-Ins" : "Open Opt-Ins";
            currentPage = 1;
            renderTable();
        });
        settingsToggle.addEventListener("click", function () {
            var expanded = settingsToggle.getAttribute("aria-expanded") === "true";
            settingsToggle.setAttribute("aria-expanded", expanded ? "false" : "true");
            settingsPanel.hidden = expanded;
        });
        retryButton.addEventListener("click", function () {
            if (currentRow) selectRow(currentRow);
        });

        renderTable();
    }());
