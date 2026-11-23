
        const reportPayload = window.walgreensReportData || {};

        function hasRows(value) {
            return Array.isArray(value) && value.length > 0;
        }

        const dailyRows = Array.isArray(reportPayload.dailyRows) ? reportPayload.dailyRows : [];
        const isCompanySizeReport = reportPayload.groupingMode === "companySize";
        const companySizeRows = Array.isArray(reportPayload.companySizeRows) ? reportPayload.companySizeRows : [];
        const breakdownRows = isCompanySizeReport ? companySizeRows : dailyRows;
        const dispositionMix = Array.isArray(reportPayload.dispositionMix) ? reportPayload.dispositionMix : [];

        const sourceColumns = hasRows(reportPayload.sourceColumns)
            ? reportPayload.sourceColumns
            : buildSourceColumnsFromDailyRows(breakdownRows);
        const hiddenSourceColumnKeys = new Set(["sepMtd", "octMtd", "programYtd", "note", "companyGoal", "phaseMetric", "phaseValue"]);
        const sourceRows = hasRows(reportPayload.sourceRows)
            ? reportPayload.sourceRows
            : buildSourceRowsFromDailyRows(breakdownRows);
        const optInCompanyRows = Array.isArray(reportPayload.optInCompanyRows) ? reportPayload.optInCompanyRows : [];
        const infoPackageCompanyRows = Array.isArray(reportPayload.infoPackageCompanyRows) ? reportPayload.infoPackageCompanyRows : [];
        const sendVoucherCompanyRows = Array.isArray(reportPayload.sendVoucherCompanyRows) ? reportPayload.sendVoucherCompanyRows : [];
        let unworkableContactRows = Array.isArray(reportPayload.unworkableContactRows) ? reportPayload.unworkableContactRows : [];
        let unworkableContactsLoadedRange = "";
        let contactableListMetadataLoaded = false;
        let contactableListMetadataLoading = false;

        let dailySort = { key: "date", direction: "asc" };
        let agentSort = { key: "optIns", direction: "desc" };
        let optInCompanySort = isCompanySizeReport ? { key: "employees", direction: "asc" } : { key: "company", direction: "asc" };
        let infoPackageCompanySort = isCompanySizeReport ? { key: "employees", direction: "asc" } : { key: "date", direction: "desc" };
        let sendVoucherCompanySort = isCompanySizeReport ? { key: "employees", direction: "asc" } : { key: "date", direction: "desc" };
        let unworkableContactSort = isCompanySizeReport ? { key: "employees", direction: "asc" } : { key: "date", direction: "desc" };
        let dispositionOnlyOptInRows = [];
        let dispositionOnlyOptInSummary = {
            confirmedCount: 0,
            leadMatchedCount: 0,
            ambiguousCount: 0,
            unmatchedCount: 0
        };
        let dispositionOnlyOptInSort = { key: "callAtUtc", direction: "desc" };
        let optInCompanyPage = 1;
        let infoPackageCompanyPage = 1;
        let sendVoucherCompanyPage = 1;
        let unworkableContactPage = 1;
        let dispositionOnlyOptInPage = 1;
        let headerCompact = false;
        const collapsedMixGroups = new Set(
            dispositionMix
                .filter(row => Array.isArray(row.children) && row.children.length > 1)
                .map(row => row.label));
        const collapsedSourceGroups = new Set();
        const optInCompanyPageSize = 25;
        const infoPackageCompanyPageSize = 25;
        const sendVoucherCompanyPageSize = 25;
        const unworkableContactPageSize = 25;
        const dispositionOnlyOptInPageSize = 25;

        const formatNumber = new Intl.NumberFormat("en-US");
        const campaignGoals = reportPayload.campaignGoals || {
            optIns: 0,
            hours: 0,
            optInsPerHour: 0,
            listReceived: 0
        };

        function safeDivide(numerator, denominator) {
            return denominator ? numerator / denominator : 0;
        }

        /**
         * Formats a complete North American phone number for display while preserving other values.
         * @param {unknown} value The stored phone value.
         * @returns {string} The formatted phone value.
         */
        function formatPhone(value) {
            const original = String(value ?? "");
            const digits = original.replace(/\D/g, "");
            const local = digits.length === 11 && digits.charAt(0) === "1"
                ? digits.substring(1)
                : digits;
            return local.length === 10
                ? `(${local.substring(0, 3)}) ${local.substring(3, 6)}-${local.substring(6)}`
                : original;
        }

        /**
         * Formats all available company address components as one display value.
         * @param {object} row The company report row.
         * @returns {string} The complete formatted address.
         */
        function formatFullAddress(row) {
            const street = [row?.address, row?.address2]
                .map(value => String(value ?? "").trim())
                .filter(Boolean)
                .join(", ");
            const cityState = [
                String(row?.city ?? "").trim(),
                String(row?.state ?? "").trim()
            ].filter(Boolean).join(", ");
            const locality = [
                cityState,
                String(row?.zip ?? "").trim()
            ].filter(Boolean).join(" ");
            return [street, locality].filter(Boolean).join(", ");
        }

        function formatAuditTimestamp(value) {
            if (!value) {
                return "an unavailable time";
            }

            const text = String(value).trim();
            const legacyJsonDate = /^\/Date\((-?\d+)(?:[+-]\d{4})?\)\/$/.exec(text);
            const storedUtc = /^(\d{4})-(\d{2})-(\d{2})(?:T|\s)(\d{2}):(\d{2})/.exec(text);
            if (storedUtc) {
                return `${storedUtc[2]}/${storedUtc[3]}/${storedUtc[1]} ${storedUtc[4]}:${storedUtc[5]} UTC`;
            }

            const storedDate = /^(\d{4})-(\d{2})-(\d{2})$/.exec(text);
            if (storedDate) {
                return `${storedDate[2]}/${storedDate[3]}/${storedDate[1]}`;
            }

            const date = legacyJsonDate
                ? new Date(Number(legacyJsonDate[1]))
                : new Date(text);
            if (Number.isNaN(date.getTime())) {
                return String(value);
            }

            const pad = number => String(number).padStart(2, "0");
            return `${pad(date.getUTCMonth() + 1)}/${pad(date.getUTCDate())}/${date.getUTCFullYear()} ` +
                `${pad(date.getUTCHours())}:${pad(date.getUTCMinutes())} UTC`;
        }

        function renderRecordExplanationButton(row, recordType) {
            if (!window.walgreensCanExplainRecords) {
                return "";
            }

            const rows = recordType === "ClinicSubmission"
                ? optInCompanyRows
                : recordType === "SendVoucher"
                    ? sendVoucherCompanyRows
                    : infoPackageCompanyRows;
            const index = rows.indexOf(row);
            return `<td class="record-explanation-cell"><button type="button" class="record-explanation-button" ` +
                `data-record-explanation-type="${recordType}" data-record-explanation-index="${index}" ` +
                `aria-label="Explain why this record is included" title="Why is this record here?">&#129300;</button></td>`;
        }

        function renderRecordEditButton(row, recordType) {
            if (!window.walgreensCanEditReportRecords) {
                return "";
            }

            const rows = recordType === "ClinicSubmission"
                ? optInCompanyRows
                : recordType === "SendVoucher"
                    ? sendVoucherCompanyRows
                    : infoPackageCompanyRows;
            const index = rows.indexOf(row);
            if (!row.recordId) {
                return `<td class="report-record-edit-cell"><button type="button" class="report-record-edit-button" ` +
                    `disabled aria-label="This BrightPattern-only row has no portal record to edit" ` +
                    `title="No portal record is available for this BrightPattern-only row.">&#9999;&#65039;</button></td>`;
            }
            return `<td class="report-record-edit-cell"><button type="button" class="report-record-edit-button" ` +
                `data-report-record-type="${recordType}" data-report-record-index="${index}" ` +
                `aria-label="Edit this report record" title="Edit this report record">&#9999;&#65039;</button></td>`;
        }

        function renderReportNotesButton(row, recordType) {
            const rows = recordType === "ClinicSubmission"
                ? optInCompanyRows
                : recordType === "SendVoucher"
                    ? sendVoucherCompanyRows
                    : infoPackageCompanyRows;
            const index = rows.indexOf(row);
            const hasNotes = Boolean(String(row.agentNotes || "").trim() || String(row.bpNotes || "").trim());
            const disabled = hasNotes ? "" : " disabled";
            const label = hasNotes ? "View Agent and BP notes" : "No Agent or BP notes available";
            return `<td class="report-notes-cell"><button type="button" class="report-notes-button" ` +
                `data-report-notes-type="${recordType}" data-report-notes-index="${index}"${disabled} ` +
                `aria-label="${label}" title="${label}">&#128196;</button></td>`;
        }

        function showReportNotes(recordType, index) {
            const rows = recordType === "ClinicSubmission"
                ? optInCompanyRows
                : recordType === "SendVoucher"
                    ? sendVoucherCompanyRows
                    : infoPackageCompanyRows;
            const row = rows[index];
            if (!row) {
                return;
            }

            document.getElementById("reportNotesCompany").textContent = row.company || "Report record";
            document.getElementById("reportAgentNotes").textContent = String(row.agentNotes || "").trim() || "(blank)";
            document.getElementById("reportBpNotes").textContent = String(row.bpNotes || "").trim() || "(blank)";
            document.getElementById("reportNotesDialog").showModal();
        }

        let reportRecordWasSaved = false;
        let activeReportRecord = null;
        let reportAgentOptions = null;
        let reportAgentOptionsPromise = null;

        const reportProgramOptions = ["Voucher Program Only", "Clinic", "Clinic + Vouchers"];
        const reportIndustryOptions = [
            "Agriculture, Forestry, Fishing and Hunting", "Construction", "Education Services",
            "Finance, Insurance and Information", "Food Services, Accommodation and Entertainment",
            "Health Care and Social Assistance", "Manufacturing", "Mining, Quarrying, Oil and Gas Extraction",
            "Professional, Scientific and Technical Services", "Public Administration",
            "Religious Organizations and Community Groups", "Retail and Wholesale Trade",
            "Transportation and Warehousing", "Other"
        ];
        const reportStateOptions = [
            "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "DC", "FL", "GA", "HI", "ID", "IL",
            "IN", "IA", "KS", "KY", "LA", "ME", "MD", "MA", "MI", "MN", "MS", "MO", "MT", "NE",
            "NV", "NH", "NJ", "NM", "NY", "NC", "ND", "OH", "OK", "OR", "PA", "RI", "SC", "SD",
            "TN", "TX", "UT", "VT", "VA", "WA", "WV", "WI", "WY"
        ];
        const reportVaccinationOptions = [
            "Flu Trivalent", "Flu HD (65+)", "COVID19", "Pneumonia",
            "Tdap (Whooping Cough)", "Shingles", "RSV", "Other"
        ];
        const reportPaymentOptions = ["Insurance", "Cash", "Direct Bill"];
        const reportYesNoOptions = [{ value: "true", label: "Yes" }, { value: "false", label: "No" }];

        function reportField(label, property, value, editor, options, locationOrder, secondaryValue) {
            return { label, property, value, editor: editor || "text", options: options || [], locationOrder, secondaryValue };
        }

        function buildReportRecordSections(payload) {
            const record = payload.record || {};
            const system = payload.system || {};
            const sections = [{
                title: "1. Report record",
                fields: [
                    reportField("Report Date", "ReportDate", payload.reportDate, "date"),
                    reportField("Agent", "Agent", system.AgentId, "agent", null, null, payload.agent)
                ]
            }];
            if (payload.entryType !== "ClinicSubmission") {
                sections.push({
                    title: "2. Program and organization",
                    fields: [
                        reportField("Program Type", "ProgramInterest", record.ProgramInterest, "select", reportProgramOptions),
                        reportField("Company", "Company", record.Company),
                        reportField("Notes", "Notes", record.Notes, "textarea")
                    ]
                }, {
                    title: "3. Contact and location",
                    fields: [
                        reportField("First Name", "ContactFirstName", record.ContactFirstName),
                        reportField("Last Name", "ContactLastName", record.ContactLastName),
                        reportField("Title", "ContactJobTitle", record.ContactJobTitle),
                        reportField("Email", "ContactEmail", record.ContactEmail, "email"),
                        reportField("Phone", "ContactPhone", record.ContactPhoneNumber, "phone", null, null, record.ContactExtension),
                        reportField("Address 1", "Address1", record.Address1),
                        reportField("Address 2", "Address2", record.Address2),
                        reportField("City", "City", record.City),
                        reportField("State", "State", record.State, "select", reportStateOptions),
                        reportField("ZIP", "ZipCode", record.ZipCode)
                    ]
                });
            } else {
                sections.push({
                    title: "2. Program and organization",
                    fields: [
                        reportField("Program Type", "ProgramInterest", record.ProgramInterest, "select", reportProgramOptions),
                        reportField("Source Employee Count", "PaydayPerxLeadEmployeeCount", record.PaydayPerxLeadEmployeeCount),
                        reportField("Estimated Number of Participants", "EstimatedParticipants", record.EstimatedParticipants, "number"),
                        reportField("Company", "Company", record.Company),
                        reportField("Primary Employee Medical Insurance", "PrimaryEmployeeMedicalInsurance", record.PrimaryEmployeeMedicalInsurance),
                        reportField("Employee Pharmacy Insurance", "EmployeePharmacyInsurance", record.EmployeePharmacyInsurance),
                        reportField("Current/Past Walgreens Contact", "CurrentPastWalgreensContact", record.CurrentPastWalgreensContact),
                        reportField("Industry", "Industry", record.Industry, "select", reportIndustryOptions)
                    ]
                }, {
                    title: "3. Primary location and contact",
                    fields: [
                        reportField("Address 1", "Address1", record.Address1), reportField("Address 2", "Address2", record.Address2),
                        reportField("City", "City", record.City), reportField("State", "State", record.State, "select", reportStateOptions),
                        reportField("ZIP", "ZipCode", record.ZipCode), reportField("First Name", "FirstName", record.FirstName),
                        reportField("Last Name", "LastName", record.LastName), reportField("Title", "MainContactJobTitle", record.MainContactJobTitle),
                        reportField("Email", "Email", record.Email, "email"),
                        reportField("Phone", "Phone", record.PhoneNumber, "phone", null, null, record.Extension),
                        reportField("Preferred Clinic Date", "PreferredClinicDate", record.PreferredClinicDate, "date"),
                        reportField("Alternate Clinic Date", "AlternateClinicDate", record.AlternateClinicDate, "date"),
                        reportField("Estimated Number of Clinics", "EstimatedClinics", record.EstimatedClinics, "number")
                    ]
                }, {
                    title: "4. Alternate contact",
                    fields: [
                        reportField("Alternate First Name", "AlternateFirstName", record.AlternateFirstName),
                        reportField("Alternate Last Name", "AlternateLastName", record.AlternateLastName),
                        reportField("Alternate Title", "AlternateContactJobTitle", record.AlternateContactJobTitle),
                        reportField("Alternate Email", "AlternateContactEmail", record.AlternateContactEmail, "email"),
                        reportField("Alternate Phone", "AlternatePhone", record.AlternateContactPhoneNumber, "phone", null, null, record.AlternateContactExtension)
                    ]
                });
                (record.AdditionalLocations || []).forEach((location, index) => sections.push({
                    title: `${sections.length + 1}. Additional Location ${index + 1}`,
                    fields: [
                        reportField("Address 1", "AdditionalLocation.Address1", location.Address1, "text", null, location.LocationOrder),
                        reportField("Address 2", "AdditionalLocation.Address2", location.Address2, "text", null, location.LocationOrder),
                        reportField("City", "AdditionalLocation.City", location.City, "text", null, location.LocationOrder),
                        reportField("State", "AdditionalLocation.State", location.State, "select", reportStateOptions, location.LocationOrder),
                        reportField("ZIP", "AdditionalLocation.ZipCode", location.ZipCode, "text", null, location.LocationOrder),
                        reportField("First Name", "AdditionalLocation.FirstName", location.FirstName, "text", null, location.LocationOrder),
                        reportField("Last Name", "AdditionalLocation.LastName", location.LastName, "text", null, location.LocationOrder),
                        reportField("Title", "AdditionalLocation.MainContactJobTitle", location.MainContactJobTitle, "text", null, location.LocationOrder),
                        reportField("Email", "AdditionalLocation.Email", location.Email, "email", null, location.LocationOrder),
                        reportField("Phone", "AdditionalLocation.Phone", location.PhoneNumber, "phone", null, location.LocationOrder, location.Extension),
                        reportField("Preferred Clinic Date", "AdditionalLocation.PreferredClinicDate", location.PreferredClinicDate, "date", null, location.LocationOrder),
                        reportField("Alternate Clinic Date", "AdditionalLocation.AlternateClinicDate", location.AlternateClinicDate, "date", null, location.LocationOrder)
                    ]
                }));
                sections.push({
                    title: `${sections.length + 1}. Clinic details`,
                    fields: [
                        reportField("Vaccinations Requested", "Vaccinations", record.Vaccinations, "multiselect", reportVaccinationOptions),
                        reportField("Other Vaccination", "OtherVaccination", record.OtherVaccination),
                        reportField("Employees in Multiple States", "MultipleStates", record.MultipleStates, "boolean", reportYesNoOptions),
                        reportField("Previous Walgreens Clinic", "PreviousWalgreensClinic", record.PreviousWalgreensClinic, "boolean", reportYesNoOptions),
                        reportField("Payment Types", "PaymentTypes", record.PaymentTypes, "multiselect", reportPaymentOptions),
                        reportField("Notes", "Notes", record.Notes, "textarea")
                    ]
                });
            }
            sections.push({
                title: `${sections.length + 1}. System record`,
                fields: [
                    reportField("Portal Record ID", null, payload.id), reportField("Status", null, payload.status),
                    reportField("Agent ID", null, system.AgentId), reportField("PaydayPerx Lead ID", null, system.PaydayPerxLeadId),
                    reportField("Global ID", null, system.GlobalId), reportField("Call ID", null, system.CallId),
                    reportField("BrightPattern Session ID", null, system.BpSessionId), reportField("BrightPattern Origin", null, system.BpOrigin)
                ]
            });
            return sections;
        }

        function reportEditorDisplayValue(item) {
            if (item.editor === "boolean") return item.value === true ? "Yes" : item.value === false ? "No" : "(blank)";
            if (item.editor === "date") return reportEditorDateInput(item.value) || "(blank)";
            if (item.editor === "agent") return item.secondaryValue || item.value || "(blank)";
            if (item.editor === "phone") {
                const phone = formatPhone(item.value || "");
                return [phone, item.secondaryValue ? `ext. ${item.secondaryValue}` : ""].filter(Boolean).join(" ") || "(blank)";
            }
            if (Array.isArray(item.value)) return item.value.length ? item.value.join(", ") : "(blank)";
            return item.value === null || item.value === undefined || String(item.value).trim() === "" ? "(blank)" : String(item.value);
        }

        function reportEditorDateInput(value) {
            if (!value) return "";
            const text = String(value);
            const iso = /^(\d{4}-\d{2}-\d{2})/.exec(text);
            if (iso) return iso[1];
            const legacy = /^\/Date\((-?\d+)/.exec(text);
            const date = legacy ? new Date(Number(legacy[1])) : new Date(text);
            if (Number.isNaN(date.getTime())) return "";
            return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
        }

        function createReportFieldEditor(item) {
            if (item.editor === "textarea") {
                const editor = document.createElement("textarea"); editor.rows = 4; editor.maxLength = 4000; editor.value = item.value || ""; return editor;
            }
            if (item.editor === "multiselect") {
                const editor = document.createElement("fieldset"); editor.className = "opt-in-inline-choices";
                (item.options || []).forEach(option => {
                    const label = document.createElement("label"); const input = document.createElement("input");
                    input.type = "checkbox"; input.value = option; input.checked = (item.value || []).includes(option);
                    label.append(input, document.createTextNode(` ${option}`)); editor.appendChild(label);
                }); return editor;
            }
            if (item.editor === "phone") {
                const editor = document.createElement("div"); editor.className = "opt-in-inline-phone";
                const phone = document.createElement("input"); phone.type = "tel"; phone.maxLength = 14; phone.value = item.value || ""; phone.dataset.primaryValue = "";
                const extension = document.createElement("input"); extension.maxLength = 5; extension.placeholder = "Ext"; extension.value = item.secondaryValue || ""; extension.dataset.secondaryValue = "";
                editor.append(phone, extension); return editor;
            }
            if (item.editor === "agent") {
                const editor = document.createElement("select");
                const current = new Option(item.secondaryValue || item.value || "Select", item.value || "", true, true);
                current.dataset.displayName = item.secondaryValue || item.value || "";
                editor.appendChild(current);
                loadReportAgentOptions().then(agents => {
                    const selectedAgentId = editor.value;
                    editor.textContent = "";
                    editor.appendChild(new Option("Select an agent", ""));
                    agents.forEach(agent => {
                        const label = getReportAgentDisplayName(agent);
                        const option = new Option(`${label} (${agent.AgentId})`, agent.AgentId, false, agent.AgentId === selectedAgentId);
                        option.dataset.displayName = label;
                        editor.appendChild(option);
                    });
                    if (selectedAgentId && !Array.from(editor.options).some(option => option.value === selectedAgentId))
                        editor.appendChild(current);
                    editor.value = selectedAgentId;
                });
                return editor;
            }
            if (item.editor === "select" || item.editor === "boolean") {
                const editor = document.createElement("select"); editor.appendChild(new Option("Select", ""));
                (item.options || []).forEach(option => {
                    const value = typeof option === "string" ? option : option.value;
                    const label = typeof option === "string" ? option : option.label;
                    const choice = new Option(label, value, false, String(item.value).toLowerCase() === String(value).toLowerCase());
                    editor.appendChild(choice);
                }); return editor;
            }
            const editor = document.createElement("input"); editor.type = item.editor === "date" ? "date" : item.editor === "number" ? "number" : item.editor === "email" ? "email" : "text";
            editor.value = item.editor === "date" ? reportEditorDateInput(item.value) : item.value || "";
            if (item.editor === "number") { editor.min = "0"; editor.step = "1"; } return editor;
        }

        function getReportEditorValues(editor, item) {
            if (item.editor === "multiselect") return { value: Array.from(editor.querySelectorAll("input:checked")).map(input => input.value).join("|") };
            if (item.editor === "phone") return { value: editor.querySelector("[data-primary-value]").value, secondaryValue: editor.querySelector("[data-secondary-value]").value };
            if (item.editor === "agent") return { value: editor.value, secondaryValue: editor.selectedOptions[0]?.dataset.displayName || "" };
            return { value: editor.value };
        }

        function getReportAgentDisplayName(agent) {
            return String(agent && (agent.DisplayName || agent.AgentId) || "").trim();
        }

        function loadReportAgentOptions() {
            if (reportAgentOptions) return Promise.resolve(reportAgentOptions);
            if (reportAgentOptionsPromise) return reportAgentOptionsPromise;
            reportAgentOptionsPromise = fetch(window.walgreensManualSaleAgentsUrl, {
                credentials: "same-origin",
                headers: { "Accept": "application/json" }
            }).then(response => response.json().catch(() => ({})).then(payload => {
                if (!response.ok || !payload.ok) throw new Error(payload.message || "The agent list could not be loaded.");
                reportAgentOptions = payload.agents || [];
                reportAgentOptionsPromise = null;
                return reportAgentOptions;
            })).catch(error => {
                reportAgentOptionsPromise = null;
                const errorElement = document.getElementById("reportRecordEditorError");
                errorElement.textContent = error.message;
                errorElement.hidden = false;
                return [];
            });
            return reportAgentOptionsPromise;
        }

        function renderReportRecordEditor(payload) {
            activeReportRecord = payload;
            const container = document.getElementById("reportRecordEditorSections");
            container.textContent = "";
            document.getElementById("reportRecordEditorTitle").textContent = `${payload.record?.Company || "Walgreens record"} - ${payload.entryType === "ClinicSubmission" ? "Opt-In" : payload.entryType === "SendVoucher" ? "Send Voucher" : "GK Send Info"} #${payload.id}`;
            document.getElementById("reportRecordEditorMeta").textContent = `Created ${formatAuditTimestamp(payload.createdAtUtc)} | Updated ${formatAuditTimestamp(payload.updatedAtUtc)}`;
            document.getElementById("reportRecordEditorStatus").textContent = payload.status || "";
            document.getElementById("reportRecordEditorStatus").className = `status-pill submission-status submission-status-${String(payload.status || "pending").toLowerCase().replace(/\s+/g, "-")}`;
            buildReportRecordSections(payload).forEach(section => {
                const sectionElement = document.createElement("section"); sectionElement.className = "opt-in-detail-section";
                const heading = document.createElement("h4"); heading.textContent = section.title; sectionElement.appendChild(heading);
                const list = document.createElement("dl"); list.className = "opt-in-field-list";
                section.fields.forEach(item => {
                    const wrapper = document.createElement("div"); const term = document.createElement("dt"); const description = document.createElement("dd");
                    term.textContent = item.label; const value = document.createElement("span"); value.className = "opt-in-field-value"; value.textContent = reportEditorDisplayValue(item); description.appendChild(value);
                    if (item.property) {
                        const edit = document.createElement("button"); edit.type = "button"; edit.className = "opt-in-edit-icon opt-in-edit-start"; edit.innerHTML = "&#9999;&#65039;"; edit.title = `Edit ${item.label}`; edit.setAttribute("aria-label", edit.title);
                        edit.addEventListener("click", () => beginReportFieldEdit(sectionElement, description, item)); description.appendChild(edit);
                    }
                    wrapper.append(term, description); list.appendChild(wrapper);
                });
                sectionElement.appendChild(list); container.appendChild(sectionElement);
            });
            document.getElementById("reportRecordEditorLoading").hidden = true;
            document.getElementById("reportRecordEditorError").hidden = true;
            document.getElementById("reportRecordEditorContent").hidden = false;
        }

        function beginReportFieldEdit(section, description, item) {
            renderReportRecordEditor(activeReportRecord);
            const matching = Array.from(document.querySelectorAll("#reportRecordEditorSections .opt-in-field-list > div"))
                .find(wrapper => wrapper.querySelector("dt")?.textContent === item.label && wrapper.closest("section").querySelector("h4")?.textContent === section.querySelector("h4")?.textContent);
            const target = matching ? matching.querySelector("dd") : description;
            target.textContent = ""; target.className = `is-editing is-editing-${item.editor}`; target.closest("section").classList.add("has-inline-editor");
            const editor = createReportFieldEditor(item); editor.classList.add("opt-in-inline-editor");
            const actions = document.createElement("span"); actions.className = "opt-in-edit-actions";
            const save = document.createElement("button"); save.type = "button"; save.className = "opt-in-edit-icon opt-in-edit-save"; save.innerHTML = "&#9989;"; save.title = `Save ${item.label}`; save.setAttribute("aria-label", save.title);
            const cancel = document.createElement("button"); cancel.type = "button"; cancel.className = "opt-in-edit-icon opt-in-edit-cancel"; cancel.innerHTML = "&#10006;&#65039;"; cancel.title = `Cancel editing ${item.label}`; cancel.setAttribute("aria-label", cancel.title);
            cancel.addEventListener("click", () => renderReportRecordEditor(activeReportRecord));
            save.addEventListener("click", () => saveReportRecordField(item, editor, save, cancel));
            actions.append(save, cancel); target.append(editor, actions);
            const focusTarget = editor.matches("input, select, textarea") ? editor : editor.querySelector("input, select, textarea"); if (focusTarget) focusTarget.focus();
        }

        function saveReportRecordField(item, editor, save, cancel) {
            const values = getReportEditorValues(editor, item); const body = new URLSearchParams();
            body.set("EntryType", activeReportRecord.entryType); body.set("Id", activeReportRecord.id); body.set("Field", item.property);
            body.set("LocationOrder", item.locationOrder || ""); body.set("Value", values.value || ""); body.set("SecondaryValue", values.secondaryValue || "");
            body.set("ExpectedUpdatedAtUtc", activeReportRecord.updatedAtUtc);
            body.set("__RequestVerificationToken", document.querySelector('input[name="__RequestVerificationToken"]')?.value || "");
            save.disabled = true; cancel.disabled = true;
            fetch(window.walgreensReportRecordFieldUrl, { method: "POST", credentials: "same-origin", headers: { "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8", "Accept": "application/json" }, body: body.toString() })
                .then(response => response.json().catch(() => ({})).then(payload => { if (!response.ok || !payload.ok) throw new Error(payload.message || "The field could not be saved."); return payload; }))
                .then(payload => { reportRecordWasSaved = true; renderReportRecordEditor(payload); })
                .catch(error => { save.disabled = false; cancel.disabled = false; document.getElementById("reportRecordEditorError").textContent = error.message; document.getElementById("reportRecordEditorError").hidden = false; });
        }

        function openReportRecordEditor(recordType, index) {
            const rows = recordType === "ClinicSubmission"
                ? optInCompanyRows
                : recordType === "SendVoucher"
                    ? sendVoucherCompanyRows
                    : infoPackageCompanyRows;
            const row = rows[index];
            if (!row?.recordId || !window.walgreensReportRecordUrl) {
                return;
            }

            const dialog = document.getElementById("reportRecordEditorDialog");
            document.getElementById("reportRecordEditorLoading").hidden = false;
            document.getElementById("reportRecordEditorContent").hidden = true;
            document.getElementById("reportRecordEditorError").hidden = true;
            dialog.showModal();
            const url = new URL(window.walgreensReportRecordUrl, window.location.origin);
            url.searchParams.set("entryType", recordType); url.searchParams.set("id", row.recordId);
            fetch(url, { credentials: "same-origin", headers: { "Accept": "application/json" } })
                .then(response => response.json().catch(() => ({})).then(payload => { if (!response.ok || !payload.ok) throw new Error(payload.message || "The report record could not be loaded."); return payload; }))
                .then(renderReportRecordEditor)
                .catch(error => { document.getElementById("reportRecordEditorLoading").hidden = true; const errorElement = document.getElementById("reportRecordEditorError"); errorElement.textContent = error.message; errorElement.hidden = false; });
        }

        function closeReportRecordEditor() {
            const dialog = document.getElementById("reportRecordEditorDialog");
            if (dialog?.open) {
                dialog.close();
            }
            activeReportRecord = null;
            if (reportRecordWasSaved) {
                window.location.reload();
            }
        }

        function showRecordExplanation(recordType, index) {
            const isOptIn = recordType === "ClinicSubmission";
            const isVoucher = recordType === "SendVoucher";
            const rows = isOptIn ? optInCompanyRows : isVoucher ? sendVoucherCompanyRows : infoPackageCompanyRows;
            const row = rows[index];
            if (!row) {
                return;
            }

            const direction = String(row.direction || "").trim().toLowerCase();
            const callDescription = direction ? `${direction} call` : "call";
            const phone = formatPhone(row.callPhone || row.phone) || "an unavailable number";
            const globalId = row.globalId || "unavailable";
            const disposition = window.ByteForgeWalgreensLabels.outcome(row.disposition) || (isOptIn ? "an unavailable disposition" : isVoucher ? "Send Voucher" : "GK Send Info");
            const callTime = formatAuditTimestamp(row.callAtUtc || row.date);
            const agent = row.agent || "an unavailable agent";
            const recordLabel = isOptIn ? "Opt In" : isVoucher ? "Send Voucher" : "GK Send Info";
            const sentences = [
                `A ${escapeHtml(callDescription)} to ${escapeHtml(phone)} with Global ID ` +
                    `<code>${escapeHtml(globalId)}</code>, made at ${escapeHtml(callTime)}, was disposed as ` +
                    `<strong>${escapeHtml(disposition)}</strong>.`
            ];

            if (row.recordId) {
                const recordUrl = new URL(window.walgreensReadOnlyRecordUrl || "/", window.location.origin);
                recordUrl.searchParams.set("submissionType", recordType);
                recordUrl.searchParams.set("submissionId", row.recordId);
                recordUrl.searchParams.set("readOnly", "true");
                sentences.push(
                    `Walgreens data was captured in portal ${recordLabel} record ` +
                    `<a href="${escapeHtml(recordUrl.toString())}" target="_blank" rel="noopener">#${escapeHtml(row.recordId)}</a> ` +
                    `by ${escapeHtml(agent)} at ${escapeHtml(formatAuditTimestamp(row.capturedAtUtc))}.`);

                if (isOptIn && row.sentAtUtc) {
                    sentences.push(`It was sent to Walgreens at ${escapeHtml(formatAuditTimestamp(row.sentAtUtc))}.`);
                } else if (isOptIn) {
                    sentences.push(`No completed Walgreens send time is recorded; the portal status is ${escapeHtml(row.status || "unavailable")}.`);
                } else {
                    sentences.push(`The information-package request status is ${escapeHtml(row.status || "unavailable")}.`);
                }
            } else {
                sentences.push(
                    `This ${recordLabel} row is missing its expected portal DB record ID. ` +
                    `Refresh the report; if it remains, the report data needs investigation.`);
            }

            document.getElementById("recordExplanationText").innerHTML = sentences.join(" ");
            document.getElementById("recordExplanationDialog").showModal();
        }

        function renderMissingOptInMatch(match) {
            const identity = [match.company, match.contact].filter(Boolean).join(" — ") || "Unidentified candidate";
            const facts = [
                match.phone ? `Phone: ${formatPhone(match.phone)}` : "",
                match.globalId ? `Global ID: ${match.globalId}` : "",
                match.callAtUtc ? `Call: ${formatAuditTimestamp(match.callAtUtc)}` : "",
                match.disposition ? `Disposition: ${match.disposition}` : "",
                match.portalStatus ? `Portal status: ${match.portalStatus}` : "",
                match.leadId ? `Lead ID: ${match.leadId}` : ""
            ].filter(Boolean);
            const recordLink = match.recordUrl
                ? `<a href="${escapeHtml(match.recordUrl)}" target="_blank" rel="noopener">View portal record #${escapeHtml(match.portalRecordId)}</a>`
                : "";
            return `<article class="missing-opt-in-result">
                <h3>${escapeHtml(identity)}</h3>
                <p class="missing-opt-in-reason">${escapeHtml(match.reason)}</p>
                ${facts.length ? `<ul>${facts.map(fact => `<li>${escapeHtml(fact)}</li>`).join("")}</ul>` : ""}
                ${recordLink}
            </article>`;
        }

        async function investigateMissingOptIn(event) {
            event.preventDefault();
            const queryInput = document.getElementById("missingOptInQuery");
            const status = document.getElementById("missingOptInStatus");
            const results = document.getElementById("missingOptInResults");
            const button = document.getElementById("runMissingOptInButton");
            const query = queryInput.value.trim();
            if (query.length < 2) {
                status.textContent = "Enter at least two characters of a name or phone number.";
                queryInput.focus();
                return;
            }

            const url = new URL(window.walgreensMissingOptInUrl, window.location.origin);
            url.searchParams.set("query", query);
            url.searchParams.set("startDate", document.getElementById("startDate").value);
            url.searchParams.set("endDate", document.getElementById("endDate").value);
            if (document.getElementById("reportTeam")) url.searchParams.set("team", document.getElementById("reportTeam").value);
            button.disabled = true;
            status.textContent = "Checking portal, lead, and BrightPattern evidence...";
            results.innerHTML = "";

            try {
                const response = await fetch(url.toString(), { credentials: "same-origin" });
                const payload = await response.json();
                if (!response.ok || !payload.ok) {
                    throw new Error(payload.message || "The diagnostic could not be completed.");
                }

                status.textContent = payload.summary || "Diagnostic complete.";
                results.innerHTML = (payload.matches || []).map(renderMissingOptInMatch).join("");
            } catch (error) {
                status.textContent = error.message || "The diagnostic could not be completed.";
            } finally {
                button.disabled = false;
            }
        }

        function getVisibleDispositionOnlyOptIns() {
            const search = document.getElementById("dispositionOnlyOptInsSearch").value.trim().toLowerCase();
            return dispositionOnlyOptInRows
                .filter(row => !search || Object.values(row).some(value => String(value ?? "").toLowerCase().includes(search)))
                .sort((a, b) =>
                    compareValues(a, b, dispositionOnlyOptInSort.key) *
                    (dispositionOnlyOptInSort.direction === "asc" ? 1 : -1));
        }

        function renderDispositionOnlyOptIns() {
            window.ByteForgeSortIndicator.update(
                "[data-disposition-only-sort]",
                "data-disposition-only-sort",
                dispositionOnlyOptInSort);
            const rows = getVisibleDispositionOnlyOptIns();
            const totalPages = Math.max(1, Math.ceil(rows.length / dispositionOnlyOptInPageSize));
            dispositionOnlyOptInPage = Math.min(dispositionOnlyOptInPage, totalPages);
            const startIndex = (dispositionOnlyOptInPage - 1) * dispositionOnlyOptInPageSize;
            const pageRows = rows.slice(startIndex, startIndex + dispositionOnlyOptInPageSize);
            const pageStart = rows.length ? startIndex + 1 : 0;
            const pageEnd = Math.min(startIndex + dispositionOnlyOptInPageSize, rows.length);

            document.getElementById("dispositionOnlyOptInsTable").innerHTML = pageRows.map(row => {
                const contact = [row.contactFirst, row.contactLast].filter(Boolean).join(" ");
                const rowIndex = dispositionOnlyOptInRows.indexOf(row);
                let classification = escapeHtml(row.classification);
                if (row.classification === "Lead matched; form missing" && row.leadId) {
                    classification =
                        `<button type="button" data-create-disposition-only-entry="${rowIndex}" ` +
                        `title="${escapeHtml(row.classification)}">&#10133; Create entry</button>`;
                } else if (row.recordUrl) {
                    classification =
                        `<a href="${escapeHtml(row.recordUrl)}" target="_blank" rel="noopener">` +
                        `&#9999;&#65039; ${escapeHtml(row.classification)}</a>`;
                }
                return `<tr>
                    <td><span class="disposition-only-classification">${classification}</span></td>
                    <td>${escapeHtml(row.company || "Unmatched")}</td>
                    <td>${escapeHtml(contact)}</td>
                    <td>${escapeHtml(formatPhone(row.phone))}</td>
                    <td title="${escapeHtml(row.agentId)}">${escapeHtml(row.agent)}</td>
                    <td>${escapeHtml(window.ByteForgeWalgreensLabels.outcome(row.disposition))}</td>
                    <td>${escapeHtml(formatAuditTimestamp(row.callAtUtc))}</td>
                    <td>${escapeHtml(row.matchMethod)}</td>
                    <td>${row.leadId ? escapeHtml(row.leadId) : ""}</td>
                    <td><code>${escapeHtml(row.globalId)}</code></td>
                    <td><code>${escapeHtml(row.callId)}</code></td>
                    <td>${escapeHtml(row.notes)}</td>
                </tr>`;
            }).join("");

            const summary = dispositionOnlyOptInSummary;
            document.getElementById("dispositionOnlyOptInsMeta").textContent =
                `${formatNumber.format(dispositionOnlyOptInRows.length)} disposition-only calls`;
            document.getElementById("dispositionOnlyOptInsSummary").textContent =
                `${formatNumber.format(summary.confirmedCount)} confirmed portal submissions excluded; ` +
                `${formatNumber.format(summary.leadMatchedCount)} lead matched; ` +
                `${formatNumber.format(summary.ambiguousCount)} ambiguous; ` +
                `${formatNumber.format(summary.unmatchedCount)} unmatched.`;
            dispositionOnlyOptInPage = WalgreensAdminListTools.renderPagination({ summaryElement: document.getElementById("dispositionOnlyOptInsPageMeta"), buttonsElement: document.getElementById("dispositionOnlyOptInsPagination"), totalRows: rows.length, pageSize: dispositionOnlyOptInPageSize, currentPage: dispositionOnlyOptInPage, onPageChange: target => { dispositionOnlyOptInPage = target; renderDispositionOnlyOptIns(); } });
            document.getElementById("dispositionOnlyOptInsEmpty").style.display = rows.length ? "none" : "block";
        }

        function createDispositionOnlyEntry(index) {
            const row = dispositionOnlyOptInRows[index];
            if (!row || !row.leadId || typeof window.openWalgreensAgentLauncher !== "function") {
                return;
            }

            window.openWalgreensAgentLauncher({
                ani: row.phone,
                callId: row.callId,
                globalId: row.globalId,
                callStartedAtUtc: row.callAtUtc,
                leadId: row.leadId
            });
        }

        async function loadDispositionOnlyOptIns() {
            const summary = document.getElementById("dispositionOnlyOptInsSummary");
            const table = document.getElementById("dispositionOnlyOptInsTable");
            const tableElement = table.closest("table");
            const downloadButton = document.getElementById("downloadDispositionOnlyOptInsButton");
            const loadingStatuses = [
                "Gathering Opt-In/Lead calls...",
                "Checking existing portal Opt-Ins...",
                "Matching calls to leads from the main table...",
                "Removing companies already in the Opt-In list...",
                "Resolving agent details...",
                "Preparing the disposition-only list..."
            ];
            let loadingStatusIndex = 0;
            dispositionOnlyOptInRows = [];
            dispositionOnlyOptInPage = 1;
            table.innerHTML =
                `<tr class="disposition-only-loading-row"><td colspan="12">` +
                `<span class="disposition-only-spinner" aria-hidden="true"></span>` +
                `<span>${escapeHtml(loadingStatuses[0])}</span></td></tr>`;
            summary.textContent = loadingStatuses[0];
            if (tableElement) {
                tableElement.setAttribute("aria-busy", "true");
            }
            downloadButton.disabled = true;
            document.getElementById("dispositionOnlyOptInsMeta").textContent = "Loading";
            document.getElementById("dispositionOnlyOptInsEmpty").style.display = "none";
            const loadingStatusTimer = window.setInterval(() => {
                loadingStatusIndex = Math.min(loadingStatusIndex + 1, loadingStatuses.length - 1);
                const status = loadingStatuses[loadingStatusIndex];
                summary.textContent = status;
                const loadingText = table.querySelector(".disposition-only-loading-row span:last-child");
                if (loadingText) {
                    loadingText.textContent = status;
                }
                if (loadingStatusIndex === loadingStatuses.length - 1) {
                    window.clearInterval(loadingStatusTimer);
                }
            }, 1800);

            const url = new URL(window.walgreensDispositionOnlyOptInsUrl, window.location.origin);
            url.searchParams.set("startDate", document.getElementById("startDate").value);
            url.searchParams.set("endDate", document.getElementById("endDate").value);
            if (document.getElementById("reportTeam")) url.searchParams.set("team", reportPayload.selectedTeam || "all");
            try {
                const response = await fetch(url.toString(), { credentials: "same-origin" });
                const payload = await response.json();
                if (!response.ok || !payload.ok) {
                    throw new Error(payload.message || "Disposition-only Opt Ins could not be loaded.");
                }

                dispositionOnlyOptInRows = Array.isArray(payload.rows) ? payload.rows : [];
                dispositionOnlyOptInSummary = {
                    confirmedCount: Number(payload.confirmedCount) || 0,
                    leadMatchedCount: Number(payload.leadMatchedCount) || 0,
                    ambiguousCount: Number(payload.ambiguousCount) || 0,
                    unmatchedCount: Number(payload.unmatchedCount) || 0
                };
                renderDispositionOnlyOptIns();
            } catch (error) {
                dispositionOnlyOptInSummary = {
                    confirmedCount: 0,
                    leadMatchedCount: 0,
                    ambiguousCount: 0,
                    unmatchedCount: 0
                };
                document.getElementById("dispositionOnlyOptInsMeta").textContent = "Unavailable";
                summary.textContent = error.message || "Disposition-only Opt Ins could not be loaded.";
                table.innerHTML = "";
                document.getElementById("dispositionOnlyOptInsEmpty").style.display = "block";
                document.getElementById("dispositionOnlyOptInsPageMeta").textContent = "";
            } finally {
                window.clearInterval(loadingStatusTimer);
                if (tableElement) {
                    tableElement.setAttribute("aria-busy", "false");
                }
                downloadButton.disabled = false;
            }
        }

        function downloadDispositionOnlyOptInsCsv() {
            const columns = [
                ["classification", "Classification"],
                ["company", "Company"],
                ["contactFirst", "Contact First"],
                ["contactLast", "Contact Last"],
                ["phone", "Phone"],
                ["agent", "Agent"],
                ["agentId", "Agent Login"],
                ["disposition", "Disposition"],
                ["callAtUtc", "Call UTC"],
                ["matchMethod", "Lead Match"],
                ["leadId", "Lead ID"],
                ["portalStatus", "Portal Status"],
                ["portalRecordId", "Portal Record ID"],
                ["globalId", "Global ID"],
                ["callId", "Call ID"],
                ["notes", "BP Notes"]
            ];
            const csv = [
                columns.map(([, label]) => csvField(label)).join(","),
                ...getVisibleDispositionOnlyOptIns().map(row =>
                    columns.map(([key]) => csvField(
                        key === "phone"
                            ? formatPhone(row[key])
                            : key === "callAtUtc"
                                ? formatAuditTimestamp(row[key])
                                : key === "disposition" ? window.ByteForgeWalgreensLabels.outcome(row[key]) : row[key])).join(","))
            ].join("\r\n");
            downloadTextFile("walgreens-disposition-only-opt-ins.csv", csv, "text/csv;charset=utf-8");
        }

        function getCompanyExportValue(row, key) {
            if (["disposition", "status", "contactStatus"].includes(key)) {
                return window.ByteForgeWalgreensLabels.outcome(row[key]);
            }
            if (key === "phone") {
                return formatPhone(row[key]);
            }
            if (key === "fullAddress") {
                return formatFullAddress(row);
            }
            return row[key];
        }

        function getNiceAxisMaximum(value) {
            if (value <= 0) {
                return 100;
            }

            // Keep 10% headroom and round up to the next multiple of 100.
            return Math.ceil(value * 1.1 / 100) * 100;
        }

        function buildSmoothChartPath(points) {
            if (!points.length) {
                return "";
            }
            if (points.length === 1) {
                return `M ${points[0].x},${points[0].y}`;
            }

            let path = `M ${points[0].x},${points[0].y}`;
            for (let index = 0; index < points.length - 1; index += 1) {
                const previous = points[index - 1] || points[index];
                const current = points[index];
                const next = points[index + 1];
                const following = points[index + 2] || next;
                const control1X = current.x + (next.x - previous.x) / 6;
                const control1Y = current.y + (next.y - previous.y) / 6;
                const control2X = next.x - (following.x - current.x) / 6;
                const control2Y = next.y - (following.y - current.y) / 6;
                path += ` C ${control1X},${control1Y} ${control2X},${control2Y} ${next.x},${next.y}`;
            }
            return path;
        }

        function sumRows(rows, key) {
            return rows.reduce((total, row) => total + (Number(row[key]) || 0), 0);
        }

        function parseDailyRowDate(row) {
            if (isCompanySizeReport && !row.isoDate) {
                return new Date(2000, 0, 1 + (Number(row.sortOrder) || 0));
            }
            return row.isoDate
                ? new Date(`${row.isoDate}T00:00:00`)
                : new Date(`${row.date} ${new Date().getFullYear()} 00:00:00`);
        }

        function dailyColumnKey(row) {
            if (isCompanySizeReport) {
                return `s${row.key || row.sortOrder}`;
            }
            const date = row.isoDate || parseDailyRowDate(row).toISOString().slice(0, 10);
            return `d${date.replace(/-/g, "")}`;
        }

        function buildSourceColumnsFromDailyRows(rows) {
            return [
                { key: "line", label: "Line", kind: "text" },
                ...rows.map(row => ({
                    key: dailyColumnKey(row),
                    label: row.date,
                    kind: "number"
                }))
            ];
        }

        function buildSourceRowsFromDailyRows(rows) {
            const metric = (group, line, selector) => {
                const values = { group, line, isGroup: false };
                rows.forEach(row => {
                    values[dailyColumnKey(row)] = selector(row);
                });
                return values;
            };

            const reportMetrics = [
                metric("Time", "Hours", row => Number(row.hours) || 0),
                metric("Opt-ins", "Opt-ins captured", row => Number(row.optIns) || 0),
                metric("True declines", "True declines", row => Number(row.declines) || 0),
                metric("Unworkables", "Unworkables", row => Number(row.unworkable) || 0),
                metric("Callbacks", "Callbacks", row => Number(row.callbacks) || 0),
                metric("GK Send Info", "Information package requested", row => Number(row.sendInfo) || 0),
                metric("Send Voucher", "Voucher requested", row => Number(row.sendVoucher) || 0),
                metric("Dialing", "Total calls", row => Number(row.dials) || 0),
                metric("Dialing", "Contacts reached", row => getContactsReached(row)),
                metric("Dialing", "Records consumed", row => getRecordsConsumed(row)),
                metric("Program statistics", "Opt-ins per hour", row => safeDivide(Number(row.optIns) || 0, Number(row.hours) || 0)),
                metric("Program statistics", "Contacts per hour", row => safeDivide(getContactsReached(row), Number(row.hours) || 0)),
                metric("Program statistics", "Dial attempts per hour", row => safeDivide(Number(row.dials) || 0, Number(row.hours) || 0)),
                metric("Program statistics", "Conversion", row => safeDivide(Number(row.optIns) || 0, getContactsReached(row)))
            ];
            return isCompanySizeReport
                ? reportMetrics.filter(row => row.group !== "Time"
                    && row.line !== "Total calls"
                    && !row.line.toLowerCase().includes("per hour"))
                : reportMetrics;
        }

        function getContactsReached(row) {
            return (Number(row.optIns) || 0)
                + (Number(row.declines) || 0)
                + (Number(row.callbacks) || 0)
                + (Number(row.sendInfo) || 0)
                + (Number(row.sendVoucher) || 0);
        }

        function getRecordsConsumed(row) {
            return (Number(row.optIns) || 0)
                + (Number(row.declines) || 0)
                + (Number(row.unworkable) || 0)
                + (Number(row.sendInfo) || 0)
                + (Number(row.sendVoucher) || 0);
        }

        function getSelectedDailyRows() {
            const { start, end } = getSelectedDateRange();
            return dailyRows.filter(row => {
                const date = parseDailyRowDate(row);
                return date >= start && date <= end;
            });
        }

        function getBreakdownRows() {
            return isCompanySizeReport ? companySizeRows : getSelectedDailyRows();
        }

        function getCampaignTotals(rows) {
            const optIns = sumRows(rows, "optIns");
            const declines = sumRows(rows, "declines");
            const callbacks = sumRows(rows, "callbacks");
            const unworkable = sumRows(rows, "unworkable");
            const sendInfo = sumRows(rows, "sendInfo");
            const sendVoucher = sumRows(rows, "sendVoucher");
            const dials = sumRows(rows, "dials");
            const hours = sumRows(rows, "hours");
            const contacts = optIns + declines + callbacks + sendInfo + sendVoucher;
            const consumed = optIns + declines + unworkable + sendInfo + sendVoucher;

            return {
                optIns,
                declines,
                callbacks,
                unworkable,
                dials,
                hours,
                contacts,
                consumed,
                captureRate: safeDivide(optIns, contacts) * 100,
                optInsPerHour: safeDivide(optIns, hours),
                contactsPerHour: safeDivide(contacts, hours),
                dialsPerHour: safeDivide(dials, hours)
            };
        }

        function renderCampaignSummary() {
            const totals = getCampaignTotals(getSelectedDailyRows());
            const campaignTotals = getCampaignTotals(dailyRows);
            const optInsRemaining = Math.max(0, campaignGoals.optIns - totals.optIns);
            const campaignOptInsRemaining = Math.max(0, campaignGoals.optIns - campaignTotals.optIns);
            const hoursRemaining = Math.max(0, campaignGoals.hours - totals.hours);
            const paceDelta = totals.optInsPerHour - campaignGoals.optInsPerHour;
            const recordsRemaining = Math.max(0, campaignGoals.listReceived - totals.consumed);
            const penetration = safeDivide(totals.consumed, campaignGoals.listReceived) * 100;

            document.getElementById("recordsConsumedValue").textContent = formatNumber.format(totals.consumed);
            document.getElementById("contactsReachedValue").textContent = formatNumber.format(totals.contacts);
            document.getElementById("optInsCapturedValue").textContent = formatNumber.format(totals.optIns);
            document.getElementById("captureRateValue").textContent = `${totals.captureRate.toFixed(1)}%`;
            document.getElementById("leadPaceValue").textContent = `${totals.optInsPerHour.toFixed(2)}/hr`;
            document.getElementById("goalRemainingValue").textContent = formatNumber.format(campaignOptInsRemaining);

            document.getElementById("optInGoalValue").textContent = formatNumber.format(campaignGoals.optIns);
            document.getElementById("actualOptInGoalValue").textContent = formatNumber.format(totals.optIns);
            document.getElementById("remainingOptInGoalValue").textContent = formatNumber.format(optInsRemaining);
            document.getElementById("hourGoalValue").textContent = campaignGoals.hours.toLocaleString("en-US", { minimumFractionDigits: 1, maximumFractionDigits: 1 });
            document.getElementById("actualHourGoalValue").textContent = totals.hours.toFixed(1);
            document.getElementById("remainingHourGoalValue").textContent = hoursRemaining.toFixed(1);
            document.getElementById("optInsPerHourGoalValue").textContent = `${campaignGoals.optInsPerHour.toFixed(2)}/hr`;
            document.getElementById("currentOptInsPerHourValue").textContent = `${totals.optInsPerHour.toFixed(2)}/hr`;
            document.getElementById("goalStatusValue").textContent = `${paceDelta >= 0 ? "Above" : "Below"} goal by ${Math.abs(paceDelta).toFixed(2)}/hr`;
            document.getElementById("optInGoalProgress").style.setProperty("--progress", `${safeDivide(totals.optIns, campaignGoals.optIns) * 100}%`);

            document.getElementById("recordsReceivedValue").textContent = formatNumber.format(campaignGoals.listReceived);
            document.getElementById("penetrationConsumedValue").textContent = formatNumber.format(totals.consumed);
            document.getElementById("recordsRemainingValue").textContent = formatNumber.format(recordsRemaining);
            document.getElementById("penetrationPercentValue").textContent = `${penetration.toFixed(1)}%`;
            document.getElementById("penetrationProgress").style.setProperty("--progress", `${penetration}%`);

            const selectedRows = getBreakdownRows();
            const bestDay = selectedRows.reduce((best, row) => !best || row.optIns > best.optIns ? row : best, null);
            document.getElementById("bestDayValue").textContent = bestDay
                ? `${bestDay.date}, ${formatNumber.format(bestDay.optIns)} opt-ins`
                : "No data";
            document.getElementById("contactRateValue").textContent = `${(safeDivide(totals.contacts, totals.dials) * 100).toFixed(1)}%`;
            document.getElementById("dialCountValue").textContent = formatNumber.format(totals.dials);
            document.getElementById("hoursValue").textContent = totals.hours.toFixed(1);
            document.getElementById("contactsPerHourValue").textContent = totals.contactsPerHour.toFixed(2);
            document.getElementById("consumedPerHourValue").textContent = safeDivide(totals.consumed, totals.hours).toFixed(2);
            document.getElementById("dialsPerHourValue").textContent = totals.dialsPerHour.toFixed(2);
            document.getElementById("callbackVolumeValue").textContent = formatNumber.format(totals.callbacks);
        }

        function renderDailyChart() {
            const rows = getBreakdownRows();
            const host = document.getElementById("dailyChart");
            host.classList.toggle("company-size-chart", isCompanySizeReport);
            if (!rows.length) {
                host.innerHTML = `<div class="empty-state">No ${isCompanySizeReport ? "company-size" : "daily"} chart rows match the selected date range.</div>`;
                return;
            }

            const maxPlottedOutcomes = Math.max(...rows.map(row =>
                (Number(row.optIns) || 0)
                + (Number(row.declines) || 0)
                + (Number(row.unworkable) || 0)
                + (Number(row.callbacks) || 0)
                + (Number(row.sendInfo) || 0)
                + (Number(row.sendVoucher) || 0)));
            const callAxisMax = getNiceAxisMaximum(maxPlottedOutcomes);
            const callTicks = [callAxisMax, callAxisMax * .75, callAxisMax * .5, callAxisMax * .25, 0];
            const companyAxisMax = isCompanySizeReport
                ? getNiceAxisMaximum(Math.max(...rows.map(row => Number(row.companyCount) || 0)))
                : 0;
            const companyTicks = isCompanySizeReport
                ? [companyAxisMax, companyAxisMax * .75, companyAxisMax * .5, companyAxisMax * .25, 0]
                : [];
            const columns = rows.map(row => {
                const segmentHeight = value => value > 0
                    ? Math.max(3, Math.round(value / callAxisMax * 244))
                    : 0;
                const leadHeight = segmentHeight(row.optIns);
                const declineHeight = segmentHeight(row.declines);
                const unworkableHeight = segmentHeight(row.unworkable);
                const callbackHeight = segmentHeight(row.callbacks);
                const sendInfoHeight = segmentHeight(row.sendInfo);
                const sendVoucherHeight = segmentHeight(row.sendVoucher);
                const optInsPerHour = safeDivide(row.optIns, row.hours);
                const sourceRange = Number(row.minimumEmployees) > 0 && Number(row.maximumEmployees) > 0
                    ? `${formatNumber.format(row.minimumEmployees)}-${formatNumber.format(row.maximumEmployees)}`
                    : "No observed values";
                const tooltip = [
                    `<div class="chart-tooltip-title">${row.date}</div>`,
                    `<p class="chart-tooltip-copy">The stacked bar shows reportable outcomes for this ${isCompanySizeReport ? "company-size band" : "day"}. Each color represents one outcome:</p>`,
                    `<div class="chart-tooltip-row"><span><i class="chart-tooltip-swatch opt-ins"></i>Opt-ins</span><strong>${formatNumber.format(row.optIns)}</strong></div>`,
                    `<div class="chart-tooltip-row"><span><i class="chart-tooltip-swatch true-declines"></i>True Declines</span><strong>${formatNumber.format(row.declines)}</strong></div>`,
                    `<div class="chart-tooltip-row"><span><i class="chart-tooltip-swatch unworkables"></i>Unworkables</span><strong>${formatNumber.format(row.unworkable)}</strong></div>`,
                    `<div class="chart-tooltip-row"><span><i class="chart-tooltip-swatch callbacks"></i>Callbacks</span><strong>${formatNumber.format(row.callbacks)}</strong></div>`,
                    `<div class="chart-tooltip-row"><span><i class="chart-tooltip-swatch" style="background:#8b5cf6"></i>GK Send Info</span><strong>${formatNumber.format(row.sendInfo || 0)}</strong></div>`,
                    `<div class="chart-tooltip-row"><span><i class="chart-tooltip-swatch" style="background:#0f9f8f"></i>Send Voucher</span><strong>${formatNumber.format(row.sendVoucher || 0)}</strong></div>`,
                    `<div class="chart-tooltip-divider"></div>`,
                    `<div class="chart-tooltip-row"><span>Total</span><strong>${formatNumber.format(getContactsReached(row) + (Number(row.unworkable) || 0))}</strong></div>`,
                    isCompanySizeReport ? `<div class="chart-tooltip-row"><span><i class="chart-tooltip-line"></i>Companies</span><strong>${formatNumber.format(row.companyCount || 0)}</strong></div>` : "",
                    isCompanySizeReport && row.sourceDataWarning ? `<div class="chart-tooltip-row chart-tooltip-warning"><span>Observed employee range</span><strong>${sourceRange}</strong></div>` : "",
                    isCompanySizeReport && row.sourceDataWarning ? `<p class="chart-tooltip-copy chart-tooltip-warning-copy">These employee counts may be incorrect in the source data.</p>` : "",
                    `<div class="chart-tooltip-divider"></div>`,
                    isCompanySizeReport ? "" : `<div class="chart-tooltip-row"><span>Dial attempts</span><strong>${formatNumber.format(row.dials)}</strong></div>`,
                    isCompanySizeReport ? "" : `<div class="chart-tooltip-row"><span>Opt-ins/hr</span><strong>${optInsPerHour.toFixed(2)}</strong></div>`,
                    `<div class="chart-tooltip-row"><span>Conversion</span><strong>${row.conversion.toFixed(1)}%</strong></div>`
                ].join("");
                return `
                    <div class="chart-column">
                        <div class="stacked-bar" tabindex="0" aria-label="${escapeHtml(row.date)} outcome breakdown" data-tooltip='${tooltip}'>
                            <div class="stack-segment opt-ins" style="height:${leadHeight}px;display:${leadHeight ? "block" : "none"}"></div>
                            <div class="stack-segment true-declines" style="height:${declineHeight}px;display:${declineHeight ? "block" : "none"}"></div>
                            <div class="stack-segment unworkables" style="height:${unworkableHeight}px;display:${unworkableHeight ? "block" : "none"}"></div>
                            <div class="stack-segment callbacks" style="height:${callbackHeight}px;display:${callbackHeight ? "block" : "none"}"></div>
                            <div class="stack-segment" style="height:${sendInfoHeight}px;display:${sendInfoHeight ? "block" : "none"};background:#8b5cf6"></div>
                            <div class="stack-segment" style="height:${sendVoucherHeight}px;display:${sendVoucherHeight ? "block" : "none"};background:#0f9f8f"></div>
                        </div>
                        <div class="chart-date">${row.date}</div>
                    </div>`;
            }).join("");
            const chartWidth = Math.max(100, rows.length / 10 * 100);
            const companyPoints = isCompanySizeReport
                ? rows.map((row, index) => ({
                    x: rows.length === 1 ? 500 : (index + .5) / rows.length * 1000,
                    y: 244 - safeDivide(Number(row.companyCount) || 0, companyAxisMax) * 244
                }))
                : [];
            const companyCurve = companyPoints.length
                ? `<svg class="company-distribution-curve" viewBox="0 0 1000 244" preserveAspectRatio="none" aria-hidden="true">
                    <path d="${buildSmoothChartPath(companyPoints)}" />
                    ${companyPoints.map(point => `<circle cx="${point.x}" cy="${point.y}" r="5" />`).join("")}
                </svg>`
                : "";
            host.innerHTML = `
                <div>
                    <div class="chart-axis left">${callTicks.map(value => {
                        const tickPosition = (1 - value / callAxisMax) * 100;
                        return `<span style="--tick-position:${tickPosition}%">${formatNumber.format(Math.round(value))}</span>`;
                    }).join("")}</div>
                    <div class="axis-label">Calls</div>
                </div>
                <div class="chart-navigation${isCompanySizeReport ? " is-static" : ""}">
                    ${isCompanySizeReport ? "" : `<button class="chart-scroll-button chart-scroll-previous" type="button" aria-label="View previous chart days" title="Previous days">&#8592;</button>`}
                    <div class="chart-scroll-viewport" tabindex="0" aria-label="${isCompanySizeReport ? "Company-size outcome and distribution chart" : "Daily pacing chart. Scroll horizontally to view more days."}">
                        <div class="chart-plot" style="--chart-day-count:${rows.length};--chart-width:${chartWidth}%">${columns}${companyCurve}</div>
                    </div>
                    ${isCompanySizeReport ? "" : `<button class="chart-scroll-button chart-scroll-next" type="button" aria-label="View next chart days" title="Next days">&#8594;</button>`}
                </div>
                ${isCompanySizeReport ? `<div class="company-chart-axis">
                    <div class="chart-axis right">${companyTicks.map(value => {
                        const tickPosition = (1 - value / companyAxisMax) * 100;
                        return `<span style="--tick-position:${tickPosition}%">${formatNumber.format(Math.round(value))}</span>`;
                    }).join("")}</div>
                    <div class="axis-label">Companies</div>
                </div>` : ""}
                <div class="chart-legend">
                    <span><i class="swatch" style="background:var(--green)"></i> Opt-ins</span>
                    <span><i class="swatch" style="background:var(--brand)"></i> True Declines</span>
                    <span><i class="swatch" style="background:#698493"></i> Unworkables</span>
                    <span><i class="swatch" style="background:#b7791f"></i> Callbacks</span>
                    <span><i class="swatch" style="background:#8b5cf6"></i> GK Send Info</span>
                    <span><i class="swatch" style="background:#0f9f8f"></i> Send Voucher</span>
                    ${isCompanySizeReport ? `<span><i class="curve-swatch"></i> Company distribution</span>` : ""}
                </div>
                ${isCompanySizeReport ? `<p class="company-data-warning">(*) We believe employee counts in the 2,000+ bucket may be incorrect in the source data.</p>` : ""}
            `;
            bindChartNavigation(host);
            bindChartTooltips();
        }

        function bindChartNavigation(host) {
            const viewport = host.querySelector(".chart-scroll-viewport");
            const previousButton = host.querySelector(".chart-scroll-previous");
            const nextButton = host.querySelector(".chart-scroll-next");
            if (!viewport || !previousButton || !nextButton) {
                return;
            }

            const updateButtonState = () => {
                const maxScrollLeft = Math.max(0, viewport.scrollWidth - viewport.clientWidth);
                previousButton.disabled = viewport.scrollLeft <= 1;
                nextButton.disabled = viewport.scrollLeft >= maxScrollLeft - 1;
            };
            const scrollChart = direction => {
                viewport.scrollBy({
                    left: direction * viewport.clientWidth,
                    behavior: "smooth"
                });
            };

            previousButton.addEventListener("click", () => scrollChart(-1));
            nextButton.addEventListener("click", () => scrollChart(1));
            viewport.addEventListener("scroll", updateButtonState, { passive: true });
            window.requestAnimationFrame(updateButtonState);
        }

        function bindChartTooltips() {
            let tooltip = document.getElementById("chartTooltip");
            if (!tooltip) {
                tooltip = document.createElement("div");
                tooltip.id = "chartTooltip";
                tooltip.className = "chart-tooltip";
                tooltip.setAttribute("role", "tooltip");
                document.getElementById("walgreensReportMockup").appendChild(tooltip);
            }

            document.querySelectorAll(".stacked-bar").forEach(bar => {
                const show = event => {
                    tooltip.innerHTML = bar.getAttribute("data-tooltip");
                    tooltip.style.display = "block";
                    bar.setAttribute("aria-describedby", tooltip.id);
                    moveTooltip(event);
                };
                const hide = () => {
                    tooltip.style.display = "none";
                    bar.removeAttribute("aria-describedby");
                };
                const moveTooltip = event => {
                    const point = event.touches ? event.touches[0] : event;
                    const x = Math.min(window.innerWidth - 270, point.clientX + 14);
                    const y = Math.min(window.innerHeight - 160, point.clientY + 14);
                    tooltip.style.left = `${Math.max(10, x)}px`;
                    tooltip.style.top = `${Math.max(10, y)}px`;
                };
                bar.addEventListener("mouseenter", show);
                bar.addEventListener("mousemove", moveTooltip);
                bar.addEventListener("mouseleave", hide);
                bar.addEventListener("focus", event => {
                    const rect = bar.getBoundingClientRect();
                    show({ clientX: rect.left + rect.width / 2, clientY: rect.top });
                });
                bar.addEventListener("blur", hide);
            });
        }

        function renderMixList() {
            const total = dispositionMix.reduce((sum, row) => sum + row.value, 0);
            const host = document.getElementById("mixList");
            if (!total) {
                host.innerHTML = `<div class="empty-state">No disposition data is available for the selected Walgreens report period.</div>`;
                return;
            }
            host.innerHTML = dispositionMix.map((row, index) => {
                const childRows = Array.isArray(row.children) ? row.children : [];
                if (childRows.length <= 1) {
                    return `
                        <div class="mix-group">
                            <div class="mix-row group-total">
                                <span class="swatch" style="background:${row.color}"></span>
                                <span>${window.ByteForgeWalgreensLabels.outcome(row.label)}</span>
                                <span class="mix-value">
                                    ${formatNumber.format(row.value)} · ${Math.round(row.value / total * 100)}%
                                </span>
                            </div>
                        </div>`;
                }

                const isCollapsed = collapsedMixGroups.has(row.label);
                const children = childRows.map(child => `
                    <div class="mix-row child-row">
                        <span class="swatch" style="background:${child.color}"></span>
                        <span>${window.ByteForgeWalgreensLabels.outcome(child.label)}</span>
                        <span class="mix-value">
                            ${formatNumber.format(child.value)} · ${Math.round(child.value / row.value * 100)}%
                        </span>
                    </div>`).join("");

                return `
                    <div class="mix-group">
                        <div class="mix-row group-total">
                            <button class="mix-toggle" type="button" style="color:${row.color};border-color:${row.color}" aria-expanded="${!isCollapsed}" aria-controls="mixGroup${index}" data-mix-group="${row.label}">${isCollapsed ? "+" : "-"}</button>
                            <span>${window.ByteForgeWalgreensLabels.outcome(row.label)}</span>
                            <span class="mix-value">
                                ${formatNumber.format(row.value)} · ${Math.round(row.value / total * 100)}%
                            </span>
                        </div>
                        <div class="mix-children" id="mixGroup${index}" ${isCollapsed ? "hidden" : ""}>
                            ${children}
                        </div>
                    </div>`;
            }).join("") + `
                <div class="mix-row group-total">
                    <span></span>
                    <span>Total</span>
                    <span class="mix-value">${formatNumber.format(total)} · 100%</span>
                </div>`;

            host.querySelectorAll("[data-mix-group]").forEach(button => {
                button.addEventListener("click", () => {
                    const group = button.getAttribute("data-mix-group");
                    if (collapsedMixGroups.has(group)) {
                        collapsedMixGroups.delete(group);
                    } else {
                        collapsedMixGroups.add(group);
                    }
                    renderMixList();
                });
            });
        }

        function compareValues(a, b, key) {
            const av = a[key];
            const bv = b[key];
            if (typeof av === "number" && typeof bv === "number") {
                return av - bv;
            }
            return String(av).localeCompare(String(bv), undefined, { numeric: true, sensitivity: "base" });
        }

        /**
         * Compares Daily Report Detail rows while keeping the friendly date label display-only.
         * @param {Record<string, unknown>} a The first daily row.
         * @param {Record<string, unknown>} b The second daily row.
         * @param {string} key The selected sort key.
         * @returns {number} The comparison result.
         */
        function compareDailyRows(a, b, key) {
            if (key === "date") {
                return parseDailyRowDate(a).getTime() - parseDailyRowDate(b).getTime();
            }

            return compareValues(a, b, key);
        }

        function escapeHtml(value) {
            return String(value ?? "")
                .replace(/&/g, "&amp;")
                .replace(/</g, "&lt;")
                .replace(/>/g, "&gt;")
                .replace(/"/g, "&quot;")
                .replace(/'/g, "&#39;");
        }

        function isSourcePercentageRow(row) {
            return String(row?.group || "").trim().toLowerCase() === "program statistics"
                && String(row?.line || "").trim().toLowerCase() === "conversion";
        }

        function formatReportValue(value, key, row) {
            if (["line", "disposition", "status", "contactStatus"].includes(key)) {
                value = window.ByteForgeWalgreensLabels.outcome(value);
            }
            if (value === "" || value === null || value === undefined) {
                return "";
            }

            if (typeof value !== "number") {
                return escapeHtml(value);
            }

            if (key === "note" || key === "companyGoal" || key === "phaseValue") {
                return escapeHtml(value);
            }

            if (isSourcePercentageRow(row)) {
                return `${(value * 100).toFixed(1)}%`;
            }

            if (!Number.isInteger(value)) {
                return value.toFixed(2);
            }

            return formatNumber.format(value);
        }

        /**
         * Gets the visual tone used for a grouped report detail header.
         * @param {string} group The workbook group name.
         * @returns {string} The tone key used by the group-row CSS rules.
         */
        function sourceGroupTone(group) {
            const normalized = String(group).toLowerCase();
            if (normalized.includes("opt")) {
                return "optins";
            }
            if (normalized.includes("decline")) {
                return "declines";
            }
            if (normalized.includes("unwork")) {
                return "unworkables";
            }
            if (normalized.includes("callback")) {
                return "callbacks";
            }
            return "operations";
        }

        /**
         * Reads the currently selected report date range.
         * @returns {{ start: Date, end: Date }} The normalized start and end dates.
         */
        function getSelectedDateRange() {
            const start = new Date(`${document.getElementById("startDate").value}T00:00:00`);
            const end = new Date(`${document.getElementById("endDate").value}T00:00:00`);

            if (start > end) {
                return { start: end, end: start };
            }

            return { start, end };
        }

        /**
         * Parses a grouped report daily column key into a date value.
         * @param {string} key The generated column key, such as d20220920.
         * @returns {Date|null} The parsed date, or null when the key is not a daily column.
         */
        function parseSourceColumnDate(key) {
            const match = /^d(\d{4})(\d{2})(\d{2})$/.exec(key);
            if (!match) {
                return null;
            }

            return new Date(`${match[1]}-${match[2]}-${match[3]}T00:00:00`);
        }

        /**
         * Gets the grouped report columns that should be visible for the selected range.
         * @returns {Array<{ key: string, label: string, kind: string }>} The visible grouped report columns.
         */
        function getVisibleSourceColumns() {
            const { start, end } = getSelectedDateRange();
            return sourceColumns.filter(column => {
                if (hiddenSourceColumnKeys.has(column.key)) {
                    return false;
                }

                const columnDate = parseSourceColumnDate(column.key);
                return !columnDate || (columnDate >= start && columnDate <= end);
            });
        }

        /**
         * Gets the visible daily grouped report columns for line total calculations.
         * @returns {Array<{ key: string, label: string, kind: string }>} The visible daily columns.
         */
        function getVisibleSourceDailyColumns() {
            return getVisibleSourceColumns().filter(column => isCompanySizeReport
                ? /^s/.test(column.key)
                : parseSourceColumnDate(column.key));
        }

        function renderSourceHeader() {
            const visibleSourceColumns = getVisibleSourceColumns();
            document.getElementById("sourceHeader").innerHTML = `
                <tr>
                    ${visibleSourceColumns.map(column => `
                        <th class="${column.kind === "number" || column.kind === "mixed" ? "numeric" : ""}">
                            ${escapeHtml(column.label)}
                        </th>
                    `).join("")}
                    <th class="numeric">Line total</th>
                </tr>`;
        }

        function renderDailyTable() {
            window.ByteForgeSortIndicator.update("[data-daily-sort]", "data-daily-sort", dailySort);
            const search = document.getElementById("dailySearch").value.trim().toLowerCase();
            const rows = getBreakdownRows()
                .map(row => ({
                    ...row,
                    optInsPerHour: safeDivide(row.optIns, row.hours)
                }))
                .filter(row => !search || Object.values(row).some(value => String(value).toLowerCase().includes(search)))
                .sort((a, b) => compareDailyRows(a, b, dailySort.key) * (dailySort.direction === "asc" ? 1 : -1));
            document.getElementById("dailyTable").innerHTML = rows.map(row => `
                <tr>
                    <td>${row.date}</td>
                    <td class="numeric">${formatNumber.format(row.optIns)}</td>
                    <td class="numeric">${formatNumber.format(row.declines)}</td>
                    <td class="numeric">${formatNumber.format(row.callbacks)}</td>
                    <td class="numeric">${formatNumber.format(Number(row.sendInfo) || 0)}</td>
                    <td class="numeric">${formatNumber.format(row.unworkable)}</td>
                    <td class="numeric">${formatNumber.format(row.dials)}</td>
                    <td class="numeric">${row.hours.toFixed(1)}</td>
                    <td class="numeric">${row.optInsPerHour.toFixed(2)}</td>
                    <td class="numeric">${safeDivide(getContactsReached(row), row.hours).toFixed(2)}</td>
                    <td class="numeric">${safeDivide(row.dials, row.hours).toFixed(2)}</td>
                    <td class="numeric">${row.conversion.toFixed(1)}%</td>
                </tr>`).join("");
            document.getElementById("dailyEmpty").style.display = rows.length ? "none" : "block";
        }

        function getVisibleAgentRows() {
            const { start, end } = getSelectedDateRange();
            const search = document.getElementById("agentSearch").value.trim().toLowerCase();
            const agentMap = new Map();

            optInCompanyRows.forEach(row => {
                const date = new Date(`${row.date}T00:00:00`);
                if (date < start || date > end) {
                    return;
                }

                if (!agentMap.has(row.agent)) {
                    agentMap.set(row.agent, {
                        agent: row.agent,
                        optIns: 0,
                        largeOptIns: 0,
                        employees: 0,
                        firstDate: row.date,
                        lastDate: row.date
                    });
                }

                const agent = agentMap.get(row.agent);
                agent.optIns += 1;
                agent.largeOptIns += Number(row.employees) >= 100 ? 1 : 0;
                agent.employees += Number(row.employees) || 0;
                agent.firstDate = row.date < agent.firstDate ? row.date : agent.firstDate;
                agent.lastDate = row.date > agent.lastDate ? row.date : agent.lastDate;
            });

            const rows = Array.from(agentMap.values());
            const totalOptIns = rows.reduce((total, row) => total + row.optIns, 0);
            return rows
                .map(row => ({
                    ...row,
                    share: safeDivide(row.optIns, totalOptIns) * 100
                }))
                .filter(row => !search || Object.values(row).some(value => String(value).toLowerCase().includes(search)))
                .sort((a, b) => compareValues(a, b, agentSort.key) * (agentSort.direction === "asc" ? 1 : -1));
        }

        function renderAgentTable() {
            window.ByteForgeSortIndicator.update("[data-agent-sort]", "data-agent-sort", agentSort);
            const rows = getVisibleAgentRows();
            document.getElementById("agentTable").innerHTML = rows.map(row => `
                <tr>
                    <td>${escapeHtml(row.agent)}</td>
                    <td class="numeric">${formatNumber.format(row.optIns)}</td>
                    <td class="numeric">${formatNumber.format(row.largeOptIns)}</td>
                    <td class="numeric">${formatNumber.format(row.employees)}</td>
                    <td class="numeric">${row.share.toFixed(1)}%</td>
                    <td>${escapeHtml(formatAppliedDate(row.firstDate))}</td>
                    <td>${escapeHtml(formatAppliedDate(row.lastDate))}</td>
                </tr>`).join("");
            document.getElementById("agentEmpty").style.display = rows.length ? "none" : "block";
        }

        /**
         * Gets the opt-in company rows matching the current search and sort settings.
         * @returns {Array<object>} The filtered and sorted opt-in company rows.
         */
        function getVisibleOptInCompanies() {
            const search = document.getElementById("optInCompanySearch").value.trim().toLowerCase();
            return optInCompanyRows
                .filter(row => !search || Object.values(row).some(value => String(value).toLowerCase().includes(search)))
                .sort((a, b) => compareValues(a, b, optInCompanySort.key) * (optInCompanySort.direction === "asc" ? 1 : -1));
        }

        /**
         * Renders the current opt-in company page in the dialog table.
         * @returns {void}
         */
        function renderOptInCompanyTable() {
            window.ByteForgeSortIndicator.update("[data-company-sort]", "data-company-sort", optInCompanySort);
            const rows = getVisibleOptInCompanies();
            const totalPages = Math.max(1, Math.ceil(rows.length / optInCompanyPageSize));
            optInCompanyPage = Math.min(optInCompanyPage, totalPages);
            const startIndex = (optInCompanyPage - 1) * optInCompanyPageSize;
            const pageRows = rows.slice(startIndex, startIndex + optInCompanyPageSize);
            const pageStart = rows.length ? startIndex + 1 : 0;
            const pageEnd = Math.min(startIndex + optInCompanyPageSize, rows.length);

            document.getElementById("optInCompanyTable").innerHTML = pageRows.map(row => `
                <tr>
                    ${renderRecordEditButton(row, "ClinicSubmission")}
                    ${renderRecordExplanationButton(row, "ClinicSubmission")}
                    <td class="company-name">${escapeHtml(row.company)}</td>
                    <td>${escapeHtml(row.city)}</td>
                    <td>${escapeHtml(row.state)}</td>
                    <td>${escapeHtml([row.contactFirst, row.contactLast].filter(Boolean).join(" "))}</td>
                    <td>${escapeHtml(row.email)}</td>
                    <td>${escapeHtml(formatPhone(row.phone))}</td>
                    <td class="numeric">${formatNumber.format(row.employees || 0)}</td>
                    ${isCompanySizeReport ? `<td>${escapeHtml(row.companySize)}</td>` : ""}
                    <td>${escapeHtml(row.program)}</td>
                    <td>${escapeHtml(window.ByteForgeWalgreensLabels.outcome(row.status))}</td>
                    <td>${escapeHtml(row.date)}</td>
                    <td>${escapeHtml(window.ByteForgeWalgreensLabels.outcome(row.disposition))}</td>
                    <td><code>${escapeHtml(row.globalId)}</code></td>
                    ${renderReportNotesButton(row, "ClinicSubmission")}
                </tr>`).join("");

            document.getElementById("optInCompanyEmpty").style.display = rows.length ? "none" : "block";
            document.getElementById("optInCompanyMeta").textContent = `${formatNumber.format(optInCompanyRows.length)} companies captured`;
            optInCompanyPage = WalgreensAdminListTools.renderPagination({ summaryElement: document.getElementById("optInCompanyPageMeta"), buttonsElement: document.getElementById("optInCompanyPagination"), totalRows: rows.length, pageSize: optInCompanyPageSize, currentPage: optInCompanyPage, onPageChange: target => { optInCompanyPage = target; renderOptInCompanyTable(); } });
        }

        /**
         * Escapes a value for a CSV field.
         * @param {unknown} value The value to serialize.
         * @returns {string} The CSV-safe field value.
         */
        function csvField(value) {
            const text = String(value ?? "");
            if (/[",\r\n]/.test(text)) {
                return `"${text.replace(/"/g, "\"\"")}"`;
            }
            return text;
        }

        /**
         * Starts a browser download for generated file content.
         * @param {string} fileName The suggested file name.
         * @param {string} content The generated file content.
         * @param {string} mimeType The file MIME type.
         * @returns {void}
         */
        function downloadTextFile(fileName, content, mimeType) {
            const blob = new Blob(["\uFEFF", content], { type: mimeType });
            const url = URL.createObjectURL(blob);
            const link = document.createElement("a");
            link.href = url;
            link.download = fileName;
            document.body.appendChild(link);
            link.click();
            link.remove();
            window.setTimeout(() => URL.revokeObjectURL(url), 0);
        }

        /**
         * Builds a stable file-name suffix from the selected report dates.
         * @returns {string} The selected date-range suffix.
         */
        function getReportFileDateSuffix() {
            const start = document.getElementById("startDate").value;
            const end = document.getElementById("endDate").value;
            return start === end ? start : `${start}_to_${end}`;
        }

        /**
         * Extracts the server-provided download name from a Content-Disposition header.
         * @param {string|null} contentDisposition The response header value.
         * @returns {string} The decoded file name, or an empty string when unavailable.
         */
        function getDownloadFileName(contentDisposition) {
            if (!contentDisposition) {
                return "";
            }

            const encodedMatch = contentDisposition.match(/filename\*=UTF-8''([^;]+)/i);
            if (encodedMatch) {
                try {
                    return decodeURIComponent(encodedMatch[1]);
                } catch {
                    return encodedMatch[1];
                }
            }

            const plainMatch = contentDisposition.match(/filename="?([^";]+)"?/i);
            return plainMatch ? plainMatch[1] : "";
        }

        /**
         * Downloads the complete selected report range as an XLSX workbook.
         * @returns {Promise<void>} A task that completes once the workbook reaches the browser.
         */
        async function downloadReportWorkbook() {
            const button = document.getElementById("exportReportWorkbookButton");
            const exportUrl = button?.dataset.exportUrl;
            if (!exportUrl) {
                window.alert("The workbook export URL is unavailable. Refresh the page and try again.");
                return;
            }

            if (button.disabled) {
                return;
            }

            const url = new URL(exportUrl, window.location.origin);
            url.searchParams.set("startDate", document.getElementById("startDate").value);
            url.searchParams.set("endDate", document.getElementById("endDate").value);
            if (document.getElementById("reportTeam")) url.searchParams.set("team", document.getElementById("reportTeam").value);

            const originalButtonText = button.textContent;
            button.disabled = true;
            button.setAttribute("aria-busy", "true");
            button.textContent = "Preparing XLSX...";

            try {
                const response = await fetch(url.toString(), {
                    credentials: "same-origin",
                    headers: {
                        Accept: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                    }
                });
                const contentType = response.headers.get("Content-Type") || "";
                if (!response.ok || !contentType.toLowerCase().includes("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")) {
                    throw new Error("The workbook could not be generated.");
                }

                const workbook = await response.blob();
                const fileName = getDownloadFileName(response.headers.get("Content-Disposition"))
                    || "walgreens-campaign-report.xlsx";
                const downloadUrl = URL.createObjectURL(workbook);
                const downloadLink = document.createElement("a");
                downloadLink.href = downloadUrl;
                downloadLink.download = fileName;
                downloadLink.style.display = "none";
                document.body.appendChild(downloadLink);
                downloadLink.click();
                downloadLink.remove();
                window.setTimeout(() => URL.revokeObjectURL(downloadUrl), 0);
            } catch (error) {
                console.error("Workbook export failed.", error);
                window.alert("The XLSX download failed. Please try again.");
            } finally {
                button.disabled = false;
                button.removeAttribute("aria-busy");
                button.textContent = originalButtonText;
            }
        }

        /**
         * Shows the shared no-contact report loading indicator and rotates honest phase messages.
         * @param {string} title The loading activity heading.
         * @param {string[]} messages Ordered phase messages shown while the request is pending.
         * @returns {Function|null} A function that stops and hides the loading indicator.
         */
        function startContactableListLoading(title, messages) {
            const dialog = document.getElementById("contactableListDialog");
            const loading = document.getElementById("contactableListLoading");
            const loadingTitle = document.getElementById("contactableListLoadingTitle");
            const loadingMessage = document.getElementById("contactableListLoadingMessage");
            const progress = loading && loading.querySelector(".contactable-list-progress");
            if (!dialog || !loading || !loadingTitle || !loadingMessage || !progress || !messages.length) {
                return null;
            }

            let loadingMessageIndex = 0;
            loading.hidden = false;
            loadingTitle.textContent = title;
            loadingMessage.textContent = messages[0];
            progress.setAttribute("aria-valuetext", messages[0]);
            dialog.setAttribute("aria-busy", "true");
            const loadingMessageTimer = window.setInterval(() => {
                loadingMessageIndex = Math.min(loadingMessageIndex + 1, messages.length - 1);
                loadingMessage.textContent = messages[loadingMessageIndex];
                progress.setAttribute("aria-valuetext", messages[loadingMessageIndex]);
                if (loadingMessageIndex === messages.length - 1) {
                    window.clearInterval(loadingMessageTimer);
                }
            }, 1400);

            return () => {
                window.clearInterval(loadingMessageTimer);
                loading.hidden = true;
                dialog.removeAttribute("aria-busy");
            };
        }

        /** Loads current metadata for the Admin no-contact status report. */
        async function loadContactableListMetadata() {
            if (contactableListMetadataLoaded || contactableListMetadataLoading) {
                return;
            }

            const status = document.getElementById("contactableListStatus");
            const metrics = document.getElementById("contactableListMetrics");
            const details = document.getElementById("contactableListDetails");
            const downloadButton = document.getElementById("downloadContactableListButton");
            if (!status || !metrics || !details || !downloadButton || !window.walgreensContactableListMetadataUrl) {
                return;
            }

            const loadingMessages = [
                "Gathering current Walgreens records...",
                "Reconciling saved outcomes with call detail...",
                "Counting report classifications..."
            ];
            contactableListMetadataLoading = true;
            const stopLoading = startContactableListLoading("Preparing current report details", loadingMessages);
            status.classList.remove("is-error");
            status.hidden = true;
            metrics.hidden = true;
            details.hidden = true;
            downloadButton.disabled = true;

            try {
                const response = await fetch(window.walgreensContactableListMetadataUrl, {
                    credentials: "same-origin",
                    headers: { Accept: "application/json" }
                });
                const payload = await response.json();
                if (!response.ok || !payload.ok) {
                    throw new Error(payload.message || "The list metadata could not be loaded.");
                }

                document.getElementById("contactableListTotal").textContent = formatNumber.format(payload.totalCount || 0);
                document.getElementById("contactableListNeverContacted").textContent = formatNumber.format(payload.neverContactedCount || 0);
                document.getElementById("contactableListUnworkable").textContent = formatNumber.format(payload.unworkableCount || 0);
                document.getElementById("contactableListSendInfo").textContent = formatNumber.format(payload.sendInfoCount || 0);
                document.getElementById("contactableListSendVoucher").textContent = formatNumber.format(payload.sendVoucherCount || 0);
                document.getElementById("contactableListOptIn").textContent = formatNumber.format(payload.optInCount || 0);
                document.getElementById("contactableListCallback").textContent = formatNumber.format(payload.callbackCount || 0);
                document.getElementById("contactableListTrueDecline").textContent = formatNumber.format(payload.trueDeclineCount || 0);
                document.getElementById("contactableListSource").textContent = payload.sourceTable || "Campaigns";
                document.getElementById("contactableListScope").textContent = payload.scopeDescription || "";
                document.getElementById("contactableListDeliveryNote").textContent = payload.deliveryNote || "";
                document.getElementById("contactableListDispositions").textContent =
                    Array.isArray(payload.unworkableDispositions)
                        ? payload.unworkableDispositions.join(", ")
                        : "";
                const reconciliationWarnings = Array.isArray(payload.reconciliationWarnings)
                    ? payload.reconciliationWarnings
                    : [];
                document.getElementById("contactableListReconciliation").textContent = reconciliationWarnings.length
                    ? reconciliationWarnings.join(" ")
                    : "All disposition detail counts match the full-campaign report.";
                const generatedAt = new Date(payload.generatedAtUtc);
                document.getElementById("contactableListGenerated").textContent = Number.isNaN(generatedAt.getTime())
                    ? payload.generatedAtUtc || ""
                    : `${new Intl.DateTimeFormat("en-US", {
                        dateStyle: "medium",
                        timeStyle: "short",
                        timeZone: "UTC"
                    }).format(generatedAt)} UTC`;

                status.textContent = reconciliationWarnings.length
                    ? `Warning: ${reconciliationWarnings.length} disposition count mismatch${reconciliationWarnings.length === 1 ? "" : "es"}. The XLSX remains available; see Reconciliation below.`
                    : "The counts below are current as of the generated time. All disposition detail counts reconcile.";
                metrics.hidden = false;
                details.hidden = false;
                downloadButton.disabled = Number(payload.totalCount || 0) === 0;
                contactableListMetadataLoaded = true;
            } catch (error) {
                console.error("Contact-list metadata failed.", error);
                status.classList.add("is-error");
                status.textContent = error.message || "The list metadata could not be loaded.";
            } finally {
                contactableListMetadataLoading = false;
                status.hidden = false;
                if (stopLoading) {
                    stopLoading();
                }
            }
        }

        /** Downloads the current Admin no-contact status workbook. */
        async function downloadContactableListWorkbook() {
            const button = document.getElementById("downloadContactableListButton");
            if (!button || button.disabled || !window.walgreensContactableListDownloadUrl) {
                return;
            }

            const originalButtonText = button.textContent;
            const stopLoading = startContactableListLoading("Preparing report XLSX", [
                "Gathering the current report rows...",
                "Building and formatting the workbook...",
                "Finishing the XLSX download..."
            ]);
            button.disabled = true;
            button.setAttribute("aria-busy", "true");
            button.textContent = "Preparing XLSX...";
            try {
                const response = await fetch(window.walgreensContactableListDownloadUrl, {
                    credentials: "same-origin",
                    headers: {
                        Accept: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                    }
                });
                const contentType = response.headers.get("Content-Type") || "";
                if (!response.ok || !contentType.toLowerCase().includes("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")) {
                    throw new Error("The no-contact report could not be generated.");
                }

                const workbook = await response.blob();
                const fileName = getDownloadFileName(response.headers.get("Content-Disposition"))
                    || "walgreens-no-contact-report.xlsx";
                const workbookUrl = URL.createObjectURL(workbook);
                const downloadLink = document.createElement("a");
                downloadLink.href = workbookUrl;
                downloadLink.download = fileName;
                downloadLink.style.display = "none";
                document.body.appendChild(downloadLink);
                downloadLink.click();
                downloadLink.remove();
                window.setTimeout(() => URL.revokeObjectURL(workbookUrl), 0);
            } catch (error) {
                console.error("Contact-list export failed.", error);
                window.alert("The contact-list XLSX download failed. Please try again.");
            } finally {
                button.disabled = false;
                button.removeAttribute("aria-busy");
                button.textContent = originalButtonText;
                if (stopLoading) {
                    stopLoading();
                }
            }
        }

        /**
         * Captures the complete admin report page and saves it as one image-backed PDF page.
         * @returns {Promise<void>} A task that completes when the PDF has been generated.
         */
        async function downloadReportPdf() {
            refreshReport();
            const button = document.getElementById("downloadReportPdfButton");
            const originalButtonText = button.textContent;
            const captureTarget = document.querySelector(".admin-shell");
            const originalScrollX = window.scrollX;
            const originalScrollY = window.scrollY;

            if (!captureTarget || typeof window.html2canvas !== "function" || !window.jspdf?.jsPDF) {
                window.alert("The PDF capture tools could not be loaded. Refresh the page and try again.");
                return;
            }

            button.disabled = true;
            button.setAttribute("aria-busy", "true");
            document.documentElement.classList.add("pdf-capture-mode");
            window.scrollTo(0, 0);

            try {
                await new Promise(resolve => window.requestAnimationFrame(() => window.requestAnimationFrame(resolve)));
                const backgroundColor = window.getComputedStyle(document.body).backgroundColor;
                const canvas = await window.html2canvas(captureTarget, {
                    backgroundColor,
                    scale: Math.min(2, Math.max(1, window.devicePixelRatio || 1)),
                    useCORS: true,
                    logging: false,
                    scrollX: 0,
                    scrollY: 0,
                    windowWidth: captureTarget.scrollWidth,
                    windowHeight: captureTarget.scrollHeight
                });
                const pageWidth = 792;
                const pageHeight = pageWidth * canvas.height / canvas.width;
                const pdf = new window.jspdf.jsPDF({
                    orientation: pageHeight > pageWidth ? "portrait" : "landscape",
                    unit: "pt",
                    format: [pageWidth, pageHeight],
                    compress: true,
                    hotfixes: ["px_scaling"]
                });

                pdf.addImage(
                    canvas.toDataURL("image/jpeg", 0.95),
                    "JPEG",
                    0,
                    0,
                    pageWidth,
                    pageHeight,
                    undefined,
                    "FAST");
                pdf.save(`walgreens-campaign-report_${getReportFileDateSuffix()}.pdf`);
            } catch (error) {
                console.error("Walgreens report PDF capture failed.", error);
                window.alert("The report could not be captured as a PDF. Please try again.");
            } finally {
                document.documentElement.classList.remove("pdf-capture-mode");
                window.scrollTo(originalScrollX, originalScrollY);
                button.disabled = false;
                button.removeAttribute("aria-busy");
                button.textContent = originalButtonText;
            }
        }

        /**
         * Downloads all opt-in company rows as a CSV file.
         * @returns {void}
         */
        function downloadOptInCompaniesCsv() {
            const columns = [
                ["date", "Date"],
                ["disposition", "Disposition"],
                ["globalId", "Global ID"],
                ["program", "Program"],
                ["status", "Status"],
                ["agent", "Agent"],
                ["company", "Company"],
                ["contactFirst", "Contact first"],
                ["contactLast", "Contact last"],
                ["email", "Email"],
                ["phone", "Phone"],
                ["employees", "Employees"],
                ["companySize", "Company size"],
                ["address", "Address"],
                ["city", "City"],
                ["state", "State"],
                ["zip", "Zip"],
                ["agentNotes", "Agent Notes"],
                ["bpNotes", "BP Notes"]
            ];
            const lines = [
                columns.map(([, label]) => csvField(label)).join(","),
                ...optInCompanyRows.map(row => columns.map(([key]) => csvField(getCompanyExportValue(row, key))).join(","))
            ];
            const blob = new Blob([lines.join("\r\n")], { type: "text/csv;charset=utf-8" });
            const link = document.createElement("a");
            link.href = URL.createObjectURL(blob);
            link.download = "walgreens-opt-in-companies.csv";
            document.body.appendChild(link);
            link.click();
            URL.revokeObjectURL(link.href);
            link.remove();
        }

        /**
         * Gets information-package request rows matching the current search and sort settings.
         * @returns {Array<object>} The filtered and sorted request rows.
         */
        function getVisibleInfoPackageCompanies() {
            const search = document.getElementById("infoPackageCompanySearch").value.trim().toLowerCase();
            return infoPackageCompanyRows
                .filter(row => !search || Object.values(row).some(value => String(value).toLowerCase().includes(search)))
                .sort((a, b) => compareValues(a, b, infoPackageCompanySort.key) * (infoPackageCompanySort.direction === "asc" ? 1 : -1));
        }

        /**
         * Renders the current information-package request page.
         * @returns {void}
         */
        function renderInfoPackageCompanyTable() {
            window.ByteForgeSortIndicator.update(
                "[data-info-package-sort]",
                "data-info-package-sort",
                infoPackageCompanySort);
            const rows = getVisibleInfoPackageCompanies();
            const totalPages = Math.max(1, Math.ceil(rows.length / infoPackageCompanyPageSize));
            infoPackageCompanyPage = Math.min(infoPackageCompanyPage, totalPages);
            const startIndex = (infoPackageCompanyPage - 1) * infoPackageCompanyPageSize;
            const pageRows = rows.slice(startIndex, startIndex + infoPackageCompanyPageSize);
            const pageStart = rows.length ? startIndex + 1 : 0;
            const pageEnd = Math.min(startIndex + infoPackageCompanyPageSize, rows.length);

            document.getElementById("infoPackageCompanyTable").innerHTML = pageRows.map(row => `
                <tr>
                    ${renderRecordEditButton(row, "SendInfo")}
                    ${renderRecordExplanationButton(row, "SendInfo")}
                    <td class="company-name">${escapeHtml(row.company)}</td>
                    <td>${escapeHtml(row.city)}</td>
                    <td>${escapeHtml(row.state)}</td>
                    <td>${escapeHtml([row.contactFirst, row.contactLast].filter(Boolean).join(" "))}</td>
                    <td>${escapeHtml(row.jobTitle)}</td>
                    <td>${escapeHtml(row.email)}</td>
                    <td>${escapeHtml(formatPhone(row.phone))}</td>
                    ${isCompanySizeReport ? `<td class="numeric">${formatNumber.format(row.employees || 0)}</td><td>${escapeHtml(row.companySize)}</td>` : ""}
                    <td>${escapeHtml(row.program)}</td>
                    <td>${escapeHtml(row.agent)}</td>
                    <td>${escapeHtml(window.ByteForgeWalgreensLabels.outcome(row.status))}</td>
                    <td>${escapeHtml(row.date)}</td>
                    <td><code>${escapeHtml(row.globalId)}</code></td>
                    ${renderReportNotesButton(row, "SendInfo")}
                </tr>`).join("");

            document.getElementById("infoPackageCompanyEmpty").style.display = rows.length ? "none" : "block";
            document.getElementById("infoPackageCompanyMeta").textContent = `${formatNumber.format(infoPackageCompanyRows.length)} companies requested information`;
            infoPackageCompanyPage = WalgreensAdminListTools.renderPagination({ summaryElement: document.getElementById("infoPackageCompanyPageMeta"), buttonsElement: document.getElementById("infoPackageCompanyPagination"), totalRows: rows.length, pageSize: infoPackageCompanyPageSize, currentPage: infoPackageCompanyPage, onPageChange: target => { infoPackageCompanyPage = target; renderInfoPackageCompanyTable(); } });
        }

        /**
         * Downloads all information-package request rows as a CSV file.
         * @returns {void}
         */
        function downloadInfoPackageCompaniesCsv() {
            const columns = [
                ["date", "Requested date"],
                ["globalId", "Global ID"],
                ["status", "Status"],
                ["agent", "Agent"],
                ["company", "Company"],
                ["contactFirst", "Contact first"],
                ["contactLast", "Contact last"],
                ["jobTitle", "Job title"],
                ["email", "Email"],
                ["phone", "Phone"],
                ["employees", "Employees"],
                ["companySize", "Company size"],
                ["program", "Program"],
                ["city", "City"],
                ["state", "State"],
                ["agentNotes", "Agent Notes"],
                ["bpNotes", "BP Notes"]
            ];
            const lines = [
                columns.map(([, label]) => csvField(label)).join(","),
                ...infoPackageCompanyRows.map(row => columns.map(([key]) => csvField(getCompanyExportValue(row, key))).join(","))
            ];
            const blob = new Blob([lines.join("\r\n")], { type: "text/csv;charset=utf-8" });
            const link = document.createElement("a");
            link.href = URL.createObjectURL(blob);
            link.download = "walgreens-gk-send-info-requests.csv";
            document.body.appendChild(link);
            link.click();
            URL.revokeObjectURL(link.href);
            link.remove();
        }

        function getVisibleSendVoucherCompanies() {
            const search = document.getElementById("sendVoucherCompanySearch").value.trim().toLowerCase();
            return sendVoucherCompanyRows
                .filter(row => !search || Object.values(row).some(value => String(value).toLowerCase().includes(search)))
                .sort((a, b) => compareValues(a, b, sendVoucherCompanySort.key) * (sendVoucherCompanySort.direction === "asc" ? 1 : -1));
        }

        function renderSendVoucherCompanyTable() {
            window.ByteForgeSortIndicator.update(
                "[data-send-voucher-sort]",
                "data-send-voucher-sort",
                sendVoucherCompanySort);
            const rows = getVisibleSendVoucherCompanies();
            const totalPages = Math.max(1, Math.ceil(rows.length / sendVoucherCompanyPageSize));
            sendVoucherCompanyPage = Math.min(sendVoucherCompanyPage, totalPages);
            const startIndex = (sendVoucherCompanyPage - 1) * sendVoucherCompanyPageSize;
            const pageRows = rows.slice(startIndex, startIndex + sendVoucherCompanyPageSize);
            const pageStart = rows.length ? startIndex + 1 : 0;
            const pageEnd = Math.min(startIndex + sendVoucherCompanyPageSize, rows.length);

            document.getElementById("sendVoucherCompanyTable").innerHTML = pageRows.map(row => `
                <tr>
                    ${renderRecordEditButton(row, "SendVoucher")}
                    ${renderRecordExplanationButton(row, "SendVoucher")}
                    <td class="company-name">${escapeHtml(row.company)}</td>
                    <td>${escapeHtml(row.city)}</td>
                    <td>${escapeHtml(row.state)}</td>
                    <td>${escapeHtml([row.contactFirst, row.contactLast].filter(Boolean).join(" "))}</td>
                    <td>${escapeHtml(row.jobTitle)}</td>
                    <td>${escapeHtml(row.email)}</td>
                    <td>${escapeHtml(formatPhone(row.phone))}</td>
                    ${isCompanySizeReport ? `<td class="numeric">${formatNumber.format(row.employees || 0)}</td><td>${escapeHtml(row.companySize)}</td>` : ""}
                    <td>${escapeHtml(row.program)}</td>
                    <td>${escapeHtml(formatFullAddress(row))}</td>
                    <td>${escapeHtml(row.agent)}</td>
                    <td>${escapeHtml(window.ByteForgeWalgreensLabels.outcome(row.status))}</td>
                    <td>${escapeHtml(row.date)}</td>
                    <td><code>${escapeHtml(row.globalId)}</code></td>
                    ${renderReportNotesButton(row, "SendVoucher")}
                </tr>`).join("");

            document.getElementById("sendVoucherCompanyEmpty").style.display = rows.length ? "none" : "block";
            document.getElementById("sendVoucherCompanyMeta").textContent =
                `${formatNumber.format(sendVoucherCompanyRows.length)} companies requested a voucher email`;
            sendVoucherCompanyPage = WalgreensAdminListTools.renderPagination({ summaryElement: document.getElementById("sendVoucherCompanyPageMeta"), buttonsElement: document.getElementById("sendVoucherCompanyPagination"), totalRows: rows.length, pageSize: sendVoucherCompanyPageSize, currentPage: sendVoucherCompanyPage, onPageChange: target => { sendVoucherCompanyPage = target; renderSendVoucherCompanyTable(); } });
        }

        function downloadSendVoucherCompaniesCsv() {
            const columns = [
                ["date", "Requested date"],
                ["globalId", "Global ID"],
                ["status", "Status"],
                ["agent", "Agent"],
                ["company", "Company"],
                ["contactFirst", "Contact first"],
                ["contactLast", "Contact last"],
                ["jobTitle", "Job title"],
                ["email", "Email"],
                ["phone", "Phone"],
                ["employees", "Employees"],
                ["companySize", "Company size"],
                ["program", "Program"],
                ["fullAddress", "Full address"],
                ["city", "City"],
                ["state", "State"],
                ["agentNotes", "Agent Notes"],
                ["bpNotes", "BP Notes"]
            ];
            const lines = [
                columns.map(([, label]) => csvField(label)).join(","),
                ...sendVoucherCompanyRows.map(row => columns.map(([key]) => csvField(getCompanyExportValue(row, key))).join(","))
            ];
            const blob = new Blob([lines.join("\r\n")], { type: "text/csv;charset=utf-8" });
            const link = document.createElement("a");
            link.href = URL.createObjectURL(blob);
            link.download = "walgreens-send-voucher-requests.csv";
            document.body.appendChild(link);
            link.click();
            URL.revokeObjectURL(link.href);
            link.remove();
        }

        function getVisibleUnworkableContacts() {
            const { start, end } = getSelectedDateRange();
            const search = document.getElementById("unworkableContactSearch").value.trim().toLowerCase();
            return unworkableContactRows
                .filter(row => {
                    const date = new Date(`${row.date}T00:00:00`);
                    return date >= start && date <= end;
                })
                .filter(row => !search || Object.values(row).some(value => String(value).toLowerCase().includes(search)))
                .sort((a, b) => compareValues(a, b, unworkableContactSort.key) * (unworkableContactSort.direction === "asc" ? 1 : -1));
        }

        function renderUnworkableContactTable() {
            window.ByteForgeSortIndicator.update(
                "[data-unworkable-sort]",
                "data-unworkable-sort",
                unworkableContactSort);
            const rows = getVisibleUnworkableContacts();
            const totalPages = Math.max(1, Math.ceil(rows.length / unworkableContactPageSize));
            unworkableContactPage = Math.min(unworkableContactPage, totalPages);
            const startIndex = (unworkableContactPage - 1) * unworkableContactPageSize;
            const pageRows = rows.slice(startIndex, startIndex + unworkableContactPageSize);
            const pageStart = rows.length ? startIndex + 1 : 0;
            const pageEnd = Math.min(startIndex + unworkableContactPageSize, rows.length);

            document.getElementById("unworkableContactTable").innerHTML = pageRows.map(row => `
                <tr>
                    <td>${escapeHtml(row.company)}</td>
                    <td>${escapeHtml([row.contactFirst, row.contactLast].filter(Boolean).join(" "))}</td>
                    <td>${escapeHtml(row.jobTitle)}</td>
                    <td>${escapeHtml(row.email)}</td>
                    <td>${escapeHtml(formatPhone(row.phone))}</td>
                    ${isCompanySizeReport ? `<td class="numeric">${formatNumber.format(row.employees || 0)}</td><td>${escapeHtml(row.companySize)}</td>` : ""}
                    <td>${escapeHtml(row.city)}</td>
                    <td>${escapeHtml(row.state)}</td>
                    <td>${escapeHtml(row.agent)}</td>
                    <td>${escapeHtml(row.date)}</td>
                    <td>${escapeHtml(window.ByteForgeWalgreensLabels.outcome(row.disposition))}</td>
                    <td><code>${escapeHtml(row.globalId)}</code></td>
                    <td>${escapeHtml(row.agentNotes)}</td>
                    <td>${escapeHtml(row.bpNotes)}</td>
                </tr>`).join("");

            document.getElementById("unworkableContactEmpty").style.display = rows.length ? "none" : "block";
            document.getElementById("unworkableContactMeta").textContent =
                `${formatNumber.format(rows.length)} Unworkable interactions in the selected period`;
            unworkableContactPage = WalgreensAdminListTools.renderPagination({ summaryElement: document.getElementById("unworkableContactPageMeta"), buttonsElement: document.getElementById("unworkableContactPagination"), totalRows: rows.length, pageSize: unworkableContactPageSize, currentPage: unworkableContactPage, onPageChange: target => { unworkableContactPage = target; renderUnworkableContactTable(); } });
        }

        async function loadUnworkableContacts() {
            if (isCompanySizeReport && unworkableContactRows.length) {
                renderUnworkableContactTable();
                return;
            }
            const startDate = document.getElementById("startDate").value;
            const endDate = document.getElementById("endDate").value;
            const rangeKey = `${startDate}|${endDate}`;
            if (unworkableContactsLoadedRange === rangeKey) {
                renderUnworkableContactTable();
                return;
            }

            const table = document.getElementById("unworkableContactTable");
            const tableElement = table.closest("table");
            const downloadButton = document.getElementById("downloadUnworkableContactsButton");
            table.innerHTML = `<tr class="disposition-only-loading-row"><td colspan="13">` +
                `<span class="disposition-only-spinner" aria-hidden="true"></span>` +
                `<span>Loading and matching Unworkable contacts...</span></td></tr>`;
            document.getElementById("unworkableContactMeta").textContent = "Loading";
            document.getElementById("unworkableContactEmpty").style.display = "none";
            downloadButton.disabled = true;
            if (tableElement) {
                tableElement.setAttribute("aria-busy", "true");
            }

            const url = new URL(window.walgreensUnworkableContactsUrl, window.location.origin);
            url.searchParams.set("startDate", startDate);
            url.searchParams.set("endDate", endDate);
            if (document.getElementById("reportTeam")) url.searchParams.set("team", reportPayload.selectedTeam || "all");
            try {
                const response = await fetch(url.toString(), { credentials: "same-origin" });
                const payload = await response.json();
                if (!response.ok || !payload.ok) {
                    throw new Error(payload.message || "Unworkable contacts could not be loaded.");
                }

                unworkableContactRows = Array.isArray(payload.rows) ? payload.rows : [];
                unworkableContactsLoadedRange = rangeKey;
                unworkableContactPage = 1;
                renderUnworkableContactTable();
            } catch (error) {
                unworkableContactRows = [];
                unworkableContactsLoadedRange = "";
                table.innerHTML = "";
                document.getElementById("unworkableContactMeta").textContent = "Unavailable";
                const empty = document.getElementById("unworkableContactEmpty");
                empty.textContent = error.message || "Unworkable contacts could not be loaded.";
                empty.style.display = "block";
                document.getElementById("unworkableContactPageMeta").textContent = "";
            } finally {
                downloadButton.disabled = unworkableContactsLoadedRange !== rangeKey;
                if (tableElement) {
                    tableElement.removeAttribute("aria-busy");
                }
            }
        }

        function downloadUnworkableContactsCsv() {
            const columns = [
                ["date", "Date"],
                ["disposition", "Disposition"],
                ["globalId", "Global ID"],
                ["agent", "Agent"],
                ["company", "Company"],
                ["contactFirst", "Contact first"],
                ["contactLast", "Contact last"],
                ["jobTitle", "Job title"],
                ["email", "Email"],
                ["phone", "Phone"],
                ["employees", "Employees"],
                ["companySize", "Company size"],
                ["fullAddress", "Full address"],
                ["city", "City"],
                ["state", "State"],
                ["zip", "ZIP"],
                ["agentNotes", "Agent Notes"],
                ["bpNotes", "BP Notes"]
            ];
            const rows = getVisibleUnworkableContacts();
            const lines = [
                columns.map(([, label]) => csvField(label)).join(","),
                ...rows.map(row => columns.map(([key]) => csvField(getCompanyExportValue(row, key))).join(","))
            ];
            downloadTextFile(
                `walgreens-unworkable-contacts_${getReportFileDateSuffix()}.csv`,
                lines.join("\r\n"),
                "text/csv;charset=utf-8");
        }

        /**
         * Calculates a selected-range total for a derived program-statistics line.
         * @param {Record<string, unknown>} row The grouped report line from the workbook data.
         * @returns {number|null} The matching aggregate, or null for an additive source line.
         */
        function calculateProgramStatisticTotal(row) {
            if (String(row?.group || "").trim().toLowerCase() !== "program statistics") {
                return null;
            }

            const totals = getCampaignTotals(getSelectedDailyRows());
            switch (String(row?.line || "").trim().toLowerCase()) {
                case "leads per hour":
                case "opt-ins per hour":
                    return totals.optInsPerHour;
                case "contacts per hour":
                    return totals.contactsPerHour;
                case "dials per hour":
                case "dial attempts per hour":
                    return totals.dialsPerHour;
                case "consumed per hour":
                case "total consumed per hour":
                    return safeDivide(totals.consumed, totals.hours);
                case "conversion":
                    return safeDivide(totals.optIns, totals.contacts);
                case "total records consumed":
                    return totals.consumed;
                default:
                    return null;
            }
        }

        /**
         * Calculates the grouped report total for one source line across visible daily columns.
         * @param {Record<string, unknown>} row The grouped report line from the workbook data.
         * @returns {number} The selected-range aggregate for the source line.
         */
        function calculateSourceLineTotal(row) {
            const programStatisticTotal = calculateProgramStatisticTotal(row);
            if (programStatisticTotal !== null) {
                return programStatisticTotal;
            }

            return getVisibleSourceDailyColumns().reduce((total, column) => {
                const value = Number(row[column.key]);
                return Number.isFinite(value) ? total + value : total;
            }, 0);
        }

        function renderSourceTable() {
            const visibleSourceColumns = getVisibleSourceColumns();
            const search = document.getElementById("sourceSearch").value.trim().toLowerCase();
            const groupedRows = [];
            sourceRows.forEach(row => {
                let group = groupedRows.find(item => item.name === row.group);
                if (!group) {
                    group = { name: row.group, rows: [] };
                    groupedRows.push(group);
                }

                if (!row.isGroup) {
                    group.rows.push(row);
                }
            });

            const visibleGroups = groupedRows
                .map(group => ({
                    ...group,
                    rows: group.rows.filter(row => !search || Object.values(row).some(value => String(value).toLowerCase().includes(search)))
                }))
                .filter(group => group.rows.length);

            document.getElementById("sourceTable").innerHTML = visibleGroups.map(group => {
                if (group.rows.length === 1) {
                    const row = group.rows[0];
                    return `
                        <tr>
                            ${visibleSourceColumns.map(column => `
                                <td class="${column.kind === "number" || column.kind === "mixed" ? "numeric" : ""}">
                                    ${column.key === "line"
                                        ? `${escapeHtml(group.name)}: ${escapeHtml(row.line)}`
                                        : formatReportValue(row[column.key], column.key, row)}
                                </td>
                            `).join("")}
                            <td class="numeric">${formatReportValue(calculateSourceLineTotal(row), "lineTotal", row)}</td>
                        </tr>`;
                }

                const isCollapsed = collapsedSourceGroups.has(group.name) && !search;
                const rows = isCollapsed ? "" : group.rows.map(row => `
                    <tr>
                        ${visibleSourceColumns.map(column => `
                            <td class="${column.kind === "number" || column.kind === "mixed" ? "numeric" : ""}">
                                ${formatReportValue(row[column.key], column.key, row)}
                            </td>
                        `).join("")}
                        <td class="numeric">${formatReportValue(calculateSourceLineTotal(row), "lineTotal", row)}</td>
                    </tr>`).join("");

                return `
                    <tr class="group-row" data-tone="${sourceGroupTone(group.name)}">
                        <td colspan="${visibleSourceColumns.length + 1}">
                            <button class="source-group-button" type="button" data-source-group="${escapeHtml(group.name)}" aria-expanded="${!isCollapsed}">
                                <span class="source-group-toggle">${isCollapsed ? "+" : "-"}</span>
                                <span>${escapeHtml(group.name)}</span>
                                <span class="source-group-count">${formatNumber.format(group.rows.length)} lines</span>
                            </button>
                        </td>
                    </tr>
                    ${rows}`;
            }).join("");

            document.querySelectorAll("[data-source-group]").forEach(button => {
                button.addEventListener("click", () => {
                    const group = button.getAttribute("data-source-group");
                    if (collapsedSourceGroups.has(group)) {
                        collapsedSourceGroups.delete(group);
                    } else {
                        collapsedSourceGroups.add(group);
                    }
                    renderSourceTable();
                });
            });

            document.getElementById("sourceEmpty").style.display = visibleGroups.length ? "none" : "block";
        }

        /**
         * Renders the grouped report header and body for the currently selected range.
         * @returns {void}
         */
        function renderSourceReport() {
            renderSourceHeader();
            renderSourceTable();
        }

        function formatAppliedDate(value) {
            const date = new Date(`${value}T00:00:00`);
            return date.toLocaleDateString("en-US", { month: "short", day: "numeric", year: "numeric" });
        }

        function setPresetDates() {
            const preset = document.getElementById("reportRange").value;
            const start = document.getElementById("startDate");
            const end = document.getElementById("endDate");
            const latestIsoDate = reportPayload.defaultEndDate || dailyRows.map(row => row.isoDate).filter(Boolean).sort().pop() || new Date().toISOString().slice(0, 10);
            const latest = new Date(`${latestIsoDate}T00:00:00`);
            const toInput = date => date.toISOString().slice(0, 10);
            const clone = date => new Date(date.getTime());

            if (preset === "custom") {
                return;
            }

            if (preset === "today") {
                start.value = toInput(latest);
                end.value = toInput(latest);
            }

            if (preset === "yesterday") {
                const yesterday = clone(latest);
                yesterday.setDate(yesterday.getDate() - 1);
                start.value = toInput(yesterday);
                end.value = toInput(yesterday);
            }

            if (preset === "last7") {
                const first = clone(latest);
                first.setDate(first.getDate() - 6);
                start.value = toInput(first);
                end.value = toInput(latest);
            }

            if (preset === "last30") {
                const first = clone(latest);
                first.setDate(first.getDate() - 29);
                start.value = toInput(first);
                end.value = toInput(latest);
            }
        }

        function updateCompactSummary() {
            const start = document.getElementById("startDate").value;
            const end = document.getElementById("endDate").value;
            document.getElementById("compactViewTitle").textContent = `Campaign Performance · ${formatAppliedDate(start)} - ${formatAppliedDate(end)}`;
            document.getElementById("compactViewSubtitle").textContent = "";
        }

        function refreshReport() {
            renderCampaignSummary();
            renderDailyChart();
            renderDailyTable();
            renderAgentTable();
            renderSourceReport();
        }

        /**
         * Reloads the report from the server for the selected inclusive date range.
         * @returns {void}
         */
        function applyReportDateRange() {
            const startInput = document.getElementById("startDate");
            const endInput = document.getElementById("endDate");
            const startDate = startInput.value;
            const endDate = endInput.value;

            startInput.setCustomValidity("");
            endInput.setCustomValidity("");
            if (!startDate || !endDate) {
                const missingInput = !startDate ? startInput : endInput;
                missingInput.setCustomValidity("Select both a start date and an end date.");
                missingInput.reportValidity();
                return;
            }

            if (startDate > endDate) {
                endInput.setCustomValidity("The end date cannot precede the start date.");
                endInput.reportValidity();
                return;
            }

            const url = new URL(window.location.href);
            url.searchParams.set("startDate", startDate);
            url.searchParams.set("endDate", endDate);
            if (document.getElementById("reportTeam")) url.searchParams.set("team", document.getElementById("reportTeam").value);
            window.location.assign(url.toString());
        }

        function updateHeaderMode() {
            const topbar = document.querySelector(".topbar");
            if (!headerCompact && window.scrollY > 140) {
                headerCompact = true;
            } else if (headerCompact && window.scrollY < 40) {
                headerCompact = false;
            }

            topbar.classList.toggle("is-compact", headerCompact);
        }

        document.querySelectorAll("[data-daily-sort]").forEach(button => {
            button.addEventListener("click", () => {
                const key = button.getAttribute("data-daily-sort");
                dailySort = { key, direction: dailySort.key === key && dailySort.direction === "asc" ? "desc" : "asc" };
                renderDailyTable();
            });
        });

        document.getElementById("dailySearch").addEventListener("input", renderDailyTable);
        document.getElementById("agentSearch").addEventListener("input", renderAgentTable);
        document.querySelectorAll("[data-agent-sort]").forEach(button => {
            button.addEventListener("click", () => {
                const key = button.getAttribute("data-agent-sort");
                agentSort = { key, direction: agentSort.key === key && agentSort.direction === "asc" ? "desc" : "asc" };
                renderAgentTable();
            });
        });
        document.getElementById("sourceSearch").addEventListener("input", renderSourceTable);
        document.getElementById("unworkableContactSearch").addEventListener("input", () => {
            unworkableContactPage = 1;
            renderUnworkableContactTable();
        });
        document.querySelectorAll("[data-unworkable-sort]").forEach(button => {
            button.addEventListener("click", () => {
                const key = button.getAttribute("data-unworkable-sort");
                unworkableContactSort = {
                    key,
                    direction: unworkableContactSort.key === key && unworkableContactSort.direction === "asc" ? "desc" : "asc"
                };
                unworkableContactPage = 1;
                renderUnworkableContactTable();
            });
        });
        const unworkableContactsButton = document.getElementById("unworkableContactsButton");
        if (unworkableContactsButton) {
            unworkableContactsButton.addEventListener("click", () => {
                document.getElementById("unworkableContactsDialog").showModal();
                loadUnworkableContacts();
            });
        }
        document.getElementById("closeUnworkableContactsButton").addEventListener("click", () => {
            document.getElementById("unworkableContactsDialog").close();
        });
        document.getElementById("downloadUnworkableContactsButton").addEventListener("click", downloadUnworkableContactsCsv);
        document.getElementById("optInCompanySearch").addEventListener("input", () => {
            optInCompanyPage = 1;
            renderOptInCompanyTable();
        });
        document.querySelectorAll("[data-company-sort]").forEach(button => {
            button.addEventListener("click", () => {
                const key = button.getAttribute("data-company-sort");
                optInCompanySort = { key, direction: optInCompanySort.key === key && optInCompanySort.direction === "asc" ? "desc" : "asc" };
                optInCompanyPage = 1;
                renderOptInCompanyTable();
            });
        });
        const optInCompaniesButton = document.getElementById("optInCompaniesButton");
        if (optInCompaniesButton) {
            optInCompaniesButton.addEventListener("click", () => {
                renderOptInCompanyTable();
                document.getElementById("optInCompaniesDialog").showModal();
            });
        }
        document.getElementById("closeOptInCompaniesButton").addEventListener("click", () => {
            document.getElementById("optInCompaniesDialog").close();
        });
        document.getElementById("downloadOptInCompaniesButton").addEventListener("click", downloadOptInCompaniesCsv);
        if (window.walgreensCanExplainRecords) {
            document.getElementById("dispositionOnlyOptInsButton").addEventListener("click", () => {
                document.getElementById("dispositionOnlyOptInsDialog").showModal();
                loadDispositionOnlyOptIns();
            });
            document.getElementById("closeDispositionOnlyOptInsButton").addEventListener("click", () => {
                document.getElementById("dispositionOnlyOptInsDialog").close();
            });
            document.getElementById("downloadDispositionOnlyOptInsButton").addEventListener(
                "click",
                downloadDispositionOnlyOptInsCsv);
            document.getElementById("dispositionOnlyOptInsSearch").addEventListener("input", () => {
                dispositionOnlyOptInPage = 1;
                renderDispositionOnlyOptIns();
            });
            document.querySelectorAll("[data-disposition-only-sort]").forEach(button => {
                button.addEventListener("click", () => {
                    const key = button.getAttribute("data-disposition-only-sort");
                    dispositionOnlyOptInSort = {
                        key,
                        direction: dispositionOnlyOptInSort.key === key &&
                            dispositionOnlyOptInSort.direction === "asc" ? "desc" : "asc"
                    };
                    dispositionOnlyOptInPage = 1;
                    renderDispositionOnlyOptIns();
                });
            });
            document.getElementById("dispositionOnlyOptInsTable").addEventListener("click", event => {
                const button = event.target.closest("[data-create-disposition-only-entry]");
                if (!button) {
                    return;
                }

                createDispositionOnlyEntry(Number(button.getAttribute("data-create-disposition-only-entry")));
            });
            document.getElementById("missingOptInButton").addEventListener("click", () => {
                document.getElementById("missingOptInStatus").textContent = "";
                document.getElementById("missingOptInResults").innerHTML = "";
                document.getElementById("missingOptInDialog").showModal();
                document.getElementById("missingOptInQuery").focus();
            });
            document.getElementById("missingOptInForm").addEventListener("submit", investigateMissingOptIn);
            document.getElementById("closeMissingOptInButton").addEventListener("click", () => {
                document.getElementById("missingOptInDialog").close();
            });
            document.querySelectorAll("#optInCompanyTable, #infoPackageCompanyTable, #sendVoucherCompanyTable").forEach(tableBody => {
                tableBody.addEventListener("click", event => {
                    const button = event.target.closest("[data-record-explanation-type]");
                    if (!button) {
                        return;
                    }

                    showRecordExplanation(
                        button.getAttribute("data-record-explanation-type"),
                        Number(button.getAttribute("data-record-explanation-index")));
                });
            });
            document.getElementById("closeRecordExplanationButton").addEventListener("click", () => {
                document.getElementById("recordExplanationDialog").close();
            });
        }
        if (window.walgreensCanEditReportRecords) {
            document.querySelectorAll("#optInCompanyTable, #infoPackageCompanyTable, #sendVoucherCompanyTable").forEach(tableBody => {
                tableBody.addEventListener("click", event => {
                    const button = event.target.closest("[data-report-record-type]");
                    if (!button) {
                        return;
                    }
                    openReportRecordEditor(
                        button.getAttribute("data-report-record-type"),
                        Number(button.getAttribute("data-report-record-index")));
                });
            });
            document.getElementById("closeReportRecordEditorButton").addEventListener("click", closeReportRecordEditor);
            document.getElementById("reportRecordEditorDialog").addEventListener("cancel", event => {
                event.preventDefault();
                closeReportRecordEditor();
            });
        }
        document.querySelectorAll("#optInCompanyTable, #infoPackageCompanyTable, #sendVoucherCompanyTable").forEach(tableBody => {
            tableBody.addEventListener("click", event => {
                const button = event.target.closest("[data-report-notes-type]");
                if (!button || button.disabled) {
                    return;
                }
                showReportNotes(
                    button.getAttribute("data-report-notes-type"),
                    Number(button.getAttribute("data-report-notes-index")));
            });
        });
        document.getElementById("closeReportNotesButton").addEventListener("click", () => {
            document.getElementById("reportNotesDialog").close();
        });
        const contactableListButton = document.getElementById("contactableListButton");
        if (contactableListButton) {
            contactableListButton.addEventListener("click", () => {
                document.getElementById("contactableListDialog").showModal();
                loadContactableListMetadata();
            });
            document.getElementById("closeContactableListButton").addEventListener("click", () => {
                document.getElementById("contactableListDialog").close();
            });
            document.getElementById("downloadContactableListButton").addEventListener(
                "click",
                downloadContactableListWorkbook);
        }
        const exportReportWorkbookButton = document.getElementById("exportReportWorkbookButton");
        if (exportReportWorkbookButton) {
            exportReportWorkbookButton.addEventListener("click", downloadReportWorkbook);
        }
        document.getElementById("downloadReportPdfButton").addEventListener("click", downloadReportPdf);
        document.getElementById("infoPackageCompanySearch").addEventListener("input", () => {
            infoPackageCompanyPage = 1;
            renderInfoPackageCompanyTable();
        });
        document.querySelectorAll("[data-info-package-sort]").forEach(button => {
            button.addEventListener("click", () => {
                const key = button.getAttribute("data-info-package-sort");
                infoPackageCompanySort = {
                    key,
                    direction: infoPackageCompanySort.key === key && infoPackageCompanySort.direction === "asc" ? "desc" : "asc"
                };
                infoPackageCompanyPage = 1;
                renderInfoPackageCompanyTable();
            });
        });
        const infoPackageCompaniesButton = document.getElementById("infoPackageCompaniesButton");
        if (infoPackageCompaniesButton) {
            infoPackageCompaniesButton.addEventListener("click", () => {
                renderInfoPackageCompanyTable();
                document.getElementById("infoPackageCompaniesDialog").showModal();
            });
        }
        document.getElementById("closeInfoPackageCompaniesButton").addEventListener("click", () => {
            document.getElementById("infoPackageCompaniesDialog").close();
        });
        document.getElementById("downloadInfoPackageCompaniesButton").addEventListener("click", downloadInfoPackageCompaniesCsv);
        document.getElementById("sendVoucherCompanySearch").addEventListener("input", () => {
            sendVoucherCompanyPage = 1;
            renderSendVoucherCompanyTable();
        });
        document.querySelectorAll("[data-send-voucher-sort]").forEach(button => {
            button.addEventListener("click", () => {
                const key = button.getAttribute("data-send-voucher-sort");
                sendVoucherCompanySort = {
                    key,
                    direction: sendVoucherCompanySort.key === key && sendVoucherCompanySort.direction === "asc" ? "desc" : "asc"
                };
                sendVoucherCompanyPage = 1;
                renderSendVoucherCompanyTable();
            });
        });
        const sendVoucherCompaniesButton = document.getElementById("sendVoucherCompaniesButton");
        if (sendVoucherCompaniesButton) {
            sendVoucherCompaniesButton.addEventListener("click", () => {
                renderSendVoucherCompanyTable();
                document.getElementById("sendVoucherCompaniesDialog").showModal();
            });
        }
        document.getElementById("closeSendVoucherCompaniesButton").addEventListener("click", () => {
            document.getElementById("sendVoucherCompaniesDialog").close();
        });
        document.getElementById("downloadSendVoucherCompaniesButton").addEventListener("click", downloadSendVoucherCompaniesCsv);
        document.getElementById("reportRange").addEventListener("change", () => {
            setPresetDates();
            updateCompactSummary();
        });
        document.getElementById("startDate").addEventListener("change", () => {
            document.getElementById("reportRange").value = "custom";
            updateCompactSummary();
        });
        document.getElementById("endDate").addEventListener("change", () => {
            document.getElementById("reportRange").value = "custom";
            updateCompactSummary();
        });
        document.getElementById("applyFiltersButton").addEventListener("click", applyReportDateRange);
        document.getElementById("changeFiltersButton").addEventListener("click", () => {
            document.querySelector(".topbar").classList.add("filters-open");
            headerCompact = true;
            updateHeaderMode();
        });
        window.addEventListener("scroll", updateHeaderMode, { passive: true });

        renderCampaignSummary();
        renderDailyChart();
        renderMixList();
        renderDailyTable();
        renderAgentTable();
        renderOptInCompanyTable();
        renderInfoPackageCompanyTable();
        renderSendVoucherCompanyTable();
        renderUnworkableContactTable();
        renderSourceReport();
        updateCompactSummary();
        updateHeaderMode();
