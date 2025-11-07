# Security

This folder contains classes related to encryption, security, and mathematical functions compatible with VB6.

## Classes

- **AESEncryption**: A comprehensive implementation of the AES (Advanced Encryption Standard) encryption algorithm. It includes methods for encrypting and decrypting data using AES, with custom bit shifting and rotation operations ported from VB6. It handles key generation, substitution boxes, and round key expansion for secure encryption.

- **Encryptor**: A high-level wrapper around AESEncryption that provides simple encrypt and decrypt methods for strings. It generates secret keys based on a seed and size, and offers static methods for one-time encryption/decryption.

- **VBMath**: Provides mathematical functions compatible with Visual Basic 6.0, particularly random number generation. It includes Rnd() for generating random floats and Randomize() for seeding the generator, mimicking VB6 behavior for legacy compatibility.