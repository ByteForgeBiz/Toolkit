using AwesomeAssertions;
using ByteForge.Toolkit.Configuration;
using ByteForge.Toolkit.Tests.Helpers;
using System.ComponentModel;

namespace ByteForge.Toolkit.Tests.Unit.Configuration
{
    /// <summary>
    /// Tests Toolkit INI documentation preservation behavior.
    /// </summary>
    [TestClass]
    public class ConfigurationDocumentationTests
    {
        /// <summary>
        /// Cleans up temporary files after each test.
        /// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
            TestConfigurationHelper.CleanupTempFiles();
        }

        /// <summary>
        /// Verifies that documentation attached to default-valued keys is removed with those keys during save.
        /// </summary>
        [TestMethod]
        public void Save_WithDocumentedDefaultValues_ShouldNotLeaveDetachedDocBlocks()
        {
            var path = TestConfigurationHelper.CreateTempConfigFile(@"[Docs]
;;;
; Doc 1
Property1=DefaultValue
;;;
; Doc 2
Property2=DefaultValue
;;;
; Doc 3
Property3=DefaultValue
OtherProperty=Non-default-value");
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();

            config.Initialize(path);
            config.GetSection<DefaultDocumentedConfig>("Docs");
            config.Save();

            var saved = File.ReadAllText(path);
            saved.Should().NotContain("Doc 1");
            saved.Should().NotContain("Doc 2");
            saved.Should().NotContain("Doc 3");
            saved.Should().NotContain("Property1=");
            saved.Should().NotContain("Property2=");
            saved.Should().NotContain("Property3=");
            saved.Should().Contain("OtherProperty=Non-default-value");
        }

        /// <summary>
        /// Test configuration section with default-valued documented properties.
        /// </summary>
        public class DefaultDocumentedConfig
        {
            /// <summary>
            /// Gets or sets the first default-valued property.
            /// </summary>
            [DefaultValue("DefaultValue")]
            public string? Property1 { get; set; }

            /// <summary>
            /// Gets or sets the second default-valued property.
            /// </summary>
            [DefaultValue("DefaultValue")]
            public string? Property2 { get; set; }

            /// <summary>
            /// Gets or sets the third default-valued property.
            /// </summary>
            [DefaultValue("DefaultValue")]
            public string? Property3 { get; set; }

            /// <summary>
            /// Gets or sets the non-default property that must not inherit detached documentation.
            /// </summary>
            [DefaultValue("DefaultValue")]
            public string? OtherProperty { get; set; }
        }
    }
}
