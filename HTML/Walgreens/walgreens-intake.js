(function (window, document) {
    "use strict";

    /** Formats US phone input without discarding digits; accepts a leading country code 1. */
    function formatPhone(value) {
        var original = String(value || "");
        var digits = original.replace(/\D/g, "");
        if (digits.length >= 10) {
            return window.formatPhoneNumber(original);
        }
        if (digits.length <= 3) {
            return digits;
        }
        if (digits.length <= 6) {
            return "(" + digits.slice(0, 3) + ") " + digits.slice(3);
        }
        return "(" + digits.slice(0, 3) + ") " + digits.slice(3, 6) + "-" + digits.slice(6);
    }

        function updateProgramFieldVisibility(intakeForm, isReadOnly, minimumClinicParticipants) {
            var selectedProgram = intakeForm
                ? intakeForm.querySelector("input[name='Program']:checked")
                : null;
            var estimatedParticipantsField = intakeForm
                ? intakeForm.querySelector("[name='EstimatedParticipants']")
                : null;
            var estimatedParticipants = estimatedParticipantsField
                ? Number(estimatedParticipantsField.value)
                : 0;
            var hasEmployeeCount = estimatedParticipantsField
                && estimatedParticipantsField.value !== "";
            var requiresVoucherOnly = hasEmployeeCount
                && estimatedParticipants < minimumClinicParticipants;
            var voucherProgramOption = intakeForm
                ? intakeForm.querySelector("[data-voucher-program]")
                : null;
            var clinicProgramOptions = intakeForm
                ? intakeForm.querySelectorAll("[data-clinic-program]")
                : [];
            var voucherOnlyMessage = document.querySelector("[data-voucher-only-message]");

            if (requiresVoucherOnly && voucherProgramOption && !isReadOnly) {
                voucherProgramOption.checked = true;
                selectedProgram = voucherProgramOption;
            }
            Array.prototype.forEach.call(clinicProgramOptions, function (programOption) {
                programOption.disabled = isReadOnly || requiresVoucherOnly;
            });
            if (voucherOnlyMessage) {
                voucherOnlyMessage.hidden = !requiresVoucherOnly;
            }

            var requiresClinicDetails = selectedProgram
                && selectedProgram.value !== "Voucher Program Only";
            var requiresVaccinationDetails = requiresClinicDetails;
            var requiresLocationDetails = requiresClinicDetails;

            document.querySelectorAll("[data-clinic-only]").forEach(function (container) {
                container.hidden = !requiresClinicDetails;
                container.querySelectorAll("input, select, textarea, button").forEach(function (control) {
                    control.disabled = isReadOnly || !requiresClinicDetails;
                });
            });

            document.querySelectorAll("[data-vaccination-details]").forEach(function (container) {
                container.hidden = !requiresVaccinationDetails;
                container.querySelectorAll("input, select, textarea").forEach(function (control) {
                    if (!control.closest("[data-clinic-only]")) {
                        control.disabled = isReadOnly || !requiresVaccinationDetails;
                    }
                });
            });

            document.querySelectorAll("[data-location-details]").forEach(function (container) {
                container.hidden = !requiresLocationDetails;
                container.querySelectorAll("input, select, textarea, button").forEach(function (control) {
                    if (!control.closest("[data-clinic-only]")) {
                        control.disabled = isReadOnly || !requiresLocationDetails;
                    }
                });
            });

            var vaccinationOptions = intakeForm
                ? intakeForm.querySelectorAll("input[name='Vaccinations']")
                : [];
            var hasVaccination = Array.prototype.some.call(vaccinationOptions, function (option) {
                return option.checked;
            });
            if (vaccinationOptions.length > 0) {
                vaccinationOptions[0].setCustomValidity(
                    requiresVaccinationDetails && !hasVaccination
                        ? "Select at least one vaccination."
                        : "");
            }
        }

    window.ByteForgeWalgreensIntake = { updateProgramFieldVisibility: updateProgramFieldVisibility, formatPhone: formatPhone };
}(window, document));
