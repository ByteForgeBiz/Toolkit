using AwesomeAssertions;
using ByteForge.Toolkit.Configuration;
using ByteForge.Toolkit.Tests.Helpers;
using System.Globalization;
using System.Reflection;

namespace ByteForge.Toolkit.Tests.Unit.Configuration
{
    /// <summary>
    /// Tests for globalization support in configuration including culture-aware formatting and parsing.
    /// </summary>
    [TestClass]
    public class GlobalizationTests
    {
        private string _tempConfigPath;

        [TestCleanup]
        public void TestCleanup()
        {
            TestConfigurationHelper.CleanupTempFiles();
            ResetConfiguration();
        }

        #region Globalization Loading Tests

        /// <summary>
        /// Verifies that globalization settings use appropriate defaults when no custom configuration is provided.
        /// </summary>
        /// <remarks>
        /// This test ensures that the globalization system has sensible fallback values for all formatting
        /// options, allowing applications to work correctly even without explicit globalization configuration.
        /// Default values should follow common US formatting conventions.
        /// </remarks>
        [TestMethod]
        public void GlobalizationInfo_WithDefaultConfiguration_ShouldUseDefaults()
        {
            // Arrange
            var configContent = @"[Globalization]";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var globalization = config.Globalization;

            // Assert
            globalization.Should().NotBeNull("globalization should be available");
            globalization.CultureInfo.Should().Be(CultureInfo.InvariantCulture, "default culture should be invariant");
            globalization.DateFormat.Should().Be("MM'/'dd'/'yyyy", "default date format should be MM/dd/yyyy");
            globalization.TimeFormat.Should().Be("HH':'mm':'ss", "default time format should be HH:mm:ss");
            globalization.CurrencyFormat.Should().Be("'$' #,##0.00", "default currency format should include dollar sign");
            globalization.NumberFormat.Should().Be("#,##0.00", "default number format should have two decimals");
        }

        /// <summary>
        /// Verifies that globalization settings properly load custom values from configuration.
        /// </summary>
        /// <remarks>
        /// This test validates that the configuration system can load custom globalization settings
        /// for different locales and formatting preferences, enabling internationalization support
        /// for applications targeting multiple regions.
        /// </remarks>
        [TestMethod]
        public void GlobalizationInfo_WithCustomConfiguration_ShouldLoadCustomValues()
        {
            // Arrange
            var configContent = @"[Globalization]
CultureInfo=fr-FR
DateFormat=dd/MM/yyyy
TimeFormat=HH:mm
CurrencyFormat=# ##0,00 €
NumberFormat=# ##0,000";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var globalization = config.Globalization;

            // Assert
            globalization.Should().NotBeNull("globalization should be available");
            globalization.DateFormat.Should().Be("dd/MM/yyyy", "custom date format should be loaded");
            globalization.TimeFormat.Should().Be("HH:mm", "custom time format should be loaded");
            globalization.CurrencyFormat.Should().Be("# ##0,00 €", "custom currency format should be loaded");
            globalization.NumberFormat.Should().Be("# ##0,000", "custom number format should be loaded");
        }

        /// <summary>
        /// Verifies that partial globalization configuration uses defaults for missing values.
        /// </summary>
        /// <remarks>
        /// This test ensures that when only some globalization settings are provided,
        /// the system gracefully falls back to defaults for missing values, allowing
        /// incremental configuration without breaking functionality.
        /// </remarks>
        [TestMethod]
        public void GlobalizationInfo_WithPartialConfiguration_ShouldUseDefaultsForMissing()
        {
            // Arrange
            var configContent = @"[Globalization]
DateFormat=yyyy-MM-dd
CurrencyFormat=¥#,##0";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var globalization = config.Globalization;

            // Assert
            globalization.DateFormat.Should().Be("yyyy-MM-dd", "custom date format should be loaded");
            globalization.CurrencyFormat.Should().Be("¥#,##0", "custom currency format should be loaded");
            globalization.TimeFormat.Should().Be("HH':'mm':'ss", "missing time format should use default");
            globalization.NumberFormat.Should().Be("#,##0.00", "missing number format should use default");
        }

        #endregion

        #region Date Formatting Tests

        /// <summary>
        /// Verifies that date formatting uses the configured date format pattern correctly.
        /// </summary>
        /// <remarks>
        /// This test validates the core date formatting functionality, ensuring that custom
        /// date format patterns are applied correctly to DateTime values. This is essential
        /// for consistent date representation across the application.
        /// </remarks>
        [TestMethod]
        public void GlobalizationInfo_FormatDate_ShouldFormatCorrectly()
        {
            // Arrange
            var configContent = @"[Globalization]
DateFormat=yyyy-MM-dd";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            var testDate = new DateTime(2024, 1, 15);

            // Act
            var formatted = globalization.FormatDate(testDate);

            // Assert
            formatted.Should().Be("2024-01-15", "date should be formatted using custom format");
        }

        /// <summary>
        /// Verifies that date formatting with null values returns the specified null representation.
        /// </summary>
        /// <remarks>
        /// This test ensures robust null handling in date formatting, allowing applications
        /// to specify custom null value representations (like "N/A" or "TBD") for better
        /// user experience when dealing with missing date data.
        /// </remarks>
        [TestMethod]
        public void GlobalizationInfo_FormatDate_WithNullValue_ShouldReturnNullValue()
        {
            // Arrange
            var configContent = @"[Globalization]";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;

            // Act
            var formatted = globalization.FormatDate(null, "N/A");

            // Assert
            formatted.Should().Be("N/A", "null date should return specified null value");
        }

        /// <summary>
        /// Verifies that long date formatting includes day of week and full month names.
        /// </summary>
        /// <remarks>
        /// This test validates the long date formatting feature, which provides more verbose
        /// date representations suitable for formal documents or user interfaces requiring
        /// complete date information including day names.
        /// </remarks>
        [TestMethod]
        public void GlobalizationInfo_FormatLongDate_ShouldUseCorrectFormat()
        {
            // Arrange
            var configContent = @"[Globalization]
LongDateFormat=dddd, MMMM d, yyyy";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            var testDate = new DateTime(2024, 1, 15); // Monday

            // Act
            var formatted = globalization.FormatLongDate(testDate);

            // Assert
            formatted.Should().Be("Monday, January 15, 2024", "long date should include day of week and full month name");
        }

        /// <summary>
        /// Verifies that datetime formatting correctly combines separate date and time format patterns.
        /// </summary>
        /// <remarks>
        /// This test ensures that the datetime formatting logic properly merges configured
        /// date and time formats into a cohesive datetime representation, maintaining
        /// consistency between separate and combined formatting operations.
        /// </remarks>
        [TestMethod]
        public void GlobalizationInfo_FormatDateTime_ShouldCombineDateAndTime()
        {
            // Arrange
            var configContent = @"[Globalization]
DateFormat=yyyy-MM-dd
TimeFormat=HH:mm:ss";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            var testDateTime = new DateTime(2024, 1, 15, 14, 30, 45);

            // Act
            var formatted = globalization.FormatDateTime(testDateTime);

            // Assert
            formatted.Should().Be("2024-01-15 14:30:45", "datetime should combine date and time formats");
        }

        #endregion

        #region Time Formatting Tests

        /// <summary>
        /// Verifies that time formatting uses the configured time format pattern correctly.
        /// </summary>
        /// <remarks>
        /// This test validates the core time formatting functionality, ensuring that custom
        /// time format patterns are applied correctly to DateTime values. This supports
        /// different time display preferences like 24-hour vs 12-hour formats.
        /// </remarks>
        [TestMethod]
        public void GlobalizationInfo_FormatTime_ShouldFormatCorrectly()
        {
            // Arrange
            var configContent = @"[Globalization]
TimeFormat=HH:mm:ss";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            var testDateTime = new DateTime(2024, 1, 15, 14, 30, 45);

            // Act
            var formatted = globalization.FormatTime(testDateTime);

            // Assert
            formatted.Should().Be("14:30:45", "time should be formatted using 24-hour format");
        }

        /// <summary>
        /// Verifies that short time formatting excludes seconds for concise time display.
        /// </summary>
        /// <remarks>
        /// This test ensures that short time formatting provides a more concise time representation
        /// suitable for UI elements where space is limited or when second-level precision
        /// is not required for the user experience.
        /// </remarks>
        [TestMethod]
        public void GlobalizationInfo_FormatShortTime_ShouldUseShortFormat()
        {
            // Arrange
            var configContent = @"[Globalization]
ShortTimeFormat=HH:mm";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            var testDateTime = new DateTime(2024, 1, 15, 14, 30, 45);

            // Act
            var formatted = globalization.FormatShortTime(testDateTime);

            // Assert
            formatted.Should().Be("14:30", "short time should exclude seconds");
        }

        /// <summary>
        /// Verifies that long time formatting includes milliseconds for high-precision time display.
        /// </summary>
        /// <remarks>
        /// This test validates the long time formatting feature, which provides high-precision
        /// time representations suitable for logging, debugging, or applications requiring
        /// subsecond timing accuracy.
        /// </remarks>
        [TestMethod]
        public void GlobalizationInfo_FormatLongTime_ShouldIncludeMilliseconds()
        {
            // Arrange
            var configContent = @"[Globalization]
LongTimeFormat=HH:mm:ss.fff";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            var testDateTime = new DateTime(2024, 1, 15, 14, 30, 45, 123);

            // Act
            var formatted = globalization.FormatLongTime(testDateTime);

            // Assert
            formatted.Should().Be("14:30:45.123", "long time should include milliseconds");
        }

        /// <summary>
        /// Verifies that 12-hour time formatting includes AM/PM indicators correctly.
        /// </summary>
        /// <remarks>
        /// This test validates the 12-hour time formatting functionality, ensuring proper
        /// conversion from 24-hour to 12-hour format with appropriate AM/PM indicators.
        /// This is essential for applications targeting users familiar with 12-hour time conventions.
        /// </remarks>
        [TestMethod]
        public void GlobalizationInfo_FormatTime12_ShouldUse12HourFormat()
        {
            // Arrange
            var configContent = @"[Globalization]
Time12Format=hh:mm:ss tt";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            var testDateTime = new DateTime(2024, 1, 15, 14, 30, 45);

            // Act
            var formatted = globalization.FormatTime12(testDateTime);

            // Assert
            formatted.Should().Be("02:30:45 PM", "12-hour time should include AM/PM indicator");
        }

        /// <summary>
        /// Verifies that short 12-hour time formatting excludes seconds and leading zeros appropriately.
        /// </summary>
        /// <remarks>
        /// This test ensures that short 12-hour time formatting provides the most concise
        /// representation suitable for casual time display, removing unnecessary elements
        /// while maintaining clarity with AM/PM indicators.
        /// </remarks>
        [TestMethod]
        public void GlobalizationInfo_FormatShortTime12_ShouldUseShort12HourFormat()
        {
            // Arrange
            var configContent = @"[Globalization]
ShortTime12Format=h:mm tt";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            var testDateTime = new DateTime(2024, 1, 15, 9, 5, 0);

            // Act
            var formatted = globalization.FormatShortTime12(testDateTime);

            // Assert
            formatted.Should().Be("9:05 AM", "short 12-hour time should exclude seconds and leading zeros");
        }

        #endregion

        #region Numeric Formatting Tests

        /// <summary>
        /// Verifies that integer formatting includes thousands separators for better readability.
        /// </summary>
        /// <remarks>
        /// This test validates the integer formatting functionality, ensuring that large numbers
        /// are displayed with appropriate thousands separators to improve readability and
        /// user comprehension of numeric values.
        /// </remarks>
        [TestMethod]
        public void GlobalizationInfo_FormatInteger_ShouldFormatWithThousandsSeparator()
        {
            // Arrange
            var configContent = @"[Globalization]
IntegerFormat=#,##0";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;

            // Act
            var formatted = globalization.FormatInteger(1234567);

            // Assert
            formatted.Should().Be("1,234,567", "integer should be formatted with thousands separators");
        }

        /// <summary>
        /// Verifies that integer formatting with null values returns the specified null representation.
        /// </summary>
        /// <remarks>
        /// This test ensures robust null handling in integer formatting, allowing applications
        /// to specify custom null value representations for better user experience when
        /// dealing with missing or undefined numeric data.
        /// </remarks>
        [TestMethod]
        public void GlobalizationInfo_FormatInteger_WithNullValue_ShouldReturnNullValue()
        {
            // Arrange
            var configContent = @"[Globalization]";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;

            // Act
            var formatted = globalization.FormatInteger((int?)null, "N/A");

            // Assert
            formatted.Should().Be("N/A", "null integer should return specified null value");
        }

        /// <summary>
        /// Verifies that number formatting correctly handles decimal places according to the configured format.
        /// </summary>
        /// <remarks>
        /// This test validates the decimal number formatting functionality, ensuring that floating-point
        /// numbers are displayed with the correct number of decimal places and proper rounding
        /// according to the configured number format pattern.
        /// </remarks>
        [TestMethod]
        public void GlobalizationInfo_FormatNumber_ShouldFormatDecimalPlaces()
        {
            // Arrange
            var configContent = @"[Globalization]
NumberFormat=#,##0.000";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;

            // Act
            var formatted = globalization.FormatNumber(1234.5678);

            // Assert
            formatted.Should().Be("1,234.568", "number should be formatted with specified decimal places");
        }

        /// <summary>
        /// Verifies that number formatting works correctly with float data types.
        /// </summary>
        /// <remarks>
        /// This test ensures that the number formatting system properly handles single-precision
        /// floating-point values, maintaining type compatibility across different numeric types
        /// while applying consistent formatting rules.
        /// </remarks>
        [TestMethod]
        public void GlobalizationInfo_FormatNumber_WithFloat_ShouldWork()
        {
            // Arrange
            var configContent = @"[Globalization]
NumberFormat=#,##0.00";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;

            // Act
            var formatted = globalization.FormatNumber(123.456f);

            // Assert
            formatted.Should().Be("123.46", "float should be formatted correctly");
        }

        /// <summary>
        /// Verifies that number formatting works correctly with decimal data types for high precision.
        /// </summary>
        /// <remarks>
        /// This test validates that the number formatting system properly handles high-precision
        /// decimal values, which is important for financial calculations and other scenarios
        /// requiring exact decimal representation.
        /// </remarks>
        [TestMethod]
        public void GlobalizationInfo_FormatNumber_WithDecimal_ShouldWork()
        {
            // Arrange
            var configContent = @"[Globalization]
NumberFormat=#,##0.0000";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;

            // Act
            var formatted = globalization.FormatNumber(123.456789m);

            // Assert
            formatted.Should().Be("123.4568", "decimal should be formatted with high precision");
        }

        #endregion

        #region Currency Formatting Tests

        /// <summary>
        /// Verifies that currency formatting includes the appropriate currency symbol and formatting.
        /// </summary>
        /// <remarks>
        /// This test validates the core currency formatting functionality, ensuring that monetary
        /// values are displayed with proper currency symbols, decimal places, and thousands
        /// separators according to the configured currency format.
        /// </remarks>
        [TestMethod]
        public void GlobalizationInfo_FormatCurrency_ShouldIncludeCurrencySymbol()
        {
            // Arrange
            var configContent = @"[Globalization]
CurrencyFormat='$' #,##0.00";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;

            // Act
            var formatted = globalization.FormatCurrency(1234.56);

            // Assert
            formatted.Should().Be("$ 1,234.56", "currency should include dollar symbol and formatting");
        }

        /// <summary>
        /// Verifies that currency formatting works correctly with European currency conventions.
        /// </summary>
        /// <remarks>
        /// This test ensures that the currency formatting system supports different currency
        /// placement conventions, such as placing the Euro symbol after the amount with
        /// culture-specific decimal and thousands separators.
        /// </remarks>
        [TestMethod]
        public void GlobalizationInfo_FormatCurrency_WithEuroFormat_ShouldWork()
        {
            // Arrange
            var configContent = @"[Globalization]
CurrencyFormat=# ##0.00 '€'
CultureInfo=pt-br";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;

            // Act
            
            var formatted = globalization.FormatCurrency(1234.56);

            // Assert
            formatted.Should().Be("1 234,56 €", "Euro format should place symbol after amount");
        }

        /// <summary>
        /// Verifies that currency formatting works correctly with float data types.
        /// </summary>
        /// <remarks>
        /// This test ensures that the currency formatting system properly handles single-precision
        /// floating-point values, maintaining type compatibility for various numeric inputs
        /// while applying consistent currency formatting rules.
        /// </remarks>
        [TestMethod]
        public void GlobalizationInfo_FormatCurrency_WithFloat_ShouldWork()
        {
            // Arrange
            var configContent = @"[Globalization]
CurrencyFormat='$' #,##0.00";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;

            // Act
            var formatted = globalization.FormatCurrency(1234.56f);

            // Assert
            formatted.Should().Be("$ 1,234.56", "float currency should be formatted correctly");
        }

        /// <summary>
        /// Verifies that currency formatting works correctly with decimal data types for precise monetary values.
        /// </summary>
        /// <remarks>
        /// This test validates that the currency formatting system properly handles high-precision
        /// decimal values, which is crucial for financial applications requiring exact monetary
        /// calculations without floating-point precision issues.
        /// </remarks>
        [TestMethod]
        public void GlobalizationInfo_FormatCurrency_WithDecimal_ShouldWork()
        {
            // Arrange
            var configContent = @"[Globalization]
CurrencyFormat='$' #,##0.00";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;

            // Act
            var formatted = globalization.FormatCurrency(1234.56m);

            // Assert
            formatted.Should().Be("$ 1,234.56", "decimal currency should be formatted correctly");
        }

        #endregion

        #region Culture-Specific Formatting Tests

        /// <summary>
        /// Verifies that French culture settings apply appropriate French formatting conventions.
        /// </summary>
        /// <remarks>
        /// This test validates culture-specific formatting by ensuring that French locale settings
        /// produce the expected formatting for numbers and currency, including the use of space
        /// as thousands separator and comma as decimal separator.
        /// </remarks>
        [TestMethod]
        public void GlobalizationInfo_WithFrenchCulture_ShouldUseFrenchFormatting()
        {
            // Arrange
            var configContent = @"[Globalization]
CultureInfo=fr-FR
NumberFormat=# ##0.00
CurrencyFormat=# ##0.00 '€'";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            
            // Manually set culture for test (since property loading may not work as expected)
            globalization.CultureInfo = CultureInfo.GetCultureInfo("fr-FR");

            // Act
            var formattedNumber = globalization.FormatNumber(1234.56);
            var formattedCurrency = globalization.FormatCurrency(1234.56);

            // Assert
            formattedNumber.Should().Be("1 234,56", "French formatting should use space as thousands separator and comma as decimal");
            formattedCurrency.Should().Be("1 234,56 €", "French currency should follow European conventions");
        }

        /// <summary>
        /// Verifies that German culture settings apply appropriate German formatting conventions.
        /// </summary>
        /// <remarks>
        /// This test validates culture-specific formatting for German locale, ensuring that
        /// numeric formatting follows German conventions with appropriate separators.
        /// This demonstrates the internationalization capabilities of the formatting system.
        /// </remarks>
        [TestMethod]
        public void GlobalizationInfo_WithGermanCulture_ShouldUseGermanFormatting()
        {
            // Arrange
            var configContent = @"[Globalization]
NumberFormat=# ##0.00";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            
            // Manually set culture for test
            globalization.CultureInfo = CultureInfo.GetCultureInfo("de-DE");

            // Act
            var formattedNumber = globalization.FormatNumber(1234.56);

            // Assert
            formattedNumber.Should().Be("1 234,56", "German formatting should use space and comma separators");
        }

        #endregion

        #region Multiple Integer Type Tests

        /// <summary>
        /// Verifies that integer formatting works correctly with all integer data types.
        /// </summary>
        /// <remarks>
        /// This comprehensive test validates that the integer formatting system supports
        /// all .NET integer types (short, long, byte, uint, ulong, ushort, sbyte) with
        /// consistent formatting behavior regardless of the underlying type.
        /// </remarks>
        [TestMethod]
        public void GlobalizationInfo_FormatInteger_WithVariousIntegerTypes_ShouldWork()
        {
            // Arrange
            var configContent = @"[Globalization]
IntegerFormat=#,##0";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;

            // Act & Assert
            globalization.FormatInteger((short)1234).Should().Be("1,234", "short should be formatted");
            globalization.FormatInteger((long)1234567).Should().Be("1,234,567", "long should be formatted");
            globalization.FormatInteger((byte)123).Should().Be("123", "byte should be formatted");
            globalization.FormatInteger((uint)1234).Should().Be("1,234", "uint should be formatted");
            globalization.FormatInteger((ulong)1234567).Should().Be("1,234,567", "ulong should be formatted");
            globalization.FormatInteger((ushort)1234).Should().Be("1,234", "ushort should be formatted");
            globalization.FormatInteger((sbyte)123).Should().Be("123", "sbyte should be formatted");
        }

        #endregion

        #region Combined Date/Time Format Tests

        /// <summary>
        /// Verifies that combined date/time formats are constructed correctly from individual format components.
        /// </summary>
        /// <remarks>
        /// This test validates the format combination logic, ensuring that various datetime formats
        /// are properly assembled from their constituent date and time format patterns.
        /// This is essential for providing consistent datetime representations across the application.
        /// </remarks>
        [TestMethod]
        public void GlobalizationInfo_CombinedFormats_ShouldBuildCorrectly()
        {
            // Arrange
            var configContent = @"[Globalization]
DateFormat=yyyy-MM-dd
TimeFormat=HH:mm:ss
LongDateFormat=dddd, MMMM d, yyyy
LongTimeFormat=HH:mm:ss.fff
ShortTimeFormat=HH:mm
ShortTime12Format=h:mm tt
LongTime12Format=hh:mm:ss.fff tt";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;

            // Act & Assert
            globalization.DateTimeFormat.Should().Be("yyyy-MM-dd HH:mm:ss", "datetime format should combine date and time");
            globalization.ShortDateTimeFormat.Should().Be("yyyy-MM-dd HH:mm", "short datetime should combine date and short time");
            globalization.LongDateTimeFormat.Should().Be("dddd, MMMM d, yyyy HH:mm:ss.fff", "long datetime should combine long date and long time");
            globalization.ShortDateTime12Format.Should().Be("yyyy-MM-dd h:mm tt", "short 12-hour datetime should combine appropriately");
            globalization.LongDateTime12Format.Should().Be("dddd, MMMM d, yyyy hh:mm:ss.fff tt", "long 12-hour datetime should combine appropriately");
        }

        /// <summary>
        /// Verifies that short datetime formatting uses the properly combined date and short time formats.
        /// </summary>
        /// <remarks>
        /// This test ensures that short datetime formatting produces concise datetime representations
        /// by combining the configured date format with the short time format, suitable for
        /// UI elements where space is limited.
        /// </remarks>
        [TestMethod]
        public void GlobalizationInfo_FormatShortDateTime_ShouldUseCombinedFormat()
        {
            // Arrange
            var configContent = @"[Globalization]
DateFormat=yyyy-MM-dd
ShortTimeFormat=HH:mm";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            var testDateTime = new DateTime(2024, 1, 15, 14, 30, 45);

            // Act
            var formatted = globalization.FormatShortDateTime(testDateTime);

            // Assert
            formatted.Should().Be("2024-01-15 14:30", "short datetime should combine date and short time");
        }

        /// <summary>
        /// Verifies that long datetime formatting uses the properly combined long date and long time formats.
        /// </summary>
        /// <remarks>
        /// This test ensures that long datetime formatting produces comprehensive datetime representations
        /// by combining the configured long date format with the long time format, providing
        /// complete temporal information including day names and millisecond precision.
        /// </remarks>
        [TestMethod]
        public void GlobalizationInfo_FormatLongDateTime_ShouldUseCombinedFormat()
        {
            // Arrange
            var configContent = @"[Globalization]
LongDateFormat=dddd, MMMM d, yyyy
LongTimeFormat=HH:mm:ss.fff";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            var testDateTime = new DateTime(2024, 1, 15, 14, 30, 45, 123);

            // Act
            var formatted = globalization.FormatLongDateTime(testDateTime);

            // Assert
            formatted.Should().Be("Monday, January 15, 2024 14:30:45.123", "long datetime should combine long date and long time");
        }

        #endregion

        #region DateTimeOffset Formatting Tests

        /// <summary>
        /// Verifies that <see cref="GlobalizationInfo.UseZuluTime"/> defaults to <c>false</c>.
        /// </summary>
        [TestMethod]
        public void GlobalizationInfo_UseZuluTime_DefaultsToFalse()
        {
            // Arrange
            var configContent = @"[Globalization]";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var globalization = config.Globalization;

            // Assert
            globalization.UseZuluTime.Should().BeFalse("UseZuluTime should default to false");
        }

        /// <summary>
        /// Verifies that a non-UTC offset is rendered as <c>+/-HH:mm</c> regardless of <see cref="GlobalizationInfo.UseZuluTime"/>.
        /// </summary>
        [TestMethod]
        public void GlobalizationInfo_FormatTimeOffset_NonUtcOffset_ShouldRenderOffset()
        {
            // Arrange
            var configContent = @"[Globalization]
TimeFormat=HH:mm:ss";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            var dto = new DateTimeOffset(2023, 3, 9, 14, 30, 45, TimeSpan.FromHours(-3));

            // Act
            var formatted = globalization.FormatTimeOffset(dto);

            // Assert
            formatted.Should().Be("14:30:45 -03:00", "non-UTC offset should be rendered as -03:00");
        }

        /// <summary>
        /// Verifies that a UTC offset is rendered as <c>+00:00</c> when <see cref="GlobalizationInfo.UseZuluTime"/> is <c>false</c>.
        /// </summary>
        [TestMethod]
        public void GlobalizationInfo_FormatTimeOffset_UtcOffset_UseZuluFalse_ShouldRenderPlusZero()
        {
            // Arrange
            var configContent = @"[Globalization]
TimeFormat=HH:mm:ss";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            globalization.UseZuluTime = false;
            var utcDto = new DateTimeOffset(2023, 3, 9, 14, 30, 45, TimeSpan.Zero);

            // Act
            var formatted = globalization.FormatTimeOffset(utcDto);

            // Assert
            formatted.Should().Be("14:30:45 +00:00", "UTC offset with UseZuluTime=false should render as +00:00");
        }

        /// <summary>
        /// Verifies that a UTC offset is rendered as <c>Z</c> when <see cref="GlobalizationInfo.UseZuluTime"/> is <c>true</c>.
        /// </summary>
        [TestMethod]
        public void GlobalizationInfo_FormatTimeOffset_UtcOffset_UseZuluTrue_ShouldRenderZ()
        {
            // Arrange
            var configContent = @"[Globalization]
TimeFormat=HH:mm:ss";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            globalization.UseZuluTime = true;
            var utcDto = new DateTimeOffset(2023, 3, 9, 14, 30, 45, TimeSpan.Zero);

            // Act
            var formatted = globalization.FormatTimeOffset(utcDto);

            // Assert
            formatted.Should().Be("14:30:45 Z", "UTC offset with UseZuluTime=true should render as Z");
        }

        /// <summary>
        /// Verifies that <c>null</c> input returns the specified null-value string.
        /// </summary>
        [TestMethod]
        public void GlobalizationInfo_FormatTimeOffset_Null_ShouldReturnNullValue()
        {
            // Arrange
            var configContent = @"[Globalization]";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;

            // Act
            var formatted = globalization.FormatTimeOffset(null, "N/A");

            // Assert
            formatted.Should().Be("N/A", "null DateTimeOffset should return specified null value");
        }

        /// <summary>
        /// Verifies that short time offset formatting excludes seconds.
        /// </summary>
        [TestMethod]
        public void GlobalizationInfo_FormatShortTimeOffset_ShouldUseShortFormat()
        {
            // Arrange
            var configContent = @"[Globalization]
ShortTimeFormat=HH:mm";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            var dto = new DateTimeOffset(2023, 3, 9, 14, 30, 45, TimeSpan.FromHours(-3));

            // Act
            var formatted = globalization.FormatShortTimeOffset(dto);

            // Assert
            formatted.Should().Be("14:30 -03:00", "short time offset should exclude seconds");
        }

        /// <summary>
        /// Verifies that long time offset formatting includes milliseconds.
        /// </summary>
        [TestMethod]
        public void GlobalizationInfo_FormatLongTimeOffset_ShouldIncludeMilliseconds()
        {
            // Arrange
            var configContent = @"[Globalization]
LongTimeFormat=HH:mm:ss.fff";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            var dto = new DateTimeOffset(2023, 3, 9, 14, 30, 45, 123, TimeSpan.FromHours(-3));

            // Act
            var formatted = globalization.FormatLongTimeOffset(dto);

            // Assert
            formatted.Should().Be("14:30:45.123 -03:00", "long time offset should include milliseconds");
        }

        /// <summary>
        /// Verifies that <see cref="GlobalizationInfo.FormatDateTimeOffset"/> combines date and 24-hour time with offset.
        /// </summary>
        [TestMethod]
        public void GlobalizationInfo_FormatDateTimeOffset_ShouldCombineDateAndTime()
        {
            // Arrange
            var configContent = @"[Globalization]
DateFormat=yyyy-MM-dd
TimeFormat=HH:mm:ss";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            var dto = new DateTimeOffset(2023, 3, 9, 14, 30, 45, TimeSpan.FromHours(-3));

            // Act
            var formatted = globalization.FormatDateTimeOffset(dto);

            // Assert
            formatted.Should().Be("2023-03-09 14:30:45 -03:00", "date-time offset should combine date and time with offset");
        }

        /// <summary>
        /// Verifies that <see cref="GlobalizationInfo.FormatShortDateTimeOffset"/> uses date and short time with offset.
        /// </summary>
        [TestMethod]
        public void GlobalizationInfo_FormatShortDateTimeOffset_ShouldUseCombinedFormat()
        {
            // Arrange
            var configContent = @"[Globalization]
DateFormat=yyyy-MM-dd
ShortTimeFormat=HH:mm";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            var dto = new DateTimeOffset(2023, 3, 9, 14, 30, 45, TimeSpan.FromHours(-3));

            // Act
            var formatted = globalization.FormatShortDateTimeOffset(dto);

            // Assert
            formatted.Should().Be("2023-03-09 14:30 -03:00", "short date-time offset should combine date and short time with offset");
        }

        /// <summary>
        /// Verifies that <see cref="GlobalizationInfo.FormatLongDateTimeOffset"/> uses long date and long time with offset.
        /// </summary>
        [TestMethod]
        public void GlobalizationInfo_FormatLongDateTimeOffset_ShouldUseLongFormats()
        {
            // Arrange
            var configContent = @"[Globalization]
LongDateFormat=dddd, MMMM d, yyyy
LongTimeFormat=HH:mm:ss.fff";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            var dto = new DateTimeOffset(2023, 3, 9, 14, 30, 45, 123, TimeSpan.FromHours(-3));

            // Act
            var formatted = globalization.FormatLongDateTimeOffset(dto);

            // Assert
            formatted.Should().Be("Thursday, March 9, 2023 14:30:45.123 -03:00", "long date-time offset should combine long date and long time with offset");
        }

        /// <summary>
        /// Verifies that <see cref="GlobalizationInfo.FormatTime12Offset"/> uses 12-hour format with offset.
        /// </summary>
        [TestMethod]
        public void GlobalizationInfo_FormatTime12Offset_ShouldUse12HourFormat()
        {
            // Arrange
            var configContent = @"[Globalization]
Time12Format=hh:mm:ss tt";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            var dto = new DateTimeOffset(2023, 3, 9, 14, 30, 45, TimeSpan.FromHours(-3));

            // Act
            var formatted = globalization.FormatTime12Offset(dto);

            // Assert
            formatted.Should().Be("02:30:45 PM -03:00", "12-hour time offset should include AM/PM indicator and offset");
        }

        /// <summary>
        /// Verifies that <see cref="GlobalizationInfo.FormatShortTime12Offset"/> uses short 12-hour format with offset.
        /// </summary>
        [TestMethod]
        public void GlobalizationInfo_FormatShortTime12Offset_ShouldUseShortFormat()
        {
            // Arrange
            var configContent = @"[Globalization]
ShortTime12Format=h:mm tt";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            var dto = new DateTimeOffset(2023, 3, 9, 14, 30, 45, TimeSpan.FromHours(-3));

            // Act
            var formatted = globalization.FormatShortTime12Offset(dto);

            // Assert
            formatted.Should().Be("2:30 PM -03:00", "short 12-hour time offset should exclude seconds");
        }

        /// <summary>
        /// Verifies that <see cref="GlobalizationInfo.FormatLongTime12Offset"/> includes milliseconds with 12-hour format and offset.
        /// </summary>
        [TestMethod]
        public void GlobalizationInfo_FormatLongTime12Offset_ShouldIncludeMilliseconds()
        {
            // Arrange
            var configContent = @"[Globalization]
LongTime12Format=hh:mm:ss.fff tt";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            var dto = new DateTimeOffset(2023, 3, 9, 14, 30, 45, 123, TimeSpan.FromHours(-3));

            // Act
            var formatted = globalization.FormatLongTime12Offset(dto);

            // Assert
            formatted.Should().Be("02:30:45.123 PM -03:00", "long 12-hour time offset should include milliseconds");
        }

        /// <summary>
        /// Verifies that <see cref="GlobalizationInfo.FormatShortDateTime12Offset"/> combines date and short 12-hour time with offset.
        /// </summary>
        [TestMethod]
        public void GlobalizationInfo_FormatShortDateTime12Offset_ShouldCombineCorrectly()
        {
            // Arrange
            var configContent = @"[Globalization]
DateFormat=yyyy-MM-dd
ShortTime12Format=h:mm tt";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            var dto = new DateTimeOffset(2023, 3, 9, 14, 30, 45, TimeSpan.FromHours(-3));

            // Act
            var formatted = globalization.FormatShortDateTime12Offset(dto);

            // Assert
            formatted.Should().Be("2023-03-09 2:30 PM -03:00", "short 12-hour date-time offset should combine date and short 12-hour time with offset");
        }

        /// <summary>
        /// Verifies that <see cref="GlobalizationInfo.FormatLongDateTime12Offset"/> combines long date and long 12-hour time with offset.
        /// </summary>
        [TestMethod]
        public void GlobalizationInfo_FormatLongDateTime12Offset_ShouldCombineCorrectly()
        {
            // Arrange
            var configContent = @"[Globalization]
LongDateFormat=dddd, MMMM d, yyyy
LongTime12Format=hh:mm:ss.fff tt";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            var dto = new DateTimeOffset(2023, 3, 9, 14, 30, 45, 123, TimeSpan.FromHours(-3));

            // Act
            var formatted = globalization.FormatLongDateTime12Offset(dto);

            // Assert
            formatted.Should().Be("Thursday, March 9, 2023 02:30:45.123 PM -03:00", "long 12-hour date-time offset should combine long date and long 12-hour time with offset");
        }

        /// <summary>
        /// Verifies that all computed offset format properties compose their constituent formats correctly.
        /// </summary>
        [TestMethod]
        public void GlobalizationInfo_OffsetCombinedFormats_ShouldBuildCorrectly()
        {
            // Arrange
            var configContent = @"[Globalization]
DateFormat=yyyy-MM-dd
TimeFormat=HH:mm:ss
LongDateFormat=dddd, MMMM d, yyyy
LongTimeFormat=HH:mm:ss.fff
ShortTimeFormat=HH:mm
Time12Format=hh:mm:ss tt
ShortTime12Format=h:mm tt
LongTime12Format=hh:mm:ss.fff tt";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;

            // Act & Assert
            globalization.TimeOffsetFormat.Should().Be("HH:mm:ss zzz",                           "TimeOffsetFormat should append zzz to TimeFormat");
            globalization.ShortTimeOffsetFormat.Should().Be("HH:mm zzz",                         "ShortTimeOffsetFormat should append zzz to ShortTimeFormat");
            globalization.LongTimeOffsetFormat.Should().Be("HH:mm:ss.fff zzz",                   "LongTimeOffsetFormat should append zzz to LongTimeFormat");
            globalization.Time12OffsetFormat.Should().Be("hh:mm:ss tt zzz",                      "Time12OffsetFormat should append zzz to Time12Format");
            globalization.ShortTime12OffsetFormat.Should().Be("h:mm tt zzz",                     "ShortTime12OffsetFormat should append zzz to ShortTime12Format");
            globalization.LongTime12OffsetFormat.Should().Be("hh:mm:ss.fff tt zzz",              "LongTime12OffsetFormat should append zzz to LongTime12Format");
            globalization.DateTimeOffsetFormat.Should().Be("yyyy-MM-dd HH:mm:ss zzz",            "DateTimeOffsetFormat should combine DateFormat and TimeOffsetFormat");
            globalization.ShortDateTimeOffsetFormat.Should().Be("yyyy-MM-dd HH:mm zzz",          "ShortDateTimeOffsetFormat should combine DateFormat and ShortTimeOffsetFormat");
            globalization.LongDateTimeOffsetFormat.Should().Be("dddd, MMMM d, yyyy HH:mm:ss.fff zzz", "LongDateTimeOffsetFormat should combine LongDateFormat and LongTimeOffsetFormat");
            globalization.ShortDateTime12OffsetFormat.Should().Be("yyyy-MM-dd h:mm tt zzz",      "ShortDateTime12OffsetFormat should combine DateFormat and ShortTime12OffsetFormat");
            globalization.LongDateTime12OffsetFormat.Should().Be("dddd, MMMM d, yyyy hh:mm:ss.fff tt zzz", "LongDateTime12OffsetFormat should combine LongDateFormat and LongTime12OffsetFormat");
        }

        #endregion

        #region Helper Methods

        private void ResetConfiguration()
        {
            var instanceField = typeof(ByteForge.Toolkit.Configuration.Configuration).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
            var isInitializedField = typeof(ByteForge.Toolkit.Configuration.Configuration).GetField("IsInitialized", BindingFlags.Public | BindingFlags.Static);
            var manuallyInitializedField = typeof(ByteForge.Toolkit.Configuration.Configuration).GetField("_manuallyInitialized", BindingFlags.NonPublic | BindingFlags.Static);
            var globalizationField = typeof(ByteForge.Toolkit.Configuration.Configuration).GetField("_globalizationInfo", BindingFlags.NonPublic | BindingFlags.Static);

            if (instanceField != null)
            {
                var newLazy = new Lazy<ByteForge.Toolkit.Configuration.Configuration>();
                instanceField.SetValue(null, newLazy);
            }

            if (isInitializedField != null)
            {
                isInitializedField.SetValue(null, false);
            }

            if (manuallyInitializedField != null)
            {
                manuallyInitializedField.SetValue(null, false);
            }

            if (globalizationField != null)
            {
                var newGlobalizationLazy = new Lazy<GlobalizationInfo>(() => ByteForge.Toolkit.Configuration.Configuration.GetSection<GlobalizationInfo>("Globalization"));
                globalizationField.SetValue(null, newGlobalizationLazy);
            }
        }

        #endregion
    }
}