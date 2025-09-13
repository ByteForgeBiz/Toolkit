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
- **Automatic Compression**: Compresses large files into ZIP archives when needed
- **Multi-Part Splitting**: Splits very large attachments into multiple manageable parts
- **File Renaming**: Support for custom attachment names while preserving original files
- **Skipped File Handling**: Graceful handling of missing or inaccessible files
- **Comprehensive Result Reporting**: Detailed feedback on processing outcomes

### Processing Strategies
1. **Direct Attachment** (≤5MB total): Files attached directly to email
2. **Compressed** (>5MB, ≤5MB compressed): Files compressed into single ZIP
3. **Compressed and Split** (>5MB compressed, ≤20MB total): Files split into multiple ZIP parts
4. **Failed** (>20MB total): Processing fails with descriptive error

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
- `CleanupTempFiles(result)` - Clean up temporary files created during processing

**Features:**
- Implements `IDisposable` for proper resource cleanup
- Automatic temporary directory management
- Intelligent file distribution for multi-part archives

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
- `ProcessingMethod` - How attachments were processed
- `TempFiles` - List of temporary files created
- `SkippedFiles` - Files that couldn't be processed
- `PartDistribution` - Information about multi-part archive distribution
- `Error` - Error message if processing failed
- `Success` - Boolean indicating processing success

### Supporting Classes
- **ProcessingMethod** - Enum defining processing strategies
- **PartInfo** - Information about individual parts in multi-part archives
- **SkippedFile** - Details about files that couldn't be processed
- **FileBucket** - Internal class for file distribution optimization

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
            Console.WriteLine("Files compressed into ZIP");
            break;
        case ProcessingMethod.CompressedAndSplit:
            Console.WriteLine($"Files split into {result.PartDistribution.Count} parts");
            break;
    }
}
else
{
    Console.WriteLine($"Processing failed: {result.Error}");
}

// Always cleanup temporary files
handler.CleanupTempFiles(result);
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
    if (result.ProcessingMethod == ProcessingMethod.CompressedAndSplit)
    {
        // Files were split into multiple parts
        Console.WriteLine("Files split into multiple ZIP archives:");
        foreach (var part in result.PartDistribution)
        {
            Console.WriteLine($"Part {part.PartNumber}: {part.FileCount} files");
            foreach (var file in part.Files)
            {
                Console.WriteLine($"  - {file}");
            }
        }
    }
    
    // Email body automatically includes processing notes
    // and attachment summaries
}

// Cleanup
handler.CleanupTempFiles(result);
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
- **Maximum Total Size**: 20MB (after compression/splitting)
- **Single Compressed Archive Limit**: 5MB

### Processing Decision Tree
1. **Total size ≤ 5MB**: Direct attachment
2. **Total size > 5MB**: Create compressed ZIP
3. **Compressed ZIP ≤ 5MB**: Attach single ZIP
4. **Compressed ZIP > 5MB but ≤ 20MB**: Split into multiple ZIP parts
5. **Total size > 20MB**: Fail with error message

### Multi-Part Archive Strategy
- Files are sorted by size (largest first)
- Greedy distribution algorithm ensures balanced part sizes
- Each part is a complete, independently extractable ZIP archive
- Part naming follows pattern: `Attachments_TIMESTAMP_Part1of3.zip`

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
- **Size Exceeded**: Files too large for processing limits
- **Compression Failed**: Issues creating ZIP archives
- **SMTP Errors**: Network or authentication problems

### Automatic Email Notifications
The system automatically adds processing notifications to email bodies:
- Compression notices for ZIP attachments
- Multi-part split notifications with part counts
- Skipped file warnings with reasons

## Best Practices

### Resource Management
```csharp
// Always use using statements for proper disposal
using var handler = new EmailAttachmentHandler();
using var email = new MailMessage();

try
{
    var result = handler.ProcessAttachments(email, files);
    // Process result...
}
finally
{
    // Cleanup is handled automatically by using statements
    // But you can also manually cleanup temp files
    handler.CleanupTempFiles(result);
}
```

### Security Considerations
- Store SMTP credentials encrypted in configuration files
- Validate all email addresses before sending
- Use secure protocols (TLS 1.2 or higher)
- Implement proper logging for audit trails
- Clean up temporary files to avoid data exposure

### Performance Optimization
- Process attachments once and reuse results for multiple recipients
- Use file name mapping to avoid file system operations
- Consider async patterns for large file processing
- Monitor temporary disk space usage for large operations

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
- **Compression functionality** with file name mapping
- **Multi-part archive creation** and distribution
- **Error handling** for missing files and size limits
- **File cleanup** and resource management
- **Performance testing** with multiple operations
- **Edge cases** and boundary conditions

Test categories: `Unit`, `Mail`

Key test files:
- `EmailAttachmentHandlerTests.cs` - Comprehensive test suite with 25+ test methods

This Mail module provides a robust, enterprise-ready email solution with intelligent attachment handling that automatically adapts to file sizes and constraints while maintaining security and providing detailed feedback on all operations.

---

## 📚 Modules

| Module | Description |
|--------|-------------|
| [🏠 Home](../readme.md) | ByteForge.Toolkit main documentation |
| [CommandLine](../CLI/readme.md) | Attribute-based CLI parsing with aliasing, typo correction, and plugin support |
| [Configuration](../Configuration/readme.md) | INI-based configuration system with typed section support |
| [Data](../Data/readme.md) | Comprehensive data processing with CSV, Database, Audio, and Exception handling |
| [DataStructures](../DataStructures/readme.md) | AVL tree and URL utility classes |
| [Logging](../Logging/readme.md) | Thread-safe logging system with async file/console output |
| [Mail](../Mail/readme.md) | Email utility with HTML support and attachment handling |
| [Security](../Security/readme.md) | AES-based string encryption with key generation and Galois Field logic |
| [Utils](../Utils/readme.md) | Miscellaneous helpers: timing, path utilities, progress bar |
| [Zip](../Zip/readme.md) | Advanced ZIP library with multi-part archives, self-extracting executables, and AES encryption |