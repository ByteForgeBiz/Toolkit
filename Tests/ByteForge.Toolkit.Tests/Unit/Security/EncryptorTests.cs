using System;
using System.Security.Cryptography;
using ByteForge.Toolkit;
using ByteForge.Toolkit.Tests.Helpers;
using FluentAssertions;

namespace ByteForge.Toolkit.Tests.Unit.Security
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("Security")]
    public class EncryptorTests
    {
        /// <summary>
        /// Verifies that the constructor creates an Encryptor instance with valid parameters.
        /// </summary>
        /// <remarks>
        /// Ensures the Encryptor can be instantiated for encryption operations.
        /// </remarks>
        [TestMethod]
        public void Constructor_ValidParameters_ShouldCreateInstance()
        {
            // Arrange & Act
            var encryptor = new Encryptor(13, 16);

            // Assert
            encryptor.Should().NotBeNull();
        }

        /// <summary>
        /// Checks that Encryptor.Default returns the same singleton instance.
        /// </summary>
        /// <remarks>
        /// Validates singleton pattern for default encryptor, preventing inconsistent state.
        /// </remarks>
        [TestMethod]
        public void Default_ShouldReturnSameInstance()
        {
            // Arrange & Act
            var default1 = Encryptor.Default;
            var default2 = Encryptor.Default;

            // Assert
            default1.Should().BeSameAs(default2, "Default should be a singleton");
            default1.Should().NotBeNull();
        }

        /// <summary>
        /// Encrypts valid plaintext and verifies the result is not empty or equal to the input.
        /// </summary>
        /// <remarks>
        /// Ensures encryption produces non-trivial, secure output.
        /// </remarks>
        [TestMethod]
        public void Encrypt_ValidPlainText_ShouldReturnEncryptedText()
        {
            // Arrange
            var encryptor = new Encryptor(13, 16);
            var plainText = "Hello, World!";

            // Act
            var encrypted = encryptor.Encrypt(plainText);

            // Assert
            encrypted.Should().NotBeNullOrEmpty();
            encrypted.Should().NotBe(plainText, "encrypted text should be different from plaintext");
        }

        /// <summary>
        /// Decrypts valid ciphertext and verifies the original text is returned.
        /// </summary>
        /// <remarks>
        /// Confirms decryption restores original data, validating round-trip integrity.
        /// </remarks>
        [TestMethod]
        public void Decrypt_ValidCipherText_ShouldReturnOriginalText()
        {
            // Arrange
            var encryptor = new Encryptor(13, 16);
            var plainText = "Hello, World!";
            var encrypted = encryptor.Encrypt(plainText);

            // Act
            var decrypted = encryptor.Decrypt(encrypted);

            // Assert
            decrypted.Should().Be(plainText);
        }

        /// <summary>
        /// Encrypts and decrypts multiple test cases, verifying round-trip preservation.
        /// </summary>
        /// <remarks>
        /// Ensures encryption and decryption work for various input types and edge cases.
        /// </remarks>
        [TestMethod]
        public void EncryptDecrypt_RoundTrip_ShouldPreserveOriginalText()
        {
            // Arrange
            var encryptor = new Encryptor(13, 16);
            var testCases = new[]
            {
                "Simple text",
                "Text with numbers 12345",
                "Special characters: !@#$%^&*()",
                "Unicode characters: 你好世界",
                "Mixed: Hello 世界! 123",
                "",
                " ", // Single space
                "   ", // Multiple spaces
                "Line1\nLine2\rLine3\r\nLine4",
                "Tab\tSeparated\tValues"
            };

            // Act & Assert
            foreach (var testCase in testCases)
            {
                AssertionHelpers.AssertEncryptionRoundTrip(
                    testCase,
                    encryptor.Encrypt,
                    encryptor.Decrypt);
            }
        }

        /// <summary>
        /// Encrypts null input and verifies graceful handling.
        /// </summary>
        /// <remarks>
        /// Ensures null input does not cause exceptions or crashes.
        /// </remarks>
        [TestMethod]
        public void Encrypt_NullInput_ShouldHandleGracefully()
        {
            // Arrange
            var encryptor = new Encryptor(13, 16);

            // Act & Assert
            var result = encryptor.Invoking(e => e.Encrypt(null))
                .Should().NotThrow("should handle null input gracefully");
        }

        /// <summary>
        /// Decrypts null input and verifies graceful handling.
        /// </summary>
        /// <remarks>
        /// Ensures null input does not cause exceptions or crashes during decryption.
        /// </remarks>
        [TestMethod]
        public void Decrypt_NullInput_ShouldHandleGracefully()
        {
            // Arrange
            var encryptor = new Encryptor(13, 16);

            // Act & Assert
            var result = encryptor.Invoking(e => e.Decrypt(null))
                .Should().NotThrow("should handle null input gracefully");
        }

        /// <summary>
        /// Encrypts an empty string and verifies correct round-trip behavior.
        /// </summary>
        /// <remarks>
        /// Ensures empty input is handled without errors and can be decrypted.
        /// </remarks>
        [TestMethod]
        public void Encrypt_EmptyString_ShouldHandleCorrectly()
        {
            // Arrange
            var encryptor = new Encryptor(13, 16);
            var plainText = string.Empty;

            // Act
            var encrypted = encryptor.Encrypt(plainText);
            var decrypted = encryptor.Decrypt(encrypted);

            // Assert
            decrypted.Should().Be(plainText);
        }

        /// <summary>
        /// Encrypts the same input twice and verifies the output is consistent.
        /// </summary>
        /// <remarks>
        /// Ensures deterministic encryption for identical input and parameters.
        /// </remarks>
        [TestMethod]
        public void Encrypt_SameInput_ShouldProduceSameOutput()
        {
            // Arrange
            var encryptor = new Encryptor(13, 16);
            var plainText = "Consistent input";

            // Act
            var encrypted1 = encryptor.Encrypt(plainText);
            var encrypted2 = encryptor.Encrypt(plainText);

            // Assert
            encrypted1.Should().Be(encrypted2, "same input should produce same encrypted output");
        }

        /// <summary>
        /// Verifies that different constructor parameters produce different encrypted results.
        /// </summary>
        /// <remarks>
        /// Ensures encryption is sensitive to configuration, supporting multiple keys.
        /// </remarks>
        [TestMethod]
        public void Constructor_DifferentParameters_ShouldProduceDifferentResults()
        {
            // Arrange
            var encryptor1 = new Encryptor(13, 16);
            var encryptor2 = new Encryptor(17, 16);
            var plainText = "Test message";

            // Act
            var encrypted1 = encryptor1.Encrypt(plainText);
            var encrypted2 = encryptor2.Encrypt(plainText);

            // Assert
            encrypted1.Should().NotBe(encrypted2, "different encryptors should produce different results");
        }

        /// <summary>
        /// Verifies that same constructor parameters produce the same encrypted results.
        /// </summary>
        /// <remarks>
        /// Ensures encryption is deterministic for identical configuration.
        /// </remarks>
        [TestMethod]
        public void Constructor_SameParameters_ShouldProduceSameResults()
        {
            // Arrange
            var encryptor1 = new Encryptor(13, 16);
            var encryptor2 = new Encryptor(13, 16);
            var plainText = "Test message";

            // Act
            var encrypted1 = encryptor1.Encrypt(plainText);
            var encrypted2 = encryptor2.Encrypt(plainText);

            // Assert
            encrypted1.Should().Be(encrypted2, "same parameters should produce same results");
        }

        /// <summary>
        /// Attempts to decrypt with the wrong encryptor, expecting a CryptographicException.
        /// </summary>
        /// <remarks>
        /// Validates error handling for mismatched keys, ensuring security.
        /// </remarks>
        [TestMethod]
        public void Decrypt_WrongEncryptor_ShouldThrowCryptographicException()
        {
            // Arrange
            var encryptor1 = new Encryptor(13, 16);
            var encryptor2 = new Encryptor(17, 16);
            var plainText = "Secret message";
            var encrypted = encryptor1.Encrypt(plainText);

            // Act & Assert
            encryptor2.Invoking(e => e.Decrypt(encrypted))
                .Should().Throw<CryptographicException>("wrong encryptor should not decrypt correctly");
        }

        /// <summary>
        /// Verifies static Encryptor.Encrypt works correctly.
        /// </summary>
        /// <remarks>
        /// Ensures static encryption methods are functional and produce secure output.
        /// </remarks>
        [TestMethod]
        public void StaticEncrypt_ShouldWorkCorrectly()
        {
            // Arrange
            var plainText = "Static encryption test";
            var seed = 42;
            var size = 16;

            // Act
            var encrypted = Encryptor.Encrypt(seed, size, plainText);

            // Assert
            encrypted.Should().NotBeNullOrEmpty();
            encrypted.Should().NotBe(plainText);
        }

        /// <summary>
        /// Verifies static Encryptor.Decrypt works correctly.
        /// </summary>
        /// <remarks>
        /// Ensures static decryption methods restore original data.
        /// </remarks>
        [TestMethod]
        public void StaticDecrypt_ShouldWorkCorrectly()
        {
            // Arrange
            var plainText = "Static decryption test";
            var seed = 42;
            var size = 16;
            var encrypted = Encryptor.Encrypt(seed, size, plainText);

            // Act
            var decrypted = Encryptor.Decrypt(seed, size, encrypted);

            // Assert
            decrypted.Should().Be(plainText);
        }

        /// <summary>
        /// Verifies static round-trip encryption and decryption for multiple test cases.
        /// </summary>
        /// <remarks>
        /// Ensures static methods work for various input and configuration.
        /// </remarks>
        [TestMethod]
        public void StaticMethods_RoundTrip_ShouldPreserveOriginalText()
        {
            // Arrange
            var testCases = new[]
            {
                ("Hello World", 13, 16),
                ("Different seed", 42, 16),
                ("Unicode 测试", 99, 24),
                ("Empty test", 1, 8)
            };

            // Act & Assert
            foreach (var (text, seed, size) in testCases)
            {
                var encrypted = Encryptor.Encrypt(seed, size, text);
                var decrypted = Encryptor.Decrypt(seed, size, encrypted);
                
                decrypted.Should().Be(text, $"round-trip should work for seed={seed}, size={size}");
            }
        }

        /// <summary>
        /// Verifies default encryptor can encrypt and decrypt correctly.
        /// </summary>
        /// <remarks>
        /// Ensures the default instance is functional for basic operations.
        /// </remarks>
        [TestMethod]
        public void Default_EncryptDecrypt_ShouldWorkCorrectly()
        {
            // Arrange
            var plainText = "Testing default encryptor";

            // Act
            var encrypted = Encryptor.Default.Encrypt(plainText);
            var decrypted = Encryptor.Default.Decrypt(encrypted);

            // Assert
            decrypted.Should().Be(plainText);
        }

        /// <summary>
        /// Verifies encryption and decryption of long text input.
        /// </summary>
        /// <remarks>
        /// Ensures the encryptor can handle large data efficiently and correctly.
        /// </remarks>
        [TestMethod]
        public void LongText_ShouldBeHandledCorrectly()
        {
            // Arrange
            var encryptor = new Encryptor(13, 16);
            var longText = new string('A', 10000); // 10KB of 'A' characters

            // Act
            var encrypted = encryptor.Encrypt(longText);
            var decrypted = encryptor.Decrypt(encrypted);

            // Assert
            decrypted.Should().Be(longText);
            encrypted.Length.Should().BeGreaterThan(0);
        }

        /// <summary>
        /// Verifies encryption and decryption of special characters.
        /// </summary>
        /// <remarks>
        /// Ensures the encryptor supports Unicode and control characters.
        /// </remarks>
        [TestMethod]
        public void SpecialCharacters_ShouldBeHandledCorrectly()
        {
            // Arrange
            var encryptor = new Encryptor(13, 16);
            var specialText = "Special: \0\x01\x02\x03\xFF\u2603\u00A9"; // null, control chars, unicode

            // Act
            var encrypted = encryptor.Encrypt(specialText);
            var decrypted = encryptor.Decrypt(encrypted);

            // Assert
            decrypted.Should().Be(specialText);
        }

        /// <summary>
        /// Attempts to decrypt invalid cipher text, expecting a CryptographicException.
        /// </summary>
        /// <remarks>
        /// Validates error handling for malformed or corrupted input.
        /// </remarks>
        [TestMethod]
        public void InvalidCipherText_ShouldThrowCryptographicException()
        {
            // Arrange
            var encryptor = new Encryptor(13, 16);
            var invalidCipherTexts = new[]
            {
                "not_encrypted_text",
                "invalid_base64_!@#$",
                "12345",
                "ÿÿÿÿÿÿÿÿ" // Invalid bytes
            };

            // Act & Assert
            foreach (var invalidText in invalidCipherTexts)
            {
                encryptor.Invoking(e => e.Decrypt(invalidText))
                    .Should().Throw<CryptographicException>($"invalid cipher text '{invalidText}' should throw");
            }
        }

        /// <summary>
        /// Verifies constructor handles edge case parameters correctly.
        /// </summary>
        /// <remarks>
        /// Ensures robustness for unusual or boundary values.
        /// </remarks>
        [TestMethod]
        public void Constructor_EdgeCases_ShouldHandleCorrectly()
        {
            // Arrange & Act & Assert
            var testCases = new[]
            {
                (0, 8),
                (1, 16),
                (int.MaxValue, 32),
                (-1, 16),
                (13, 1)
            };

            foreach (var (seed, size) in testCases)
            {
                var creating = () => new Encryptor(seed, size);
                creating.Should().NotThrow($"should handle seed={seed}, size={size}");
                
                var encryptor = creating();
                encryptor.Should().NotBeNull();
            }
        }

        /// <summary>
        /// Performance test for multiple encryption and decryption operations.
        /// </summary>
        /// <remarks>
        /// Ensures the encryptor is efficient for repeated use.
        /// </remarks>
        [TestMethod]
        public void Performance_MultipleOperations_ShouldBeReasonablyFast()
        {
            // Arrange
            var encryptor = new Encryptor(13, 16);
            var testText = "Performance test message";
            var iterations = 1000;
            var startTime = DateTime.UtcNow;

            // Act
            for (int i = 0; i < iterations; i++)
            {
                var encrypted = encryptor.Encrypt(testText);
                var decrypted = encryptor.Decrypt(encrypted);
                
                // Verify correctness during performance test
                if (decrypted != testText)
                {
                    Assert.Fail($"Performance test failed at iteration {i}");
                }
            }

            // Assert
            var duration = DateTime.UtcNow - startTime;
            duration.Should().BeLessThan(TimeSpan.FromSeconds(5), 
                $"Should complete {iterations} encrypt/decrypt cycles in reasonable time");
        }
    }
}