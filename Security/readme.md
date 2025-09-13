# Security.md

## ByteForge.Toolkit Encryption System

Secure AES-based string encryption with deterministic key generation. Built for .NET Framework 4.8.

### 🚀 Features
- **AES encryption**: Custom implementation with Galois Field mathematics
- **Deterministic key generation**: Seed-based key derivation for reproducible encryption
- **Hexadecimal encoding**: Safe string representation of encrypted binary data
- **High-level API**: Simple encryption/decryption via `Encryptor` class
- **Low-level access**: Direct AES operations through `AESEncryption` for advanced use cases
- **Default instance**: Pre-configured `Encryptor.Default` for quick usage
- **Thread-safe**: Safe for concurrent use across multiple threads

### 🧱 Components
- `Encryptor`: High-level API for string ops
- `AESEncryption`: Low-level AES routines (SubBytes, MixColumns, ShiftRows, etc)

### 🧪 Basic Examples

#### Instance-based Encryption
```csharp
// Create encryptor with custom seed and key size
var encryptor = new Encryptor(seed: 12345, size: 32);
string encrypted = encryptor.Encrypt("Secret message");
string decrypted = encryptor.Decrypt(encrypted);
// decrypted == "Secret message"
```

#### Default Instance (Quick Start)
```csharp
// Use pre-configured default instance
string encrypted = Encryptor.Default.Encrypt("Quick encryption");
string decrypted = Encryptor.Default.Decrypt(encrypted);
```

#### Static Methods (One-off Usage)
```csharp
// Static methods for one-time encryption
string encrypted = Encryptor.Encrypt(12345, 32, "Data to encrypt");
string decrypted = Encryptor.Decrypt(12345, 32, encrypted);
```

#### Configuration Storage Example
```csharp
// Encrypt sensitive configuration data
var encryptor = new Encryptor(Environment.MachineName.GetHashCode(), 32);
string encryptedPassword = encryptor.Encrypt("MySecretPassword");

// Store encryptedPassword in config file
// Later, decrypt when needed
string password = encryptor.Decrypt(encryptedPassword);
```

### 🔒 Security Considerations

#### Key Management
- **Store seeds securely**: Never hardcode seeds in source code or store them in plain text
- **Use adequate key sizes**: 32+ characters recommended for strong security
- **Avoid seed reuse**: Don't reuse the same seed across different domains or applications
- **Environment-based seeds**: Consider using machine-specific or environment-specific seed generation

#### Implementation Security
- **Deterministic encryption**: Same plaintext + seed will always produce the same ciphertext
- **Hexadecimal output**: Encrypted strings are safe for storage in text-based formats
- **No salt by design**: This implementation prioritizes deterministic results over unique ciphertext per encryption

#### Production Considerations
- **Audit encryption usage**: Log encryption operations (but never the keys or plaintext)
- **Rotate keys periodically**: Implement key rotation strategies for long-term data
- **Secure key derivation**: Consider using more sophisticated key derivation functions for high-security scenarios

### ✅ Best Practices

#### Usage Patterns
- Use `Encryptor` class for all application encryption needs
- Prefer instance-based usage over static methods for better testability
- Use `Encryptor.Default` for quick prototyping, custom instances for production
- Consider wrapping encryption operations in a service layer

#### Security Practices
- **Never log keys or seeds**: Exclude sensitive values from logging and debugging output
- **Validate inputs**: Check for null/empty strings before encryption
- **Handle exceptions**: Implement proper error handling for encryption/decryption failures
- **Use configuration**: Store encrypted values in configuration files, decrypt at runtime

#### Testing and Development
- **Mock encryption**: Create testable interfaces for encryption services
- **Test round-trips**: Verify that `Decrypt(Encrypt(text))` always equals the original text
- **Performance testing**: Measure encryption overhead for your specific use cases

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
