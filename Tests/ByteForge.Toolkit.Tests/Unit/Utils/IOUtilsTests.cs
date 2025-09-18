using System;
using System.IO;
using System.Linq;
using ByteForge.Toolkit;
using AwesomeAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ByteForge.Toolkit.Tests.Unit.Utils
{
    /// <summary>
    /// Unit tests for the <see cref="IOUtils"/> class, which provides utility methods for IO operations.
    /// </summary>
    /// <remarks>
    /// These tests validate the file retrieval and path normalization functions of the IOUtils class.
    /// The tests cover a range of scenarios including pattern matching, subdirectory searching,
    /// path normalization, and edge cases such as empty directories and invalid paths.
    /// </remarks>
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("Utils")]
    public class IOUtilsTests
    {
        private string _testDirectory = null!;

        #region Setup and Cleanup

        /// <summary>
        /// Sets up a temporary test directory with known file structure for each test.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            // Create a unique temporary directory for each test
            _testDirectory = Path.Combine(Path.GetTempPath(), $"IOUtilsTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
        }

        /// <summary>
        /// Cleans up the temporary test directory after each test.
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }

        #endregion

        #region GetFiles Tests

        /// <summary>
        /// Verifies that GetFiles returns files matching a single pattern.
        /// </summary>
        /// <remarks>
        /// Tests basic functionality with a single file pattern.
        /// </remarks>
        [TestMethod]
        public void GetFiles_SinglePattern_ShouldReturnMatchingFiles()
        {
            // Arrange
            var txtFiles = new[] { "file1.txt", "file2.txt", "document.txt" };
            var otherFiles = new[] { "data.log", "config.xml", "image.jpg" };
            
            CreateTestFiles(txtFiles.Concat(otherFiles));

            // Act
            var result = IOUtils.GetFiles(_testDirectory, "*.txt");

            // Assert
            result.Should().HaveCount(3, "should find all .txt files");
            result.Select(Path.GetFileName).Should().BeEquivalentTo(txtFiles, 
                "should return only .txt files");
        }

        /// <summary>
        /// Verifies that GetFiles handles multiple patterns separated by semicolon.
        /// </summary>
        /// <remarks>
        /// Tests the multiple pattern functionality using semicolon separator.
        /// </remarks>
        [TestMethod]
        public void GetFiles_MultiplePatternsWithSemicolon_ShouldReturnAllMatches()
        {
            // Arrange
            var testFiles = new[] { "log1.txt", "log2.log", "data.xml", "config.ini", "image.jpg" };
            CreateTestFiles(testFiles);

            // Act
            var result = IOUtils.GetFiles(_testDirectory, "*.txt;*.log;*.xml");

            // Assert
            result.Should().HaveCount(3, "should find files matching any of the patterns");
            result.Select(Path.GetFileName).Should().BeEquivalentTo(
                ["log1.txt", "log2.log", "data.xml"],
                "should return files matching txt, log, or xml patterns");
        }

        /// <summary>
        /// Verifies that GetFiles handles multiple patterns separated by pipe.
        /// </summary>
        /// <remarks>
        /// Tests the multiple pattern functionality using pipe separator.
        /// </remarks>
        [TestMethod]
        public void GetFiles_MultiplePatternsWithPipe_ShouldReturnAllMatches()
        {
            // Arrange
            var testFiles = new[] { "app.exe", "library.dll", "readme.txt", "config.json" };
            CreateTestFiles(testFiles);

            // Act
            var result = IOUtils.GetFiles(_testDirectory, "*.exe|*.dll");

            // Assert
            result.Should().HaveCount(2, "should find files matching either pattern");
            result.Select(Path.GetFileName).Should().BeEquivalentTo(
                ["app.exe", "library.dll"],
                "should return executable and library files");
        }

        /// <summary>
        /// Verifies that GetFiles with TopDirectoryOnly doesn't search subdirectories.
        /// </summary>
        /// <remarks>
        /// Tests default search behavior (TopDirectoryOnly).
        /// </remarks>
        [TestMethod]
        public void GetFiles_TopDirectoryOnly_ShouldNotSearchSubdirectories()
        {
            // Arrange
            var rootFiles = new[] { "root1.txt", "root2.txt" };
            CreateTestFiles(rootFiles);

            // Create subdirectory with files
            var subDir = Path.Combine(_testDirectory, "SubFolder");
            Directory.CreateDirectory(subDir);
            File.WriteAllText(Path.Combine(subDir, "sub1.txt"), "content");
            File.WriteAllText(Path.Combine(subDir, "sub2.txt"), "content");

            // Act
            var result = IOUtils.GetFiles(_testDirectory, "*.txt", SearchOption.TopDirectoryOnly);

            // Assert
            result.Should().HaveCount(2, "should only find files in the root directory");
            result.Select(Path.GetFileName).Should().BeEquivalentTo(rootFiles,
                "should only return root directory files");
        }

        /// <summary>
        /// Verifies that GetFiles with AllDirectories searches subdirectories recursively.
        /// </summary>
        /// <remarks>
        /// Tests recursive search functionality.
        /// </remarks>
        [TestMethod]
        public void GetFiles_AllDirectories_ShouldSearchSubdirectoriesRecursively()
        {
            // Arrange
            var rootFiles = new[] { "root.txt" };
            CreateTestFiles(rootFiles);

            // Create nested subdirectories with files
            var subDir1 = Path.Combine(_testDirectory, "Sub1");
            var subDir2 = Path.Combine(_testDirectory, "Sub1", "Sub2");
            Directory.CreateDirectory(subDir2); // Creates both Sub1 and Sub2

            File.WriteAllText(Path.Combine(subDir1, "sub1.txt"), "content");
            File.WriteAllText(Path.Combine(subDir2, "sub2.txt"), "content");

            // Act
            var result = IOUtils.GetFiles(_testDirectory, "*.txt", SearchOption.AllDirectories);

            // Assert
            result.Should().HaveCount(3, "should find files in root and all subdirectories");
            result.Select(Path.GetFileName).Should().BeEquivalentTo(
                ["root.txt", "sub1.txt", "sub2.txt"],
                "should return files from all directory levels");
        }

        /// <summary>
        /// Verifies that GetFiles handles empty directories correctly.
        /// </summary>
        /// <remarks>
        /// Tests behavior when no files match the pattern or directory is empty.
        /// </remarks>
        [TestMethod]
        public void GetFiles_EmptyDirectory_ShouldReturnEmptyArray()
        {
            // Act
            var result = IOUtils.GetFiles(_testDirectory, "*.txt");

            // Assert
            result.Should().NotBeNull("should return an array, not null");
            result.Should().BeEmpty("should return empty array when no files match");
        }

        /// <summary>
        /// Verifies that GetFiles handles non-existent patterns correctly.
        /// </summary>
        /// <remarks>
        /// Tests behavior when pattern doesn't match any files.
        /// </remarks>
        [TestMethod]
        public void GetFiles_NoMatchingFiles_ShouldReturnEmptyArray()
        {
            // Arrange
            CreateTestFiles(["file1.txt", "file2.log"]);

            // Act
            var result = IOUtils.GetFiles(_testDirectory, "*.xyz");

            // Assert
            result.Should().NotBeNull("should return an array, not null");
            result.Should().BeEmpty("should return empty array when no files match pattern");
        }

        /// <summary>
        /// Verifies that GetFiles throws appropriate exception for invalid directory.
        /// </summary>
        /// <remarks>
        /// Tests error handling when directory doesn't exist.
        /// </remarks>
        [TestMethod]
        public void GetFiles_NonExistentDirectory_ShouldThrowDirectoryNotFoundException()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_testDirectory, "NonExistent");

            // Act & Assert
            Action act = () => IOUtils.GetFiles(nonExistentPath, "*.txt");
            act.Should().Throw<DirectoryNotFoundException>("should throw when directory doesn't exist");
        }

        #endregion

        #region GetUniversalPath Tests

        /// <summary>
        /// Verifies that GetUniversalPath normalizes relative paths correctly.
        /// </summary>
        /// <remarks>
        /// Tests path normalization without network drive mapping.
        /// </remarks>
        [TestMethod]
        public void GetUniversalPath_RelativePath_ShouldReturnFullPath()
        {
            // Arrange
            var relativePath = "..\\TestFolder\\file.txt";

            // Act
            var result = IOUtils.GetUniversalPath(relativePath);

            // Assert
            result.Should().NotBeNull("should return a path");
            Path.IsPathRooted(result).Should().BeTrue("should return a full path");
            result.Should().NotContain("..", "should resolve relative path components");
        }

        /// <summary>
        /// Verifies that GetUniversalPath handles current directory paths.
        /// </summary>
        /// <remarks>
        /// Tests normalization of current directory references.
        /// </remarks>
        [TestMethod]
        public void GetUniversalPath_CurrentDirectory_ShouldReturnFullPath()
        {
            // Arrange
            var currentDirPath = ".\\file.txt";

            // Act
            var result = IOUtils.GetUniversalPath(currentDirPath);

            // Assert
            result.Should().NotBeNull("should return a path");
            Path.IsPathRooted(result).Should().BeTrue("should return a full path");
            result.Should().EndWith("file.txt", "should preserve filename");
        }

        /// <summary>
        /// Verifies that GetUniversalPath returns UNC paths unchanged.
        /// </summary>
        /// <remarks>
        /// Tests that existing UNC paths are not modified.
        /// </remarks>
        [TestMethod]
        public void GetUniversalPath_UNCPath_ShouldReturnUnchanged()
        {
            // Arrange
            var uncPath = @"\\server\share\folder\file.txt";

            // Act
            var result = IOUtils.GetUniversalPath(uncPath);

            // Assert
            result.Should().Be(uncPath, "UNC paths should be returned as-is");
        }

        /// <summary>
        /// Verifies that GetUniversalPath handles local drive paths correctly.
        /// </summary>
        /// <remarks>
        /// Tests behavior with standard local drive paths (non-network mapped).
        /// </remarks>
        [TestMethod]
        public void GetUniversalPath_LocalDrivePath_ShouldReturnNormalizedPath()
        {
            // Arrange
            var localPath = @"C:\Temp\TestFile.txt";

            // Act
            var result = IOUtils.GetUniversalPath(localPath);

            // Assert
            result.Should().NotBeNull("should return a path");
            result.Should().StartWith(@"C:\", "should preserve drive letter for local drives");
            result.Should().EndWith("TestFile.txt", "should preserve filename");
        }

        /// <summary>
        /// Verifies that GetUniversalPath handles empty and null paths appropriately.
        /// </summary>
        /// <remarks>
        /// Tests edge cases with invalid input parameters.
        /// </remarks>
        [TestMethod]
        public void GetUniversalPath_InvalidPaths_ShouldThrowException()
        {
            // Act & Assert
            Action act1 = () => IOUtils.GetUniversalPath(null!);
            act1.Should().Throw<Exception>("should throw for null path");

            Action act2 = () => IOUtils.GetUniversalPath("");
            act2.Should().Throw<Exception>("should throw for empty path");
        }

        /// <summary>
        /// Verifies that GetUniversalPath handles paths with multiple directory separators.
        /// </summary>
        /// <remarks>
        /// Tests normalization of paths with redundant separators.
        /// </remarks>
        [TestMethod]
        public void GetUniversalPath_PathWithMultipleSeparators_ShouldNormalize()
        {
            // Arrange
            var pathWithExtraSeparators = @"C:\Temp\\\\Multiple\\\Separators\\file.txt";

            // Act
            var result = IOUtils.GetUniversalPath(pathWithExtraSeparators);

            // Assert
            result.Should().NotBeNull("should return a path");
            result.Should().NotContain(@"\\", "should normalize multiple separators");
            result.Should().EndWith("file.txt", "should preserve filename");
        }

        /// <summary>
        /// Performance test to ensure GetUniversalPath performs adequately.
        /// </summary>
        /// <remarks>
        /// Verifies that path normalization operations don't cause performance issues.
        /// </remarks>
        [TestMethod]
        public void GetUniversalPath_Performance_ShouldHandleMultipleCallsQuickly()
        {
            // Arrange
            var iterations = 1000;
            var testPath = @"C:\Temp\TestFolder\..\TestFile.txt";
            var startTime = DateTime.UtcNow;

            // Act
            for (var i = 0; i < iterations; i++)
            {
                var result = IOUtils.GetUniversalPath(testPath);
                result.Should().NotBeNull();
            }

            // Assert
            var duration = DateTime.UtcNow - startTime;
            duration.Should().BeLessThan(TimeSpan.FromSeconds(2), 
                $"should handle {iterations} path normalizations quickly");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates test files in the test directory.
        /// </summary>
        /// <param name="fileNames">Names of files to create.</param>
        private void CreateTestFiles(System.Collections.Generic.IEnumerable<string> fileNames)
        {
            foreach (var fileName in fileNames)
            {
                var filePath = Path.Combine(_testDirectory, fileName);
                File.WriteAllText(filePath, $"Test content for {fileName}");
            }
        }

        #endregion
    }
}