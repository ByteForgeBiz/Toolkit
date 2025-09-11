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
    public class AESEncryptionTests
    {
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

        [TestMethod]
        public void GenerateKey_SameSeedAndSize_ShouldGenerateSameKey()
        {
            // Arrange & Act
            var key1 = AESEncryption.GenerateKey(42, 16);
            var key2 = AESEncryption.GenerateKey(42, 16);

            // Assert
            key1.Should().Be(key2, "same seed and size should generate same key");
        }

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

        [TestMethod]
        public void Constructor_ShouldCreateInstance()
        {
            // Arrange & Act
            var aes = new AESEncryption();

            // Assert
            aes.Should().NotBeNull();
        }

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

        [TestMethod]
        public void Performance_MultipleEncryptDecryptOperations_ShouldBeReasonablyFast()
        {
            // Arrange
            var aes = new AESEncryption();
            var key = AESEncryption.GenerateKey(42, 16);
            var testText = "Performance test message for AES encryption";
            var iterations = 500;
            var startTime = DateTime.UtcNow;

            // Act
            for (int i = 0; i < iterations; i++)
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
            var duration = DateTime.UtcNow - startTime;
            duration.Should().BeLessThan(TimeSpan.FromSeconds(3), 
                $"Should complete {iterations} encrypt/decrypt cycles in reasonable time");
        }

        [TestMethod]
        public void KeyGeneration_Performance_ShouldBeReasonablyFast()
        {
            // Arrange
            var iterations = 1000;
            var startTime = DateTime.UtcNow;

            // Act
            for (int i = 0; i < iterations; i++)
            {
                var key = AESEncryption.GenerateKey(i, 16);
                key.Should().NotBeNullOrEmpty();
            }

            // Assert
            var duration = DateTime.UtcNow - startTime;
            duration.Should().BeLessThan(TimeSpan.FromSeconds(2), 
                $"Should generate {iterations} keys in reasonable time");
        }

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

        [TestMethod]
        public void KeyGeneration_ConsistencyAcrossMultipleCalls_ShouldBeDeterministic()
        {
            // Arrange
            var seed = 12345;
            var size = 16;
            var iterations = 10;

            // Act
            var keys = new string[iterations];
            for (int i = 0; i < iterations; i++)
            {
                keys[i] = AESEncryption.GenerateKey(seed, size);
            }

            // Assert
            for (int i = 1; i < iterations; i++)
            {
                keys[i].Should().Be(keys[0], $"key generation should be deterministic (iteration {i})");
            }
        }
    }
}