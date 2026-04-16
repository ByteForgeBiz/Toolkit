# Mail Module

The Mail module provides utilities for sending emails with smart attachment handling. It automatically determines the best way to attach files based on their combined size: direct attachment for small totals, ZIP compression for medium sizes, and multi-part ZIP archives when compressed output would still exceed the limit.

---

## Key Classes

| Class | Description |
|-------|-------------|
| `MailUtil` | Static helper for sending emails via SMTP |
| `EmailAttachmentHandler` | Prepares and attaches files to a `MailMessage`, compressing as needed |
| `AttachmentProcessResult` | Describes the outcome of attachment processing |
| `TempFileAttachment` | Internal `Attachment` subtype that deletes its temp file on dispose |
| `ProcessingMethod` | Enum describing how files were attached |
| `PartInfo` | Describes one part of a multi-part archive |
| `SkippedFile` | Records a file that was excluded and why |

---

## `MailUtil`

Static class. Reads SMTP settings from the `[Mail Server]` config section. Validates sender/recipient email addresses before sending.

### `SendMail` overloads

```csharp
// Send a plain message
static bool SendMail(string subject, string body);

// Send with a list of file attachments
static bool SendMail(string subject, string body, List<string> filePaths,
                     Dictionary<string, string>? fileNameMap = null,
                     bool throwOnError = false);

// Send with pre-built MailMessage
static bool SendMail(MailMessage message, bool throwOnError = false);
```

`fileNameMap` allows renaming files before attaching them: the key is the original file path, the value is the display name to use in the email.

SMTP connection details come from `MailServerSettings` (loaded from `[Mail Server]`):

| Config key | Property | Default |
|------------|----------|---------|
| `sServer` | `Server` | — |
| `iPort` | `Port` | 587 |
| `sSecurityProtocol` | `SecurityProtocol` | TLS 1.2 |
| `esUser` | `User` | — (AES-decrypted) |
| `esPass` | `Password` | — (AES-decrypted) |
| `sSender` | `Sender` | — |
| `sSendTo` | `SendTo` | — |

---

## `EmailAttachmentHandler`

Implements `IDisposable`. Prepares files for email attachment based on size thresholds.

**Size thresholds:**
- Max per individual file: **5 MB**
- Max total uncompressed: **5 MB** — above this, files are compressed
- Max total (compressed or uncompressed): **20 MB** — above this, splits into multiple ZIP parts

### Primary method

```csharp
AttachmentProcessResult ProcessAttachments(
    MailMessage email,
    List<string>? filePaths,
    Dictionary<string, string>? fileNameMap = null,
    bool throwOnError = false);
```

**Processing logic:**

| Condition | Result |
|-----------|--------|
| Total size ≤ 5 MB | Files attached directly (`ProcessingMethod.DirectAttachment`) |
| Total size > 5 MB, compressed ≤ 20 MB | Single ZIP attachment (`ProcessingMethod.Compressed`) |
| Compressed size > 20 MB | Multiple ZIP parts, each ≤ 20 MB (`ProcessingMethod.MultiPart`) |
| Any failure | `ProcessingMethod.Failed` |

A human-readable summary of how files were processed is appended to `email.Body`.

Temp files are created in `%TEMP%\TempEmailAttachments\`. `Dispose()` deletes the entire temp directory.

---

## `AttachmentProcessResult`

| Property | Type | Description |
|----------|------|-------------|
| `ProcessingMethod` | `ProcessingMethod` | How files were attached |
| `SkippedFiles` | `List<SkippedFile>` | Files excluded from processing |
| `PartDistribution` | `List<PartInfo>` | Breakdown of multi-part archives |
| `Error` | `string?` | Error message if processing failed |
| `Success` | `bool` | `true` when `Error` is null or empty |

---

## `ProcessingMethod`

```csharp
public enum ProcessingMethod
{
    None,             // Not yet processed
    DirectAttachment, // Files attached without compression
    Compressed,       // All files in a single ZIP
    MultiPart,        // Files split across multiple ZIPs
    Failed            // Processing encountered an error
}
```

---

## `PartInfo`

Describes one ZIP archive in a multi-part attachment scenario.

| Property | Type | Description |
|----------|------|-------------|
| `PartNumber` | `int` | 1-based part index |
| `FileCount` | `int` | Number of files in this part |
| `Files` | `List<string>` | Paths of files included in this part |

---

## `SkippedFile`

| Property | Type | Description |
|----------|------|-------------|
| `FilePath` | `string` | Path of the skipped file |
| `Reason` | `string` | Human-readable reason for exclusion |

---

## `TempFileAttachment`

Internal class. Extends `System.Net.Mail.Attachment`. On `Dispose()`, deletes the underlying temp file from disk. Used internally by `EmailAttachmentHandler` so that attachments clean up after themselves when the `MailMessage` is disposed.

---

## Usage

### Send a simple email

```csharp
MailUtil.SendMail("Daily Report", "Please find the report attached.",
    filePaths: new List<string> { @"C:\Reports\daily.xlsx" });
```

### Send with renamed attachments

```csharp
var nameMap = new Dictionary<string, string>
{
    [@"C:\exports\2026-04-15.csv"] = "April 2026 Export.csv"
};

MailUtil.SendMail("Monthly Export", "See attachment.",
    filePaths: new List<string> { @"C:\exports\2026-04-15.csv" },
    fileNameMap: nameMap);
```

### Inspect attachment processing result

```csharp
using var handler = new EmailAttachmentHandler();
var message = new MailMessage("from@example.com", "to@example.com", "Subject", "Body");

var result = handler.ProcessAttachments(message, filePaths: myFiles);

Log.Info($"Method: {result.ProcessingMethod}");

foreach (var skipped in result.SkippedFiles)
    Log.Warning($"Skipped {skipped.FilePath}: {skipped.Reason}");

if (result.ProcessingMethod == ProcessingMethod.MultiPart)
{
    foreach (var part in result.PartDistribution)
        Log.Info($"Part {part.PartNumber}: {part.FileCount} files");
}
```

### Use the handler directly (when not using MailUtil)

```csharp
using var handler = new EmailAttachmentHandler();

var email = new MailMessage("sender@corp.com", "recipient@corp.com")
{
    Subject = "Report",
    Body   = "See attached."
};

handler.ProcessAttachments(email, myFilePaths);

using var smtp = new SmtpClient("mail.corp.com");
smtp.Send(email);
// handler.Dispose() cleans up temp files
```

---

## INI Configuration

```ini
[Mail Server]
sServer=smtp.example.com
iPort=587
sSecurityProtocol=TLS1.2
esUser=[encrypted_username]
esPass=[encrypted_password]
sSender=noreply@example.com
sSendTo=admin@example.com
```

Credentials are AES-encrypted using `Encryptor.Default` (seed=13, size=16). Use the Security module to generate encrypted values.

---

## Related Modules

| Module | Description |
|--------|-------------|
| [Security](../Security/readme.md) | AES encryption used for SMTP credentials |
| [Logging](../Logging/readme.md) | Logging used throughout the module |
| [Configuration](../Configuration/readme.md) | INI config used to load SMTP settings |
