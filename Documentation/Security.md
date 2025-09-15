# ByteForge.Toolkit Encryption Classes

This library provides robust AES (Advanced Encryption Standard) encryption functionality implemented in C# targeting .NET Framework 4.8. It includes two main classes: `AESEncryption` and `Encryptor`, which work together to provide secure data encryption and decryption capabilities.

## Features

- AES encryption and decryption of strings
- Key generation using seed values
- Support for various key sizes
- Galois Field operations for cryptographic security
- Implementations of core AES operations including SubBytes, MixColumns, and key scheduling

## Classes

### Encryptor

The `Encryptor` class provides a high-level interface for encryption operations. It encapsulates the complexity of AES encryption and offers simple methods for encrypting and decrypting strings.

#### Usage

```csharp
// Create an instance with a seed and key size
var encryptor = new Encryptor(seed: 12345, size: 32);

// Encrypt a string
string encrypted = encryptor.Encrypt("Hello, World!");

// Decrypt the string
string decrypted = encryptor.Decrypt(encrypted);

// Static methods for one-off operations
string encrypted = Encryptor.Encrypt(12345, 32, "Hello, World!");
string decrypted = Encryptor.Decrypt(12345, 32, encrypted);
```

### AESEncryption

The `AESEncryption` class implements the core AES encryption algorithm. It includes:

- Byte substitution operations
- Key scheduling and round key generation
- Galois Field operations
- Buffer encryption and decryption
- Various utility methods for bit manipulation

This class is used internally by the `Encryptor` class and typically shouldn't be used directly unless you need low-level access to the encryption operations.

## Technical Details

### Key Generation

Keys are generated using a deterministic process based on a seed value. The `GenerateKey` method creates keys of specified lengths using ASCII letters and digits:

```csharp
string key = AESEncryption.GenerateKey(seed: 12345, length: 32);
```

### Encryption Process

The encryption process follows these steps:

1. Input text is converted to bytes using Unicode encoding
2. The key is generated from the provided seed
3. Data is padded to match the AES block size (16 bytes)
4. Each block is encrypted using the AES algorithm
5. The result is converted to a hexadecimal string

### Decryption Process

Decryption reverses the encryption process:

1. Hexadecimal string is converted back to bytes
2. The same key is generated using the seed
3. Blocks are decrypted using the AES algorithm
4. Padding is removed
5. Bytes are converted back to text using Unicode encoding

## Implementation Notes

- Uses .NET Framework 4.8
- Implements standard AES operations including SubBytes, ShiftRows, MixColumns, and AddRoundKey
- Includes optimized implementations of Galois Field operations
- Provides comprehensive error checking and input validation
- Includes XML documentation for all public methods and classes

## Security Considerations

- Always use sufficiently large key sizes (32 characters recommended)
- Store seeds securely as they are crucial for decryption
- Use unique seeds for different encryption contexts
- Remember that using the same seed will generate the same key

## Dependencies

The library is self-contained and requires only:
- .NET Framework 4.8
- Microsoft.VisualBasic (for VB6-compatible random number generation)

## Example Implementation

Here's a complete example of using the encryption system:

```csharp
using ByteForge.Toolkit;

public class Program
{
    public static void Main()
    {
        // Create an encryptor with a seed and key size
        var encryptor = new Encryptor(seed: 12345, size: 32);

        // Encrypt some sensitive data
        string sensitiveData = "My secret information";
        string encrypted = encryptor.Encrypt(sensitiveData);

        // The encrypted string can be safely stored or transmitted
        Console.WriteLine($"Encrypted: {encrypted}");

        // Later, decrypt the data using the same seed and size
        string decrypted = encryptor.Decrypt(encrypted);
        Console.WriteLine($"Decrypted: {decrypted}");
    }
}
```