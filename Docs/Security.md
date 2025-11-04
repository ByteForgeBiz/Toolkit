# Security Module

## ByteForge.Toolkit Security & Encryption System

A comprehensive AES-based encryption framework providing secure string encryption with deterministic key generation, custom Galois Field mathematics, and enterprise-grade security features. Built for .NET Framework 4.8 with focus on configuration security and credential protection.

### 🚀 Key Features

#### Core Encryption Capabilities
- **Custom AES Implementation**: Full AES encryption with custom Galois Field mathematics for optimal performance
- **Deterministic Key Generation**: Seed-based key derivation enabling reproducible encryption across sessions
- **Hexadecimal Encoding**: Safe text representation of encrypted binary data for storage and transmission
- **Variable Key Sizes**: Support for 16, 24, and 32-byte keys with configurable strength
- **High-Performance**: Optimized bit operations and lookup tables for maximum throughput

#### Enterprise Integration
- **Configuration Security**: Seamless integration with ByteForge Configuration system for credential protection
- **Database Security**: Automatic encryption/decryption of database credentials in DBAccess
- **Thread Safety**: Safe for concurrent use across multiple threads and application domains
- **Static & Instance APIs**: Flexible usage patterns for different application architectures

#### Advanced Security Features
- **No External Dependencies**: Self-contained implementation without third-party cryptographic libraries
- **Byte-Level Operations**: Direct control over SubBytes, ShiftRows, MixColumns transformations
- **Key Schedule Generation**: Automatic forward and reverse key scheduling
- **Galois Field Arithmetic**: Custom implementation of GF(2^8) operations for AES S-boxes

### 🧱 Architecture Overview

#### Core Components

##### `Encryptor` Class
- **High-Level API**: Simple encryption/decryption operations for string data
- **Default Instance**: Pre-configured `Encryptor.Default` with standard parameters
- **Instance Management**: Reusable encryptor instances with persistent key material
- **Static Methods**: One-off encryption operations without instance creation

##### `AESEncryption` Class (Internal)
- **Low-Level AES Operations**: Complete AES implementation with all transformations
- **Key Management**: Forward and reverse key schedule generation and management
- **Galois Field Operations**: Custom GF(2^8) arithmetic for S-box computations
- **Byte Transformations**: SubBytes, ShiftRows, MixColumns, and AddRoundKey operations
- **Lookup Tables**: Optimized pre-computed tables for performance-critical operations

#### Security Architecture
```
Application Layer
├── Encryptor.Default (Quick Access)
├── Encryptor Instances (Reusable)
└── Static Methods (One-time Use)
    
Core Layer  
└── AESEncryption (Internal Implementation)
    ├── Key Schedule Generation
    ├── Galois Field Mathematics
    ├── AES Transformations
    └── Byte-Level Operations
```

### 🧪 Usage Examples

#### Basic String Encryption
```csharp
// Create encryptor with custom seed and key size
var encryptor = new Encryptor(seed: 12345, size: 32);
string encrypted = encryptor.Encrypt("Secret message");
string decrypted = encryptor.Decrypt(encrypted);
// decrypted == "Secret message"

// Result: encrypted = "A1B2C3D4E5F6..." (hexadecimal)
```

#### Default Instance (Quick Start)
```csharp
// Use pre-configured default instance (seed: 13, size: 16)
string encrypted = Encryptor.Default.Encrypt("Quick encryption");
string decrypted = Encryptor.Default.Decrypt(encrypted);

Console.WriteLine($"Original: Quick encryption");
Console.WriteLine($"Encrypted: {encrypted}");
Console.WriteLine($"Decrypted: {decrypted}");
```

#### Static Methods (One-off Operations)
```csharp
// Static methods for one-time encryption without instance creation
string encrypted = Encryptor.Encrypt(12345, 32, "Data to encrypt");
string decrypted = Encryptor.Decrypt(12345, 32, encrypted);

// Perfect for utility functions and simple encryption needs
```

#### Enterprise Configuration Security
```csharp
// Database credential encryption in configuration
public class SecureConfiguration
{
    private static readonly Encryptor _encryptor = 
        new Encryptor(Environment.MachineName.GetHashCode(), 32);
    
    public void SaveDatabaseCredentials(string username, string password)
    {
        var config = Configuration.GetSection<DatabaseOptions>("Database");
        config.EncryptedUser = _encryptor.Encrypt(username);
        config.EncryptedPassword = _encryptor.Encrypt(password);
        Configuration.Save();
    }
    
    public (string username, string password) LoadCredentials()
    {
        var config = Configuration.GetSection<DatabaseOptions>("Database");
        return (
            _encryptor.Decrypt(config.EncryptedUser),
            _encryptor.Decrypt(config.EncryptedPassword)
        );
    }
}
```

#### Multiple Key Sizes and Seeds
```csharp
// Different encryption strength levels
var light = new Encryptor(1001, 16);      // 128-bit keys (fast)
var medium = new Encryptor(2002, 24);     // 192-bit keys (balanced)
var strong = new Encryptor(3003, 32);     // 256-bit keys (maximum security)

string data = "Sensitive information";
string lightEncrypted = light.Encrypt(data);
string mediumEncrypted = medium.Encrypt(data);
string strongEncrypted = strong.Encrypt(data);

// Each produces different ciphertext with different security levels
```

#### Environment-Specific Encryption
```csharp
// Machine-specific encryption for local configuration
public class MachineSpecificEncryption
{
    private static readonly Encryptor _machineEncryptor = new Encryptor(
        Environment.MachineName.GetHashCode() ^ Environment.UserName.GetHashCode(), 
        32
    );
    
    public static string EncryptForMachine(string plaintext)
        => _machineEncryptor.Encrypt(plaintext);
    
    public static string DecryptForMachine(string ciphertext)
        => _machineEncryptor.Decrypt(ciphertext);
}

// Credentials encrypted on one machine cannot be decrypted on another
```

#### Bulk Data Processing
```csharp
// Efficient processing of multiple values
var encryptor = new Encryptor(9876, 32);
var sensitiveData = new[] { "password1", "password2", "api_key", "secret_token" };

// Encrypt all values
var encryptedData = sensitiveData
    .Select(data => encryptor.Encrypt(data))
    .ToArray();

// Store encrypted data in configuration or database
foreach (var encrypted in encryptedData)
{
    Console.WriteLine($"Encrypted: {encrypted}");
}

// Later, decrypt all values
var decryptedData = encryptedData
    .Select(encrypted => encryptor.Decrypt(encrypted))
    .ToArray();
```

### 🔒 Security Considerations

#### Cryptographic Strength
- **AES-128/192/256**: Full AES support with 128-bit (16-byte), 192-bit (24-byte), and 256-bit (32-byte) keys
- **Custom Implementation**: Self-contained AES with no external cryptographic dependencies
- **Galois Field Operations**: Proper GF(2^8) arithmetic for mathematically sound S-box operations
- **Key Schedule**: Full forward and reverse key expansion following AES specification

#### Key Management Best Practices
- **Secure Seed Storage**: Never hardcode seeds in source code; use configuration or environment variables
- **Seed Complexity**: Use high-entropy seeds derived from multiple sources (machine name, user, timestamp)
- **Key Size Selection**: 32-byte keys recommended for maximum security; 16-byte acceptable for performance-critical scenarios
- **Seed Uniqueness**: Use different seeds for different applications and environments
- **Rotation Strategy**: Implement periodic seed rotation for long-term data protection

#### Implementation Security Model
- **Deterministic Encryption**: Same plaintext + seed always produces identical ciphertext (by design)
- **No Random IV/Salt**: Prioritizes deterministic results for configuration and credential management
- **Hexadecimal Encoding**: Encrypted output is safe for text storage, databases, and configuration files
- **Thread Safety**: All encryption operations are thread-safe with no shared mutable state
- **Memory Management**: Minimal memory footprint with automatic cleanup of sensitive data

#### Production Security Requirements

##### Operational Security
- **Audit Logging**: Log encryption/decryption events (never log keys, seeds, or plaintext)
- **Error Handling**: Implement proper exception handling without exposing cryptographic details
- **Performance Monitoring**: Monitor encryption overhead in high-throughput scenarios
- **Fallback Mechanisms**: Plan for key recovery and migration scenarios

##### Data Protection
- **Input Validation**: Validate all inputs before encryption/decryption operations
- **Output Handling**: Treat all encrypted data as sensitive and apply appropriate access controls
- **Storage Security**: Protect configuration files and databases containing encrypted data
- **Transport Security**: Use TLS when transmitting encrypted data over networks

##### Compliance Considerations
- **Deterministic Nature**: Understand implications for compliance requirements (some require unique ciphertexts)
- **Key Escrow**: Consider key recovery requirements for regulated environments
- **Algorithm Approval**: Verify AES usage meets organizational and regulatory security standards
- **Documentation**: Maintain detailed documentation of encryption usage and key management procedures

#### Security Limitations & Trade-offs

##### By Design Limitations
- **No Salt/IV**: Deterministic encryption means identical plaintexts produce identical ciphertexts
- **Pattern Analysis**: Repeated data will show patterns in encrypted form
- **Dictionary Attacks**: Static encryption makes dictionary attacks possible against known plaintexts

##### Recommended Mitigations
- **Unique Seeds**: Use highly unique seeds per application domain to minimize pattern exposure
- **Data Variety**: Avoid encrypting highly predictable or repetitive data where possible
- **Access Controls**: Implement strong access controls on systems containing encrypted data
- **Monitoring**: Monitor for unusual access patterns to encrypted configuration data

### ✅ Best Practices & Recommendations

#### Application Architecture Patterns

##### Service-Oriented Approach
```csharp
// Recommended: Encapsulate encryption in a service
public interface IEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}

public class ConfigurationEncryptionService : IEncryptionService
{
    private readonly Encryptor _encryptor;
    
    public ConfigurationEncryptionService()
    {
        var seed = GetSecureSeedFromConfiguration();
        _encryptor = new Encryptor(seed, 32);
    }
    
    public string Encrypt(string plaintext) => _encryptor.Encrypt(plaintext);
    public string Decrypt(string ciphertext) => _encryptor.Decrypt(ciphertext);
    
    private int GetSecureSeedFromConfiguration()
    {
        // Combine multiple entropy sources
        var machineSeed = Environment.MachineName.GetHashCode();
        var userSeed = Environment.UserName.GetHashCode();
        var configSeed = Configuration.GetValue<int>("EncryptionSeed");
        return machineSeed ^ userSeed ^ configSeed;
    }
}
```

##### Dependency Injection Integration
```csharp
// Register encryption service in DI container
services.AddSingleton<IEncryptionService, ConfigurationEncryptionService>();

// Use in application components
public class DatabaseManager
{
    private readonly IEncryptionService _encryption;
    
    public DatabaseManager(IEncryptionService encryption)
    {
        _encryption = encryption;
    }
    
    public void SaveConnection(string connectionString)
    {
        var encrypted = _encryption.Encrypt(connectionString);
        // Save encrypted value
    }
}
```

#### Development & Testing Best Practices

##### Test-Friendly Design
```csharp
// Create testable encryption components
public class TestableEncryptor : IEncryptionService
{
    private readonly Encryptor _encryptor;
    
    public TestableEncryptor(int testSeed = 12345, int keySize = 16)
    {
        _encryptor = new Encryptor(testSeed, keySize);
    }
    
    public string Encrypt(string plaintext) => _encryptor.Encrypt(plaintext);
    public string Decrypt(string ciphertext) => _encryptor.Decrypt(ciphertext);
}

// Unit tests
[Test]
public void EncryptDecrypt_RoundTrip_ReturnsOriginalText()
{
    var encryptor = new TestableEncryptor();
    var original = "Test data";
    
    var encrypted = encryptor.Encrypt(original);
    var decrypted = encryptor.Decrypt(encrypted);
    
    Assert.AreEqual(original, decrypted);
    Assert.AreNotEqual(original, encrypted);
}
```

##### Performance Testing
```csharp
// Benchmark encryption performance
public class EncryptionBenchmark
{
    private readonly Encryptor _encryptor = new Encryptor(12345, 32);
    
    [Benchmark]
    public string EncryptShortString()
        => _encryptor.Encrypt("password123");
    
    [Benchmark]
    public string EncryptLongString()
        => _encryptor.Encrypt(new string('x', 1000));
    
    [Benchmark]
    public string DecryptString()
        => _encryptor.Decrypt("A1B2C3D4E5F6...");
}
```

#### Production Deployment Guidelines

##### Configuration Management
```csharp
// Secure configuration pattern
public static class SecureConfig
{
    private static readonly Lazy<IEncryptionService> _encryption = 
        new Lazy<IEncryptionService>(() => CreateEncryptionService());
    
    public static IEncryptionService Encryption => _encryption.Value;
    
    private static IEncryptionService CreateEncryptionService()
    {
        // Load seed from multiple sources
        var envSeed = Environment.GetEnvironmentVariable("ENCRYPTION_SEED");
        var configSeed = ConfigurationManager.AppSettings["EncryptionSeed"];
        var machineSeed = Environment.MachineName.GetHashCode();
        
        if (string.IsNullOrEmpty(envSeed) || string.IsNullOrEmpty(configSeed))
            throw new InvalidOperationException("Encryption configuration missing");
        
        var combinedSeed = envSeed.GetHashCode() ^ configSeed.GetHashCode() ^ machineSeed;
        return new ConfigurationEncryptionService(combinedSeed, 32);
    }
}
```

##### Error Handling & Logging
```csharp
// Comprehensive error handling
public class SafeEncryptionService : IEncryptionService
{
    private readonly IEncryptionService _innerService;
    private readonly ILogger _logger;
    
    public SafeEncryptionService(IEncryptionService innerService, ILogger logger)
    {
        _innerService = innerService;
        _logger = logger;
    }
    
    public string Encrypt(string plaintext)
    {
        try
        {
            if (string.IsNullOrEmpty(plaintext))
                throw new ArgumentException("Plaintext cannot be null or empty");
            
            var result = _innerService.Encrypt(plaintext);
            _logger.LogInfo("Encryption operation completed successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError("Encryption failed", ex);
            throw new EncryptionException("Failed to encrypt data", ex);
        }
    }
    
    public string Decrypt(string ciphertext)
    {
        try
        {
            if (string.IsNullOrEmpty(ciphertext))
                throw new ArgumentException("Ciphertext cannot be null or empty");
            
            var result = _innerService.Decrypt(ciphertext);
            _logger.LogInfo("Decryption operation completed successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError("Decryption failed", ex);
            throw new DecryptionException("Failed to decrypt data", ex);
        }
    }
}
```

#### Security Implementation Guidelines

##### Input Validation & Sanitization
```csharp
public static class SecureEncryptionHelper
{
    public static string EncryptWithValidation(this Encryptor encryptor, string plaintext)
    {
        ValidateInput(plaintext, nameof(plaintext));
        return encryptor.Encrypt(plaintext);
    }
    
    public static string DecryptWithValidation(this Encryptor encryptor, string ciphertext)
    {
        ValidateInput(ciphertext, nameof(ciphertext));
        ValidateHexadecimal(ciphertext);
        return encryptor.Decrypt(ciphertext);
    }
    
    private static void ValidateInput(string input, string paramName)
    {
        if (string.IsNullOrEmpty(input))
            throw new ArgumentException($"{paramName} cannot be null or empty", paramName);
    }
    
    private static void ValidateHexadecimal(string input)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(input, "^[0-9A-Fa-f]+$"))
            throw new ArgumentException("Ciphertext must be valid hexadecimal");
    }
}
```

##### Monitoring & Auditing
```csharp
// Audit-enabled encryption service
public class AuditedEncryptionService : IEncryptionService
{
    private readonly IEncryptionService _innerService;
    private readonly IAuditLogger _auditLogger;
    
    public string Encrypt(string plaintext)
    {
        var operationId = Guid.NewGuid();
        _auditLogger.LogOperationStart(operationId, "Encrypt", plaintext.Length);
        
        try
        {
            var result = _innerService.Encrypt(plaintext);
            _auditLogger.LogOperationSuccess(operationId, result.Length);
            return result;
        }
        catch (Exception ex)
        {
            _auditLogger.LogOperationFailure(operationId, ex.Message);
            throw;
        }
    }
    
    // Similar pattern for Decrypt
}
```

#### Key Management Strategies

##### Multi-Environment Configuration
```csharp
// Environment-specific encryption
public class EnvironmentAwareEncryption
{
    private readonly Dictionary<string, Encryptor> _encryptors;
    
    public EnvironmentAwareEncryption()
    {
        _encryptors = new Dictionary<string, Encryptor>
        {
            ["Development"] = new Encryptor(1001, 16),    // Lighter encryption for dev
            ["Testing"] = new Encryptor(2002, 24),        // Medium encryption for test
            ["Production"] = new Encryptor(3003, 32)      // Strong encryption for prod
        };
    }
    
    public string Encrypt(string plaintext)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        return _encryptors[environment].Encrypt(plaintext);
    }
}
```

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

