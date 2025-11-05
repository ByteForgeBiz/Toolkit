# Mail Module

The Mail module provides comprehensive email processing capabilities with intelligent attachment handling, including automatic compression, splitting, and file renaming features. This module is designed for enterprise applications that need robust email functionality with size-aware processing.

## Key Features

### Email Sending Capabilities
- **HTML and Plain Text Support**: Send emails with rich HTML content or plain text
- **Multiple Recipients**: Support for semicolon-separated recipient lists
- **Secure SMTP Configuration**: Encrypted credential storage using AES encryption
- **Automatic Email Validation**: Built-in validation for sender and recipient email addresses
- **Configurable SMTP Settings**: Support for various SMTP servers with customizable ports and security protocols

### Intelligent Attachment Processing
- **Size-Aware Processing**: Automatically handles files based on total size constraints
- **Automatic Compression**: Creates ZIP archives for files exceeding size limits
- **File Renaming**: Support for custom attachment names while preserving original files
- **Skipped File Handling**: Graceful handling of missing or inaccessible files
- **Automatic Cleanup**: Uses `TempFileAttachment` for self-managing temporary files
- **Comprehensive Result Reporting**: Detailed feedback on processing outcomes

### Processing Strategies
1. **Direct Attachment** (≤5MB total): Files attached directly to email
2. **Compressed** (>5MB): Files compressed into a single ZIP archive
3. **Failed** (>20MB total): Processing fails with descriptive error

## Main Classes

### MailUtil
Static utility class providing high-level email sending functionality.

**Key Methods:**
- `SendMail(subject, body, isHtml)` - Send to default recipient
- `SendMail(emailAddresses, subject, body, isHtml, filesToAttach)` - Send with attachments
- `SendMail(emailAddresses, subject, body, isHtml, fileNameMap, filesToAttach)` - Send with file renaming

**Properties:**
- `Settings` - Access to mail server configuration

### EmailAttachmentHandler
Core class for processing email attachments with size restrictions and compression.

**Key Methods:**
- `ProcessAttachments(email, filesToAttach, fileNameMap, addAttachmentSummary)` - Process files for attachment

**Features:**
- Implements `IDisposable` for proper resource cleanup
- Automatic temporary directory management
- Uses `TempFileAttachment` for self-cleaning temporary files
- ZIP compression for large file handling

### MailServerSettings
Configuration class for SMTP server settings.

**Properties:**
- `Server` - SMTP server address
- `Port` - SMTP port (default: 587)
- `SecurityProtocol` - Security protocol (default: TLS 1.2)
- `EncryptedUser/EncryptedPassword` - Encrypted credentials
- `User/Password` - Auto-decrypted credentials
- `Sender` - From email address
- `SendTo` - Default recipient address

### AttachmentProcessResult
Result object containing detailed processing information.

**Properties:**
- `ProcessingMethod` - How attachments were processed (None, DirectAttachment, Compressed, Failed)
- `SkippedFiles` - Files that couldn't be processed with reasons
- `PartDistribution` - Information about multi-part archives (for future use)
- `Error` - Error message if processing failed
- `Success` - Boolean indicating processing success

### Supporting Classes
- **TempFileAttachment** - Self-cleaning attachment class that deletes temp files on disposal
- **ProcessingMethod** - Enum defining processing strategies (None, DirectAttachment, Compressed, MultiPart, Failed)
- **PartInfo** - Information about individual parts in multi-part archives (reserved for future use)
- **SkippedFile** - Details about files that couldn't be processed

## Usage Examples

### Basic Email Sending
```csharp
// Simple email to default recipient
bool success = MailUtil.SendMail(
    "Monthly Report",
    "<h1>Report Summary</h1><p>Please find attached files.</p>",
    isHtml: true
);

// Email with attachments to specific recipients
bool success = MailUtil.SendMail(
    "user1@example.com;user2@example.com",
    "Project Files",
    "Here are the latest project files.",
    isHtml: false,
    "document1.pdf", "spreadsheet.xlsx"
);
```

### Advanced Attachment Processing
```csharp
// Create file name mapping for custom attachment names
var fileNameMap = new Dictionary<string, string>
{
    { "internal_report_20250913.pdf", "Client_Report.pdf" },
    { "temp_data.xlsx", "Analysis_Data.xlsx" }
};

// Process attachments with renaming
using var handler = new EmailAttachmentHandler();
using var email = new MailMessage();

var result = handler.ProcessAttachments(
    email,
    new List<string> { @"C:\temp\internal_report_20250913.pdf", @"C:\data\temp_data.xlsx" },
    fileNameMap,
    addAttachmentSummary: true
);

if (result.Success)
{
    // Send email with processed attachments
    // Files will appear as "Client_Report.pdf" and "Analysis_Data.xlsx"
    
    // Check processing method used
    switch (result.ProcessingMethod)
    {
        case ProcessingMethod.DirectAttachment:
            Console.WriteLine("Files attached directly");
            break;
        case ProcessingMethod.Compressed:
            Console.WriteLine("Files compressed into ZIP archive");
            break;
    }
}
else
{
    Console.WriteLine($"Processing failed: {result.Error}");
}

// Temporary files are automatically cleaned up when email is disposed
// or when TempFileAttachment objects are disposed
```

### Handling Large Files
```csharp
using var handler = new EmailAttachmentHandler();
using var email = new MailMessage
{
    From = new MailAddress("sender@example.com"),
    Subject = "Large File Transfer",
    Body = "Large files are attached in compressed format."
};

var largeFiles = new List<string>
{
    @"C:\data\large_dataset.csv",
    @"C:\reports\annual_report.pdf",
    @"C:\logs\application.log"
};

var result = handler.ProcessAttachments(email, largeFiles);

if (result.Success)
{
    if (result.ProcessingMethod == ProcessingMethod.Compressed)
    {
        // Files were compressed into a ZIP archive
        Console.WriteLine("Files compressed into ZIP archive");
        Console.WriteLine($"ZIP attachment: {email.Attachments[0].Name}");
    }
    
    // Email body automatically includes processing notes
    // and attachment summaries
}

// Temporary ZIP files are automatically cleaned up when email is disposed
```

## Configuration

### INI Configuration Example
```ini
[Mail Server]
sServer=smtp.office365.com
iPort=587
sSecurityProtocol=Tls12
esUser=ENCRYPTED_USERNAME_HERE
esPass=ENCRYPTED_PASSWORD_HERE
sSender=noreply@yourcompany.com
sSendTo=admin@yourcompany.com
```

### Configuration Access
```csharp
// Access mail settings
var settings = MailUtil.Settings;
Console.WriteLine($"SMTP Server: {settings.Server}");
Console.WriteLine($"Port: {settings.Port}");
Console.WriteLine($"Security: {settings.SecurityProtocol}");

// Credentials are automatically decrypted
Console.WriteLine($"Username: {settings.User}");
```

## Size Limits and Processing Logic

### File Size Constraints
- **Individual Direct Attachment Limit**: 5MB per file
- **Total Direct Attachment Limit**: 5MB combined
- **Maximum Total Size**: 20MB (after compression)
- **ZIP Archive Limit**: Single archive approach

### Processing Decision Tree
1. **Total size ≤ 5MB**: Direct attachment
2. **Total size > 5MB and ≤ 20MB**: Compress into ZIP archive
3. **Total size > 20MB**: Fail with error message

### Compression Strategy
- Files exceeding 5MB total are compressed into a single ZIP archive
- ZIP file is created in temporary directory with timestamp naming
- `TempFileAttachment` automatically handles cleanup when email is disposed
- ZIP naming follows pattern: `Attachments_YYYYMMDDHHMMSS.zip`

## Dependencies

### Required Modules
- **Configuration**: For loading SMTP settings (`MailServerSettings`)
- **Security**: For credential encryption/decryption (`Encryptor`)
- **Logging**: For error logging and diagnostics (`Log`)

### System Dependencies
- `System.Net.Mail` - Core email functionality
- `System.IO.Compression` - ZIP archive creation
- `System.ComponentModel` - Configuration attributes

## Error Handling and Logging

### Comprehensive Error Reporting
```csharp
var result = handler.ProcessAttachments(email, files);

if (!result.Success)
{
    Console.WriteLine($"Processing failed: {result.Error}");
}

// Check for skipped files
if (result.SkippedFiles.Count > 0)
{
    Console.WriteLine("Some files were skipped:");
    foreach (var skipped in result.SkippedFiles)
    {
        Console.WriteLine($"- {skipped.FilePath}: {skipped.Reason}");
    }
}
```

### Common Error Scenarios
- **File Not Found**: Files missing from disk
- **Access Denied**: Insufficient permissions to read files
- **Size Exceeded**: Files too large for processing limits (>20MB)
- **Compression Failed**: Issues creating ZIP archives
- **SMTP Errors**: Network or authentication problems

### Automatic Email Notifications
The system automatically adds processing notifications to email bodies:
- Compression notices for ZIP attachments
- Skipped file warnings with reasons
- Attachment summaries with file names and sizes

## Best Practices

### Resource Management
```csharp
// Always use using statements for proper disposal
using var handler = new EmailAttachmentHandler();
using var email = new MailMessage();

try
{
    var result = handler.ProcessAttachments(email, files);
    
    if (result.Success)
    {
        // Send email - temp files will be cleaned up automatically
        smtp.Send(email);
    }
}
// TempFileAttachment objects automatically clean up when email is disposed
```

### Security Considerations
- Store SMTP credentials encrypted in configuration files
- Validate all email addresses before sending
- Use secure protocols (TLS 1.2 or higher)
- Implement proper logging for audit trails
- Temporary files are automatically cleaned up by `TempFileAttachment`

### Performance Optimization
- Process attachments once and reuse results for multiple recipients
- Use file name mapping to avoid file system operations
- ZIP compression is efficient for multiple files
- Monitor temporary disk space usage for large compression operations

### File Naming Strategy
```csharp
// Use meaningful file names in attachments
var fileNameMap = new Dictionary<string, string>
{
    // Map internal paths to user-friendly names
    { @"C:\temp\rpt_20250913_internal.pdf", "Monthly_Report_September.pdf" },
    { @"C:\logs\app_20250913.log", "Application_Logs.txt" },
    { "data.csv", "Customer_Data_Export.csv" }
};

// Use simple filename keys for better performance
var simpleMap = new Dictionary<string, string>
{
    { "rpt_20250913_internal.pdf", "Monthly_Report.pdf" },
    { "app_20250913.log", "App_Logs.txt" }
};
```

## Integration Points

### With Configuration Module
```csharp
// Mail settings are automatically loaded from configuration
var settings = Configuration.GetSection<MailServerSettings>("Mail Server");
```

### With Security Module
```csharp
// Credentials are automatically decrypted
public string User => Encryptor.Default.Decrypt(EncryptedUser);
public string Password => Encryptor.Default.Decrypt(EncryptedPassword);
```

### With Logging Module
```csharp
// Errors are automatically logged
Log.Error("Failed to send email", exception);
Log.Warning($"Invalid email addresses removed: {string.Join(", ", invalidAddrs)}");
```

## Testing

The module includes comprehensive unit tests covering:
- **Direct attachment scenarios** with various file sizes
- **ZIP compression functionality** with file name mapping
- **Large file compression** with automatic ZIP creation
- **Error handling** for missing files and size limits
- **Automatic cleanup** through `TempFileAttachment` disposal
- **Performance testing** with multiple operations
- **Edge cases** and boundary conditions

Test categories: `Unit`, `Mail`

Key test files:
- `EmailAttachmentHandlerTests.cs` - Comprehensive test suite with 20+ test methods covering all processing scenarios

This Mail module provides a robust, enterprise-ready email solution with intelligent attachment handling that automatically adapts to file sizes using ZIP compression while maintaining security and providing detailed feedback on all operations.

---

## 📚 Modules

| Module                                | Description                                                                     |
|---------------------------------------|---------------------------------------------------------------------------------|
| [🏠 Home](./Home.md)                  | ByteForge.Toolkit main documentation                                            |
| [CommandLine](./CLI.md)               | Attribute-based CLI parsing with aliasing, typo correction, and plugin support  |
| [Configuration](./Configuration.md)   | INI-based configuration system with typed section support                       |
| [Data](./Data.md)                     | Comprehensive data processing with CSV, Database, Audio, and Exception handling |
| [DataStructures](./DataStructures.md) | AVL tree and URL utility classes                                                |
| [Logging](./Logging.md)               | Thread-safe logging system with async file/console output                       |
| [Mail](./Mail.md)                     | Email utility with HTML support and attachment handling                         |
| [Net](./Net.md)                       | FTP/FTPS/SFTP high-level transfer client                                        |
| [Security](./Security.md)             | AES-based string encryption with key generation and Galois Field logic          |
| [Utils](./Utils.md)                   | Miscellaneous helpers: timing, path utilities, progress bar                     |
| [Core](./Core.md)                     | Embedded resource deployment (WinSCP)                                           |
| [HTML](./HTML.md)                     | Web UI framework components                                                     |

