using AwesomeAssertions;
using ByteForge.Toolkit.CLI;

namespace ByteForge.Toolkit.Tests.Unit.CLI
{
    /// <summary>
    /// Unit tests validating construction, properties, edge cases, and attribute usage for <see cref="OptionAttribute"/>.
    /// Ensures correct handling of descriptions, aliases (including null, empty, duplicates, whitespace, unicode),
    /// mutability of the Name property, and immutability of read-only collections.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("CLI")]
    public class OptionAttributeTests
    {
        #region Constructor Tests - Description Only

        /// <summary>
        /// Verifies that providing only a description correctly initializes the description and defaults other members.
        /// </summary>
        [TestMethod]
        public void Constructor_WithDescriptionOnly_ShouldInitializePropertiesCorrectly()
        {
            // Arrange
            var description = "Test option description";

            // Act
            var attribute = new OptionAttribute(description);

            // Assert
            attribute.Description.Should().Be(description);
            attribute.Aliases.Should().NotBeNull();
            attribute.Aliases.Should().BeEmpty();
            attribute.Name.Should().BeNull();
        }

        /// <summary>
        /// Ensures a null description is accepted and stored as null without throwing.
        /// </summary>
        [TestMethod]
        public void Constructor_WithNullDescription_ShouldAcceptNull()
        {
            // Arrange
            string description = null;

            // Act
            var attribute = new OptionAttribute(description);

            // Assert
            attribute.Description.Should().BeNull();
            attribute.Aliases.Should().NotBeNull();
            attribute.Aliases.Should().BeEmpty();
        }

        /// <summary>
        /// Ensures an empty string description is accepted and preserved as empty.
        /// </summary>
        [TestMethod]
        public void Constructor_WithEmptyDescription_ShouldAcceptEmpty()
        {
            // Arrange
            var description = string.Empty;

            // Act
            var attribute = new OptionAttribute(description);

            // Assert
            attribute.Description.Should().BeEmpty();
            attribute.Aliases.Should().NotBeNull();
            attribute.Aliases.Should().BeEmpty();
        }

        #endregion

        #region Constructor Tests - Description with Aliases

        /// <summary>
        /// Verifies that providing a description and aliases initializes all values correctly.
        /// </summary>
        [TestMethod]
        public void Constructor_WithDescriptionAndAliases_ShouldInitializeCorrectly()
        {
            // Arrange
            var description = "Test option with aliases";
            var aliases = new[] { "v", "verbose" };

            // Act
            var attribute = new OptionAttribute(description, aliases);

            // Assert
            attribute.Description.Should().Be(description);
            attribute.Aliases.Should().BeEquivalentTo(aliases);
            attribute.Name.Should().BeNull();
        }

        /// <summary>
        /// Verifies that an empty alias array results in an empty alias collection.
        /// </summary>
        [TestMethod]
        public void Constructor_WithDescriptionAndEmptyAliases_ShouldHandleCorrectly()
        {
            // Arrange
            var description = "Test option";
            var aliases = new string[0];

            // Act
            var attribute = new OptionAttribute(description, aliases);

            // Assert
            attribute.Description.Should().Be(description);
            attribute.Aliases.Should().BeEmpty();
        }

        /// <summary>
        /// Ensures that passing null for aliases results in an empty alias collection (no exception).
        /// </summary>
        [TestMethod]
        public void Constructor_WithDescriptionAndNullAliases_ShouldHandleCorrectly()
        {
            // Arrange
            var description = "Test option";

            // Act
            var attribute = new OptionAttribute(description, (string[])null);

            // Assert
            attribute.Description.Should().Be(description);
            attribute.Aliases.Should().BeEmpty();
        }

        /// <summary>
        /// Confirms that multiple aliases are all stored without loss.
        /// </summary>
        [TestMethod]
        public void Constructor_WithMultipleAliases_ShouldStoreAllAliases()
        {
            // Arrange
            var description = "Test option";
            var aliases = new[] { "v", "verbose", "verb", "detail" };

            // Act
            var attribute = new OptionAttribute(description, aliases);

            // Assert
            attribute.Aliases.Should().HaveCount(4);
            attribute.Aliases.Should().Contain("v");
            attribute.Aliases.Should().Contain("verbose");
            attribute.Aliases.Should().Contain("verb");
            attribute.Aliases.Should().Contain("detail");
        }

        #endregion

        #region Property Tests

        /// <summary>
        /// Verifies that the mutable Name property can be set to a non-null value.
        /// </summary>
        [TestMethod]
        public void Name_Property_ShouldBeSettable()
        {
            // Arrange
            var attribute = new OptionAttribute("Test description");
            var name = "test-name";

            // Act
            attribute.Name = name;

            // Assert
            attribute.Name.Should().Be(name);
        }

        /// <summary>
        /// Verifies that the Name property accepts null.
        /// </summary>
        [TestMethod]
        public void Name_Property_ShouldAcceptNull()
        {
            // Arrange
            var attribute = new OptionAttribute("Test description");

            // Act
            attribute.Name = null;

            // Assert
            attribute.Name.Should().BeNull();
        }

        /// <summary>
        /// Verifies that the Name property accepts an empty string.
        /// </summary>
        [TestMethod]
        public void Name_Property_ShouldAcceptEmpty()
        {
            // Arrange
            var attribute = new OptionAttribute("Test description");

            // Act
            attribute.Name = string.Empty;

            // Assert
            attribute.Name.Should().BeEmpty();
        }

        /// <summary>
        /// Ensures the Description property is read-only (no setter).
        /// </summary>
        [TestMethod]
        public void Description_Property_ShouldBeReadOnly()
        {
            // Act & Assert
            typeof(OptionAttribute).GetProperty(nameof(OptionAttribute.Description)).CanWrite.Should().BeFalse();
        }

        /// <summary>
        /// Ensures the Aliases property is read-only (no setter).
        /// </summary>
        [TestMethod]
        public void Aliases_Property_ShouldBeReadOnly()
        {
            // Act & Assert
            typeof(OptionAttribute).GetProperty(nameof(OptionAttribute.Aliases)).CanWrite.Should().BeFalse();
        }

        /// <summary>
        /// Confirms that repeated access to Aliases returns the same reference (no defensive copy).
        /// </summary>
        [TestMethod]
        public void Aliases_ShouldReturnSameReference()
        {
            // Arrange
            var aliases = new[] { "alias1", "alias2" };
            var attribute = new OptionAttribute("desc", aliases);

            // Act
            var retrievedAliases1 = attribute.Aliases;
            var retrievedAliases2 = attribute.Aliases;

            // Assert
            ReferenceEquals(retrievedAliases1, retrievedAliases2).Should().BeTrue();
        }

        #endregion

        #region Edge Cases

        /// <summary>
        /// Verifies that whitespace-only descriptions are preserved verbatim.
        /// </summary>
        [TestMethod]
        public void Constructor_WithWhitespaceDescription_ShouldPreserveWhitespace()
        {
            // Arrange
            var description = "   \t\n  ";

            // Act
            var attribute = new OptionAttribute(description);

            // Assert
            attribute.Description.Should().Be("   \t\n  ");
        }

        /// <summary>
        /// Ensures that whitespace-only aliases are preserved as provided.
        /// </summary>
        [TestMethod]
        public void Constructor_WithWhitespaceAliases_ShouldPreserveWhitespace()
        {
            // Arrange
            var description = "Test";
            var aliases = new[] { " ", "\t", "\n" };

            // Act
            var attribute = new OptionAttribute(description, aliases);

            // Assert
            attribute.Aliases.Should().Contain(" ");
            attribute.Aliases.Should().Contain("\t");
            attribute.Aliases.Should().Contain("\n");
        }

        /// <summary>
        /// Validates handling of special characters in description and aliases.
        /// </summary>
        [TestMethod]
        public void Constructor_WithSpecialCharacters_ShouldHandleCorrectly()
        {
            // Arrange
            var description = "Option with special chars: !@#$%^&*()";
            var aliases = new[] { "@opt", "#option", "opt!" };

            // Act
            var attribute = new OptionAttribute(description, aliases);

            // Assert
            attribute.Description.Should().Be(description);
            attribute.Aliases.Should().Contain("@opt");
            attribute.Aliases.Should().Contain("#option");
            attribute.Aliases.Should().Contain("opt!");
        }

        /// <summary>
        /// Confirms unicode characters are accepted and preserved in description and aliases.
        /// </summary>
        [TestMethod]
        public void Constructor_WithUnicodeCharacters_ShouldHandleCorrectly()
        {
            // Arrange
            var description = "选项描述";
            var aliases = new[] { "选项", "🎯", "café" };

            // Act
            var attribute = new OptionAttribute(description, aliases);

            // Assert
            attribute.Description.Should().Be("选项描述");
            attribute.Aliases.Should().Contain("选项");
            attribute.Aliases.Should().Contain("🎯");
            attribute.Aliases.Should().Contain("café");
        }

        /// <summary>
        /// Ensures very long strings are stored without truncation or error.
        /// </summary>
        [TestMethod]
        public void Constructor_WithLongStrings_ShouldHandleCorrectly()
        {
            // Arrange
            var description = new string('a', 1000);
            var aliases = new[] { new string('b', 100), new string('c', 200) };

            // Act
            var attribute = new OptionAttribute(description, aliases);

            // Assert
            attribute.Description.Should().HaveLength(1000);
            attribute.Aliases[0].Should().HaveLength(100);
            attribute.Aliases[1].Should().HaveLength(200);
        }

        /// <summary>
        /// Verifies that duplicate aliases are preserved (no deduplication logic applied).
        /// </summary>
        [TestMethod]
        public void Constructor_WithDuplicateAliases_ShouldPreserveDuplicates()
        {
            // Arrange
            var description = "Test option";
            var aliases = new[] { "verbose", "v", "verbose", "detail", "v" };

            // Act
            var attribute = new OptionAttribute(description, aliases);

            // Assert
            attribute.Aliases.Should().HaveCount(5);
            attribute.Aliases.Should().Contain("verbose");
            attribute.Aliases.Should().Contain("v");
            attribute.Aliases.Should().Contain("detail");
        }

        #endregion

        #region Attribute Usage Tests

        /// <summary>
        /// Ensures the attribute is restricted to parameter targets as defined by <see cref="AttributeUsageAttribute"/>.
        /// </summary>
        [TestMethod]
        public void AttributeUsage_ShouldAllowParameterTarget()
        {
            // Arrange & Act
            var attributeUsage = typeof(OptionAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
                .Cast<AttributeUsageAttribute>().FirstOrDefault();

            // Assert
            attributeUsage.Should().NotBeNull();
            attributeUsage.ValidOn.Should().Be(AttributeTargets.Parameter);
        }

        #endregion

        #region Integration Tests

        /// <summary>
        /// Validates a full configuration scenario including description, aliases, and a custom name.
        /// </summary>
        [TestMethod]
        public void FullConfiguration_ShouldWorkCorrectly()
        {
            // Arrange
            var description = "Verbose output option";
            var aliases = new[] { "v", "verbose", "detail" };
            var name = "verbose-output";

            // Act
            var attribute = new OptionAttribute(description, aliases);
            attribute.Name = name;

            // Assert
            attribute.Description.Should().Be(description);
            attribute.Aliases.Should().BeEquivalentTo(aliases);
            attribute.Name.Should().Be(name);
        }

        /// <summary>
        /// Confirms that the Name property can be reassigned multiple times, including null.
        /// </summary>
        [TestMethod]
        public void Name_CanBeChangedMultipleTimes()
        {
            // Arrange
            var attribute = new OptionAttribute("Test description");

            // Act & Assert
            attribute.Name = "first-name";
            attribute.Name.Should().Be("first-name");

            attribute.Name = "second-name";
            attribute.Name.Should().Be("second-name");

            attribute.Name = null;
            attribute.Name.Should().BeNull();

            attribute.Name = "final-name";
            attribute.Name.Should().Be("final-name");
        }

        #endregion
    }
}