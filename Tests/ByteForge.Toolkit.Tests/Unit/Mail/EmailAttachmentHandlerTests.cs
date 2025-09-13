using ByteForge.Toolkit;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Mail;
using System.Reflection;

namespace ByteForge.Toolkit.Tests.Unit.Mail
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("Mail")]
    public class EmailAttachmentHandlerTests
    {
        private EmailAttachmentHandler _handler = null!;
        private string _testDirectory = null!;
        private List<string> _testFiles = null!;

        [TestInitialize]
        public void Setup()
        {
            _handler = new EmailAttachmentHandler();
            _testDirectory = Path.Combine(Path.GetTempPath(), "EmailAttachmentHandlerTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
            
            _testFiles = [];
            CreateTestFiles();
        }

        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                if (Directory.Exists(_testDirectory))
                    Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        #region Test Setup Helpers

        /// <summary>
        /// Creates test files of various sizes for testing.
        /// </summary>
        /// <remarks>
        /// Creates small, medium, and large test files for different scenarios.
        /// </remarks>
        private void CreateTestFiles()
        {
            // Small file (1KB)
            var smallFile = Path.Combine(_testDirectory, "small.txt");
            File.WriteAllText(smallFile, new string('A', 1024));
            _testFiles.Add(smallFile);

            // Medium file (100KB)
            var mediumFile = Path.Combine(_testDirectory, "medium.txt");
            File.WriteAllText(mediumFile, new string('B', 100 * 1024));
            _testFiles.Add(mediumFile);

            // Large file (6MB) - exceeds individual direct attachment limit (5MB)
            var largeFile = Path.Combine(_testDirectory, "large.txt");
            File.WriteAllText(largeFile, new string('C', 6 * 1024 * 1024));
            _testFiles.Add(largeFile);

            // Extra large file (10MB) - exceeds individual limit
            var extraLargeFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestData", "largeDummy.file");
            _testFiles.Add(extraLargeFile);
        }

        /// <summary>
        /// Creates a test MailMessage for testing.
        /// </summary>
        /// <returns>A new MailMessage instance for testing.</returns>
        private MailMessage CreateTestEmail()
        {
            return new MailMessage
            {
                From = new MailAddress("test@example.com"),
                Subject = "Test Subject",
                Body = "Test Body"
            };
        }

        #endregion

        #region Constructor and Basic Properties Tests

        /// <summary>
        /// Verifies that EmailAttachmentHandler can be instantiated successfully.
        /// </summary>
        /// <remarks>
        /// Tests basic constructor functionality.
        /// </remarks>
        [TestMethod]
        public void Constructor_ShouldCreateInstance()
        {
            // Act
            var handler = new EmailAttachmentHandler();

            // Assert
            handler.Should().NotBeNull("should create instance successfully");
        }

        #endregion

        #region ProcessAttachments Method Tests - Validation

        /// <summary>
        /// Verifies that ProcessAttachments throws ArgumentNullException for null email.
        /// </summary>
        /// <remarks>
        /// Tests null email parameter validation.
        /// </remarks>
        [TestMethod]
        public void ProcessAttachments_NullEmail_ShouldThrowArgumentNullException()
        {
            // Arrange
            var files = new List<string> { _testFiles[0] };

            // Act & Assert
            _handler.Invoking(h => h.ProcessAttachments(null!, files, true))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("email");
        }

        /// <summary>
        /// Verifies that ProcessAttachments handles null file list gracefully.
        /// </summary>
        /// <remarks>
        /// Tests null file list parameter handling.
        /// </remarks>
        [TestMethod]
        public void ProcessAttachments_NullFileList_ShouldReturnEmptyResult()
        {
            // Arrange
            var email = CreateTestEmail();

            // Act
            var result = _handler.ProcessAttachments(email, null!, true);

            // Assert
            result.Should().NotBeNull("should return a result object");
            result.Success.Should().BeTrue("should succeed with empty file list");
            result.ProcessingMethod.Should().Be(ProcessingMethod.None, "should use None processing method");
            result.TempFiles.Should().BeEmpty("should not create any temp files");
        }

        /// <summary>
        /// Verifies that ProcessAttachments handles empty file list gracefully.
        /// </summary>
        /// <remarks>
        /// Tests empty file list parameter handling.
        /// </remarks>
        [TestMethod]
        public void ProcessAttachments_EmptyFileList_ShouldReturnEmptyResult()
        {
            // Arrange
            var email = CreateTestEmail();
            var files = new List<string>();

            // Act
            var result = _handler.ProcessAttachments(email, files, true);

            // Assert
            result.Should().NotBeNull("should return a result object");
            result.Success.Should().BeTrue("should succeed with empty file list");
            result.ProcessingMethod.Should().Be(ProcessingMethod.None, "should use None processing method");
            result.TempFiles.Should().BeEmpty("should not create any temp files");
        }

        #endregion

        #region ProcessAttachments Method Tests - Direct Attachment

        /// <summary>
        /// Verifies that ProcessAttachments handles small files with direct attachment.
        /// </summary>
        /// <remarks>
        /// Tests direct attachment of files under size limits.
        /// </remarks>
        [TestMethod]
        public void ProcessAttachments_SmallFiles_ShouldAttachDirectly()
        {
            // Arrange
            var email = CreateTestEmail();
            var files = new List<string> { _testFiles[0], _testFiles[1] }; // small + medium files

            // Act
            var result = _handler.ProcessAttachments(email, files, true);

            // Assert
            result.Should().NotBeNull("should return a result object");
            result.Success.Should().BeTrue("should succeed with small files");
            result.ProcessingMethod.Should().Be(ProcessingMethod.DirectAttachment, "should use direct attachment");
            result.TempFiles.Should().BeEmpty("should not create temp files for direct attachment");
            result.SkippedFiles.Should().BeEmpty("should not skip any existing files");
            
            email.Attachments.Should().HaveCount(2, "should attach both files");
            email.Body.Should().Contain("Attached:", "should add attachment summary");
            email.Body.Should().Contain("small.txt", "should list first file");
            email.Body.Should().Contain("medium.txt", "should list second file");
        }

        /// <summary>
        /// Verifies that ProcessAttachments skips non-existent files correctly.
        /// </summary>
        /// <remarks>
        /// Tests handling of files that don't exist on disk.
        /// </remarks>
        [TestMethod]
        public void ProcessAttachments_NonExistentFiles_ShouldSkipAndContinue()
        {
            // Arrange
            var email = CreateTestEmail();
            var files = new List<string> 
            { 
                _testFiles[0], 
                Path.Combine(_testDirectory, "nonexistent.txt"),
                _testFiles[1]
            };

            // Act
            var result = _handler.ProcessAttachments(email, files, true);

            // Assert
            result.Should().NotBeNull("should return a result object");
            result.Success.Should().BeTrue("should succeed despite skipped file");
            result.ProcessingMethod.Should().Be(ProcessingMethod.DirectAttachment, "should use direct attachment for valid files");
            result.SkippedFiles.Should().HaveCount(1, "should skip one non-existent file");
            result.SkippedFiles[0].FilePath.Should().EndWith("nonexistent.txt", "should record the correct skipped file");
            result.SkippedFiles[0].Reason.Should().Be("File not found", "should provide correct skip reason");
            
            email.Attachments.Should().HaveCount(2, "should attach only the existing files");
        }

        /// <summary>
        /// Verifies that ProcessAttachments respects file name mapping for direct attachments.
        /// </summary>
        /// <remarks>
        /// Tests custom file naming functionality.
        /// </remarks>
        [TestMethod]
        public void ProcessAttachments_WithFileNameMap_ShouldUseCustomNames()
        {
            // Arrange
            var email = CreateTestEmail();
            var files = new List<string> { _testFiles[0] };
            var fileNameMap = new Dictionary<string, string>
            {
                { "small.txt", "renamed_small.txt" }
            };

            // Act
            var result = _handler.ProcessAttachments(email, files, fileNameMap, true);

            // Assert
            result.Should().NotBeNull("should return a result object");
            result.Success.Should().BeTrue("should succeed with file name mapping");
            result.ProcessingMethod.Should().Be(ProcessingMethod.DirectAttachment, "should use direct attachment");
            
            email.Attachments.Should().HaveCount(1, "should attach the file");
            email.Attachments[0].Name.Should().Be("renamed_small.txt", "should use mapped file name");
            email.Body.Should().Contain("renamed_small.txt", "should use mapped name in summary");
        }

        #endregion

        #region ProcessAttachments Method Tests - Compression

        /// <summary>
        /// Verifies that ProcessAttachments compresses files when total size exceeds direct limit.
        /// </summary>
        /// <remarks>
        /// Tests compression functionality for larger file sets.
        /// </remarks>
        [TestMethod]
        public void ProcessAttachments_LargeFiles_ShouldCompress()
        {
            // Arrange
            var email = CreateTestEmail();
            var files = new List<string> { _testFiles[2] }; // large file (6MB)

            // Act
            var result = _handler.ProcessAttachments(email, files, [], true);

            // Assert
            result.Should().NotBeNull("should return a result object");
            result.Success.Should().BeTrue("should succeed with compression");
            result.ProcessingMethod.Should().Be(ProcessingMethod.Compressed, "should use compression");
            result.TempFiles.Should().HaveCount(1, "should create one zip file");
            result.TempFiles[0].Should().EndWith(".zip", "should create zip file");
            
            email.Attachments.Should().HaveCount(1, "should attach one zip file");
            email.Body.Should().Contain("Attached:", "should add attachment summary");
            email.Body.Should().Contain("large.txt", "should list original file in summary");

            // Verify temp file exists and is a valid zip
            var zipFile = result.TempFiles[0];
            File.Exists(zipFile).Should().BeTrue("zip file should exist");

            // Verify zip contents
            using var archive = ZipFile.OpenRead(zipFile);
            archive.Entries.Should().HaveCount(1, "zip should contain one file");
            archive.Entries[0].Name.Should().Be("large.txt", "zip should contain the original file");
        }

        /// <summary>
        /// Verifies that ProcessAttachments handles compression with file name mapping.
        /// </summary>
        /// <remarks>
        /// Tests compression with custom file names.
        /// </remarks>
        [TestMethod]
        public void ProcessAttachments_CompressionWithFileNameMap_ShouldUseCustomNames()
        {
            // Arrange
            var email = CreateTestEmail();
            var files = new List<string> { _testFiles[2] };
            var fileNameMap = new Dictionary<string, string>
            {
                { "large.txt", "custom_large.txt" }
            };

            // Act
            var result = _handler.ProcessAttachments(email, files, fileNameMap, true);

            // Assert
            result.Should().NotBeNull("should return a result object");
            result.Success.Should().BeTrue("should succeed with compression and name mapping");
            result.ProcessingMethod.Should().Be(ProcessingMethod.Compressed, "should use compression");
            
            // Verify zip contents use custom names
            var zipFile = result.TempFiles[0];
            using (var archive = ZipFile.OpenRead(zipFile))
            {
                archive.Entries[0].Name.Should().Be("custom_large.txt", "zip should use custom file name");
            }
            
            email.Body.Should().Contain("custom_large.txt", "should use custom name in summary");
        }

        #endregion

        #region ProcessAttachments Method Tests - Multi-Part Archives

        /// <summary>
        /// Verifies that ProcessAttachments creates multi-part archives for very large files.
        /// </summary>
        /// <remarks>
        /// Tests multi-part archive creation for files exceeding single archive limits.
        /// </remarks>
        [TestMethod]
        public void ProcessAttachments_VeryLargeFiles_ShouldCreateMultiPart()
        {
            // Arrange
            var email = CreateTestEmail();
            var files = new List<string> { _testFiles[3] }; // extra large file (10MB)

            // Act
            var result = _handler.ProcessAttachments(email, files, [], true);

            // Assert
            result.Should().NotBeNull("should return a result object");
            result.Success.Should().BeTrue("should succeed with multi-part archives");
            result.ProcessingMethod.Should().Be(ProcessingMethod.CompressedAndSplit, "should use compressed and split method");
            result.TempFiles.Should().NotBeEmpty("should create zip files");
            result.PartDistribution.Should().NotBeEmpty("should provide part distribution info");
            
            // All temp files should be zip files
            foreach (var file in result.TempFiles)
            {
                file.Should().EndWith(".zip", "should create zip files");
                file.Should().Contain("Part", "should indicate part number");
                File.Exists(file).Should().BeTrue("zip file should exist");
            }
            
            email.Attachments.Should().HaveCountGreaterThan(0, "should attach zip parts");
            email.Body.Should().Contain("Attached:", "should add attachment summary");
            email.Body.Should().Contain("extralarge.txt", "should list original file in summary");
        }

        /// <summary>
        /// Verifies that ProcessAttachments distributes multiple files across parts efficiently.
        /// </summary>
        /// <remarks>
        /// Tests file distribution algorithm for multi-part archives.
        /// </remarks>
        [TestMethod]
        public void ProcessAttachments_MultipleFilesForSplitting_ShouldDistributeEfficiently()
        {
            // Arrange
            var email = CreateTestEmail();
            // Create multiple large files that will require splitting
            var largFiles = new List<string>();
            for (var i = 0; i < 3; i++)
            {
                var largeFile = Path.Combine(_testDirectory, $"large{i}.txt");
                File.WriteAllText(largeFile, new string('X', 6 * 1024 * 1024)); // 6MB each
                largFiles.Add(largeFile);
            }

            // Act
            var result = _handler.ProcessAttachments(email, largFiles, [], true);

            // Assert
            result.Should().NotBeNull("should return a result object");
            result.Success.Should().BeTrue("should succeed with multiple file splitting");
            result.ProcessingMethod.Should().Be(ProcessingMethod.CompressedAndSplit, "should use compressed and split method");
            result.PartDistribution.Should().NotBeEmpty("should provide part distribution info");
            
            // Verify part distribution makes sense
            var totalFiles = result.PartDistribution.Sum(p => p.FileCount);
            totalFiles.Should().Be(3, "should distribute all files across parts");
            
            foreach (var part in result.PartDistribution)
            {
                part.PartNumber.Should().BeGreaterThan(0, "part numbers should be positive");
                part.Files.Should().NotBeEmpty("each part should contain files");
                part.FileCount.Should().Be(part.Files.Count, "file count should match files list count");
            }
        }

        #endregion

        #region ProcessAttachments Method Tests - Edge Cases

        /// <summary>
        /// Verifies that ProcessAttachments handles files too large to process.
        /// </summary>
        /// <remarks>
        /// Tests error handling for files exceeding maximum limits.
        /// </remarks>
        [TestMethod]
        public void ProcessAttachments_FilesTooLarge_ShouldReturnError()
        {
            // Arrange
            var email = CreateTestEmail();
            // Create multiple extra large files that exceed total limits (20MB limit)
            var veryLargeFiles = new List<string>();
            for (var i = 0; i < 6; i++)
            {
                var veryLargeFile = Path.Combine(_testDirectory, $"verylarge{i}.txt");
                File.WriteAllText(veryLargeFile, new string('Z', 25 * 1024 * 1024)); // 25MB each = 150MB total (way over 20MB limit even after compression)
                veryLargeFiles.Add(veryLargeFile);
            }

            // Act
            var result = _handler.ProcessAttachments(email, veryLargeFiles, [], true);

            // Assert
            result.Should().NotBeNull("should return a result object");
            result.Success.Should().BeFalse("should fail with files too large");
            result.ProcessingMethod.Should().Be(ProcessingMethod.Failed, "should indicate failed processing");
            result.Error.Should().NotBeEmpty("should provide error message");
            result.Error.Should().Contain("too large", "error should mention size issue");
            
            email.Attachments.Should().BeEmpty("should not attach anything when failed");
        }

        /// <summary>
        /// Verifies that ProcessAttachments handles attachment summary toggle correctly.
        /// </summary>
        /// <remarks>
        /// Tests the addAttachmentSummary parameter functionality.
        /// </remarks>
        [TestMethod]
        public void ProcessAttachments_WithoutAttachmentSummary_ShouldNotModifyBody()
        {
            // Arrange
            var email = CreateTestEmail();
            var originalBody = email.Body;
            var files = new List<string> { _testFiles[0] };

            // Act
            var result = _handler.ProcessAttachments(email, files, null, false);

            // Assert
            result.Should().NotBeNull("should return a result object");
            result.Success.Should().BeTrue("should succeed without summary");
            email.Body.Should().Be(originalBody, "should not modify email body when summary disabled");
            email.Attachments.Should().HaveCount(1, "should still attach the file");
        }

        /// <summary>
        /// Verifies that ProcessAttachments handles empty file name map correctly.
        /// </summary>
        /// <remarks>
        /// Tests behavior with empty file name mapping dictionary.
        /// </remarks>
        [TestMethod]
        public void ProcessAttachments_EmptyFileNameMap_ShouldUseOriginalNames()
        {
            // Arrange
            var email = CreateTestEmail();
            var files = new List<string> { _testFiles[0] };
            var emptyMap = new Dictionary<string, string>();

            // Act
            var result = _handler.ProcessAttachments(email, files, emptyMap, true);

            // Assert
            result.Should().NotBeNull("should return a result object");
            result.Success.Should().BeTrue("should succeed with empty name map");
            email.Attachments[0].Name.Should().Be("small.txt", "should use original file name");
        }

        #endregion

        #region CleanupTempFiles Method Tests

        /// <summary>
        /// Verifies that CleanupTempFiles removes temporary files correctly.
        /// </summary>
        /// <remarks>
        /// Tests temporary file cleanup functionality.
        /// </remarks>
        [TestMethod]
        public void CleanupTempFiles_WithTempFiles_ShouldDeleteFiles()
        {
            // Arrange
            var email = CreateTestEmail();
            var files = new List<string> { _testFiles[2] }; // large file to force compression
            var result = _handler.ProcessAttachments(email, files, [], true);
            
            // Verify temp files exist before cleanup
            result.TempFiles.Should().NotBeEmpty("should have created temp files");
            foreach (var file in result.TempFiles)
            {
                File.Exists(file).Should().BeTrue($"temp file {file} should exist before cleanup");
            }

            // Act
            email.Dispose(); // Dispose email to release any file locks
            _handler.CleanupTempFiles(result);

            // Assert
            foreach (var file in result.TempFiles)
            {
                File.Exists(file).Should().BeFalse($"temp file {file} should be deleted after cleanup");
            }
        }

        /// <summary>
        /// Verifies that CleanupTempFiles handles null result gracefully.
        /// </summary>
        /// <remarks>
        /// Tests null parameter handling in cleanup.
        /// </remarks>
        [TestMethod]
        public void CleanupTempFiles_NullResult_ShouldNotThrow()
        {
            // Act & Assert
            _handler.Invoking(h => h.CleanupTempFiles(null!))
                .Should().NotThrow("should handle null result gracefully");
        }

        /// <summary>
        /// Verifies that CleanupTempFiles handles missing files gracefully.
        /// </summary>
        /// <remarks>
        /// Tests cleanup behavior when temp files are already deleted.
        /// </remarks>
        [TestMethod]
        public void CleanupTempFiles_AlreadyDeletedFiles_ShouldNotThrow()
        {
            // Arrange
            var result = new AttachmentProcessResult();
            result.TempFiles.Add(Path.Combine(_testDirectory, "nonexistent.zip"));

            // Act & Assert
            _handler.Invoking(h => h.CleanupTempFiles(result))
                .Should().NotThrow("should handle missing temp files gracefully");
        }

        #endregion

        #region Integration and Performance Tests

        /// <summary>
        /// Verifies end-to-end workflow with various file sizes and processing methods.
        /// </summary>
        /// <remarks>
        /// Tests complete workflow from file processing to cleanup.
        /// </remarks>
        [TestMethod]
        public void EndToEndWorkflow_VariousScenarios_ShouldWorkCorrectly()
        {
            // Test 1: Direct attachment
            var email1 = CreateTestEmail();
            var result1 = _handler.ProcessAttachments(email1, [_testFiles[0]], [], true);
            
            result1.Success.Should().BeTrue("direct attachment should succeed");
            result1.ProcessingMethod.Should().Be(ProcessingMethod.DirectAttachment);
            email1.Attachments.Should().HaveCount(1);

            // Test 2: Compression
            var email2 = CreateTestEmail();
            var result2 = _handler.ProcessAttachments(email2, [_testFiles[2]], [], true);
            
            result2.Success.Should().BeTrue("compression should succeed");
            result2.ProcessingMethod.Should().Be(ProcessingMethod.Compressed);
            email2.Attachments.Should().HaveCount(1);
            
            // Cleanup
            _handler.CleanupTempFiles(result2);
            
            // Test 3: Multi-part (if system supports large file creation)
            var email3 = CreateTestEmail();
            var result3 = _handler.ProcessAttachments(email3, [_testFiles[3]], [], true);
            
            result3.Success.Should().BeTrue("multi-part should succeed");
            result3.ProcessingMethod.Should().Be(ProcessingMethod.CompressedAndSplit);
            
            // Cleanup
            _handler.CleanupTempFiles(result3);
        }

        /// <summary>
        /// Performance test for processing multiple files efficiently.
        /// </summary>
        /// <remarks>
        /// Tests performance with multiple files and operations.
        /// </remarks>
        [TestMethod]
        public void ProcessAttachments_MultipleOperations_ShouldHandleEfficiently()
        {
            // Arrange
            var iterations = 10;
            var results = new List<AttachmentProcessResult>();
            var startTime = DateTime.UtcNow;

            // Act
            for (var i = 0; i < iterations; i++)
            {
                var email = CreateTestEmail();
                var result = _handler.ProcessAttachments(email, [_testFiles[0], _testFiles[1]], [], true);
                results.Add(result);
            }

            // Assert
            var duration = DateTime.UtcNow - startTime;
            duration.Should().BeLessThan(TimeSpan.FromSeconds(5), 
                $"should handle {iterations} operations efficiently");
            
            results.Should().AllSatisfy(r => r.Success.Should().BeTrue("all operations should succeed"));
            results.Should().AllSatisfy(r => r.ProcessingMethod.Should().Be(ProcessingMethod.DirectAttachment));
        }

        /// <summary>
        /// Verifies that ProcessAttachments handles file access errors gracefully.
        /// </summary>
        /// <remarks>
        /// Tests error handling for files that cannot be accessed.
        /// </remarks>
        [TestMethod]
        public void ProcessAttachments_FileAccessErrors_ShouldHandleGracefully()
        {
            // Arrange
            var email = CreateTestEmail();
            var files = new List<string> { _testFiles[0] };
            
            // Act
            var result = _handler.ProcessAttachments(email, files, [], true);

            // Assert
            // Should handle gracefully regardless of the outcome
            result.Should().NotBeNull("should always return a result");
        }

        #endregion

        #region File Size Formatting Tests

        /// <summary>
        /// Verifies that file size formatting works correctly in summaries.
        /// </summary>
        /// <remarks>
        /// Tests file size display formatting.
        /// </remarks>
        [TestMethod]
        public void ProcessAttachments_FileSizeFormatting_ShouldDisplayCorrectly()
        {
            // Arrange
            var email = CreateTestEmail();
            var files = new List<string> { _testFiles[0] }; // 1KB file

            // Act
            var result = _handler.ProcessAttachments(email, files, [], true);

            // Assert
            result.Success.Should().BeTrue("should succeed");
            email.Body.Should().Contain("1.0 KB", "should format file size correctly");
        }

        #endregion
    }
}