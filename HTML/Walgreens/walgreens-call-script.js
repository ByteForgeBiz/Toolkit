(function (window, document) {
    "use strict";

    var script = readScriptData();
    if (!script || !script.Steps || !script.Steps.length) {
        return;
    }

    var state = {
        stepId: script.Steps[0].Id,
        visited: []
    };

    var panelEl = document.querySelector("[data-agent-script-panel]");
    var stepsEl = document.querySelector("[data-agent-script-steps]");
    var cardEl = document.querySelector("[data-agent-script-card]");
    var actionsEl = document.querySelector("[data-agent-script-actions]");
    var outcomeUrl = panelEl ? (panelEl.getAttribute("data-agent-script-outcome-url") || "") : "";

    function readScriptData() {
        var el = document.getElementById("walgreensCallScriptData");
        if (!el) {
            return null;
        }

        try {
            return JSON.parse(el.textContent || el.innerText || "{}");
        } catch (ex) {
            return null;
        }
    }

    function getStep(id) {
        for (var i = 0; i < script.Steps.length; i++) {
            if (script.Steps[i].Id === id) {
                return script.Steps[i];
            }
        }

        return null;
    }

    function getFieldValue(name) {
        var field = document.querySelector("[name='" + name + "']");
        if (!field) {
            return "";
        }

        if (field.type === "radio") {
            var checked = document.querySelector("[name='" + name + "']:checked");
            return checked ? checked.value : "";
        }

        return field.value || "";
    }

    function getPlaceholderContext() {
        var firstName = getFieldValue("FirstName");
        var lastName = getFieldValue("LastName");
        var contactName = [firstName, lastName].filter(Boolean).join(" ").trim();
        var agentName = panelEl ? (panelEl.getAttribute("data-agent-name") || "") : "";
        var agentId = getFieldValue("AgentId");

        return {
            AgentName: agentName || agentId || "[agent name]",
            FirstName: firstName || "[First Name]",
            LastName: lastName || "[Last Name]",
            ContactName: contactName || "[contact name provided]",
            Company: getFieldValue("Company") || "[Business/Organization]",
            Program: getFieldValue("Program") || "[Program]",
            AgentId: agentId || "[Agent ID]",
            CallId: getFieldValue("CallId") || "[Call ID]",
            Ani: getFieldValue("Ani") || "[Outbound Call]",
            Dnis: getFieldValue("Dnis") || "[DNIS]",
            CallerId: script.CallerId || "Walgreens IMZ",
            SupportEmail: script.SupportEmail || "WalgreensIMZProgram@walgreens.com"
        };
    }

    function normalizePlaceholderKey(key) {
        return String(key || "").toLowerCase().replace(/[^a-z0-9]/g, "");
    }

    function resolvePlaceholders(text) {
        var context = getPlaceholderContext();
        var normalizedContext = {};
        Object.keys(context).forEach(function (key) {
            normalizedContext[normalizePlaceholderKey(key)] = context[key];
        });

        return String(text || "")
            .replace(/<([^<>]+)>/g, function (match, key) {
                var normalizedKey = normalizePlaceholderKey(key);
                return Object.prototype.hasOwnProperty.call(normalizedContext, normalizedKey) ? normalizedContext[normalizedKey] : match;
            })
            .replace(/\[Name\]/gi, context.AgentName);
    }

    function escapeHtml(value) {
        return String(value == null ? "" : value)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#39;");
    }

    function formatScriptText(text) {
        return escapeHtml(resolvePlaceholders(text))
            .replace(/(\[[^\]\r\n]*\])/g, "<span class=\"agent-script-placeholder\">$1</span>")
            .replace(/\r?\n/g, "<br>");
    }

    function render() {
        var active = getStep(state.stepId) || script.Steps[0];
        renderSteps(active);
        renderCard(active);
        renderActions(active);
    }

    function renderSteps(active) {
        if (!stepsEl) {
            return;
        }

        stepsEl.innerHTML = script.Steps.map(function (step) {
            var css = "agent-script-step";
            var marker = "○";
            if (step.Id === active.Id) {
                css += " is-active";
                marker = "▶";
            } else if (state.visited.indexOf(step.Id) >= 0) {
                css += " is-done";
                marker = "✓";
            }

            return "<li>" +
                "<button type=\"button\" class=\"" + css + "\" data-agent-script-jump=\"" + escapeHtml(step.Id) + "\">" +
                "<span>" + marker + "</span>" +
                "<strong>" + escapeHtml(step.Label || step.Id) + "</strong>" +
                "</button>" +
                "</li>";
        }).join("");
    }

    function renderCard(step) {
        if (!cardEl) {
            return;
        }

        var html = "";
        if (step.Text) {
            html += "<section class=\"agent-script-card-section agent-script-card-say\" aria-label=\"Agent says\">" +
                "<div class=\"agent-script-card-label\">Say</div>" +
                "<div class=\"agent-script-card-text\">" + formatScriptText(step.Text) + "</div>" +
                "</section>";
        }

        if (step.Note) {
            html += "<section class=\"agent-script-card-section agent-script-card-know\" aria-label=\"Agent notes\">" +
                "<div class=\"agent-script-card-label\">Know</div>" +
                "<div class=\"agent-script-card-note\">" + formatScriptText(step.Note) + "</div>" +
                "</section>";
        }

        cardEl.innerHTML = html;
    }

    function renderActions(step) {
        if (!actionsEl) {
            return;
        }

        if (step.Type === "program") {
            actionsEl.innerHTML = ["Voucher Program Only", "Clinic", "Clinic + Vouchers"].map(function (program) {
                var option = Array.prototype.find.call(document.querySelectorAll("input[name='Program']"), function (field) { return field.value === program; });
                return '<button type="button" class="agent-script-action" data-agent-script-program="' + escapeHtml(program) + '"' +
                    (!option || option.disabled ? ' disabled' : '') + '>' + escapeHtml(program) + '</button>';
            }).join("");
            return;
        }

        if (step.Type === "decision") {
            actionsEl.innerHTML =
                "<button type=\"button\" class=\"agent-script-action agent-script-action-yes\" data-agent-script-action=\"yes\">Interested</button>" +
                "<button type=\"button\" class=\"agent-script-action agent-script-action-no\" data-agent-script-action=\"no\">Not Interested</button>";
            return;
        }

        if (step.Type === "done") {
            if (step.Id === "not-interested" || step.Id === "not interested") {
                actionsEl.innerHTML =
                    "<button type=\"button\" class=\"agent-script-action agent-script-action-no\" data-agent-script-action=\"confirm-not-interested\">Confirm Not Interested</button>" +
                    "<button type=\"button\" class=\"agent-script-action\" data-agent-script-action=\"restart\">Restart</button>";
                return;
            }

            actionsEl.innerHTML =
                "<button type=\"button\" class=\"agent-script-action\" data-agent-script-action=\"restart\">Restart</button>";
            return;
        }

        actionsEl.innerHTML = "<button type=\"button\" class=\"agent-script-action agent-script-action-next\" data-agent-script-action=\"next\">Continue</button>";
        if (step.Id === "intro" || step.Id === "background") {
            actionsEl.innerHTML += "<button type=\"button\" class=\"agent-script-action\" data-agent-script-action=\"voicemail\">Voicemail</button>";
        }
    }

    function getAntiForgeryToken() {
        var field = document.querySelector("input[name='__RequestVerificationToken']");
        return field ? field.value : "";
    }

    function setOutcomeStatus(message, isError) {
        var status = document.querySelector("[data-agent-script-outcome-status]");
        if (!status && actionsEl) {
            status = document.createElement("span");
            status.setAttribute("data-agent-script-outcome-status", "true");
            status.className = "agent-script-outcome-status";
            actionsEl.appendChild(status);
        }

        if (status) {
            status.className = "agent-script-outcome-status" + (isError ? " is-error" : "");
            status.textContent = message || "";
        }
    }

    function saveOutcome(outcome, stepId) {
        if (!outcomeUrl) {
            return;
        }

        var body = new URLSearchParams();
        body.append("__RequestVerificationToken", getAntiForgeryToken());
        body.append("Outcome", outcome);
        body.append("StepId", stepId || "");
        [
            "PaydayPerxLeadId",
            "CanonicalCompanyLeadId",
            "CallId",
            "AgentId",
            "AgentName",
            "Ani",
            "RawAni",
            "Dnis",
            "GlobalId",
            "BpSessionId",
            "BpOrigin",
            "ScriptOpenedAtUtc",
            "CallStartedAtUtc"
        ].forEach(function (name) {
            body.append(name, getFieldValue(name));
        });

        var intakeForm = document.querySelector(".agent-intake-form");
        if (intakeForm) {
            new FormData(intakeForm).forEach(function (value, name) {
                if (name !== "__RequestVerificationToken" && name !== "submissionAction") {
                    body.append("intake." + name, value);
                }
            });
        }

        (window.ByteForgeEffortClient ? window.ByteForgeEffortClient.fetch : fetch)(outcomeUrl, {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
                "X-Requested-With": "XMLHttpRequest"
            },
            credentials: "same-origin",
            body: body.toString()
        })
            .then(function (response) { return response.json(); })
            .then(function (response) {
                setOutcomeStatus(response.message || "Lead outcome saved.", !response.ok);
                if (response.completed && actionsEl) {
                    Array.prototype.forEach.call(actionsEl.querySelectorAll("button"), function (button) {
                        button.disabled = true;
                    });
                }
            })
            .catch(function () {
                setOutcomeStatus("Lead outcome could not be saved.", true);
            });
    }

    function navigateTo(stepId, scrollToAnchor) {
        // Menu jumps and the rebuttal route must keep the intake program consistent
        // with the branch, just as the introduction's program buttons do.
        var programStep = script.Steps.filter(function (step) { return step.Type === "program"; })[0];
        if (programStep && stepId === programStep.Yes) {
            var voucher = document.querySelector("input[data-voucher-program]");
            if (voucher && !voucher.disabled && !voucher.checked) {
                voucher.checked = true;
                voucher.dispatchEvent(new Event("change", { bubbles: true }));
            }
        } else if (programStep && stepId === programStep.No) {
            var selected = document.querySelector("input[name='Program']:checked");
            if (!selected || selected.disabled || selected.value === "Voucher Program Only") {
                stepId = programStep.Id;
            }
        }
        if (state.stepId && state.visited.indexOf(state.stepId) < 0) {
            state.visited.push(state.stepId);
        }

        state.stepId = stepId;
        render();

        if (scrollToAnchor) {
            scrollActiveAnchor();
        }
    }

    function scrollActiveAnchor() {
        var step = getStep(state.stepId);
        if (!step || !step.Anchor) {
            return;
        }

        var el = document.getElementById(step.Anchor);
        if (el && el.getBoundingClientRect) {
            var panelHeight = panelEl && panelEl.getBoundingClientRect ? panelEl.getBoundingClientRect().height : 0;
            var targetTop = el.getBoundingClientRect().top + (window.pageYOffset || document.documentElement.scrollTop || 0);
            var topOffset = panelHeight + 16;

            window.scrollTo({
                top: Math.max(0, targetTop - topOffset),
                behavior: "smooth"
            });
        }
    }

    function handleAction(action) {
        var step = getStep(state.stepId);
        if (!step) {
            return;
        }

        if (action === "restart") {
            state = { stepId: script.Steps[0].Id, visited: [] };
            render();
            return;
        }

        if (action === "voicemail") {
            saveOutcome("Voicemail", step.Id);
            navigateTo("voicemail", false);
            return;
        }

        if (action === "confirm-not-interested") {
            saveOutcome("NotInterested", step.Id);
            return;
        }

        if (action === "next" && step.Next) {
            navigateTo(step.Next, true);
            return;
        }

        if (action === "yes" && step.Yes) {
            navigateTo(step.Yes, true);
            return;
        }

        if (action === "no" && step.No) {
            navigateTo(step.No, true);
        }
    }

    function bindFormUpdates() {
        function refreshFormContext() {
            // Keep the navigation nodes stable when a field loses focus during a click.
            // Replacing them on change would swallow the agent's pending menu action.
            renderCard(getStep(state.stepId) || script.Steps[0]);
            if (actionsEl) {
                Array.prototype.forEach.call(actionsEl.querySelectorAll("[data-agent-script-program]"), function (button) {
                    var program = button.getAttribute("data-agent-script-program");
                    var option = Array.prototype.find.call(document.querySelectorAll("input[name='Program']"), function (field) { return field.value === program; });
                    button.disabled = !option || option.disabled;
                });
            }
        }
        var fields = document.querySelectorAll("input, select, textarea");
        Array.prototype.forEach.call(fields, function (field) {
            field.addEventListener("input", refreshFormContext);
            field.addEventListener("change", refreshFormContext);
        });
    }

    document.addEventListener("click", function (event) {
        var programButton = event.target.closest("[data-agent-script-program]");
        if (programButton && !programButton.disabled) {
            var program = programButton.getAttribute("data-agent-script-program");
            var option = Array.prototype.find.call(document.querySelectorAll("input[name='Program']"), function (field) { return field.value === program; });
            var current = getStep(state.stepId);
            if (option && !option.disabled && current && current.Type === "program") {
                option.checked = true;
                option.dispatchEvent(new Event("change", { bubbles: true }));
                navigateTo(program === "Voucher Program Only" ? current.Yes : current.No, true);
            }
            return;
        }
        var jump = event.target.closest("[data-agent-script-jump]");
        if (jump) {
            navigateTo(jump.getAttribute("data-agent-script-jump"), true);
            return;
        }

        var action = event.target.closest("[data-agent-script-action]");
        if (action) {
            handleAction(action.getAttribute("data-agent-script-action"));
        }
    });

    bindFormUpdates();
    render();
}(window, document));
