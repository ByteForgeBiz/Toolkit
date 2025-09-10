# Security.md

## ByteForge.Toolkit Encryption System

Secure AES-based string encryption with deterministic key generation. Built for .NET Framework 4.8.

### 🚀 Features
- AES encryption with Galois Field math
- Seed-based deterministic keygen
- Hexadecimal encoding for output
- Encryption/Decryption via `Encryptor`
- Custom AES implementation with `AESEncryption`

### 🧱 Components
- `Encryptor`: High-level API for string ops
- `AESEncryption`: Low-level AES routines (SubBytes, MixColumns, ShiftRows, etc)

### 🧪 Basic Example
```csharp
var encryptor = new Encryptor(seed: 12345, size: 32);
string encrypted = encryptor.Encrypt("Secret");
string decrypted = encryptor.Decrypt(encrypted);
```

#### One-off static use
```csharp
string result = Encryptor.Encrypt(12345, 32, "Data");
```

### 🔒 Security Considerations
- Always store seeds securely
- Use long keys (32+ chars recommended)
- Avoid reusing seeds across domains

### ✅ Best Practices
- Use `Encryptor` for all app encryption needs
- Do not expose or log keys/seeds
- Consider wrapping encryption in service layer for testability

### 🔗 Related Modules
- [Database](../Data/Database/readme.md)
- [Configuration](../Configuration/readme.md)
