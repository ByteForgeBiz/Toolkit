using ByteForge.Toolkit;

namespace ByteForge.Toolkit.Tests.Helpers
{
    /// <summary>
    /// Helper class for creating isolated Configuration instances in tests.
    /// </summary>
    public static class ConfigurationTestHelper
    {
        /// <summary>
        /// Creates an isolated Configuration instance for testing with the specified INI content.
        /// </summary>
        /// <param name="iniContent">The INI file content.</param>
        /// <returns>An initialized Configuration instance.</returns>
        public static IConfigurationManager CreateTestConfiguration(string iniContent)
        {
            var config = new Configuration();
            var tempPath = TestConfigurationHelper.CreateTempConfigFile(iniContent);
            ((IConfigurationManager)config).Initialize(tempPath);
            return config;
        }

        /// <summary>
        /// Creates an uninitialized Configuration instance for testing.
        /// </summary>
        /// <returns>An uninitialized Configuration instance.</returns>
        public static IConfigurationManager CreateConfiguration()
        {
            return new Configuration();
        }

        /// <summary>
        /// Creates a test configuration with standard test content.
        /// </summary>
        /// <returns>An initialized Configuration instance with standard test data.</returns>
        public static IConfigurationManager CreateStandardTestConfiguration()
        {
            var content = @"[TestSection]
StringValue=Test String
IntValue=42
BoolValue=true
DoubleValue=3.14159
DateValue=2024-01-15
TimeoutValue=45
CustomName=Mapped Value

[DatabaseSettings]
Server=localhost
Port=1433
Username=testuser
Password=testpass
Timeout=30
UseSSL=true

[GlobalizationSettings]
Culture=en-US
DateFormat=MM/dd/yyyy
NumberFormat=#,##0.00
CurrencySymbol=$";

            return CreateTestConfiguration(content);
        }
    }
}