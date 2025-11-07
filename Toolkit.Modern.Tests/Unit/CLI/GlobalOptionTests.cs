using AwesomeAssertions;
using ByteForge.Toolkit.CommandLine;

namespace ByteForge.Toolkit.Tests.Unit.CLI
{
    /// <summary>
    /// Unit tests validating construction, alias generation, matching logic, and edge cases
    /// for the <see cref="GlobalOption"/> command-line abstraction.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("CLI")]
    public class GlobalOptionTests
    {
        #region Constructor Tests - Action without Value

        /// <summary>
        /// Verifies that constructing a <see cref="GlobalOption"/> without a value action
        /// initializes expected defaults and executes the provided action.
        /// </summary>
        [TestMethod]
        public void Constructor_WithActionNoValue_ShouldInitializeCorrectly()
        {
            // Arrange
            var name = "help";
            var description = "Show help information";
            var actionExecuted = false;
            Action action = () => actionExecuted = true;

            // Act
            var option = new GlobalOption(name, description, action);

            // Assert
            option.Name.Should().Be(name);
            option.Description.Should().Be(description);
            option.ExpectsValue.Should().BeFalse();
            option.CustomAliases.Should().BeEmpty();
            option.Action.Should().NotBeNull();
            
            // Test action execution
            option.Action(null);
            actionExecuted.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that custom aliases are preserved when constructing an option without a value.
        /// </summary>
        [TestMethod]
        public void Constructor_WithActionNoValueAndAliases_ShouldInitializeCorrectly()
        {
            // Arrange
            var name = "verbose";
            var description = "Enable verbose output";
            var aliases = new[] { "v", "detail" };
            var actionExecuted = false;
            Action action = () => actionExecuted = true;

            // Act
            var option = new GlobalOption(name, description, action, false, aliases);

            // Assert
            option.Name.Should().Be(name);
            option.Description.Should().Be(description);
            option.ExpectsValue.Should().BeFalse();
            option.CustomAliases.Should().BeEquivalentTo(aliases);
        }

        #endregion

        #region Constructor Tests - Action with Value

        /// <summary>
        /// Ensures that a value-accepting action is registered and executed with the expected value.
        /// </summary>
        [TestMethod]
        public void Constructor_WithActionWithValue_ShouldInitializeCorrectly()
        {
            // Arrange
            var name = "output";
            var description = "Output file path";
            string receivedValue = null;
            Action<string> action = value => receivedValue = value;

            // Act
            var option = new GlobalOption(name, description, action);

            // Assert
            option.Name.Should().Be(name);
            option.Description.Should().Be(description);
            option.ExpectsValue.Should().BeTrue();
            option.CustomAliases.Should().BeEmpty();
            
            // Test action execution
            option.Action("test.txt");
            receivedValue.Should().Be("test.txt");
        }

        /// <summary>
        /// Verifies construction with value action and custom aliases.
        /// </summary>
        [TestMethod]
        public void Constructor_WithActionWithValueAndAliases_ShouldInitializeCorrectly()
        {
            // Arrange
            var name = "config";
            var description = "Configuration file";
            var aliases = new[] { "c", "cfg" };
            string receivedValue = null;
            Action<string> action = value => receivedValue = value;

            // Act
            var option = new GlobalOption(name, description, action, true, aliases);

            // Assert
            option.Name.Should().Be(name);
            option.Description.Should().Be(description);
            option.ExpectsValue.Should().BeTrue();
            option.CustomAliases.Should().BeEquivalentTo(aliases);
        }

        #endregion

        #region Name Normalization Tests

        /// <summary>
        /// Ensures leading dashes are stripped from the provided name.
        /// </summary>
        [TestMethod]
        public void Constructor_WithDashPrefixedName_ShouldStripDashes()
        {
            // Arrange
            var name = "--help";
            var description = "Show help";
            Action action = () => { };

            // Act
            var option = new GlobalOption(name, description, action);

            // Assert
            option.Name.Should().Be("help");
        }

        /// <summary>
        /// Ensures leading slashes are stripped from the provided name.
        /// </summary>
        [TestMethod]
        public void Constructor_WithSlashPrefixedName_ShouldStripSlashes()
        {
            // Arrange
            var name = "/verbose";
            var description = "Verbose output";
            Action action = () => { };

            // Act
            var option = new GlobalOption(name, description, action);

            // Assert
            option.Name.Should().Be("verbose");
        }

        /// <summary>
        /// Ensures mixed prefix characters are completely removed from the name.
        /// </summary>
        [TestMethod]
        public void Constructor_WithMixedPrefixes_ShouldStripAllPrefixes()
        {
            // Arrange
            var name = "--/config";
            var description = "Config file";
            Action action = () => { };

            // Act
            var option = new GlobalOption(name, description, action);

            // Assert
            option.Name.Should().Be("config");
        }

        #endregion

        #region Validation Tests

        /// <summary>
        /// Validates that a null name throws an <see cref="ArgumentException"/>.
        /// </summary>
        [TestMethod]
        public void Constructor_WithNullName_ShouldThrowArgumentException()
        {
            // Arrange
            string name = null;
            var description = "Test description";
            Action action = () => { };

            // Act & Assert
            Action act = () => new GlobalOption(name, description, action);
            act.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// Validates that an empty string name throws an <see cref="ArgumentException"/>.
        /// </summary>
        [TestMethod]
        public void Constructor_WithEmptyName_ShouldThrowArgumentException()
        {
            // Arrange
            var name = string.Empty;
            var description = "Test description";
            Action action = () => { };

            // Act & Assert
            Action act = () => new GlobalOption(name, description, action);
            act.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// Validates that a whitespace-only name throws an <see cref="ArgumentException"/>.
        /// </summary>
        [TestMethod]
        public void Constructor_WithWhitespaceName_ShouldThrowArgumentException()
        {
            // Arrange
            var name = "   ";
            var description = "Test description";
            Action action = () => { };

            // Act & Assert
            Action act = () => new GlobalOption(name, description, action);
            act.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// Validates that a null description throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [TestMethod]
        public void Constructor_WithNullDescription_ShouldThrowArgumentNullException()
        {
            // Arrange
            var name = "test";
            string description = null;
            Action action = () => { };

            // Act & Assert
            Action act = () => new GlobalOption(name, description, action);
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Validates that a null non-value action throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [TestMethod]
        public void Constructor_WithNullAction_ShouldThrowArgumentNullException()
        {
            // Arrange
            var name = "test";
            var description = "Test description";
            Action action = null;

            // Act & Assert
            Action act = () => new GlobalOption(name, description, action);
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Validates that a null value action throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [TestMethod]
        public void Constructor_WithNullActionWithValue_ShouldThrowArgumentNullException()
        {
            // Arrange
            var name = "test";
            var description = "Test description";
            Action<string> action = null;

            // Act & Assert
            Action act = () => new GlobalOption(name, description, action);
            act.Should().Throw<ArgumentNullException>();
        }

        #endregion

        #region Alias Generation Tests

        /// <summary>
        /// Ensures basic aliases are generated for a simple name.
        /// </summary>
        [TestMethod]
        public void GenerateAliases_WithSimpleName_ShouldCreateExpectedAliases()
        {
            // Arrange
            var option = new GlobalOption("help", "Show help", () => { });
            var usedNames = new HashSet<string>();

            // Act
            option.GenerateAliases(usedNames);

            // Assert
            option.AllAliases.Should().NotBeEmpty();
            option.AllAliases.Should().Contain("--help");
            option.AllAliases.Should().Contain("-h");
            option.AllAliases.Should().Contain("/h");
        }

        /// <summary>
        /// Ensures shortened forms are generated for longer names.
        /// </summary>
        [TestMethod]
        public void GenerateAliases_WithLongName_ShouldCreateShortAliases()
        {
            // Arrange
            var option = new GlobalOption("verbose", "Verbose output", () => { });
            var usedNames = new HashSet<string>();

            // Act
            option.GenerateAliases(usedNames);

            // Assert
            option.AllAliases.Should().Contain("--verbose");
            option.AllAliases.Should().Contain("--ver");
            option.AllAliases.Should().Contain("-v");
            option.AllAliases.Should().Contain("/v");
        }

        /// <summary>
        /// Verifies custom aliases are included among generated aliases.
        /// </summary>
        [TestMethod]
        public void GenerateAliases_WithCustomAliases_ShouldIncludeCustomAliases()
        {
            // Arrange
            var option = new GlobalOption("help", "Show help", () => { }, false, "?", "info");
            var usedNames = new HashSet<string>();

            // Act
            option.GenerateAliases(usedNames);

            // Assert
            option.AllAliases.Should().Contain("--?");
            option.AllAliases.Should().Contain("--info");
        }

        /// <summary>
        /// Ensures conflicting aliases (already used) are excluded from the final alias set.
        /// </summary>
        [TestMethod]
        public void GenerateAliases_WithConflictingNames_ShouldExcludeConflicts()
        {
            // Arrange
            var option = new GlobalOption("help", "Show help", () => { });
            var usedNames = new HashSet<string> { "--help", "-h" };

            // Act
            option.GenerateAliases(usedNames);

            // Assert
            option.AllAliases.Should().NotContain("--help");
            option.AllAliases.Should().NotContain("-h");
            option.AllAliases.Should().Contain("/h"); // Non-conflicting
        }

        #endregion

        #region Matching Tests

        /// <summary>
        /// Validates that matching fails before alias generation.
        /// </summary>
        [TestMethod]
        public void Matches_BeforeAliasGeneration_ShouldReturnFalse()
        {
            // Arrange
            var option = new GlobalOption("help", "Show help", () => { });

            // Act & Assert
            option.Matches("--help").Should().BeFalse();
            option.Matches("-h").Should().BeFalse();
        }

        /// <summary>
        /// Validates that known aliases match after alias generation.
        /// </summary>
        [TestMethod]
        public void Matches_AfterAliasGeneration_ShouldMatchCorrectly()
        {
            // Arrange
            var option = new GlobalOption("help", "Show help", () => { });
            var usedNames = new HashSet<string>();
            option.GenerateAliases(usedNames);

            // Act & Assert
            option.Matches("--help").Should().BeTrue();
            option.Matches("-h").Should().BeTrue();
            option.Matches("/h").Should().BeTrue();
            option.Matches("--invalid").Should().BeFalse();
        }

        /// <summary>
        /// Ensures matching logic is case-insensitive across aliases.
        /// </summary>
        [TestMethod]
        public void Matches_ShouldBeCaseInsensitive()
        {
            // Arrange
            var option = new GlobalOption("help", "Show help", () => { });
            var usedNames = new HashSet<string>();
            option.GenerateAliases(usedNames);

            // Act & Assert
            option.Matches("--HELP").Should().BeTrue();
            option.Matches("-H").Should().BeTrue();
            option.Matches("/H").Should().BeTrue();
        }

        #endregion

        #region Edge Cases

        /// <summary>
        /// Ensures an empty description is permitted.
        /// </summary>
        [TestMethod]
        public void Constructor_WithEmptyDescription_ShouldAcceptEmpty()
        {
            // Arrange
            var name = "test";
            var description = string.Empty;
            Action action = () => { };

            // Act
            var option = new GlobalOption(name, description, action);

            // Assert
            option.Description.Should().BeEmpty();
        }

        /// <summary>
        /// Ensures null aliases parameter results in an empty alias list.
        /// </summary>
        [TestMethod]
        public void Constructor_WithNullAliases_ShouldCreateEmptyArray()
        {
            // Arrange
            var name = "test";
            var description = "Test";
            Action action = () => { };

            // Act
            var option = new GlobalOption(name, description, action, false, null);

            // Assert
            option.CustomAliases.Should().NotBeNull();
            option.CustomAliases.Should().BeEmpty();
        }

        /// <summary>
        /// Ensures extremely short names are retained unmodified.
        /// </summary>
        [TestMethod]
        public void Constructor_WithShortName_ShouldHandleCorrectly()
        {
            // Arrange
            var name = "a";
            var description = "Short option";
            Action action = () => { };

            // Act
            var option = new GlobalOption(name, description, action);

            // Assert
            option.Name.Should().Be("a");
        }

        /// <summary>
        /// Ensures aliases for short names are generated correctly.
        /// </summary>
        [TestMethod]
        public void GenerateAliases_WithShortName_ShouldCreateCorrectAliases()
        {
            // Arrange
            var option = new GlobalOption("ab", "Short name", () => { });
            var usedNames = new HashSet<string>();

            // Act
            option.GenerateAliases(usedNames);

            // Assert
            option.AllAliases.Should().Contain("--ab");
            option.AllAliases.Should().Contain("-a");
            option.AllAliases.Should().Contain("/a");
        }

        #endregion

        #region Integration Tests

        /// <summary>
        /// Executes an end-to-end workflow: construction, alias generation, matching, and action invocation.
        /// </summary>
        [TestMethod]
        public void FullWorkflow_ShouldWorkCorrectly()
        {
            // Arrange
            var actionExecuted = false;
            var receivedValue = string.Empty;
            Action<string> action = value =>
            {
                actionExecuted = true;
                receivedValue = value;
            };

            var option = new GlobalOption("config", "Configuration file", action, true, "c", "cfg");
            var usedNames = new HashSet<string>();

            // Act
            option.GenerateAliases(usedNames);
            var matches = option.Matches("--config");
            option.Action("test.config");

            // Assert
            matches.Should().BeTrue();
            actionExecuted.Should().BeTrue();
            receivedValue.Should().Be("test.config");
            option.ExpectsValue.Should().BeTrue();
        }

        #endregion
    }
}