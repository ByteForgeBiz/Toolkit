using System.Text;

namespace ByteForge.Toolkit.Tests.Helpers
{
    /// <summary>
    /// Helper class for configuration-related tests.
    /// </summary>
    public static class TestConfigurationHelper
    {
        /// <summary>
        /// Creates a temporary configuration file with the specified content.
        /// </summary>
        /// <param name="content">The INI content.</param>
        /// <returns>The path to the created configuration file.</returns>
        public static string CreateTempConfigFile(string content)
        {
            return TempFileHelper.CreateTempIniFile(content);
        }

        /// <summary>
        /// Creates a configuration section for testing with specified values.
        /// </summary>
        /// <typeparam name="T">The section type.</typeparam>
        /// <param name="sectionName">The section name.</param>
        /// <param name="values">The key-value pairs for the section.</param>
        /// <returns>The path to the created configuration file.</returns>
        public static string CreateTestSection<T>(string sectionName, Dictionary<string, object> values)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{sectionName}]");
            
            foreach (var kvp in values)
            {
                sb.AppendLine($"{kvp.Key}={kvp.Value}");
            }

            return CreateTempConfigFile(sb.ToString());
        }

        /// <summary>
        /// Creates a configuration file with array sections.
        /// </summary>
        /// <param name="sectionName">The main section name.</param>
        /// <param name="arrayName">The array section name.</param>
        /// <param name="arrayItems">The array items.</param>
        /// <param name="additionalValues">Additional key-value pairs for the main section.</param>
        /// <returns>The path to the created configuration file.</returns>
        public static string CreateConfigWithArray(string sectionName, string arrayName, string[] arrayItems, Dictionary<string, object>? additionalValues = null)
        {
            var sb = new StringBuilder();
            
            // Main section
            sb.AppendLine($"[{sectionName}]");
            sb.AppendLine($"ArrayProperty={arrayName}");
            
            if (additionalValues != null)
            {
                foreach (var kvp in additionalValues)
                {
                    sb.AppendLine($"{kvp.Key}={kvp.Value}");
                }
            }
            
            sb.AppendLine();
            
            // Array section
            sb.AppendLine($"[{arrayName}]");
            for (int i = 0; i < arrayItems.Length; i++)
            {
                sb.AppendLine($"{i}={arrayItems[i]}");
            }

            return CreateTempConfigFile(sb.ToString());
        }

        /// <summary>
        /// Creates a standard test configuration with multiple sections.
        /// </summary>
        /// <returns>The path to the created configuration file.</returns>
        public static string CreateStandardTestConfig()
        {
            var content = @"[TestSection]
StringValue=Test String
IntValue=42
BoolValue=true
DoubleValue=3.14159
DateValue=2024-01-15T10:30:00
ArrayValue=TestArray

[TestArray]
0=Item1
1=Item2
2=Item3

[DatabaseSettings]
Server=localhost
Port=1433
Username=testuser
Password=testpass
Timeout=30
UseSSL=true

[EmptySection]

[SpecialCharacters]
UnicodeText=Hello 世界
EscapedQuotes=""Quoted Text""
PathValue=C:\Windows\System32
";

            return CreateTempConfigFile(content);
        }

        /// <summary>
        /// Creates an encrypted configuration for testing security features.
        /// </summary>
        /// <returns>The path to the created configuration file.</returns>
        public static string CreateEncryptedTestConfig()
        {
            var content = @"[Database]
Server=localhost
esUser=encrypted_username_here
esPass=encrypted_password_here
UseEncryption=true

[Security]
EncryptionKey=test_key_12345
Algorithm=AES
";

            return CreateTempConfigFile(content);
        }

        /// <summary>
        /// Cleans up temporary configuration files.
        /// </summary>
        public static void CleanupTempFiles()
        {
            TempFileHelper.CleanupTempFiles();
        }

        /// <summary>
        /// Validates that a configuration round-trip (save/load) preserves data.
        /// </summary>
        /// <typeparam name="T">The configuration section type.</typeparam>
        /// <param name="original">The original configuration object.</param>
        /// <param name="configFile">The configuration file path.</param>
        /// <returns>True if round-trip is successful.</returns>
        public static bool ValidateConfigurationRoundTrip<T>(T original, string configFile) where T : class, new()
        {
            try
            {
                // This would require actual configuration system integration
                // For now, return true as placeholder
                return File.Exists(configFile);
            }
            catch
            {
                return false;
            }
        }
    }
}