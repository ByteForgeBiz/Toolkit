using AwesomeAssertions;
using ByteForge.Toolkit.CLI;

namespace ByteForge.Toolkit.Tests.Unit.CLI
{
    /// <summary>
    /// Test suite validating <see cref="CommandAttribute"/> construction, property behaviors,
    /// edge cases, and attribute usage declarations.
    /// </summary>
    /// <remarks>
    /// Pseudocode Plan:
    /// <list type="number">
    /// <item>
    /// Constructor Tests:<br/>
    ///    - Validate property assignment with standard inputs.<br/>
    ///    - Validate no-alias constructor path (empty array expected).<br/>
    ///    - Accept null description.<br/>
    ///    - Accept null name.<br/>
    ///    - Accept empty strings (no normalization).<br/>
    ///    - Preserve all provided aliases including multiple entries.
    /// </item>
    /// <item>
    /// Property Tests:<br/>
    ///    - Ensure properties are read-only via reflection (no public setters).<br/>
    ///    - Ensure alias array reference is stable (no defensive copy exposed on each access).
    /// </item>
    /// <item>
    /// Edge Cases:<br/>
    ///    - Whitespace-only strings preserved verbatim.<br/>
    ///    - Special characters accepted unchanged.<br/>
    ///    - Unicode characters (including emoji and non-Latin scripts) retained.<br/>
    ///    - Long string inputs handled without truncation.
    /// </item>
    /// <item>
    /// Attribute Usage:<br/>
    ///    - Confirm attribute can target classes.<br/>
    ///    - Confirm attribute can also target methods.
    /// </item>
    /// </list>
    /// </remarks>
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("CLI")]
    public class CommandAttributeTests
    {
        #region Constructor Tests

        /// <summary>
        /// Verifies that providing description, name, and aliases initializes corresponding properties.
        /// </summary>
        [TestMethod]
        public void Constructor_WithValidParameters_ShouldInitializeProperties()
        {
            // Arrange
            var description = "Test command description";
            var name = "test-command";
            var aliases = new[] { "tc", "test" };

            // Act
            var attribute = new CommandAttribute(name, description, aliases);

            // Assert
            attribute.Description.Should().Be(description);
            attribute.Name.Should().Be(name);
            attribute.Aliases.Should().BeEquivalentTo(aliases);
        }

        /// <summary>
        /// Verifies that omitting aliases results in a non-null, empty alias collection.
        /// </summary>
        [TestMethod]
        public void Constructor_WithoutAliases_ShouldInitializeWithEmptyAliases()
        {
            // Arrange
            var description = "Test command description";
            var name = "test-command";

            // Act
            var attribute = new CommandAttribute(name, description);

            // Assert
            attribute.Description.Should().Be(description);
            attribute.Name.Should().Be(name);
            attribute.Aliases.Should().NotBeNull();
            attribute.Aliases.Should().BeEmpty();
        }

        /// <summary>
        /// Ensures that a null description is accepted and stored as null.
        /// </summary>
        [TestMethod]
        public void Constructor_WithNullDescription_ShouldAcceptNull()
        {
            // Arrange
            string description = null;
            var name = "test-command";

            // Act
            var attribute = new CommandAttribute(name, description);

            // Assert
            attribute.Description.Should().BeNull();
            attribute.Name.Should().Be(name);
        }

        /// <summary>
        /// Ensures that a null name is accepted and stored as null.
        /// </summary>
        [TestMethod]
        public void Constructor_WithNullName_ShouldAcceptNull()
        {
            // Arrange
            var description = "Test description";
            string name = null;

            // Act
            var attribute = new CommandAttribute(name, description);

            // Assert
            attribute.Description.Should().Be(description);
            attribute.Name.Should().BeNull();
        }

        /// <summary>
        /// Verifies that empty strings are preserved (not normalized to null).
        /// </summary>
        [TestMethod]
        public void Constructor_WithEmptyStrings_ShouldAcceptEmptyStrings()
        {
            // Arrange
            var description = string.Empty;
            var name = string.Empty;

            // Act
            var attribute = new CommandAttribute(name, description);

            // Assert
            attribute.Description.Should().BeEmpty();
            attribute.Name.Should().BeEmpty();
        }

        /// <summary>
        /// Ensures that all provided aliases are stored without omission.
        /// </summary>
        [TestMethod]
        public void Constructor_WithMultipleAliases_ShouldStoreAllAliases()
        {
            // Arrange
            var description = "Test command";
            var name = "test";
            var aliases = new[] { "t", "tst", "testing", "test-cmd" };

            // Act
            var attribute = new CommandAttribute(name, description, aliases);

            // Assert
            attribute.Aliases.Should().HaveCount(4);
            attribute.Aliases.Should().Contain("t");
            attribute.Aliases.Should().Contain("tst");
            attribute.Aliases.Should().Contain("testing");
            attribute.Aliases.Should().Contain("test-cmd");
        }

        #endregion

        #region Property Tests

        /// <summary>
        /// Uses reflection to verify that the public properties are read-only (no setters).
        /// </summary>
        [TestMethod]
        public void Properties_ShouldBeReadOnly()
        {
            // Arrange
            var attribute = new CommandAttribute("desc", "name", "alias1", "alias2");

            // Act & Assert
            typeof(CommandAttribute).GetProperty(nameof(CommandAttribute.Name)).CanWrite.Should().BeFalse();
            typeof(CommandAttribute).GetProperty(nameof(CommandAttribute.Description)).CanWrite.Should().BeFalse();
            typeof(CommandAttribute).GetProperty(nameof(CommandAttribute.Aliases)).CanWrite.Should().BeFalse();
        }

        /// <summary>
        /// Ensures repeated access to <see cref="CommandAttribute.Aliases"/> returns the same reference,
        /// indicating it is not copied on each call.
        /// </summary>
        [TestMethod]
        public void Aliases_ShouldReturnSameReference()
        {
            // Arrange
            var aliases = new[] { "alias1", "alias2" };
            var attribute = new CommandAttribute("desc", "name", aliases);

            // Act
            var retrievedAliases1 = attribute.Aliases;
            var retrievedAliases2 = attribute.Aliases;

            // Assert
            ReferenceEquals(retrievedAliases1, retrievedAliases2).Should().BeTrue();
        }

        #endregion

        #region Edge Cases

        /// <summary>
        /// Verifies that whitespace-only strings are preserved as-is (no trimming).
        /// </summary>
        [TestMethod]
        public void Constructor_WithWhitespaceStrings_ShouldPreserveWhitespace()
        {
            // Arrange
            var description = "   ";
            var name = "\t\n";
            var aliases = new[] { " ", "\t" };

            // Act
            var attribute = new CommandAttribute(name, description, aliases);

            // Assert
            attribute.Description.Should().Be("   ");
            attribute.Name.Should().Be("\t\n");
            attribute.Aliases.Should().Contain(" ");
            attribute.Aliases.Should().Contain("\t");
        }

        /// <summary>
        /// Ensures that special characters are accepted without alteration.
        /// </summary>
        [TestMethod]
        public void Constructor_WithSpecialCharacters_ShouldHandleCorrectly()
        {
            // Arrange
            var description = "Command with special chars: !@#$%^&*()";
            var name = "special-command!";
            var aliases = new[] { "@special", "#cmd" };

            // Act
            var attribute = new CommandAttribute(name, description, aliases);

            // Assert
            attribute.Description.Should().Be(description);
            attribute.Name.Should().Be(name);
            attribute.Aliases.Should().Contain("@special");
            attribute.Aliases.Should().Contain("#cmd");
        }

        /// <summary>
        /// Ensures that Unicode (including emoji and multi-language text) is preserved.
        /// </summary>
        [TestMethod]
        public void Constructor_WithUnicodeCharacters_ShouldHandleCorrectly()
        {
            // Arrange
            var description = "测试命令";
            var name = "тест";
            var aliases = new[] { "🚀", "café" };

            // Act
            var attribute = new CommandAttribute(name, description, aliases);

            // Assert
            attribute.Description.Should().Be("测试命令");
            attribute.Name.Should().Be("тест");
            attribute.Aliases.Should().Contain("🚀");
            attribute.Aliases.Should().Contain("café");
        }

        /// <summary>
        /// Verifies that very long string inputs are stored fully (no truncation or failure).
        /// </summary>
        [TestMethod]
        public void Constructor_WithLongStrings_ShouldHandleCorrectly()
        {
            // Arrange
            var description = new string('a', 1000);
            var name = new string('b', 500);
            var aliases = new[] { new string('c', 100), new string('d', 200) };

            // Act
            var attribute = new CommandAttribute(name, description, aliases);

            // Assert
            attribute.Description.Should().HaveLength(1000);
            attribute.Name.Should().HaveLength(500);
            attribute.Aliases[0].Should().HaveLength(100);
            attribute.Aliases[1].Should().HaveLength(200);
        }

        #endregion

        #region Attribute Usage Tests

        /// <summary>
        /// Confirms <see cref="CommandAttribute"/> is valid on classes.
        /// </summary>
        [TestMethod]
        public void AttributeUsage_ShouldAllowClassTarget()
        {
            // Arrange & Act
            var attributeUsage = typeof(CommandAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
                .Cast<AttributeUsageAttribute>().FirstOrDefault();

            // Assert
            attributeUsage.Should().NotBeNull();
            (attributeUsage.ValidOn & AttributeTargets.Class).Should().Be(AttributeTargets.Class);
        }

        /// <summary>
        /// Confirms <see cref="CommandAttribute"/> is valid on methods.
        /// </summary>
        [TestMethod]
        public void AttributeUsage_ShouldAllowMethodTarget()
        {
            // Arrange & Act
            var attributeUsage = typeof(CommandAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
                .Cast<AttributeUsageAttribute>().FirstOrDefault();

            // Assert
            attributeUsage.Should().NotBeNull();
            (attributeUsage.ValidOn & AttributeTargets.Method).Should().Be(AttributeTargets.Method);
        }

        #endregion
    }
}