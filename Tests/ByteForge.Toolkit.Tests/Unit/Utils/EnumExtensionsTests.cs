using System;
using System.ComponentModel;
using ByteForge.Toolkit;
using AwesomeAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ByteForge.Toolkit.Tests.Unit.Utils
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("Utils")]
    public class EnumExtensionsTests
    {
        #region Test Enums

        /// <summary>
        /// Test enum with description attributes for testing GetDescription method.
        /// </summary>
        private enum TestEnumWithDescriptions
        {
            [System.ComponentModel.Description("First Option")]
            FirstOption,

            [System.ComponentModel.Description("Second Option with Special Characters !@#$%")]
            SecondOption,

            [System.ComponentModel.Description("")]
            EmptyDescription,

            NoDescription
        }

        /// <summary>
        /// Test enum without any description attributes.
        /// </summary>
        private enum TestEnumWithoutDescriptions
        {
            Value1,
            Value2,
            Value3
        }

        /// <summary>
        /// Test flags enum with descriptions.
        /// </summary>
        [Flags]
        private enum TestFlagsEnum
        {
            [System.ComponentModel.Description("No flags set")]
            None = 0,

            [System.ComponentModel.Description("First flag")]
            Flag1 = 1,

            [System.ComponentModel.Description("Second flag")]
            Flag2 = 2,

            [System.ComponentModel.Description("Third flag")]
            Flag4 = 4,

            [System.ComponentModel.Description("Combined flags")]
            Combined = Flag1 | Flag2
        }

        #endregion

        #region GetDescription Tests

        /// <summary>
        /// Verifies that GetDescription returns the correct description for enum values with Description attributes.
        /// </summary>
        /// <remarks>
        /// Tests that the GetDescription extension method properly retrieves Description attribute values.
        /// </remarks>
        [TestMethod]
        public void GetDescription_WithDescriptionAttribute_ShouldReturnDescription()
        {
            // Arrange & Act & Assert
            TestEnumWithDescriptions.FirstOption.GetDescription()
                .Should().Be("First Option", "should return the description attribute value");

            TestEnumWithDescriptions.SecondOption.GetDescription()
                .Should().Be("Second Option with Special Characters !@#$%", "should handle special characters in description");
        }

        /// <summary>
        /// Verifies that GetDescription returns the enum name when no Description attribute is present.
        /// </summary>
        /// <remarks>
        /// Tests fallback behavior when Description attribute is not found on enum value.
        /// </remarks>
        [TestMethod]
        public void GetDescription_WithoutDescriptionAttribute_ShouldReturnEnumName()
        {
            // Arrange & Act & Assert
            TestEnumWithDescriptions.NoDescription.GetDescription()
                .Should().Be("NoDescription", "should return enum name when no description attribute exists");

            TestEnumWithoutDescriptions.Value1.GetDescription()
                .Should().Be("Value1", "should return enum name for enums without description attributes");

            TestEnumWithoutDescriptions.Value2.GetDescription()
                .Should().Be("Value2", "should return enum name for enums without description attributes");
        }

        /// <summary>
        /// Verifies that GetDescription handles empty description attributes correctly.
        /// </summary>
        /// <remarks>
        /// Tests behavior when Description attribute exists but contains an empty string.
        /// </remarks>
        [TestMethod]
        public void GetDescription_WithEmptyDescriptionAttribute_ShouldReturnEmptyString()
        {
            // Act & Assert
            TestEnumWithDescriptions.EmptyDescription.GetDescription()
                .Should().Be("", "should return empty string when description attribute is empty");
        }

        /// <summary>
        /// Verifies that GetDescription works correctly with flags enums.
        /// </summary>
        /// <remarks>
        /// Tests GetDescription behavior with enum values decorated with [Flags] attribute.
        /// </remarks>
        [TestMethod]
        public void GetDescription_WithFlagsEnum_ShouldReturnDescription()
        {
            // Act & Assert
            TestFlagsEnum.None.GetDescription()
                .Should().Be("No flags set", "should return description for flags enum value");

            TestFlagsEnum.Flag1.GetDescription()
                .Should().Be("First flag", "should return description for individual flag");

            TestFlagsEnum.Combined.GetDescription()
                .Should().Be("Combined flags", "should return description for combined flag value");
        }

        /// <summary>
        /// Verifies that GetDescription works with combined flags that don't have their own description.
        /// </summary>
        /// <remarks>
        /// Tests behavior when flags are combined dynamically and don't have a specific description.
        /// </remarks>
        [TestMethod]
        public void GetDescription_WithCombinedFlags_ShouldReturnEnumName()
        {
            // Arrange - This will match the Combined enum value which has description "Combined flags"
            var combinedFlags = TestFlagsEnum.Flag1 | TestFlagsEnum.Flag2;

            // Act & Assert
            combinedFlags.GetDescription()
                .Should().Be("Combined flags", "Flag1 | Flag2 matches the Combined enum value which has its own description");
        }

        /// <summary>
        /// Verifies that GetDescription returns enum name for dynamic flag combinations without predefined values.
        /// </summary>
        /// <remarks>
        /// Tests behavior when multiple flags are combined that don't correspond to a single enum value.
        /// </remarks>
        [TestMethod]
        public void GetDescription_WithDynamicCombinedFlags_ShouldReturnEnumName()
        {
            // Arrange - Combine flags that don't have a predefined combined value
            var dynamicCombination = TestFlagsEnum.Flag1 | TestFlagsEnum.Flag4;

            // Act & Assert
            dynamicCombination.GetDescription()
                .Should().Be("Flag1, Flag4", "should return string representation for dynamic flag combinations");
        }

        /// <summary>
        /// Verifies that GetDescription works with standard .NET enums.
        /// </summary>
        /// <remarks>
        /// Tests GetDescription with built-in .NET enum types to ensure compatibility.
        /// </remarks>
        [TestMethod]
        public void GetDescription_WithStandardEnums_ShouldReturnEnumName()
        {
            // Act & Assert
            DayOfWeek.Monday.GetDescription()
                .Should().Be("Monday", "should work with standard .NET enums");

            StringComparison.OrdinalIgnoreCase.GetDescription()
                .Should().Be("OrdinalIgnoreCase", "should work with standard .NET enums");
        }

        /// <summary>
        /// Verifies GetDescription handles numeric enum values correctly.
        /// </summary>
        /// <remarks>
        /// Tests behavior when enum values are cast from integers or have explicit numeric values.
        /// </remarks>
        [TestMethod]
        public void GetDescription_WithNumericValues_ShouldHandleCorrectly()
        {
            // Arrange - Create enum value from integer
            var numericEnumValue = (TestEnumWithDescriptions)0; // Should be FirstOption

            // Act & Assert
            numericEnumValue.GetDescription()
                .Should().Be("First Option", "should work with enum values created from numeric values");
        }

        /// <summary>
        /// Verifies GetDescription handles undefined enum values gracefully.
        /// </summary>
        /// <remarks>
        /// Tests behavior when enum is cast from an integer value that doesn't correspond to a defined enum member.
        /// </remarks>
        [TestMethod]
        public void GetDescription_WithUndefinedEnumValue_ShouldReturnNumericString()
        {
            // Arrange - Create undefined enum value
            var undefinedValue = (TestEnumWithDescriptions)999;

            // Act & Assert
            undefinedValue.GetDescription()
                .Should().Be("999", "should return numeric string for undefined enum values");
        }

        /// <summary>
        /// Performance test to ensure GetDescription performs adequately for repeated use.
        /// </summary>
        /// <remarks>
        /// Verifies that reflection operations in GetDescription don't cause performance issues.
        /// </remarks>
        [TestMethod]
        public void GetDescription_Performance_ShouldHandleMultipleCallsQuickly()
        {
            // Arrange
            var iterations = 10000;
            var testEnum = TestEnumWithDescriptions.FirstOption;
            var startTime = DateTime.UtcNow;

            // Act
            for (var i = 0; i < iterations; i++)
            {
                var description = testEnum.GetDescription();
                description.Should().Be("First Option");
            }

            // Assert
            var duration = DateTime.UtcNow - startTime;
            duration.Should().BeLessThan(TimeSpan.FromSeconds(2), 
                $"should handle {iterations} GetDescription calls quickly");
        }

        #endregion
    }
}