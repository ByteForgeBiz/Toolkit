using System;
using ByteForge.Toolkit;
using ByteForge.Toolkit.Tests.Helpers;
using FluentAssertions;

namespace ByteForge.Toolkit.Tests.Unit.DataStructures
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("DataStructures")]
    public class UrlTests
    {
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

        [TestMethod]
        public void Combine_BothUrlsEmpty_ShouldReturnEmpty()
        {
            // Act
            var result = Url.Combine("", "");

            // Assert
            result.Should().Be("");
        }

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

        [TestMethod]
        public void Combine_ParamsArrayNull_ShouldReturnEmpty()
        {
            // Act
            var result = Url.Combine((string[])null);

            // Assert
            result.Should().Be("");
        }

        [TestMethod]
        public void Combine_ParamsArrayEmpty_ShouldReturnEmpty()
        {
            // Act
            var result = Url.Combine([]);

            // Assert
            result.Should().Be("");
        }

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

        [TestMethod]
        public void GetDomain_InvalidUrl_ShouldThrowException()
        {
            // Arrange
            var invalidUrl = "not-a-valid-url";

            // Act & Assert
            Action action = () => Url.GetDomain(invalidUrl);
            action.Should().Throw<UriFormatException>();
        }

        [TestMethod]
        public void GetDomain_NullUrl_ShouldThrowException()
        {
            // Act & Assert
            Action action = () => Url.GetDomain(null);
            action.Should().Throw<ArgumentNullException>();
        }

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