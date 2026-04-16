# Security Unit Tests

Tests for `ByteForge.Toolkit.Security`.

**Test classes:** `AESEncryptionTests`, `EncryptorTests`
**Test categories:** `Unit`, `Security`
**Source module:** `Toolkit.Modern/Security/`

## Test Classes

### AESEncryptionTests

Validates the static `AESEncryption` class, which provides low-level AES key generation and symmetric encryption.

| Test area | Coverage |
|-----------|---------|
| `GenerateKey(int seed, int size)` | Different seeds produce different keys; same seed and size produce the same key (deterministic) |
| `GenerateKey` — invalid size | Throws for sizes outside supported values (e.g. 0, negative) |
| `Encrypt(string plaintext)` | Returns a non-null, non-empty string; result differs from plaintext |
| `Decrypt(string ciphertext)` | Round-trip: `Decrypt(Encrypt(x)) == x` |
| Empty string | Encryption and decryption of `""` |
| Null input | `Encrypt(null)` and `Decrypt(null)` — expected behavior (throw or return null) |
| Unicode and special characters | Multi-byte strings survive the round-trip unchanged |
| Large inputs | Strings of several thousand characters encrypt and decrypt correctly |
| Key uniqueness | Two `GenerateKey` calls with different seeds produce different byte sequences |
| Performance | Encryption and decryption complete within a measured time threshold |

`AssertionHelpers.AssertEncryptionRoundTrip` is used in round-trip tests.

### EncryptorTests

Validates the `Encryptor` class, which provides a higher-level encryption interface built on top of `AESEncryption`.

| Test area | Coverage |
|-----------|---------|
| Constructor `Encryptor(int seed, int size)` | Creates a non-null instance |
| `Encryptor.Default` | Returns the same singleton on repeated access |
| `Encrypt(string plaintext)` | Result is non-null, non-empty, and differs from plaintext |
| `Decrypt(string ciphertext)` | Round-trip: `Decrypt(Encrypt(x)) == x` |
| Empty/null inputs | Consistent behavior for edge-case inputs |
| `Encryptor.Default` round-trip | Default instance can encrypt and decrypt |
| Key isolation | Two `Encryptor` instances with different seeds produce different ciphertexts for the same plaintext |
| Performance | Large string encryption completes within a measured threshold |

`TempFileHelper` is used where file encryption/decryption is tested.

## Security Properties Verified

- **Deterministic key generation:** same seed + size = same key (required for stored-credential decryption)
- **Ciphertext distinctness:** encrypted output differs from plaintext
- **Round-trip integrity:** plaintext is recovered exactly after decrypt
- **Singleton stability:** `Encryptor.Default` is stateless and thread-safe (same instance always returned)

## Prerequisites

No external dependencies. All tests are fully in-memory.

## Running These Tests

```powershell
# All Security tests
dotnet test --filter "TestCategory=Security"

# By class
dotnet test --filter "FullyQualifiedName~AESEncryptionTests"
dotnet test --filter "FullyQualifiedName~EncryptorTests"
```

---

## Documentation Links

| Location | Description | Documentation |
|----------|-------------|---------------|
| **Tests root** | Test project overview | [../../README.md](../../README.md) |
| **Unit overview** | Unit test organization | [../readme.md](../readme.md) |
| **Helpers** | Test helper classes | [../../Helpers/README.md](../../Helpers/README.md) |
| **Security source** | Production module | [../../../Toolkit.Modern/Security/readme.md](../../../Toolkit.Modern/Security/readme.md) |
