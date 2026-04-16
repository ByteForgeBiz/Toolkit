# Logging

This folder contains the XML log reader pipeline that parses the structured session log produced by `winscp.exe` and surfaces its contents as navigable XML nodes to the rest of the wrapper.

WinSCP writes an XML log file while it runs (path passed via `/xmllog`). The wrapper reads this file incrementally — without waiting for WinSCP to finish — and extracts operation results, file listings, errors, and other structured data from the XML elements.

---

## Architecture

```
SessionLogReader          (reads the raw XML file from disk, polls for new data)
        |
   CustomLogReader        (abstract base; XML navigation helpers)
        |
   ElementLogReader       (scoped to a single XML element's contents)
        |
   SessionElementLogReader (detects unexpected session close during reading)
```

Each layer wraps the one below and restricts the XML cursor to a bounded region of the document.

---

## Types

### `CustomLogReader` (abstract)

The abstract base for all log readers. Wraps an `XmlReader` and provides WinSCP-specific navigation helpers.

| Member | Description |
|--------|-------------|
| `Read(LogReadFlags)` | Abstract; reads the next XML node. |
| `IsElement()` | Returns `true` if the current node is an XML element in the WinSCP session namespace (`http://winscp.net/schema/session/1.0`). |
| `IsElement(string localName)` | Matches by namespace and local name. |
| `IsNonEmptyElement(string)` | Matches a non-self-closing element. |
| `IsEndElement(string)` | Matches a closing tag. |
| `GetEmptyElementValue(string, out string)` | Reads the `value` attribute from a self-closing element if its local name matches. |
| `TryWaitForNonEmptyElement(string, LogReadFlags)` | Advances until the named element is found, or EOF. |
| `WaitForNonEmptyElement(string, LogReadFlags)` | Like above but throws `SessionLocalException` if not found. |
| `TryWaitForEmptyElement(string, LogReadFlags)` | Advances until a self-closing element with the given name is found. |
| `WaitForGroupAndCreateLogReader()` | Convenience method: waits for a `<group>` element and wraps it in an `ElementLogReader`. |
| `CreateLogReader()` | Creates an `ElementLogReader` scoped to the current element. |

---

### `SessionLogReader`

Concrete implementation that reads from the WinSCP XML log file on disk. Inherits from `CustomLogReader`.

| Member | Description |
|--------|-------------|
| `SessionLogReader(Session)` | Constructor; initializes the read position counter. |
| `Read(LogReadFlags)` | Opens the file if not already open, reads the next XML node with polling/retry, handles `<failure>` elements by raising `Session.RaiseFailed`. |
| `SetTimeouted()` | Marks the session as timed out; subsequent `Read` calls return `false` immediately. |
| `Dispose()` | Logs remaining file contents for diagnostics, then cleans up the file stream and XML reader. |

**Polling behavior:** When the XML reader reaches end-of-file but the log is not yet closed (WinSCP is still writing), `SessionLogReader` sleeps using `Session.DispatchEvents` with an exponential back-off starting at 50 ms and capping at 500 ms. If the file must be re-opened between reads (e.g., because WinSCP replaced it), the reader skips forward past already-processed nodes.

---

### `ElementLogReader`

Restricts reading to the contents of a single XML element. Tracks depth to detect when the closing tag is reached, at which point `Read` returns `false`. Automatically drains remaining content on `Dispose` to avoid leaving the parent reader mid-element.

| Member | Description |
|--------|-------------|
| `ElementLogReader(CustomLogReader)` | Must be created when the parent reader is positioned on a non-empty element. |
| `Read(LogReadFlags)` | Delegates to the parent reader; returns `false` when the corresponding end element is encountered. |
| `ReadToEnd(LogReadFlags)` | Consumes all remaining nodes in the element. |

---

### `SessionElementLogReader`

Extends `ElementLogReader` with session-close detection. If `Read` returns `false` (element ended) while the reader is not in the process of being disposed, it throws `SessionLocalException("Session has unexpectedly closed")`. Used for long-running session-level XML groups where premature termination indicates a connection failure.
