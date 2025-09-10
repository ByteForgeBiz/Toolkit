# MailUtil.md

## ByteForge Mail Utility Module

`MailUtil` is a robust utility class designed to simplify and standardize email sending in .NET Framework 4.8 applications. It includes support for HTML or plain text emails, automatic attachment handling with file renaming capabilities, secure SMTP configuration, and file size-aware compression/splitting for large attachments.

### 🚀 Features
- Send emails with plain text or HTML content
- Attach multiple files with optional renaming in email attachments
- Automatic file compression when attachments exceed size limits
- Multi-part archive splitting for very large attachments
- Strongly-typed configuration using the new Configuration system
- Secure credential storage using AES encryption (via `Encryptor`)
- Comprehensive attachment processing with detailed result reporting
- Automatic cleanup of temporary files
- Skipped file handling and summary reporting

### 🧱 Dependencies
- [Encryptor](../Security/readme.md): Used to decrypt email credentials
- [Configuration](../Configuration/readme.md): Loads SMTP settings using strongly-typed sections

### 🧪 Basic Usage Example
```csharp
// Simple email with attachments
MailUtil.SendMail(
    "recipient@example.com",
    "Monthly Report",
    "<p>Please find the reports attached.</p>",
    isHtml: true,
    filesToAttach: new[] { "report1.pdf", "report2.xlsx" }
);
```

### 🏷️ File Renaming Example
```csharp
// Create a mapping for renaming files in attachments
var fileNameMap = new Dictionary<string, string>
{
    { @"C:\logs\application_20250903.log", "client.log" },
    { @"C:\temp\report_internal.pdf", "monthly_report.pdf" }
};

// Using EmailAttachmentHandler directly for advanced scenarios
var handler = new EmailAttachmentHandler();
var result = handler.ProcessAttachments(
    email, 
    filesToAttach, 
    fileNameMap,  // Optional file renaming
    addAttachmentSummary: true
);

if (result.Success)
{
    // Files will appear in email with renamed attachments
    // Original files on disk remain unchanged
}
```

### 📜 Configuration Example (INI)
```ini
[Mail Server]
sServer=smtp.example.com
iPort=587
sSecurityProtocol=Tls12
esUser=ENCRYPTED_USERNAME
esPass=ENCRYPTED_PASSWORD
sSender=noreply@example.com
sSendTo=admin@example.com
```

### 🔧 Configuration Class
The mail settings are now managed through a strongly-typed configuration class:

```csharp
public class MailServerSettings
{
    [ConfigName("sServer")]
    public string Server { get; set; }
    
    [ConfigName("iPort")]
    [DefaultValue(587)]
    public int Port { get; set; }
    
    [ConfigName("sSecurityProtocol")]
    [DefaultValue(SecurityProtocolType.Tls12)]
    public SecurityProtocolType SecurityProtocol { get; set; }
    
    [ConfigName("esUser")]
    public string EncryptedUser { get; set; }
    
    [ConfigName("esPass")]
    public string EncryptedPassword { get; set; }
    
    [ConfigName("sSender")]
    public string Sender { get; set; }
    
    [ConfigName("sSendTo")]
    public string SendTo { get; set; }
    
    [Ignore]
    public string User => Encryptor.Default.Decrypt(EncryptedUser);
    
    [Ignore]
    public string Password => Encryptor.Default.Decrypt(EncryptedPassword);
}
```

### 📦 Attachment Processing Logic

The `EmailAttachmentHandler` uses intelligent processing strategies:

1. **Direct Attachment** (≤5MB total): Files attached directly to email
2. **Compressed** (>5MB, ≤5MB compressed): Files compressed into single ZIP
3. **Compressed and Split** (>5MB compressed, ≤20MB total): Files split into multiple ZIP parts
4. **Failed** (>20MB total): Processing fails with descriptive error

#### Processing Result
```csharp
public class AttachmentProcessResult
{
    public ProcessingMethod ProcessingMethod { get; set; }
    public List<string> TempFilesCreated { get; set; }
    public List<SkippedFile> SkippedFiles { get; set; }
    public List<PartInfo> PartDistribution { get; set; }
    public string Error { get; set; }
    public bool Success => string.IsNullOrEmpty(Error);
}
```

### 🔐 Credential Handling
Credentials (`esUser`, `esPass`) are encrypted in the configuration file and automatically decrypted via `Encryptor.Default.Decrypt()` when accessed through the `User` and `Password` properties.

### 📁 File Renaming Capabilities
- **Dictionary Mapping**: Use `Dictionary<string, string>` to map original file paths to desired attachment names
- **ZIP Archives**: Renamed files maintain their new names even when compressed
- **Multi-part Archives**: File names are preserved across split archive parts
- **Email Summaries**: Display names reflect the renamed files in email body summaries
- **Original Files Preserved**: Renaming only affects email attachments, not source files

### 🧹 Cleanup and Best Practices
```csharp
var handler = new EmailAttachmentHandler();
var result = handler.ProcessAttachments(email, files, fileNameMap);

try
{
    // Send email here
    smtp.Send(email);
}
finally
{
    // Always cleanup temporary files
    handler.CleanupTempFiles(result);
}
```

### ✅ Best Practices
- Store SMTP credentials encrypted in configuration files
- Use file renaming to provide user-friendly attachment names with simple filename keys
- Ensure renamed attachment names are unique if attaching multiple files with similar names
- Keep total attachments under 20MB to avoid processing failures
- Handle the `AttachmentProcessResult` to check for skipped files or errors
- Always call `CleanupTempFiles()` to remove temporary ZIP archives
- Review the `Log` for detailed error information on send failures
- Use strongly-typed configuration for maintainable mail settings

### 🔍 Error Handling
The system provides comprehensive error reporting:
- **Skipped Files**: Files that couldn't be processed (missing, locked, corrupted)
- **Size Limits**: Clear errors when files exceed maximum allowable sizes
- **Processing Errors**: Detailed error messages for compression or splitting failures
- **SMTP Errors**: Logged via the `Log` utility for debugging email delivery issues

### 🔗 Related Modules
- [Utils](../Utils/readme.md)
- [Configuration](../Configuration/readme.md)
- [Security](../Security/readme.md)
- [Logging](../Logging/readme.md)