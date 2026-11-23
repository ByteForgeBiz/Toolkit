# ByteForge explorer UI

`CSS/ByteForge-explorer.css` and `JavaScript/ByteForge-explorer.js` are the source assets for the OneDrive Transfers desktop browser UI. The desktop project links them into its `www` output; edit these Toolkit files first. They are also mirrored into the central ByteForge source repository.

`ByteForgeExplorerPane` manages a pane's lazy tree, immutable listing snapshot, search/sort/page state, selection, favorites, breadcrumbs and internal drag-and-drop. Both copy buttons and drops use the native preflight/confirmation API. The API uses POST, exact-origin checks and an ephemeral session header; no file payloads or office keys belong in these components.

Semantic CSS variables pair foregrounds, surfaces, boundaries, selection and errors for both color schemes. Page size is hidden behind each settings gear. Dialog errors must be visible inside the open dialog. Maintain keyboard controls, normal-weight input values, numeric HTML icon entities and responsive layouts when extending this UI.
