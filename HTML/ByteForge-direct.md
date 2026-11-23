# ByteForge direct upload

`JavaScript/ByteForge-direct.js` and `CSS/ByteForge-direct.css` implement the responsive office browser, conflict form and direct HTTPS uploader. `ByteForgeDirectUpload` accepts an authenticated `api(operation, arguments)` adapter. The office MVC page supplies cookie/antiforgery authentication; the tray explorer supplies its authenticated loopback adapter, whose native proxy attaches the office bearer key.

Data is sent in 384 KiB chunks, encoded as bounded base64 JSON for a common transport across the browser and helper. This adds base64 overhead but avoids buffering entire files or implementing a second local multipart protocol. Requests remain below 1 MiB. Per-chunk SHA-256 receipts allow safe uncertain-response retries; reselected files must match every accepted prefix chunk before resuming. The office worker rechecks every staged chunk, computes the final hash and publishes a destination-volume temporary file atomically.

The standalone office UI works without Windows or the helper and does not require Microsoft sign-in. The helper's internal pane-to-pane transfers still use OneDrive; files selected from the browser or dropped externally onto its office pane use direct HTTPS. Direct transfers require the browser page to stay open.

Responsive layouts collapse secondary file columns and the folder tree on phones, expose a folder toggle, convert history rows and conflict choices to cards, and retain touch-size controls. Completed upload controls disappear. Preserve both theme token sets, regular-weight input values, keyboard access and numeric HTML icon entities.
