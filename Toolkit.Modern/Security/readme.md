# Security Module

The Security module provides AES encryption and decryption with VB6-compatible semantics. It is used primarily to encrypt credentials stored in configuration files (passwords, connection strings) and to decrypt them at runtime.

---

## Key Classes

| Class | Description |
|-------|-------------|
| `Encryptor` | Public wrapper — encrypt/decrypt strings using a seed-generated key |
| `AESEncryption` | Internal AES implementation ported from VB6 |
| `VBMath` | Internal VB6-compatible random number generator |

---

## `Encryptor`

The public API for encryption. Generates an AES key from a numeric seed and key size, then encrypts and decrypts strings.

### Constructors

```csharp
// Generate a key from seed and size
Encryptor(int seed, int size)
```

### Instance methods

```csharp
string? Encrypt(string? plainText)
string? Decrypt(string? cipherText)
```

Returns `null` if the input is `null`.

### Static methods

```csharp
// One-shot encrypt
static string? Encrypt(int seed, int size, string? text)

// One-shot decrypt
static string? Decrypt(int seed, int size, string? text)
```

### Default instance

```csharp
Encryptor.Default  // seed = 13, size = 16
```

`Encryptor.Default` is the instance used by `MailUtil` and other modules to decrypt credentials stored in INI configuration files (config keys prefixed with `es`, e.g., `esUser`, `esPass`).

---

## Usage

### Encrypt a credential for storage

```csharp
var encryptor = new Encryptor(seed: 13, size: 16);
string encrypted = encryptor.Encrypt("MySecretPassword");
// Store 'encrypted' in the INI file under an 'es'-prefixed key
```

### Decrypt at runtime

```csharp
var encryptor = new Encryptor(seed: 13, size: 16);
string plainText = encryptor.Decrypt(configValue);
```

### Using the default instance

```csharp
string plain = Encryptor.Default.Decrypt(encryptedValue);
```

### One-shot static usage

```csharp
string cipher  = Encryptor.Encrypt(seed: 42, size: 16, text: "hello");
string decoded = Encryptor.Decrypt(seed: 42, size: 16, text: cipher);
```

---

## `AESEncryption` (internal)

Implements AES encryption compatible with a legacy VB6 implementation. Not intended for direct use.

**Key characteristics:**
- Uses VB6-compatible 32-bit left/right bit-shift semantics (`LShift`/`RShift`) rather than C#'s native `<<`/`>>` operators
- GF(2⁸) Galois Field arithmetic via log/anti-log lookup tables
- Standard AES S-box generation, round key expansion, and `EncryptBytes`/`DecryptBytes` operations
- Input/output block size: 32 bytes
- Public surface: `Encrypt(plainText, key)` → hex string; `Decrypt(cipherText, key)` from hex

**Key generation:**
```csharp
string key = AESEncryption.GenerateKey(seed, length);
```
Uses `VBMath.Rnd(-1)` and `VBMath.Randomize(seed)` to seed the generator identically to VB6, then generates `length` pseudo-random characters.

---

## `VBMath` (internal)

A port of `Microsoft.VisualBasic.Core.VBMath` providing VB6-compatible pseudo-random number generation.

| Method | Description |
|--------|-------------|
| `Rnd()` | Returns next float in [0, 1) using the current seed |
| `Rnd(float)` | Negative → reseed; zero → return current; positive → advance |
| `Randomize()` | Seeds using system timer |
| `Randomize(double)` | Seeds with the supplied value (XORs with current seed, endian-aware) |

This is used exclusively by `AESEncryption.GenerateKey()` to reproduce the same key-generation sequence as the original VB6 code, ensuring backward compatibility with encrypted data produced by the legacy system.

---

## Configuration Integration

Credentials in INI files are stored encrypted under keys prefixed with `es`:

```ini
[Mail Server]
esUser=[encrypted_username]
esPass=[encrypted_password]

[Production]
esUser=[encrypted_connection_user]
esPass=[encrypted_connection_pass]
```

The Configuration module recognizes the `es` prefix and automatically decrypts values using `Encryptor.Default` when the section is deserialized into a model class that has matching `esXxx`-mapped properties.

---

## Design Notes

- The VB6-compatible implementation exists to maintain backward compatibility with encrypted data written by legacy systems. Do not use this module for new security-sensitive scenarios requiring modern key management or authenticated encryption.
- `Encryptor.Default` (seed=13, size=16) is a shared default. For stronger isolation, create separate `Encryptor` instances with different seeds and sizes for different credential domains.

---

## Related Modules

| Module | Description |
|--------|-------------|
| [Configuration](../Configuration/readme.md) | Reads encrypted `esXxx` values from INI files |
| [Mail](../Mail/readme.md) | Uses `Encryptor.Default` to decrypt SMTP credentials |
| [Data](../Data/readme.md) | Uses `Encryptor.Default` to decrypt database credentials |
