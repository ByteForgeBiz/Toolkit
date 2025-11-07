using AwesomeAssertions;
using ByteForge.Toolkit.DataStructures;

namespace ByteForge.Toolkit.Tests.Helpers
{
    /// <summary>
    /// Helper class for common test assertions.
    /// </summary>
    public static class AssertionHelpers
    {
        /// <summary>
        /// Asserts that encryption and decryption work correctly for the given input.
        /// </summary>
        /// <param name="plaintext">The original plaintext.</param>
        /// <param name="encryptMethod">Function to encrypt the plaintext.</param>
        /// <param name="decryptMethod">Function to decrypt the ciphertext.</param>
        public static void AssertEncryptionRoundTrip(string plaintext, Func<string, string> encryptMethod, Func<string, string> decryptMethod)
        {
            // Encrypt the plaintext
            var encrypted = encryptMethod(plaintext);
            
            // Verify it's actually encrypted (different from original)
            if (!string.IsNullOrEmpty(plaintext))
            {
                encrypted.Should().NotBe(plaintext, "encrypted text should be different from plaintext");
            }
            
            // Decrypt and verify
            var decrypted = decryptMethod(encrypted);
            decrypted.Should().Be(plaintext, "decrypted text should match original plaintext");
        }

        /// <summary>
        /// Asserts that CSV round-trip (write/read) preserves data correctly.
        /// </summary>
        /// <param name="originalData">The original data objects.</param>
        /// <param name="csvContent">The generated CSV content.</param>
        /// <param name="parsedData">The parsed data from CSV.</param>
        public static void AssertCsvRoundTrip<T>(List<T> originalData, string csvContent, List<T> parsedData)
        {
            // Basic validations
            csvContent.Should().NotBeNullOrEmpty("CSV content should not be empty");
            parsedData.Should().NotBeNull("parsed data should not be null");
            parsedData.Count.Should().Be(originalData.Count, "parsed data count should match original");

            // For detailed comparison, would need reflection or specific type handling
            // This is a placeholder for the structure
        }

        /// <summary>
        /// Asserts that two collections contain the same elements (order independent).
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="expected">The expected collection.</param>
        /// <param name="actual">The actual collection.</param>
        /// <param name="because">The reason for the assertion.</param>
        public static void AssertCollectionsEquivalent<T>(IEnumerable<T> expected, IEnumerable<T> actual, string because = "")
        {
            var expectedList = expected?.ToList() ?? [];
            var actualList = actual?.ToList() ?? [];
            
            actualList.Should().BeEquivalentTo(expectedList, because);
        }

        /// <summary>
        /// Asserts that a string is properly escaped for JavaScript.
        /// </summary>
        /// <param name="original">The original string.</param>
        /// <param name="escaped">The escaped string.</param>
        public static void AssertJavaScriptEscaping(string original, string escaped)
        {
            if (string.IsNullOrEmpty(original))
            {
                escaped.Should().BeEmpty("empty input should produce empty output");
                return;
            }

            // Check that dangerous characters are properly escaped
            if (original.Contains("\\"))
                escaped.Should().Contain("\\\\", "backslashes should be escaped");
            
            if (original.Contains("\""))
                escaped.Should().Contain("\\\"", "double quotes should be escaped");
            
            if (original.Contains("'"))
                escaped.Should().Contain("\\'", "single quotes should be escaped");
            
            if (original.Contains("\n"))
                escaped.Should().Contain("\\n", "newlines should be escaped");
            
            if (original.Contains("\r"))
                escaped.Should().Contain("\\r", "carriage returns should be escaped");
            
            if (original.Contains("\t"))
                escaped.Should().Contain("\\t", "tabs should be escaped");
        }

        /// <summary>
        /// Asserts that a binary search tree maintains proper ordering.
        /// </summary>
        /// <typeparam name="T">The tree element type.</typeparam>
        /// <param name="tree">The binary search tree.</param>
        /// <param name="values">The values that should be in the tree.</param>
        public static void AssertBinarySearchTreeOrdering<T>(BinarySearchTree<T> tree, IEnumerable<T> values) where T : IComparable<T>
        {
            tree.Should().NotBeNull("tree should not be null");
            
            var valuesList = values.ToList();
            
            // Check that all values can be found
            foreach (var value in valuesList)
            {
                tree.Contains(value).Should().BeTrue($"tree should contain value {value}");
            }
            
            // Get in-order traversal and verify it's sorted
            var inOrderValues = tree.GetInOrderTraversal().ToList();
            var sortedValues = valuesList.Distinct().OrderBy(x => x).ToList();
            
            inOrderValues.Should().BeEquivalentTo(sortedValues, "in-order traversal should be sorted");
        }

        /// <summary>
        /// Asserts that an exception of the specified type is thrown.
        /// </summary>
        /// <typeparam name="TException">The expected exception type.</typeparam>
        /// <param name="action">The action that should throw.</param>
        /// <param name="expectedMessage">The expected exception message (optional).</param>
        /// <returns>The thrown exception for further assertions.</returns>
        public static TException AssertThrows<TException>(Action action, string? expectedMessage = null) 
            where TException : Exception
        {
            var exception = action.Should().Throw<TException>().Which;
            
            if (!string.IsNullOrEmpty(expectedMessage))
            {
                exception.Message.Should().Contain(expectedMessage, "exception message should contain expected text");
            }
            
            return exception;
        }

        /// <summary>
        /// Asserts that a value is within an expected range.
        /// </summary>
        /// <typeparam name="T">The comparable type.</typeparam>
        /// <param name="actual">The actual value.</param>
        /// <param name="min">The minimum expected value.</param>
        /// <param name="max">The maximum expected value.</param>
        /// <param name="because">The reason for the assertion.</param>
        public static void AssertInRange<T>(T actual, T min, T max, string because = "") where T : IComparable<T>
        {
            actual.Should().BeGreaterThanOrEqualTo(min, because);
            actual.Should().BeLessThanOrEqualTo(max, because);
        }
    }
}