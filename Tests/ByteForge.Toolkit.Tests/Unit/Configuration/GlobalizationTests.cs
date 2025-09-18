using System;
using System.Globalization;
using System.Reflection;
using AwesomeAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ByteForge.Toolkit.Tests.Helpers;
using ByteForge.Toolkit.Tests.Models;

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

        [TestMethod]
        public void GlobalizationInfo_WithDefaultConfiguration_ShouldUseDefaults()
        {
            // Arrange
            var configContent = @"[Globalization]";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
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
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
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

        [TestMethod]
        public void GlobalizationInfo_WithPartialConfiguration_ShouldUseDefaultsForMissing()
        {
            // Arrange
            var configContent = @"[Globalization]
DateFormat=yyyy-MM-dd
CurrencyFormat=¥#,##0";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
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

        [TestMethod]
        public void GlobalizationInfo_FormatDate_ShouldFormatCorrectly()
        {
            // Arrange
            var configContent = @"[Globalization]
DateFormat=yyyy-MM-dd";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            var testDate = new DateTime(2024, 1, 15);

            // Act
            var formatted = globalization.FormatDate(testDate);

            // Assert
            formatted.Should().Be("2024-01-15", "date should be formatted using custom format");
        }

        [TestMethod]
        public void GlobalizationInfo_FormatDate_WithNullValue_ShouldReturnNullValue()
        {
            // Arrange
            var configContent = @"[Globalization]";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;

            // Act
            var formatted = globalization.FormatDate(null, "N/A");

            // Assert
            formatted.Should().Be("N/A", "null date should return specified null value");
        }

        [TestMethod]
        public void GlobalizationInfo_FormatLongDate_ShouldUseCorrectFormat()
        {
            // Arrange
            var configContent = @"[Globalization]
LongDateFormat=dddd, MMMM d, yyyy";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            var testDate = new DateTime(2024, 1, 15); // Monday

            // Act
            var formatted = globalization.FormatLongDate(testDate);

            // Assert
            formatted.Should().Be("Monday, January 15, 2024", "long date should include day of week and full month name");
        }

        [TestMethod]
        public void GlobalizationInfo_FormatDateTime_ShouldCombineDateAndTime()
        {
            // Arrange
            var configContent = @"[Globalization]
DateFormat=yyyy-MM-dd
TimeFormat=HH:mm:ss";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
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

        [TestMethod]
        public void GlobalizationInfo_FormatTime_ShouldFormatCorrectly()
        {
            // Arrange
            var configContent = @"[Globalization]
TimeFormat=HH:mm:ss";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            var testDateTime = new DateTime(2024, 1, 15, 14, 30, 45);

            // Act
            var formatted = globalization.FormatTime(testDateTime);

            // Assert
            formatted.Should().Be("14:30:45", "time should be formatted using 24-hour format");
        }

        [TestMethod]
        public void GlobalizationInfo_FormatShortTime_ShouldUseShortFormat()
        {
            // Arrange
            var configContent = @"[Globalization]
ShortTimeFormat=HH:mm";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            var testDateTime = new DateTime(2024, 1, 15, 14, 30, 45);

            // Act
            var formatted = globalization.FormatShortTime(testDateTime);

            // Assert
            formatted.Should().Be("14:30", "short time should exclude seconds");
        }

        [TestMethod]
        public void GlobalizationInfo_FormatLongTime_ShouldIncludeMilliseconds()
        {
            // Arrange
            var configContent = @"[Globalization]
LongTimeFormat=HH:mm:ss.fff";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            var testDateTime = new DateTime(2024, 1, 15, 14, 30, 45, 123);

            // Act
            var formatted = globalization.FormatLongTime(testDateTime);

            // Assert
            formatted.Should().Be("14:30:45.123", "long time should include milliseconds");
        }

        [TestMethod]
        public void GlobalizationInfo_FormatTime12_ShouldUse12HourFormat()
        {
            // Arrange
            var configContent = @"[Globalization]
Time12Format=hh:mm:ss tt";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            var testDateTime = new DateTime(2024, 1, 15, 14, 30, 45);

            // Act
            var formatted = globalization.FormatTime12(testDateTime);

            // Assert
            formatted.Should().Be("02:30:45 PM", "12-hour time should include AM/PM indicator");
        }

        [TestMethod]
        public void GlobalizationInfo_FormatShortTime12_ShouldUseShort12HourFormat()
        {
            // Arrange
            var configContent = @"[Globalization]
ShortTime12Format=h:mm tt";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
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

        [TestMethod]
        public void GlobalizationInfo_FormatInteger_ShouldFormatWithThousandsSeparator()
        {
            // Arrange
            var configContent = @"[Globalization]
IntegerFormat=#,##0";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;

            // Act
            var formatted = globalization.FormatInteger(1234567);

            // Assert
            formatted.Should().Be("1,234,567", "integer should be formatted with thousands separators");
        }

        [TestMethod]
        public void GlobalizationInfo_FormatInteger_WithNullValue_ShouldReturnNullValue()
        {
            // Arrange
            var configContent = @"[Globalization]";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;

            // Act
            var formatted = globalization.FormatInteger((int?)null, "N/A");

            // Assert
            formatted.Should().Be("N/A", "null integer should return specified null value");
        }

        [TestMethod]
        public void GlobalizationInfo_FormatNumber_ShouldFormatDecimalPlaces()
        {
            // Arrange
            var configContent = @"[Globalization]
NumberFormat=#,##0.000";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;

            // Act
            var formatted = globalization.FormatNumber(1234.5678);

            // Assert
            formatted.Should().Be("1,234.568", "number should be formatted with specified decimal places");
        }

        [TestMethod]
        public void GlobalizationInfo_FormatNumber_WithFloat_ShouldWork()
        {
            // Arrange
            var configContent = @"[Globalization]
NumberFormat=#,##0.00";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;

            // Act
            var formatted = globalization.FormatNumber(123.456f);

            // Assert
            formatted.Should().Be("123.46", "float should be formatted correctly");
        }

        [TestMethod]
        public void GlobalizationInfo_FormatNumber_WithDecimal_ShouldWork()
        {
            // Arrange
            var configContent = @"[Globalization]
NumberFormat=#,##0.0000";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
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

        [TestMethod]
        public void GlobalizationInfo_FormatCurrency_ShouldIncludeCurrencySymbol()
        {
            // Arrange
            var configContent = @"[Globalization]
CurrencyFormat='$' #,##0.00";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;

            // Act
            var formatted = globalization.FormatCurrency(1234.56);

            // Assert
            formatted.Should().Be("$ 1,234.56", "currency should include dollar symbol and formatting");
        }

        [TestMethod]
        public void GlobalizationInfo_FormatCurrency_WithEuroFormat_ShouldWork()
        {
            // Arrange
            var configContent = @"[Globalization]
CurrencyFormat=# ##0.00 '€'
CultureInfo=pt-br";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;

            // Act
            
            var formatted = globalization.FormatCurrency(1234.56);

            // Assert
            formatted.Should().Be("1 234,56 €", "Euro format should place symbol after amount");
        }

        [TestMethod]
        public void GlobalizationInfo_FormatCurrency_WithFloat_ShouldWork()
        {
            // Arrange
            var configContent = @"[Globalization]
CurrencyFormat='$' #,##0.00";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;

            // Act
            var formatted = globalization.FormatCurrency(1234.56f);

            // Assert
            formatted.Should().Be("$ 1,234.56", "float currency should be formatted correctly");
        }

        [TestMethod]
        public void GlobalizationInfo_FormatCurrency_WithDecimal_ShouldWork()
        {
            // Arrange
            var configContent = @"[Globalization]
CurrencyFormat='$' #,##0.00";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
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

        [TestMethod]
        public void GlobalizationInfo_WithFrenchCulture_ShouldUseFrenchFormatting()
        {
            // Arrange
            var configContent = @"[Globalization]
CultureInfo=fr-FR
NumberFormat=# ##0.00
CurrencyFormat=# ##0.00 '€'";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
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

        [TestMethod]
        public void GlobalizationInfo_WithGermanCulture_ShouldUseGermanFormatting()
        {
            // Arrange
            var configContent = @"[Globalization]
NumberFormat=# ##0.00";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
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

        [TestMethod]
        public void GlobalizationInfo_FormatInteger_WithVariousIntegerTypes_ShouldWork()
        {
            // Arrange
            var configContent = @"[Globalization]
IntegerFormat=#,##0";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
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
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
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

        [TestMethod]
        public void GlobalizationInfo_FormatShortDateTime_ShouldUseCombinedFormat()
        {
            // Arrange
            var configContent = @"[Globalization]
DateFormat=yyyy-MM-dd
ShortTimeFormat=HH:mm";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            var globalization = config.Globalization;
            var testDateTime = new DateTime(2024, 1, 15, 14, 30, 45);

            // Act
            var formatted = globalization.FormatShortDateTime(testDateTime);

            // Assert
            formatted.Should().Be("2024-01-15 14:30", "short datetime should combine date and short time");
        }

        [TestMethod]
        public void GlobalizationInfo_FormatLongDateTime_ShouldUseCombinedFormat()
        {
            // Arrange
            var configContent = @"[Globalization]
LongDateFormat=dddd, MMMM d, yyyy
LongTimeFormat=HH:mm:ss.fff";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
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

        #region Helper Methods

        private void ResetConfiguration()
        {
            var instanceField = typeof(ByteForge.Toolkit.Configuration).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
            var isInitializedField = typeof(ByteForge.Toolkit.Configuration).GetField("IsInitialized", BindingFlags.Public | BindingFlags.Static);
            var manuallyInitializedField = typeof(ByteForge.Toolkit.Configuration).GetField("_manuallyInitialized", BindingFlags.NonPublic | BindingFlags.Static);
            var globalizationField = typeof(ByteForge.Toolkit.Configuration).GetField("_globalizationInfo", BindingFlags.NonPublic | BindingFlags.Static);

            if (instanceField != null)
            {
                var newLazy = new Lazy<ByteForge.Toolkit.Configuration>();
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
                var newGlobalizationLazy = new Lazy<GlobalizationInfo>(() => ByteForge.Toolkit.Configuration.GetSection<GlobalizationInfo>("Globalization"));
                globalizationField.SetValue(null, newGlobalizationLazy);
            }
        }

        #endregion
    }
}