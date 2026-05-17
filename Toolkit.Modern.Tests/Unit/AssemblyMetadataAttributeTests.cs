using AwesomeAssertions;

namespace ByteForge.Toolkit.Tests.Unit
{
    [TestClass]
    [TestCategory("Unit")]
    public class AssemblyMetadataAttributeTests
    {
        /// <summary>
        /// Verifies that assembly developer metadata attributes preserve constructor values.
        /// </summary>
        [TestMethod]
        public void AssemblyDeveloperAttributes_ShouldExposeConfiguredValues()
        {
            new AssemblyDeveloperAttribute("Paulo Santos").Name.Should().Be("Paulo Santos");
            new AssemblyDeveloperCompanyAttribute("ByteForge, LLC.").Name.Should().Be("ByteForge, LLC.");
            new AssemblyCompanyUrlAttribute("https://byteforge.example").Url.Should().Be("https://byteforge.example");
            new AssemblyDeveloperCompanyUrlAttribute("https://dev.example").Url.Should().Be("https://dev.example");
        }
    }
}
