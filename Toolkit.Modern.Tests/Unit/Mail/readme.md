# Mail Unit Tests

Tests for `ByteForge.Toolkit.Mail`.

**Test class:** `EmailAttachmentHandlerTests`
**Test categories:** `Unit`, `Mail`
**Source module:** `Toolkit.Modern/Mail/`

## Test Class

### EmailAttachmentHandlerTests

Validates `EmailAttachmentHandler`, which manages the lifecycle and processing of email attachments.

`[TestInitialize]` creates a fresh `EmailAttachmentHandler` instance and a dedicated temp directory under `Path.GetTempPath()`. Three test files are created:

| File | Size | Purpose |
|------|------|---------|
| `small.txt` | 1 KB | Small attachment tests |
| `medium.txt` | 100 KB | Medium attachment tests |
| `large.txt` | 1 MB+ | Large/performance tests |

`[TestCleanup]` recursively deletes the temp directory.

| Test area | Coverage |
|-----------|---------|
| Attachment creation | Creating attachments from file paths via `System.Net.Mail.Attachment` |
| Temporary file management | `EmailAttachmentHandler` creates and tracks temp files; they are removed on dispose |
| Attachment processing | Applying processing logic (compression, format conversion) |
| Multi-part attachments | Handling multiple attachments in a single batch |
| Size limits | Attachments exceeding configured size limits are rejected or flagged |
| File type filtering | Accepted and rejected MIME types or extensions |
| Resource cleanup | `Dispose()` releases file handles and removes temp files |
| Error handling | Non-existent files, access-denied paths, zero-byte files |
| Performance | Large attachment processing completes within acceptable time |

## Prerequisites

No network or SMTP server required. Tests operate entirely on the local file system using temp directories.

## Running These Tests

```powershell
# All Mail tests
dotnet test --filter "TestCategory=Mail"

# By class name
dotnet test --filter "FullyQualifiedName~EmailAttachmentHandlerTests"
```

---

## Documentation Links

| Location | Description | Documentation |
|----------|-------------|---------------|
| **Tests root** | Test project overview | [../../README.md](../../README.md) |
| **Unit overview** | Unit test organization | [../readme.md](../readme.md) |
| **Helpers** | Test helper classes | [../../Helpers/README.md](../../Helpers/README.md) |
