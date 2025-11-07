using AwesomeAssertions;
using ByteForge.Toolkit.Utils;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

namespace ByteForge.Toolkit.Tests.Unit.Utils
{
    /// <summary>
    /// Unit tests for <c>NameUtil</c> covering capitalization rules, particles, prefixes, hyphenation, complex names, and edge cases.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("Utils")]
    public class NameCapitalizerTests
    {
        #region CapitalizeName Tests

        /// <summary>
        /// Ensures <c>CapitalizeName</c> returns <c>null</c> when the input is <c>null</c>.
        /// </summary>
        [TestMethod]
        public void CapitalizeName_NullInput_ShouldReturnNull()
        {
            // Arrange
            string input = null;

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// Ensures an empty string input returns an empty string.
        /// </summary>
        [TestMethod]
        public void CapitalizeName_EmptyInput_ShouldReturnEmpty()
        {
            // Arrange
            var input = string.Empty;

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().BeEmpty();
        }

        /// <summary>
        /// Ensures whitespace-only input is returned unchanged.
        /// </summary>
        [TestMethod]
        public void CapitalizeName_WhitespaceInput_ShouldReturnWhitespace()
        {
            // Arrange
            var input = "   ";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("   ");
        }

        /// <summary>
        /// Ensures a simple lowercase name is capitalized.
        /// </summary>
        [TestMethod]
        public void CapitalizeName_SimpleName_ShouldCapitalizeCorrectly()
        {
            // Arrange
            var input = "john";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("John");
        }

        /// <summary>
        /// Ensures an all-uppercase name is normalized to proper case.
        /// </summary>
        [TestMethod]
        public void CapitalizeName_AllUppercase_ShouldNormalizeCorrectly()
        {
            // Arrange
            var input = "JOHN";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("John");
        }

        /// <summary>
        /// Ensures a mixed-case name is normalized to proper case.
        /// </summary>
        [TestMethod]
        public void CapitalizeName_MixedCase_ShouldNormalizeCorrectly()
        {
            // Arrange
            var input = "jOhN";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("John");
        }

        /// <summary>
        /// Ensures multiple internal spaces are collapsed appropriately and words capitalized.
        /// </summary>
        [TestMethod]
        public void CapitalizeName_MultipleSpaces_ShouldNormalizeSpacing()
        {
            // Arrange
            var input = "john    doe";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("John Doe");
        }

        /// <summary>
        /// Ensures leading and trailing spaces are trimmed and name is capitalized.
        /// </summary>
        [TestMethod]
        public void CapitalizeName_LeadingTrailingSpaces_ShouldTrimAndCapitalize()
        {
            // Arrange
            var input = "  john doe  ";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("John Doe");
        }

        #endregion

        #region Particle Tests

        /// <summary>
        /// Ensures lowercase particles in the middle (e.g., 'de la') remain lowercase while surrounding names are capitalized.
        /// </summary>
        [TestMethod]
        public void CapitalizeName_ParticleInMiddle_ShouldKeepLowercase()
        {
            // Arrange
            var input = "maria de la cruz";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("Maria de la Cruz");
        }

        /// <summary>
        /// Ensures a leading particle is capitalized if at the start.
        /// </summary>
        [TestMethod]
        public void CapitalizeName_ParticleAtStart_ShouldCapitalize()
        {
            // Arrange
            var input = "de la cruz";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("De la Cruz");
        }

        /// <summary>
        /// Ensures 'van der' particles in the middle are preserved in lowercase.
        /// </summary>
        [TestMethod]
        public void CapitalizeName_VanParticle_ShouldKeepLowercase()
        {
            // Arrange
            var input = "jan van der berg";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("Jan van der Berg");
        }

        /// <summary>
        /// Ensures 'von' particle is preserved in lowercase when inside the name.
        /// </summary>
        [TestMethod]
        public void CapitalizeName_VonParticle_ShouldKeepLowercase()
        {
            // Arrange
            var input = "ludwig von beethoven";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("Ludwig von Beethoven");
        }

        /// <summary>
        /// Ensures multiple differing particles are handled correctly with proper casing.
        /// </summary>
        [TestMethod]
        public void CapitalizeName_MultipleDifferentParticles_ShouldHandleCorrectly()
        {
            // Arrange
            var input = "pedro del rio y garcia";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("Pedro del Rio y Garcia");
        }

        #endregion

        #region Mc/Mac Prefix Tests

        /// <summary>
        /// Ensures 'Mc' prefix capitalizes the following letter (e.g., McDonald).
        /// </summary>
        [TestMethod]
        public void CapitalizeName_McPrefix_ShouldCapitalizeCorrectly()
        {
            // Arrange
            var input = "mcdonald";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("McDonald");
        }

        /// <summary>
        /// Ensures 'Mac' prefix capitalizes the next portion (e.g., MacArthur).
        /// </summary>
        [TestMethod]
        public void CapitalizeName_MacPrefix_ShouldCapitalizeCorrectly()
        {
            // Arrange
            var input = "macarthur";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("MacArthur");
        }

        /// <summary>
        /// Ensures known 'Mc' exceptions do not capitalize the third letter (e.g., Mckay).
        /// </summary>
        [TestMethod]
        public void CapitalizeName_McPrefixException_ShouldNotCapitalizeSecondLetter()
        {
            // Arrange
            var input = "mckay";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("Mckay");
        }

        /// <summary>
        /// Ensures known 'Mac' exceptions do not force extra capitalization (e.g., Mackenzie).
        /// </summary>
        [TestMethod]
        public void CapitalizeName_MacPrefixException_ShouldNotCapitalizeSecondLetter()
        {
            // Arrange
            var input = "mackenzie";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("Mackenzie");
        }

        /// <summary>
        /// Ensures 'mc' alone is capitalized properly.
        /// </summary>
        [TestMethod]
        public void CapitalizeName_McOnly_ShouldReturnMc()
        {
            // Arrange
            var input = "mc";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("Mc");
        }

        /// <summary>
        /// Ensures 'mac' alone is capitalized properly.
        /// </summary>
        [TestMethod]
        public void CapitalizeName_MacOnly_ShouldReturnMac()
        {
            // Arrange
            var input = "mac";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("Mac");
        }

        #endregion

        #region O' and D' Prefix Tests

        /// <summary>
        /// Ensures O' prefix is properly capitalized (e.g., O'Connor).
        /// </summary>
        [TestMethod]
        public void CapitalizeName_OApostrophePrefix_ShouldCapitalizeCorrectly()
        {
            // Arrange
            var input = "o'connor";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("O'Connor");
        }

        /// <summary>
        /// Ensures D' prefix is properly capitalized (e.g., D'Angelo).
        /// </summary>
        [TestMethod]
        public void CapitalizeName_DApostrophePrefix_ShouldCapitalizeCorrectly()
        {
            // Arrange
            var input = "d'angelo";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("D'Angelo");
        }

        /// <summary>
        /// Ensures prefix-only O' form is capitalized correctly.
        /// </summary>
        [TestMethod]
        public void CapitalizeName_OApostropheOnly_ShouldReturnOApostrophe()
        {
            // Arrange
            var input = "o'";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("O'");
        }

        /// <summary>
        /// Ensures prefix-only D' form is capitalized correctly.
        /// </summary>
        [TestMethod]
        public void CapitalizeName_DApostropheOnly_ShouldReturnDApostrophe()
        {
            // Arrange
            var input = "d'";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("D'");
        }

        #endregion

        #region Hyphenated Name Tests

        /// <summary>
        /// Ensures a hyphenated name capitalizes each segment.
        /// </summary>
        [TestMethod]
        public void CapitalizeName_HyphenatedName_ShouldCapitalizeBothParts()
        {
            // Arrange
            var input = "jean-claude";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("Jean-Claude");
        }

        /// <summary>
        /// Ensures multiple hyphenated segments are all capitalized.
        /// </summary>
        [TestMethod]
        public void CapitalizeName_MultipleHyphens_ShouldCapitalizeAllParts()
        {
            // Arrange
            var input = "anne-marie-louise";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("Anne-Marie-Louise");
        }

        /// <summary>
        /// Ensures hyphenated first part with trailing particles is handled correctly.
        /// </summary>
        [TestMethod]
        public void CapitalizeName_HyphenatedWithParticle_ShouldHandleCorrectly()
        {
            // Arrange
            var input = "marie-christine de la tour";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("Marie-Christine de la Tour");
        }

        #endregion

        #region Complex Name Tests

        /// <summary>
        /// Ensures a complex name with apostrophes, hyphens, prefixes, and particles is processed correctly.
        /// </summary>
        [TestMethod]
        public void CapitalizeName_ComplexNameWithMultipleFeatures_ShouldHandleCorrectly()
        {
            // Arrange
            var input = "sean o'connor-mcdonald de la cruz";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("Sean O'Connor-mcdonald de la Cruz");
        }

        /// <summary>
        /// Ensures real-world uppercase multi-particle example is normalized.
        /// </summary>
        [TestMethod]
        public void CapitalizeName_RealWorldExample1_ShouldHandleCorrectly()
        {
            // Arrange
            var input = "JOHN VAN DER BERG";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("John van der Berg");
        }

        /// <summary>
        /// Ensures lowercase multi-word particle sequence is handled (capitalizing internal 'Los').
        /// </summary>
        [TestMethod]
        public void CapitalizeName_RealWorldExample2_ShouldHandleCorrectly()
        {
            // Arrange
            var input = "maria de los angeles";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("Maria de Los Angeles");
        }

        #endregion

        #region CapitalizeFullName Tests

        /// <summary>
        /// Ensures <c>CapitalizeFullName</c> returns <c>null</c> for <c>null</c> input.
        /// </summary>
        [TestMethod]
        public void CapitalizeFullName_NullInput_ShouldReturnNull()
        {
            // Arrange
            string input = null;

            // Act
            var result = NameUtil.CapitalizeFullName(input);

            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// Ensures empty string input returns empty for full name capitalization.
        /// </summary>
        [TestMethod]
        public void CapitalizeFullName_EmptyInput_ShouldReturnEmpty()
        {
            // Arrange
            var input = string.Empty;

            // Act
            var result = NameUtil.CapitalizeFullName(input);

            // Assert
            result.Should().BeEmpty();
        }

        /// <summary>
        /// Ensures a simple full name without commas is capitalized properly.
        /// </summary>
        [TestMethod]
        public void CapitalizeFullName_SimpleNameWithoutComma_ShouldCapitalizeCorrectly()
        {
            // Arrange
            var input = "john doe";

            // Act
            var result = NameUtil.CapitalizeFullName(input);

            // Assert
            result.Should().Be("John Doe");
        }

        /// <summary>
        /// Ensures comma-delimited names are capitalized segment-wise and commas preserved (no spaces kept).
        /// </summary>
        [TestMethod]
        public void CapitalizeFullName_NameWithComma_ShouldCapitalizeEachPart()
        {
            // Arrange
            var input = "smith, john";

            // Act
            var result = NameUtil.CapitalizeFullName(input);

            // Assert
            result.Should().Be("Smith,John");
        }

        /// <summary>
        /// Ensures comma-delimited names with trailing particles retain particle casing rules.
        /// </summary>
        [TestMethod]
        public void CapitalizeFullName_NameWithCommaAndSpaces_ShouldCapitalizeAndPreserveFormat()
        {
            // Arrange
            var input = "smith, john de la";

            // Act
            var result = NameUtil.CapitalizeFullName(input);

            // Assert
            result.Should().Be("Smith,John de la");
        }

        /// <summary>
        /// Ensures multiple comma-separated segments are all capitalized.
        /// </summary>
        [TestMethod]
        public void CapitalizeFullName_MultipleCommas_ShouldHandleAllSegments()
        {
            // Arrange
            var input = "smith, john, jr";

            // Act
            var result = NameUtil.CapitalizeFullName(input);

            // Assert
            result.Should().Be("Smith,John,Jr");
        }

        /// <summary>
        /// Ensures names with Mc prefix in comma format are handled correctly.
        /// </summary>
        [TestMethod]
        public void CapitalizeFullName_CommaWithMcPrefix_ShouldHandleCorrectly()
        {
            // Arrange
            var input = "mcdonald, sean";

            // Act
            var result = NameUtil.CapitalizeFullName(input);

            // Assert
            result.Should().Be("McDonald,Sean");
        }

        /// <summary>
        /// Ensures names with particles after a comma are capitalized correctly.
        /// </summary>
        [TestMethod]
        public void CapitalizeFullName_CommaWithParticles_ShouldHandleCorrectly()
        {
            // Arrange
            var input = "cruz, maria de la";

            // Act
            var result = NameUtil.CapitalizeFullName(input);

            // Assert
            result.Should().Be("Cruz,Maria de la");
        }

        #endregion

        #region Edge Cases

        /// <summary>
        /// Ensures a single character is capitalized.
        /// </summary>
        [TestMethod]
        public void CapitalizeName_SingleCharacter_ShouldCapitalize()
        {
            // Arrange
            var input = "a";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("A");
        }

        /// <summary>
        /// Ensures alphanumeric names maintain digits while capitalizing leading alpha portion.
        /// </summary>
        [TestMethod]
        public void CapitalizeName_NumbersAndSpecialChars_ShouldHandleGracefully()
        {
            // Arrange
            var input = "john123";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("John123");
        }

        /// <summary>
        /// Ensures a single hyphen is returned unchanged.
        /// </summary>
        [TestMethod]
        public void CapitalizeName_OnlyHyphen_ShouldReturnHyphen()
        {
            // Arrange
            var input = "-";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("-");
        }

        /// <summary>
        /// Ensures trailing hyphen with empty second segment is preserved.
        /// </summary>
        [TestMethod]
        public void CapitalizeName_EmptyHyphenatedPart_ShouldHandleGracefully()
        {
            // Arrange
            var input = "john-";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("John-");
        }

        /// <summary>
        /// Ensures consecutive hyphens are preserved while capitalizing valid segments.
        /// </summary>
        [TestMethod]
        public void CapitalizeName_ConsecutiveHyphens_ShouldHandleGracefully()
        {
            // Arrange
            var input = "john--marie";

            // Act
            var result = NameUtil.CapitalizeName(input);

            // Assert
            result.Should().Be("John--Marie");
        }

        #endregion
    }
}