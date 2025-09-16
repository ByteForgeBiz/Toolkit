using FluentAssertions;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

namespace ByteForge.Toolkit.Tests.Unit.DataStructures
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("DataStructures")]
    public class UrlTests
    {
        /// <summary>
        /// Combines two URLs and verifies correct concatenation.
        /// </summary>
        /// <remarks>
        /// Ensures the URL combination logic produces valid paths for typical use cases.
        /// </remarks>
        [TestMethod]
        public void Combine_TwoUrls_ShouldCombineCorrectly()
        {
            // Arrange
            var url1 = "http://www.example.com";
            var url2 = "api/v1/users";

            // Act
            var result = Url.Combine(url1, url2);

            // Assert
            result.Should().Be("http://www.example.com/api/v1/users");
        }

        /// <summary>
        /// Combines URLs when the second is absolute, expecting the second URL as result.
        /// </summary>
        /// <remarks>
        /// Validates that absolute URLs override base URLs, preventing incorrect path construction.
        /// </remarks>
        [TestMethod]
        public void Combine_SecondUrlIsAbsolute_ShouldReturnSecondUrl()
        {
            // Arrange
            var url1 = "http://www.example.com/api";
            var url2 = "https://api.different.com/v2/data";

            // Act
            var result = Url.Combine(url1, url2);

            // Assert
            result.Should().Be("https://api.different.com/v2/data");
        }

        /// <summary>
        /// Combines URLs when the second starts with a slash, expecting the second URL as result.
        /// </summary>
        /// <remarks>
        /// Ensures that root-relative URLs are handled correctly, avoiding malformed paths.
        /// </remarks>
        [TestMethod]
        public void Combine_SecondUrlStartsWithSlash_ShouldReturnSecondUrl()
        {
            // Arrange
            var url1 = "http://www.example.com/api";
            var url2 = "/different/path";

            // Act
            var result = Url.Combine(url1, url2);

            // Assert
            result.Should().Be("/different/path");
        }

        /// <summary>
        /// Combines two empty URLs, expecting an empty result.
        /// </summary>
        /// <remarks>
        /// Verifies edge case handling for empty input, preventing null reference or unexpected output.
        /// </remarks>
        [TestMethod]
        public void Combine_BothUrlsEmpty_ShouldReturnEmpty()
        {
            // Act
            var result = Url.Combine("", "");

            // Assert
            result.Should().Be("");
        }

        /// <summary>
        /// Combines an empty first URL with a non-empty second URL.
        /// </summary>
        /// <remarks>
        /// Ensures that the combination logic returns the second URL when the first is empty.
        /// </remarks>
        [TestMethod]
        public void Combine_FirstUrlEmpty_ShouldReturnSecondUrl()
        {
            // Arrange
            var url2 = "api/users";

            // Act
            var result = Url.Combine("", url2);

            // Assert
            result.Should().Be("api/users");
        }

        /// <summary>
        /// Combines a non-empty first URL with an empty second URL.
        /// </summary>
        /// <remarks>
        /// Ensures that the combination logic returns the first URL when the second is empty.
        /// </remarks>
        [TestMethod]
        public void Combine_SecondUrlEmpty_ShouldReturnFirstUrl()
        {
            // Arrange
            var url1 = "http://www.example.com";

            // Act
            var result = Url.Combine(url1, "");

            // Assert
            result.Should().Be("http://www.example.com");
        }

        /// <summary>
        /// Combines three URLs and verifies correct concatenation.
        /// </summary>
        /// <remarks>
        /// Tests multi-segment URL construction for API endpoints and resource paths.
        /// </remarks>
        [TestMethod]
        public void Combine_ThreeUrls_ShouldCombineCorrectly()
        {
            // Arrange
            var url1 = "https://api.example.com";
            var url2 = "v1";
            var url3 = "users/123";

            // Act
            var result = Url.Combine(url1, url2, url3);

            // Assert
            result.Should().Be("https://api.example.com/v1/users/123");
        }

        /// <summary>
        /// Combines four URLs and verifies correct concatenation.
        /// </summary>
        /// <remarks>
        /// Ensures the combination logic supports multiple segments for deep resource paths.
        /// </remarks>
        [TestMethod]
        public void Combine_FourUrls_ShouldCombineCorrectly()
        {
            // Arrange
            var url1 = "https://api.example.com";
            var url2 = "v1";
            var url3 = "users";
            var url4 = "123/profile";

            // Act
            var result = Url.Combine(url1, url2, url3, url4);

            // Assert
            result.Should().Be("https://api.example.com/v1/users/123/profile");
        }

        /// <summary>
        /// Combines an array of URLs and verifies correct concatenation.
        /// </summary>
        /// <remarks>
        /// Validates support for params array overload, useful for dynamic path construction.
        /// </remarks>
        [TestMethod]
        public void Combine_ParamsArray_ShouldCombineAllUrls()
        {
            // Arrange
            var urls = new[] { "https://api.example.com", "v1", "users", "123", "profile" };

            // Act
            var result = Url.Combine(urls);

            // Assert
            result.Should().Be("https://api.example.com/v1/users/123/profile");
        }

        /// <summary>
        /// Combines a null array of URLs, expecting an empty result.
        /// </summary>
        /// <remarks>
        /// Ensures null input is handled gracefully, preventing exceptions.
        /// </remarks>
        [TestMethod]
        public void Combine_ParamsArrayNull_ShouldReturnEmpty()
        {
            // Act
            var result = Url.Combine((string[])null);

            // Assert
            result.Should().Be("");
        }

        /// <summary>
        /// Combines an empty array of URLs, expecting an empty result.
        /// </summary>
        /// <remarks>
        /// Verifies that empty arrays do not cause errors or unexpected output.
        /// </remarks>
        [TestMethod]
        public void Combine_ParamsArrayEmpty_ShouldReturnEmpty()
        {
            // Act
            var result = Url.Combine([]);

            // Assert
            result.Should().Be("");
        }

        /// <summary>
        /// Combines a base URL with a parameters array and verifies correct concatenation.
        /// </summary>
        /// <remarks>
        /// Tests overloads for combining base URLs with additional segments.
        /// </remarks>
        [TestMethod]
        public void Combine_BaseUrlWithParametersArray_ShouldCombineCorrectly()
        {
            // Arrange
            var baseUrl = "https://api.example.com";
            var parameters = new[] { "v1", "users", "123" };

            // Act
            var result = Url.Combine(baseUrl, parameters);

            // Assert
            result.Should().Be("https://api.example.com/v1/users/123");
        }

        /// <summary>
        /// Combines a base URL with null parameters, expecting an empty result.
        /// </summary>
        /// <remarks>
        /// Ensures null parameters are handled without exceptions.
        /// </remarks>
        [TestMethod]
        public void Combine_BaseUrlWithNullParameters_ShouldReturnEmpty()
        {
            // Arrange
            var baseUrl = "https://api.example.com";

            // Act
            var result = Url.Combine(baseUrl, (string[])null);

            // Assert
            result.Should().Be("");
        }

        /// <summary>
        /// Combines a base URL with empty parameters, expecting an empty result.
        /// </summary>
        /// <remarks>
        /// Verifies that empty parameters do not cause errors or unexpected output.
        /// </remarks>
        [TestMethod]
        public void Combine_BaseUrlWithEmptyParameters_ShouldReturnEmpty()
        {
            // Arrange
            var baseUrl = "https://api.example.com";

            // Act
            var result = Url.Combine(baseUrl, new string[0]);

            // Assert
            result.Should().Be("");
        }

        /// <summary>
        /// Combines URLs containing backslashes and verifies normalization to forward slashes.
        /// </summary>
        /// <remarks>
        /// Ensures path normalization for cross-platform compatibility and correctness.
        /// </remarks>
        [TestMethod]
        public void Combine_UrlsWithBackslashes_ShouldNormalizeToForwardSlashes()
        {
            // Arrange
            var url1 = "http://www.example.com\\api";
            var url2 = "v1\\users";

            // Act
            var result = Url.Combine(url1, url2);

            // Assert
            result.Should().Be("http://www.example.com/api/v1/users");
        }

        /// <summary>
        /// Combines HTTPS URLs and verifies protocol preservation.
        /// </summary>
        /// <remarks>
        /// Ensures secure protocol is not lost during combination.
        /// </remarks>
        [TestMethod]
        public void Combine_HttpsUrl_ShouldPreserveProtocol()
        {
            // Arrange
            var url1 = "https://secure.example.com";
            var url2 = "api/secure-data";

            // Act
            var result = Url.Combine(url1, url2);

            // Assert
            result.Should().Be("https://secure.example.com/api/secure-data");
        }

        /// <summary>
        /// Combines FTP URLs and verifies correct result.
        /// </summary>
        /// <remarks>
        /// Validates support for non-HTTP protocols in URL combination.
        /// </remarks>
        [TestMethod]
        public void Combine_FtpUrl_ShouldWork()
        {
            // Arrange
            var url1 = "ftp://files.example.com";
            var url2 = "documents/file.txt";

            // Act
            var result = Url.Combine(url1, url2);

            // Assert
            result.Should().Be("ftp://files.example.com/documents/file.txt");
        }

        /// <summary>
        /// Combines FTPS URLs and verifies correct result.
        /// </summary>
        /// <remarks>
        /// Ensures compatibility with secure FTP protocol.
        /// </remarks>
        [TestMethod]
        public void Combine_FtpsUrl_ShouldWork()
        {
            // Arrange
            var url1 = "ftps://secure-files.example.com";
            var url2 = "confidential/data.zip";

            // Act
            var result = Url.Combine(url1, url2);

            // Assert
            result.Should().Be("ftps://secure-files.example.com/confidential/data.zip");
        }

        /// <summary>
        /// Gets the domain from a valid HTTP URL.
        /// </summary>
        /// <remarks>
        /// Verifies domain extraction for standard URLs, supporting host-based logic.
        /// </remarks>
        [TestMethod]
        public void GetDomain_ValidHttpUrl_ShouldReturnHost()
        {
            // Arrange
            var url = "http://www.example.com/path/to/resource";

            // Act
            var domain = Url.GetDomain(url);

            // Assert
            domain.Should().Be("www.example.com");
        }

        /// <summary>
        /// Gets the domain from a valid HTTPS URL.
        /// </summary>
        /// <remarks>
        /// Ensures domain extraction works for secure URLs.
        /// </remarks>
        [TestMethod]
        public void GetDomain_ValidHttpsUrl_ShouldReturnHost()
        {
            // Arrange
            var url = "https://api.example.com:8080/v1/users";

            // Act
            var domain = Url.GetDomain(url);

            // Assert
            domain.Should().Be("api.example.com");
        }

        /// <summary>
        /// Gets the domain from a URL with a subdomain.
        /// </summary>
        /// <remarks>
        /// Validates extraction of full host including subdomains.
        /// </remarks>
        [TestMethod]
        public void GetDomain_UrlWithSubdomain_ShouldReturnFullHost()
        {
            // Arrange
            var url = "https://sub.domain.example.com/api";

            // Act
            var domain = Url.GetDomain(url);

            // Assert
            domain.Should().Be("sub.domain.example.com");
        }

        /// <summary>
        /// Gets the domain from a URL with a port, expecting host without port.
        /// </summary>
        /// <remarks>
        /// Ensures port numbers are excluded from domain extraction.
        /// </remarks>
        [TestMethod]
        public void GetDomain_UrlWithPort_ShouldReturnHostWithoutPort()
        {
            // Arrange
            var url = "http://localhost:3000/api/users";

            // Act
            var domain = Url.GetDomain(url);

            // Assert
            domain.Should().Be("localhost");
        }

        /// <summary>
        /// Attempts to get the domain from an invalid URL, expecting an exception.
        /// </summary>
        /// <remarks>
        /// Verifies error handling for malformed URLs.
        /// </remarks>
        [TestMethod]
        public void GetDomain_InvalidUrl_ShouldThrowException()
        {
            // Arrange
            var invalidUrl = "not-a-valid-url";

            // Act & Assert
            Action action = () => Url.GetDomain(invalidUrl);
            action.Should().Throw<UriFormatException>();
        }

        /// <summary>
        /// Attempts to get the domain from a null URL, expecting an exception.
        /// </summary>
        /// <remarks>
        /// Ensures null input is handled with appropriate exceptions.
        /// </remarks>
        [TestMethod]
        public void GetDomain_NullUrl_ShouldThrowException()
        {
            // Act & Assert
            Action action = () => Url.GetDomain(null);
            action.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Performance test for URL combination logic.
        /// </summary>
        /// <remarks>
        /// Ensures the combination logic is efficient for repeated operations.
        /// </remarks>
        [TestMethod]
        public void Combine_PerformanceTest_ShouldHandleMultipleOperations()
        {
            // Arrange
            var baseUrl = "https://api.example.com";
            var segments = new[] { "v1", "users", "123", "profile", "avatar" };
            var startTime = DateTime.UtcNow;

            // Act
            for (int i = 0; i < 1000; i++)
            {
                var result = Url.Combine(baseUrl, segments);
                result.Should().NotBeNullOrEmpty();
            }

            // Assert
            var duration = DateTime.UtcNow - startTime;
            duration.Should().BeLessThan(TimeSpan.FromSeconds(1), "URL combination should be fast");
        }
    }
}