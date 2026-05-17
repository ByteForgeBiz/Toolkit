using AwesomeAssertions;
using ByteForge.Toolkit.Logging;

namespace ByteForge.Toolkit.Tests.Unit.Logging
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("Logging")]
    public class LogSecretMaskerTests
    {
        /// <summary>
        /// Verifies that common secret key shapes are redacted.
        /// </summary>
        [TestMethod]
        public void Mask_WithKnownSecretKeys_ShouldRedactValues()
        {
            var input = "password=hunter2 client_secret='abc123' AccessToken:xyz refresh_token=refreshSecret";

            var result = LogSecretMasker.Mask(input);

            result.Should().Contain("password=[REDACTED]");
            result.Should().Contain("client_secret='[REDACTED]'");
            result.Should().Contain("AccessToken:[REDACTED]");
            result.Should().Contain("refresh_token=[REDACTED]");
            result.Should().NotContain("hunter2");
            result.Should().NotContain("abc123");
            result.Should().NotContain("xyz");
            result.Should().NotContain("refreshSecret");
        }

        /// <summary>
        /// Verifies that non-sensitive text is not changed.
        /// </summary>
        [TestMethod]
        public void Mask_WithNoSecretKeys_ShouldReturnOriginalText()
        {
            var input = "status=ok user=paul tokenized=false";

            var result = LogSecretMasker.Mask(input);

            result.Should().Be(input);
        }

        /// <summary>
        /// Verifies that null object input preserves legacy null behavior.
        /// </summary>
        [TestMethod]
        public void Mask_WithNullObject_ShouldReturnNull()
        {
            var result = LogSecretMasker.Mask((object)null);

            result.Should().BeNull();
        }
    }
}
