# Security Tests

This directory contains unit tests for the ByteForge.Toolkit Security module, which provides encryption and security-related functionality.

## Overview

The Security module offers tools for data encryption, hashing, and secure data handling. These tests ensure that security functions work correctly and provide appropriate protection for sensitive data.

## Test Classes

### AESEncryptionTests

Tests for the AESEncryption class, which implements Advanced Encryption Standard (AES) encryption:

- Encryption and decryption of strings and byte arrays
- Key management and derivation
- Initialization vector (IV) handling
- Salt generation and processing
- Error handling for invalid inputs
- Performance of encryption operations
- Compatibility across different platforms and versions
- Security aspects like key strength and entropy

### EncryptorTests

Tests for the Encryptor class, which provides a higher-level encryption interface:

- String and file encryption/decryption
- Password-based encryption
- Default encryption settings
- Custom encryption configuration
- Error handling during encryption operations
- Performance with various data sizes
- Round-trip verification of encrypted data

## Testing Strategy

The Security tests follow a comprehensive approach that covers:

1. **Correctness**: Ensuring encryption and decryption work properly
2. **Security**: Validating that encryption provides adequate protection
3. **Performance**: Measuring encryption and decryption speed
4. **Error handling**: Testing behavior with invalid inputs
5. **Compatibility**: Ensuring encrypted data can be decrypted correctly
6. **Edge cases**: Handling unusual inputs and scenarios

## Test Helpers

These tests utilize helper classes:

- **AssertionHelpers.AssertEncryptionRoundTrip**: Validates that data can be encrypted and then decrypted back to its original form
- **TempFileHelper**: Manages temporary files for testing file encryption

## Security Considerations

The security tests verify not only functional correctness but also security properties:

1. **Key strength**: Using sufficiently strong encryption keys
2. **Salt uniqueness**: Ensuring salts are properly generated and applied
3. **IV handling**: Proper initialization vector management
4. **Error messages**: Not leaking sensitive information in error cases
5. **Memory handling**: Proper clearing of sensitive data from memory

## Notes

While the tests provide good coverage of encryption functionality, they do not replace a formal security audit. The tests focus on correct implementation and behavior rather than cryptographic strength or resistance to specific attacks.

