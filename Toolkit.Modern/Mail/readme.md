# Mail Module

## Overview

The **Mail** module provides utilities for email processing, including attachment handling, batch processing, and temporary file management. It integrates with .NET's email capabilities to provide higher-level email operation abstractions.

---

## Purpose

Email operations in enterprise applications need:
1. **Attachment extraction** - Process incoming email attachments
2. **Batch processing** - Handle multiple attachments efficiently
3. **Temporary storage** - Manage transient attachment files
4. **Error recovery** - Handle malformed attachments gracefully
5. **Detailed logging** - Track email operations

---

## Key Classes

### `EmailAttachmentHandler`
**Purpose:** Main class for email attachment processing.

**Responsibilities:**
- Extract attachments from email messages
- Manage temporary attachment storage
- Process attachments in batches
- Track processing results

**Key Methods:**
```csharp
// Single attachment processing
AttachmentProcessResult ProcessAttachment(Attachment attachment);

// Batch processing
IList<AttachmentProcessResult> ProcessAttachments(IEnumerable<Attachment> attachments);

// Save attachment
TempFileAttachment SaveAttachmentToTemp(Attachment attachment);

// Clean up
void CleanupTemporaryFiles();
void CleanupTemporaryFile(string filePath);
```

### `MailUtil`
**Purpose:** General-purpose email utilities.

**Methods:**
```csharp
// Email validation
static bool IsValidEmailAddress(string email);

// Address parsing
static MailAddress ParseMailAddress(string addressString);

// Content encoding
static string EncodeEmailHeader(string header);
static string DecodeEmailHeader(string header);
```

### `TempFileAttachment`
**Purpose:** Represents a temporary attachment file.

**Properties:**
```csharp
string OriginalFileName { get; }
string TemporaryFilePath { get; }
long FileSize { get; }
string ContentType { get; }
DateTime CreatedAt { get; }
bool IsDisposed { get; }
```

**Methods:**
```csharp
// Access file content
byte[] ReadAllBytes();
string ReadAsText(Encoding encoding = null);
Stream OpenRead();

// Cleanup
void Dispose();
```

### `AttachmentProcessResult`
**Purpose:** Result of attachment processing operation.

**Properties:**
```csharp
bool IsSuccess { get; }
string OriginalFileName { get; }
TempFileAttachment TempFile { get; }
ProcessingMethod Method { get; }
string ErrorMessage { get; }
DateTime ProcessedAt { get; }
Exception LastException { get; }
```

### `ProcessingMethod`
**Purpose:** Different strategies for processing attachments.

**Values:**
```csharp
public enum ProcessingMethod
{
  SaveToTemp,      // Extract to temporary file
    ExtractContent,  // Extract content for processing
    Archive,         // Archive multiple attachments
    Scan,    // Scan for viruses
  Convert          // Convert to different format
}
```

### `PartInfo`
**Purpose:** Information about an email part (body, attachment, etc.).

**Properties:**
```csharp
string FileName { get; }
string ContentType { get; }
string ContentDisposition { get; }
long ContentSize { get; }
bool IsAttachment { get; }
bool IsInline { get; }
```

### `SkippedFile`
**Purpose:** Information about skipped attachments.

**Properties:**
```csharp
string FileName { get; }
string Reason { get; }  // Why it was skipped
DateTime SkippedAt { get; }
```

---

## Usage Patterns

### Basic Attachment Processing

```csharp
var handler = new EmailAttachmentHandler();

// Process single attachment
var attachment = GetAttachmentFromEmail();
var result = handler.ProcessAttachment(attachment);

if (result.IsSuccess)
{
    byte[] fileData = result.TempFile.ReadAllBytes();
    ProcessAttachmentData(fileData);
}
else
{
    Log.Warning($"Failed to process: {result.ErrorMessage}");
}
```

### Batch Processing

```csharp
var handler = new EmailAttachmentHandler();

// Get email message
var message = GetEmailMessage();
var attachments = message.Attachments;

// Process all attachments
var results = handler.ProcessAttachments(attachments);

foreach (var result in results)
{
    if (result.IsSuccess)
    {
   ProcessFile(result.TempFile);
    }
    else
    {
   Log.Warning($"Skipped {result.OriginalFileName}: {result.ErrorMessage}");
  }
}

// Clean up temporary files
handler.CleanupTemporaryFiles();
```

### Selective Attachment Processing

```csharp
var handler = new EmailAttachmentHandler();

var validAttachments = new List<AttachmentProcessResult>();

foreach (var attachment in email.Attachments)
{
    // Filter by type
    if (!IsAcceptedFileType(attachment.ContentType))
    {
     Log.Info($"Skipping unsupported type: {attachment.FileName}");
      continue;
    }
 
    // Filter by size (max 50 MB)
    if (attachment.ContentLength > 50 * 1024 * 1024)
    {
 Log.Warning($"Skipping large file: {attachment.FileName}");
    continue;
    }
    
    // Process accepted attachment
    var result = handler.ProcessAttachment(attachment);
    if (result.IsSuccess)
        validAttachments.Add(result);
}
```

### Save to Permanent Location

```csharp
var handler = new EmailAttachmentHandler();

foreach (var attachment in email.Attachments)
{
    var result = handler.ProcessAttachment(attachment);
 
    if (result.IsSuccess)
    {
        // Save from temporary to permanent location
        string permanentPath = Path.Combine(
            @"C:\uploads",
    SanitizeFileName(result.OriginalFileName)
        );
        
        File.Copy(result.TempFile.TemporaryFilePath, permanentPath, overwrite: true);
        Log.Info($"Saved: {permanentPath}");
    }
}
```

---

## Email Validation

### Email Address Validation

```csharp
// Built-in validation
bool isValid = MailUtil.IsValidEmailAddress("user@example.com");

if (!isValid)
{
    Log.Warning("Invalid email address format");
}

// Custom validation
var addresses = new[]
{
    "valid@example.com",
    "invalid@",
    "@example.com",
    "test@domain.co.uk"
};

foreach (var addr in addresses)
{
  if (MailUtil.IsValidEmailAddress(addr))
        Console.WriteLine($"Valid: {addr}");
    else
        Console.WriteLine($"Invalid: {addr}");
}
```

### Address Parsing

```csharp
try
{
  var address = MailUtil.ParseMailAddress("John Doe <john@example.com>");
    Console.WriteLine($"Name: {address.DisplayName}");
    Console.WriteLine($"Email: {address.Address}");
}
catch (FormatException ex)
{
    Log.Error("Invalid address format", ex);
}
```

---

## Content Encoding

### Email Header Encoding

```csharp
// Encode header for international characters
string header = "Subject: ????";
string encoded = MailUtil.EncodeEmailHeader(header);

// Use in email
email.Subject = encoded;
email.Send();

// Decode when reading
string decoded = MailUtil.DecodeEmailHeader(email.Subject);
Console.WriteLine(decoded);  // "????"
```

---

## Temporary File Management

### Automatic Cleanup

```csharp
using (var handler = new EmailAttachmentHandler())
{
 var results = handler.ProcessAttachments(attachments);
  
    // Process files
    foreach (var result in results)
    {
        ProcessFile(result.TempFile.TemporaryFilePath);
    }
    
    // Automatic cleanup on dispose
}  // Files cleaned up here
```

### Manual Cleanup

```csharp
var handler = new EmailAttachmentHandler();

var result = handler.ProcessAttachment(attachment);
ProcessFile(result.TempFile);

// Explicitly clean specific file
handler.CleanupTemporaryFile(result.TempFile.TemporaryFilePath);

// Or clean all temporary files
handler.CleanupTemporaryFiles();
```

### Temporary File Properties

```csharp
var result = handler.ProcessAttachment(attachment);
var tempFile = result.TempFile;

Console.WriteLine($"Original name: {tempFile.OriginalFileName}");
Console.WriteLine($"Temp path: {tempFile.TemporaryFilePath}");
Console.WriteLine($"Size: {tempFile.FileSize} bytes");
Console.WriteLine($"Content type: {tempFile.ContentType}");
Console.WriteLine($"Created: {tempFile.CreatedAt}");
```

---

## Email Part Information

### Extract Part Information

```csharp
var message = GetEmailMessage();

// Get all parts
var parts = message.AlternateViews
    .Select(v => new PartInfo 
    { 
        ContentType = v.ContentType.ToString(),
     ContentSize = v.ContentStream.Length,
        IsInline = true
  })
    .ToList();

foreach (var part in parts)
{
    Console.WriteLine($"Type: {part.ContentType}");
    Console.WriteLine($"Size: {part.ContentSize}");
    Console.WriteLine($"Inline: {part.IsInline}");
}
```

---

## Error Handling

### Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| Attachment too large | Exceeds size limit | Increase limit or reject |
| Unsupported file type | Unsafe file extension | Add to whitelist or reject |
| Corrupted attachment | Damaged file in email | Log and skip |
| Temp folder full | No space for temporary files | Increase temp folder size |
| Permission denied | Can't write to temp | Check folder permissions |

### Exception Handling

```csharp
try
{
  var result = handler.ProcessAttachment(attachment);
    result.LastException?.Let(ex => Log.Error("Processing error", ex));
}
catch (IOException ex)
{
    Log.Error("File I/O error while processing attachment", ex);
}
catch (UnauthorizedAccessException ex)
{
    Log.Error("Permission denied accessing temporary files", ex);
}
catch (Exception ex)
{
    Log.Error("Unexpected error processing attachment", ex);
}
```

---

## File Organization

### `EmailAttachmentHandler.cs`
Main attachment handler class.

### `MailUtil.cs`
General email utilities.

### `TempFileAttachment.cs`
Temporary attachment wrapper.

### `AttachmentProcessResult.cs`
Processing result model.

### `ProcessingMethod.cs`
Processing method enumeration.

### `PartInfo.cs`
Email part information.

### `SkippedFile.cs`
Skipped attachment information.

---

## Integration Example

```csharp
public class EmailProcessingService
{
    private readonly ILogger _logger;
    
    public EmailProcessingService(ILogger logger)
    {
        _logger = logger;
    }
    
    public async Task ProcessIncomingEmailAsync(MailMessage email)
    {
      _logger.Info($"Processing email from {email.From}");
    
        var handler = new EmailAttachmentHandler();
        
      try
     {
      var results = handler.ProcessAttachments(email.Attachments);
     
            foreach (var result in results)
            {
         if (!result.IsSuccess)
           {
     _logger.Warning($"Skipped attachment: {result.OriginalFileName}");
             continue;
        }
                
       // Process valid attachment
                await ImportAttachmentAsync(result.TempFile);
         }
     }
        finally
        {
     handler.CleanupTemporaryFiles();
    }
    }
    
    private async Task ImportAttachmentAsync(TempFileAttachment tempFile)
    {
        // Import logic
        _logger.Info($"Imported: {tempFile.OriginalFileName}");
    }
}
```

---

## Best Practices

### 1. **Always Cleanup**
```csharp
// Good
using (var handler = new EmailAttachmentHandler())
{
    // Process attachments
}  // Auto-cleanup

// Bad
var handler = new EmailAttachmentHandler();
var results = handler.ProcessAttachments(attachments);
// Forgot to cleanup - temp files may accumulate
```

### 2. **Validate File Types**
```csharp
// Good
var acceptedTypes = new[] { ".csv", ".xlsx", ".txt" };
if (!acceptedTypes.Contains(Path.GetExtension(attachment.FileName)))
    return;

// Bad
handler.ProcessAttachment(attachment);  // Accept anything
```

### 3. **Monitor File Sizes**
```csharp
// Good
const long MAX_SIZE = 50 * 1024 * 1024;  // 50 MB
if (attachment.ContentLength > MAX_SIZE)
{
    _logger.Warning("File too large");
    return;
}

// Bad
handler.ProcessAttachment(attachment);  // No size check
```

### 4. **Log Operations**
```csharp
// Good
var result = handler.ProcessAttachment(attachment);
if (result.IsSuccess)
    _logger.Info($"Processed: {result.OriginalFileName}");
else
    _logger.Warning($"Failed: {result.ErrorMessage}");

// Bad
var result = handler.ProcessAttachment(attachment);  // Silent failure
```

---

## Summary

The Mail module provides:

**Key Strengths:**
- ? Simple attachment processing
- ? Batch operation support
- ? Automatic temporary file cleanup
- ? Email validation utilities
- ? Header encoding support
- ? Error handling and logging

**Best For:**
- Email server integration
- Automated attachment processing
- Document ingestion systems
- Email data extraction
- Batch email operations

**Not Ideal For:**
- Real-time email streaming
- Complex MIME structure processing
- Complete email client implementation

