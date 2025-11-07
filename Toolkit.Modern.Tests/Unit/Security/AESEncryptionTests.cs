using AwesomeAssertions;
using ByteForge.Toolkit.Security;
using ByteForge.Toolkit.Tests.Helpers;
using System.Diagnostics;
using System.Security.Cryptography;

namespace ByteForge.Toolkit.Tests.Unit.Security
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("Security")]
    public class AESEncryptionTests
    {
        /// <summary>
        /// Verifies that GenerateKey produces a key for valid parameters.
        /// </summary>
        /// <remarks>
        /// Ensures key generation is functional and produces unique keys for different seeds.
        /// </remarks>
        [TestMethod]
        public void GenerateKey_ValidParameters_ShouldGenerateKey()
        {
            // Arrange & Act
            var key1 = AESEncryption.GenerateKey(13, 16);
            var key2 = AESEncryption.GenerateKey(17, 16);

            // Assert
            key1.Should().NotBeNullOrEmpty();
            key2.Should().NotBeNullOrEmpty();
            key1.Should().NotBe(key2, "different seeds should generate different keys");
        }

        /// <summary>
        /// Verifies that GenerateKey produces the same key for identical seed and size.
        /// </summary>
        /// <remarks>
        /// Ensures deterministic key generation for repeatable encryption.
        /// </remarks>
        [TestMethod]
        public void GenerateKey_SameSeedAndSize_ShouldGenerateSameKey()
        {
            // Arrange & Act
            var key1 = AESEncryption.GenerateKey(42, 16);
            var key2 = AESEncryption.GenerateKey(42, 16);

            // Assert
            key1.Should().Be(key2, "same seed and size should generate same key");
        }

        /// <summary>
        /// Verifies that GenerateKey produces keys of different lengths for different sizes.
        /// </summary>
        /// <remarks>
        /// Ensures key size parameter is respected, supporting various encryption strengths.
        /// </remarks>
        [TestMethod]
        public void GenerateKey_DifferentSizes_ShouldGenerateDifferentLengthKeys()
        {
            // Arrange & Act
            var key8 = AESEncryption.GenerateKey(42, 8);
            var key16 = AESEncryption.GenerateKey(42, 16);
            var key32 = AESEncryption.GenerateKey(42, 32);

            // Assert
            key8.Length.Should().BeLessThan(key16.Length);
            key16.Length.Should().BeLessThan(key32.Length);
        }

        /// <summary>
        /// Verifies that the AESEncryption constructor creates an instance.
        /// </summary>
        /// <remarks>
        /// Ensures the AESEncryption class can be instantiated for encryption operations.
        /// </remarks>
        [TestMethod]
        public void Constructor_ShouldCreateInstance()
        {
            // Arrange & Act
            var aes = new AESEncryption();

            // Assert
            aes.Should().NotBeNull();
        }

        /// <summary>
        /// Encrypts and decrypts valid inputs, verifying correct round-trip behavior.
        /// </summary>
        /// <remarks>
        /// Ensures AES encryption and decryption work for typical use cases.
        /// </remarks>
        [TestMethod]
        public void EncryptDecrypt_ValidInputs_ShouldWorkCorrectly()
        {
            // Arrange
            var aes = new AESEncryption();
            var plaintext = "Hello, AES Encryption!";
            var key = AESEncryption.GenerateKey(42, 16);

            // Act
            var encrypted = aes.Encrypt(plaintext, key);
            var decrypted = aes.Decrypt(encrypted, key);

            // Assert
            encrypted.Should().NotBeNullOrEmpty();
            encrypted.Should().NotBe(plaintext, "encrypted text should differ from plaintext");
            decrypted.Should().Be(plaintext, "decrypted text should match original");
        }

        /// <summary>
        /// Encrypts and decrypts multiple test cases, verifying round-trip preservation.
        /// </summary>
        /// <remarks>
        /// Ensures AES encryption and decryption work for various input types and edge cases.
        /// </remarks>
        [TestMethod]
        public void EncryptDecrypt_RoundTrip_MultipleTestCases()
        {
            // Arrange
            var aes = new AESEncryption();
            var key = AESEncryption.GenerateKey(13, 16);
            var testCases = new[]
            {
                "",
                " ",
                "a",
                "Hello World",
                "1234567890",
                "Special chars: !@#$%^&*()",
                "Unicode: 你好世界 🌍",
                "Mixed: Hello 世界! Test 123",
                "Newlines:\nLine1\nLine2\n",
                "Tabs:\tTab1\tTab2\t",
                "Long text: " + new string('x', 1000),
                "JSON-like: {\"key\": \"value\", \"number\": 42}",
                "XML-like: <root><item>value</item></root>"
            };

            // Act & Assert
            foreach (var testCase in testCases)
            {
                AssertionHelpers.AssertEncryptionRoundTrip(
                    testCase,
                    text => aes.Encrypt(text, key),
                    cipher => aes.Decrypt(cipher, key));
            }
        }

        /// <summary>
        /// Encrypts the same input with the same key twice, verifying output consistency.
        /// </summary>
        /// <remarks>
        /// Ensures deterministic encryption for identical input and key.
        /// </remarks>
        [TestMethod]
        public void Encrypt_SameInputSameKey_ShouldProduceSameOutput()
        {
            // Arrange
            var aes = new AESEncryption();
            var plaintext = "Consistent test";
            var key = AESEncryption.GenerateKey(42, 16);

            // Act
            var encrypted1 = aes.Encrypt(plaintext, key);
            var encrypted2 = aes.Encrypt(plaintext, key);

            // Assert
            encrypted1.Should().Be(encrypted2, "same input and key should produce same output");
        }

        /// <summary>
        /// Encrypts the same input with different keys, verifying output difference.
        /// </summary>
        /// <remarks>
        /// Ensures encryption is sensitive to key changes, supporting multiple keys.
        /// </remarks>
        [TestMethod]
        public void Encrypt_DifferentKeys_ShouldProduceDifferentOutputs()
        {
            // Arrange
            var aes = new AESEncryption();
            var plaintext = "Test message";
            var key1 = AESEncryption.GenerateKey(42, 16);
            var key2 = AESEncryption.GenerateKey(43, 16);

            // Act
            var encrypted1 = aes.Encrypt(plaintext, key1);
            var encrypted2 = aes.Encrypt(plaintext, key2);

            // Assert
            encrypted1.Should().NotBe(encrypted2, "different keys should produce different outputs");
        }

        /// <summary>
        /// Attempts to decrypt with the wrong key, expecting a CryptographicException.
        /// </summary>
        /// <remarks>
        /// Validates error handling for mismatched keys, ensuring security.
        /// </remarks>
        [TestMethod]
        public void Decrypt_WrongKey_ShouldThrowCryptographicException()
        {
            // Arrange
            var aes = new AESEncryption();
            var plaintext = "Secret message";
            var correctKey = AESEncryption.GenerateKey(42, 16);
            var wrongKey = AESEncryption.GenerateKey(43, 16);
            var encrypted = aes.Encrypt(plaintext, correctKey);

            // Act & Assert
            aes.Invoking(a => a.Decrypt(encrypted, wrongKey))
                .Should().Throw<CryptographicException>("decrypting with wrong key should fail");
        }

        /// <summary>
        /// Encrypts null plaintext and verifies graceful handling.
        /// </summary>
        /// <remarks>
        /// Ensures null input does not cause exceptions or crashes.
        /// </remarks>
        [TestMethod]
        public void Encrypt_NullPlaintext_ShouldHandleGracefully()
        {
            // Arrange
            var aes = new AESEncryption();
            var key = AESEncryption.GenerateKey(42, 16);

            // Act & Assert
            aes.Invoking(a => a.Encrypt(null, key))
                .Should().NotThrow("should handle null plaintext gracefully");
        }

        /// <summary>
        /// Attempts to encrypt with a null key, expecting an ArgumentNullException.
        /// </summary>
        /// <remarks>
        /// Verifies error handling for missing key input.
        /// </remarks>
        [TestMethod]
        public void Encrypt_NullKey_ShouldThrowArgumentNullException()
        {
            // Arrange
            var aes = new AESEncryption();
            var plaintext = "Test";

            // Act & Assert
            aes.Invoking(a => a.Encrypt(plaintext, null))
                .Should().Throw<ArgumentNullException>("key should not be null");
        }

        /// <summary>
        /// Decrypts null ciphertext and verifies graceful handling.
        /// </summary>
        /// <remarks>
        /// Ensures null input does not cause exceptions or crashes during decryption.
        /// </remarks>
        [TestMethod]
        public void Decrypt_NullCiphertext_ShouldHandleGracefully()
        {
            // Arrange
            var aes = new AESEncryption();
            var key = AESEncryption.GenerateKey(42, 16);

            // Act & Assert
            aes.Invoking(a => a.Decrypt(null, key))
                .Should().NotThrow("should handle null ciphertext gracefully");
        }

        /// <summary>
        /// Attempts to decrypt invalid ciphertext, expecting a CryptographicException.
        /// </summary>
        /// <remarks>
        /// Validates error handling for malformed or corrupted input.
        /// </remarks>
        [TestMethod]
        public void Decrypt_InvalidCiphertext_ShouldThrowCryptographicException()
        {
            // Arrange
            var aes = new AESEncryption();
            var key = AESEncryption.GenerateKey(42, 16);
            var invalidCiphertexts = new[]
            {
                "not_encrypted",
                "invalid_base64_!@#",
                "123456789",
                "ÿÿÿÿÿÿÿÿ"
            };

            // Act & Assert
            foreach (var invalidCiphertext in invalidCiphertexts)
            {
                aes.Invoking(a => a.Decrypt(invalidCiphertext, key))
                    .Should().Throw<CryptographicException>("invalid ciphertext should not decrypt correctly");
            }
        }

        /// <summary>
        /// Verifies GenerateKey handles edge case parameters correctly.
        /// </summary>
        /// <remarks>
        /// Ensures robustness for unusual or boundary values.
        /// </remarks>
        [TestMethod]
        public void GenerateKey_EdgeCases_ShouldHandleCorrectly()
        {
            // Arrange & Act & Assert
            var testCases = new[]
            {
                (0, 1),
                (1, 8),
                (int.MaxValue, 16),
                (int.MinValue, 32),
                (-1, 16),
                (42, 0)
            };

            foreach (var (seed, size) in testCases)
            {
                var generating = () => AESEncryption.GenerateKey(seed, size);
                generating.Should().NotThrow($"should handle seed={seed}, size={size}");
                
                var key = generating();
                key.Should().NotBeNull($"key should not be null for seed={seed}, size={size}");
            }
        }

        /// <summary>
        /// Encrypts and decrypts an empty string, verifying correct round-trip behavior.
        /// </summary>
        /// <remarks>
        /// Ensures empty input is handled without errors and can be decrypted.
        /// </remarks>
        [TestMethod]
        public void EncryptDecrypt_EmptyString_ShouldWorkCorrectly()
        {
            // Arrange
            var aes = new AESEncryption();
            var key = AESEncryption.GenerateKey(42, 16);
            var emptyString = string.Empty;

            // Act
            var encrypted = aes.Encrypt(emptyString, key);
            var decrypted = aes.Decrypt(encrypted, key);

            // Assert
            decrypted.Should().Be(emptyString);
        }

        /// <summary>
        /// Encrypts and decrypts whitespace strings, verifying preservation of whitespace.
        /// </summary>
        /// <remarks>
        /// Ensures whitespace is not lost or altered during encryption and decryption.
        /// </remarks>
        [TestMethod]
        public void EncryptDecrypt_WhitespaceString_ShouldPreserveWhitespace()
        {
            // Arrange
            var aes = new AESEncryption();
            var key = AESEncryption.GenerateKey(42, 16);
            var whitespaceStrings = new[]
            {
                " ",
                "  ",
                "\t",
                "\n",
                "\r\n",
                "   \t\n  "
            };

            // Act & Assert
            foreach (var whitespace in whitespaceStrings)
            {
                var encrypted = aes.Encrypt(whitespace, key);
                var decrypted = aes.Decrypt(encrypted, key);
                
                decrypted.Should().Be(whitespace, $"whitespace should be preserved: '{whitespace}'");
            }
        }

        /// <summary>
        /// Encrypts and decrypts Unicode content, verifying correct handling.
        /// </summary>
        /// <remarks>
        /// Ensures Unicode characters are supported, preventing data loss.
        /// </remarks>
        [TestMethod]
        public void EncryptDecrypt_UnicodeContent_ShouldHandleCorrectly()
        {
            // Arrange
            var aes = new AESEncryption();
            var key = AESEncryption.GenerateKey(42, 16);
            var unicodeTexts = new[]
            {
                "英语",
                "العربية",
                "русский",
                "日本語",
                "한국어",
                "Français",
                "Español",
                "🌍🚀💻🎉",
                "Mixed: Hello 世界 مرحبا 🌟"
            };

            // Act & Assert
            foreach (var unicodeText in unicodeTexts)
            {
                var encrypted = aes.Encrypt(unicodeText, key);
                var decrypted = aes.Decrypt(encrypted, key);
                
                decrypted.Should().Be(unicodeText, $"unicode should be preserved: {unicodeText}");
            }
        }

        /// <summary>
        /// Performance test for multiple encrypt/decrypt operations.
        /// </summary>
        /// <remarks>
        /// Ensures AES encryption is efficient for repeated use.
        /// </remarks>
        [TestMethod]
        public void Performance_MultipleEncryptDecryptOperations_ShouldBeReasonablyFast()
        {
            // Arrange
            var aes = new AESEncryption();
            var key = AESEncryption.GenerateKey(42, 16);
            var testText = "Performance test message for AES encryption";
            var iterations = 5000;
            var stopWatch = Stopwatch.StartNew();

            // Act
            for (var i = 0; i < iterations; i++)
            {
                var encrypted = aes.Encrypt(testText, key);
                var decrypted = aes.Decrypt(encrypted, key);
                
                // Verify correctness during performance test
                if (decrypted != testText)
                {
                    Assert.Fail($"Performance test failed at iteration {i}");
                }
            }

            // Assert
            stopWatch.Stop();
            var duration = stopWatch.Elapsed;
            duration.Should().BeLessThan(TimeSpan.FromSeconds(2), 
                $"Should complete {iterations} encrypt/decrypt cycles in reasonable time");
        }

        /// <summary>
        /// Verifies that the encryption implementation supports decrypting and re-encrypting 
        /// legacy encrypted values, ensuring compatibility with previously encrypted data.
        /// </summary>
        /// <remarks>
        /// This test ensures that the <see cref="AESEncryption"/> class can correctly decrypt a
        /// legacy encrypted string and re-encrypt it to produce the same encrypted value.<br/>
        /// Compatibility with legacy data is critical for maintaining seamless operation when 
        /// transitioning to updated encryption workflows.
        /// </remarks>
        [TestMethod]
        public void EncryptDecrypt_ShouldSupportLegacy()
        {
            var aes = new AESEncryption();
            var key = AESEncryption.GenerateKey(13, 16);

            var encryptedLegacy = "7A77D979A90B997E67338B66C1532B63A21DBBD911D99BD5A5013F11C50C239C";
            var plaintext = "api_user";

            var decrypted = aes.Decrypt(encryptedLegacy, key);
            decrypted.Should().Be(plaintext, "decrypted legacy value should match expected plaintext");

            var reEncrypted = aes.Encrypt(decrypted, key);
            reEncrypted.Should().Be(encryptedLegacy, "re-encryption should match legacy encrypted value");
        }

        /// <summary>
        /// Performance test for key generation.
        /// </summary>
        /// <remarks>
        /// Ensures key generation is efficient for repeated use.
        /// </remarks>
        [TestMethod]
        public void KeyGeneration_Performance_ShouldBeReasonablyFast()
        {
            // Arrange
            var iterations = 5000;
            var stopWatch = Stopwatch.StartNew();

            // Act
            for (var i = 0; i < iterations; i++)
            {
                var key = AESEncryption.GenerateKey(i, 16);
                key.Should().NotBeNullOrEmpty();
            }

            // Assert
            stopWatch.Stop();
            var duration = stopWatch.Elapsed;
            duration.Should().BeLessThan(TimeSpan.FromMilliseconds(50), 
                $"Should generate {iterations} keys in reasonable time");
        }

        /// <summary>
        /// Encrypts and decrypts large data, verifying correct handling.
        /// </summary>
        /// <remarks>
        /// Ensures AES encryption can handle large data efficiently and correctly.
        /// </remarks>
        [TestMethod]
        public void EncryptDecrypt_LargeData_ShouldHandleCorrectly()
        {
            // Arrange
            var aes = new AESEncryption();
            var key = AESEncryption.GenerateKey(42, 16);
            var largeText = new string('A', 10000); // 10KB of data

            // Act
            var encrypted = aes.Encrypt(largeText, key);
            var decrypted = aes.Decrypt(encrypted, key);

            // Assert
            decrypted.Should().Be(largeText);
            encrypted.Should().NotBeNullOrEmpty();
            encrypted.Length.Should().BeGreaterThan(0);
        }

        /// <summary>
        /// Verifies key generation consistency across multiple calls.
        /// </summary>
        /// <remarks>
        /// Ensures deterministic key generation for repeatable encryption.
        /// </remarks>
        [TestMethod]
        public void KeyGeneration_ConsistencyAcrossMultipleCalls_ShouldBeDeterministic()
        {
            // Arrange
            var seed = 12345;
            var size = 16;
            var iterations = 10;

            // Act
            var keys = new string[iterations];
            for (var i = 0; i < iterations; i++)
            {
                keys[i] = AESEncryption.GenerateKey(seed, size);
            }

            // Assert
            for (var i = 1; i < iterations; i++)
            {
                keys[i].Should().Be(keys[0], $"key generation should be deterministic (iteration {i})");
            }
        }
    }
}