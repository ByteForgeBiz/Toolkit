(function (global) {
    "use strict";
    // In-memory request identities only. Accepted efforts live exclusively on the server.
    var identities = new Map();
    function id() {
        if (global.crypto.randomUUID) { return global.crypto.randomUUID(); }
        var bytes = new Uint8Array(16);
        global.crypto.getRandomValues(bytes);
        bytes[6] = (bytes[6] & 15) | 64;
        bytes[8] = (bytes[8] & 63) | 128;
        var hex = Array.prototype.map.call(bytes, function (value) { return value.toString(16).padStart(2, "0"); }).join("");
        return hex.slice(0, 8) + "-" + hex.slice(8, 12) + "-" + hex.slice(12, 16) + "-" + hex.slice(16, 20) + "-" + hex.slice(20);
    }
    global.ByteForgeEffortClient = {
        fetch: function (url, options) {
            var payload = options.body instanceof FormData ? options.body : new URLSearchParams(options.body);
            var identityPayload = [];
            payload.forEach(function (value, name) {
                if (name !== "__RequestVerificationToken" && name !== "effortId") { identityPayload.push([name, value]); }
            });
            var key = url + "|" + JSON.stringify(identityPayload);
            if (!identities.has(key)) { identities.set(key, id()); }
            payload.set("effortId", identities.get(key));
            options.body = payload instanceof FormData ? payload : payload.toString();
            return global.fetch(url, options);
        }
    };
}(window));
