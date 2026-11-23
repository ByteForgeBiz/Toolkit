(function (window) {
    "use strict";
    window.ByteForgeWalgreensLabels = {
        outcome: function (value) {
            return /^(?:GK )?Send Info$/i.test(String(value || "").trim()) ? "GK Send Info" : value;
        }
    };
}(window));
