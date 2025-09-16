using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ByteForge.Toolkit;
using ByteForge.Toolkit.Tests.Helpers;
using FluentAssertions;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

namespace ByteForge.Toolkit.Tests.Unit.CSV
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("CSV")]
    public class CSVWriterTests
    {
        /// <summary>
        /// Test record class for CSV writing tests.
        /// </summary>
        public class TestRecord : CSVRecord
        {
            [CSVColumn(0, "Name")]
            public string Name { get; set; }

            [CSVColumn(1, "Age")]
            public int Age { get; set; }

            [CSVColumn(2, "Email")]
            public string Email { get; set; }

            public override void Validate()
            {
                // No-op for testing purposes
            }
        }

        /// <summary>
        /// Simple POCO class for WriteObjects tests.
        /// </summary>
        public class SimplePerson
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public string City { get; set; }
        }

        [TestInitialize]
        public void TestInitialize()
        {
            // Clean up any temp files before each test
            TempFileHelper.CleanupTempFiles();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Clean up any temp files after each test
            TempFileHelper.CleanupTempFiles();
        }

        /// <summary>
        /// Verifies that the CSVWriter constructor with file path creates an instance.
        /// </summary>
        /// <remarks>
        /// Ensures the CSVWriter can be instantiated with a file path.
        /// </remarks>
        [TestMethod]
        public void Constructor_WithFilePath_ShouldCreateInstance()
        {
            // Arrange
            var filePath = TempFileHelper.GetTempFilePath(".csv");

            // Act
            using var writer = new CSVWriter(filePath);

            // Assert
            writer.Should().NotBeNull();
            writer.Format.Should().NotBeNull();
            writer.Format.Should().Be(CSVFormat.Default);
        }

        /// <summary>
        /// Verifies that the CSVWriter constructor with custom format works correctly.
        /// </summary>
        /// <remarks>
        /// Ensures custom CSV format is properly set.
        /// </remarks>
        [TestMethod]
        public void Constructor_WithCustomFormat_ShouldUseCustomFormat()
        {
            // Arrange
            var filePath = TempFileHelper.GetTempFilePath(".csv");
            var customFormat = new CSVFormat
            {
                Delimiter = ';',
                QuoteChar = '\'',
                HasHeader = false
            };

            // Act
            using var writer = new CSVWriter(filePath, customFormat);

            // Assert
            writer.Format.Should().Be(customFormat);
            writer.Format.Delimiter.Should().Be(';');
            writer.Format.QuoteChar.Should().Be('\'');
            writer.Format.HasHeader.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that the CSVWriter constructor with stream creates an instance.
        /// </summary>
        /// <remarks>
        /// Ensures the CSVWriter can be instantiated with a stream.
        /// </remarks>
        [TestMethod]
        public void Constructor_WithStream_ShouldCreateInstance()
        {
            // Arrange
            using var stream = new MemoryStream();

            // Act
            using var writer = new CSVWriter(stream);

            // Assert
            writer.Should().NotBeNull();
            writer.Format.Should().NotBeNull();
        }

        /// <summary>
        /// Verifies that the CSVWriter constructor with TextWriter creates an instance.
        /// </summary>
        /// <remarks>
        /// Ensures the CSVWriter can be instantiated with a TextWriter.
        /// </remarks>
        [TestMethod]
        public void Constructor_WithTextWriter_ShouldCreateInstance()
        {
            // Arrange
            using var stringWriter = new StringWriter();

            // Act
            using var writer = new CSVWriter(stringWriter);

            // Assert
            writer.Should().NotBeNull();
            writer.Format.Should().NotBeNull();
        }

        /// <summary>
        /// Verifies that constructor throws ArgumentNullException for null file path.
        /// </summary>
        /// <remarks>
        /// Ensures proper validation of constructor parameters.
        /// </remarks>
        [TestMethod]
        public void Constructor_WithNullFilePath_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            AssertionHelpers.AssertThrows<ArgumentNullException>(() => new CSVWriter((string)null));
        }

        /// <summary>
        /// Verifies that constructor throws ArgumentNullException for empty file path.
        /// </summary>
        /// <remarks>
        /// Ensures proper validation of constructor parameters.
        /// </remarks>
        [TestMethod]
        public void Constructor_WithEmptyFilePath_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            AssertionHelpers.AssertThrows<ArgumentNullException>(() => new CSVWriter(""));
        }

        /// <summary>
        /// Verifies that constructor throws ArgumentNullException for null stream.
        /// </summary>
        /// <remarks>
        /// Ensures proper validation of constructor parameters.
        /// </remarks>
        [TestMethod]
        public void Constructor_WithNullStream_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            AssertionHelpers.AssertThrows<ArgumentNullException>(() => new CSVWriter((Stream)null));
        }

        /// <summary>
        /// Verifies that constructor throws ArgumentNullException for null TextWriter.
        /// </summary>
        /// <remarks>
        /// Ensures proper validation of constructor parameters.
        /// </remarks>
        [TestMethod]
        public void Constructor_WithNullTextWriter_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            AssertionHelpers.AssertThrows<ArgumentNullException>(() => new CSVWriter((TextWriter)null));
        }

        /// <summary>
        /// Verifies that WriteHeader correctly writes column headers.
        /// </summary>
        /// <remarks>
        /// Ensures header row is properly formatted and written.
        /// </remarks>
        [TestMethod]
        public void WriteHeader_ValidColumns_ShouldWriteCorrectly()
        {
            // Arrange
            using var stringWriter = new StringWriter();
            using var writer = new CSVWriter(stringWriter);
            var columns = new[] { "Name", "Age", "Email" };

            // Act
            writer.WriteHeader(columns);
            writer.Flush();

            // Assert
            var result = stringWriter.ToString();
            result.Should().Contain("\"Name\",\"Age\",\"Email\""); // Default format quotes headers
        }

        /// <summary>
        /// Verifies that WriteHeader with quotes works correctly.
        /// </summary>
        /// <remarks>
        /// Ensures quoted headers are properly formatted.
        /// </remarks>
        [TestMethod]
        public void WriteHeader_WithQuoting_ShouldWriteQuotedHeaders()
        {
            // Arrange
            using var stringWriter = new StringWriter();
            var format = new CSVFormat { HeaderQuoted = true, QuoteChar = '"' };
            using var writer = new CSVWriter(stringWriter, format);
            var columns = new[] { "Name", "Age", "Email" };

            // Act
            writer.WriteHeader(columns);
            writer.Flush();

            // Assert
            var result = stringWriter.ToString();
            result.Should().Contain("\"Name\",\"Age\",\"Email\"");
        }

        /// <summary>
        /// Verifies that WriteHeader throws exception when called twice.
        /// </summary>
        /// <remarks>
        /// Ensures header can only be written once.
        /// </remarks>
        [TestMethod]
        public void WriteHeader_CalledTwice_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var stringWriter = new StringWriter();
            using var writer = new CSVWriter(stringWriter);
            var columns = new[] { "Name", "Age" };
            writer.WriteHeader(columns);

            // Act & Assert
            AssertionHelpers.AssertThrows<InvalidOperationException>(() => writer.WriteHeader(columns));
        }

        /// <summary>
        /// Verifies that WriteHeader throws ArgumentNullException for null columns.
        /// </summary>
        /// <remarks>
        /// Ensures proper validation of header parameters.
        /// </remarks>
        [TestMethod]
        public void WriteHeader_WithNullColumns_ShouldThrowArgumentNullException()
        {
            // Arrange
            using var stringWriter = new StringWriter();
            using var writer = new CSVWriter(stringWriter);

            // Act & Assert
            AssertionHelpers.AssertThrows<ArgumentNullException>(() => writer.WriteHeader(null));
        }

        /// <summary>
        /// Verifies that WriteRow correctly writes data rows.
        /// </summary>
        /// <remarks>
        /// Ensures data rows are properly formatted and written.
        /// </remarks>
        [TestMethod]
        public void WriteRow_ValidData_ShouldWriteCorrectly()
        {
            // Arrange
            using var stringWriter = new StringWriter();
            var format = new CSVFormat { HasHeader = false, DataQuoted = true, QuoteChar = '"' };
            using var writer = new CSVWriter(stringWriter, format);
            var values = new[] { "John Doe", "30", "john@example.com" };

            // Act
            writer.WriteRow(values);
            writer.Flush();

            // Assert
            var result = stringWriter.ToString();
            result.Should().Contain("\"John Doe\",\"30\",\"john@example.com\""); // Default format quotes data
        }

        /// <summary>
        /// Verifies that WriteRow with special characters requires quoting.
        /// </summary>
        /// <remarks>
        /// Ensures values with delimiters or quotes are properly escaped.
        /// </remarks>
        [TestMethod]
        public void WriteRow_WithSpecialCharacters_ShouldQuoteValues()
        {
            // Arrange
            using var stringWriter = new StringWriter();
            var format = new CSVFormat { HasHeader = false, QuoteChar = '"' };
            using var writer = new CSVWriter(stringWriter, format);
            var values = new[] { "Smith, John", "30", "john\"doe@example.com" };

            // Act
            writer.WriteRow(values);
            writer.Flush();

            // Assert
            var result = stringWriter.ToString();
            result.Should().Contain("\"Smith, John\""); // Comma requires quoting
            result.Should().Contain("john\"\"doe@example.com"); // Quote escaped as double quote
        }

        /// <summary>
        /// Verifies that WriteRow throws exception when header is required but not written.
        /// </summary>
        /// <remarks>
        /// Ensures header requirement is enforced.
        /// </remarks>
        [TestMethod]
        public void WriteRow_HeaderRequiredButNotWritten_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var stringWriter = new StringWriter();
            var format = new CSVFormat { HasHeader = true };
            using var writer = new CSVWriter(stringWriter, format);
            var values = new[] { "John", "30" };

            // Act & Assert
            AssertionHelpers.AssertThrows<InvalidOperationException>(() => writer.WriteRow(values));
        }

        /// <summary>
        /// Verifies that WriteRow throws exception for field count mismatch.
        /// </summary>
        /// <remarks>
        /// Ensures data consistency with header column count.
        /// </remarks>
        [TestMethod]
        public void WriteRow_FieldCountMismatch_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var stringWriter = new StringWriter();
            using var writer = new CSVWriter(stringWriter);
            var columns = new[] { "Name", "Age", "Email" };
            writer.WriteHeader(columns);

            // Act & Assert
            var wrongFieldCount = new[] { "John", "30" }; // Missing email field
            AssertionHelpers.AssertThrows<InvalidOperationException>(() => writer.WriteRow(wrongFieldCount));
        }

        /// <summary>
        /// Verifies that WriteRow throws ArgumentNullException for null values.
        /// </summary>
        /// <remarks>
        /// Ensures proper validation of row data.
        /// </remarks>
        [TestMethod]
        public void WriteRow_WithNullValues_ShouldThrowArgumentNullException()
        {
            // Arrange
            using var stringWriter = new StringWriter();
            using var writer = new CSVWriter(stringWriter);

            // Act & Assert
            AssertionHelpers.AssertThrows<ArgumentNullException>(() => writer.WriteRow(null));
        }

        /// <summary>
        /// Verifies that WriteRows correctly writes multiple data rows.
        /// </summary>
        /// <remarks>
        /// Ensures multiple rows are properly written in sequence.
        /// </remarks>
        [TestMethod]
        public void WriteRows_ValidData_ShouldWriteAllRows()
        {
            // Arrange
            using var stringWriter = new StringWriter();
            var format = new CSVFormat { HasHeader = false, DataQuoted = true, QuoteChar = '"' };
            using var writer = new CSVWriter(stringWriter, format);
            var rows = new[]
            {
                new[] { "John", "30", "john@example.com" },
                new[] { "Jane", "25", "jane@example.com" },
                new[] { "Bob", "35", "bob@example.com" }
            };

            // Act
            writer.WriteRows(rows);
            writer.Flush();

            // Assert
            var result = stringWriter.ToString();
            result.Should().Contain("\"John\",\"30\",\"john@example.com\"");
            result.Should().Contain("\"Jane\",\"25\",\"jane@example.com\"");
            result.Should().Contain("\"Bob\",\"35\",\"bob@example.com\"");
        }

        /// <summary>
        /// Verifies that WriteRows throws ArgumentNullException for null rows.
        /// </summary>
        /// <remarks>
        /// Ensures proper validation of rows parameter.
        /// </remarks>
        [TestMethod]
        public void WriteRows_WithNullRows_ShouldThrowArgumentNullException()
        {
            // Arrange
            using var stringWriter = new StringWriter();
            using var writer = new CSVWriter(stringWriter);

            // Act & Assert
            AssertionHelpers.AssertThrows<ArgumentNullException>(() => writer.WriteRows(null));
        }

        /// <summary>
        /// Verifies that WriteRecords correctly writes CSV records with headers.
        /// </summary>
        /// <remarks>
        /// Ensures ICSVRecord objects are properly written with column mapping.
        /// </remarks>
        [TestMethod]
        public void WriteRecords_WithValidRecords_ShouldWriteCorrectly()
        {
            // Arrange
            using var stringWriter = new StringWriter();
            using var writer = new CSVWriter(stringWriter);
            var records = new[]
            {
                new TestRecord { Name = "John Doe", Age = 30, Email = "john@example.com" },
                new TestRecord { Name = "Jane Smith", Age = 25, Email = "jane@example.com" }
            };

            // Act
            writer.WriteRecords(records);
            writer.Flush();

            // Assert
            var result = stringWriter.ToString();
            result.Should().Contain("\"Name\",\"Age\",\"Email\""); // Header with default quoting
            result.Should().Contain("\"John Doe\",\"30\",\"john@example.com\""); // Data with default quoting
            result.Should().Contain("\"Jane Smith\",\"25\",\"jane@example.com\"");
        }

        /// <summary>
        /// Verifies that WriteRecords handles empty collection gracefully.
        /// </summary>
        /// <remarks>
        /// Ensures empty collections don't cause errors.
        /// </remarks>
        [TestMethod]
        public void WriteRecords_WithEmptyCollection_ShouldNotWriteAnything()
        {
            // Arrange
            using var stringWriter = new StringWriter();
            using var writer = new CSVWriter(stringWriter);
            var records = new TestRecord[0];

            // Act
            writer.WriteRecords(records);
            writer.Flush();

            // Assert
            var result = stringWriter.ToString();
            result.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that WriteRecords throws ArgumentNullException for null records.
        /// </summary>
        /// <remarks>
        /// Ensures proper validation of records parameter.
        /// </remarks>
        [TestMethod]
        public void WriteRecords_WithNullRecords_ShouldThrowArgumentNullException()
        {
            // Arrange
            using var stringWriter = new StringWriter();
            using var writer = new CSVWriter(stringWriter);

            // Act & Assert
            AssertionHelpers.AssertThrows<ArgumentNullException>(() => writer.WriteRecords<TestRecord>(null));
        }

        /// <summary>
        /// Verifies that WriteRecords reports progress correctly.
        /// </summary>
        /// <remarks>
        /// Ensures progress events are fired during record writing.
        /// </remarks>
        [TestMethod]
        public void WriteRecords_ShouldReportProgress()
        {
            // Arrange
            using var stringWriter = new StringWriter();
            using var writer = new CSVWriter(stringWriter);
            var progressReports = new List<float>();
            writer.Progress += (sender, e) => progressReports.Add(e.Progress);

            // Create enough records to trigger progress reporting
            var records = Enumerable.Range(0, 250)
                .Select(i => new TestRecord { Name = $"Person{i}", Age = 20 + i % 50, Email = $"person{i}@example.com" })
                .ToArray();

            // Act
            writer.WriteRecords(records);

            // Assert
            progressReports.Should().NotBeEmpty("progress should be reported");
            progressReports.Last().Should().Be(100f, "final progress should be 100%");
        }

        /// <summary>
        /// Verifies that WriteObjects correctly writes POCO objects.
        /// </summary>
        /// <remarks>
        /// Ensures plain objects can be written using reflection.
        /// </remarks>
        [TestMethod]
        public void WriteObjects_WithValidObjects_ShouldWriteCorrectly()
        {
            // Arrange
            using var stringWriter = new StringWriter();
            using var writer = new CSVWriter(stringWriter);
            var objects = new[]
            {
                new SimplePerson { Name = "John", Age = 30, City = "New York" },
                new SimplePerson { Name = "Jane", Age = 25, City = "Chicago" }
            };

            // Act
            writer.WriteObjects(objects);
            writer.Flush();

            // Assert
            var result = stringWriter.ToString();
            result.Should().Contain("\"Name\",\"Age\",\"City\""); // Header with default quoting
            result.Should().Contain("\"John\",\"30\",\"New York\""); // Data with default quoting
            result.Should().Contain("\"Jane\",\"25\",\"Chicago\"");
        }

        /// <summary>
        /// Verifies that WriteObjects without header works correctly.
        /// </summary>
        /// <remarks>
        /// Ensures WriteObjects can skip header when requested.
        /// </remarks>
        [TestMethod]
        public void WriteObjects_WithoutHeader_ShouldNotWriteHeader()
        {
            // Arrange
            using var stringWriter = new StringWriter();
            var format = new CSVFormat { HasHeader = false, DataQuoted = true, QuoteChar = '"' };
            using var writer = new CSVWriter(stringWriter, format);
            var objects = new[]
            {
                new SimplePerson { Name = "John", Age = 30, City = "New York" }
            };

            // Act
            writer.WriteObjects(objects, includeHeader: false);
            writer.Flush();

            // Assert
            var result = stringWriter.ToString();
            result.Should().NotContain("\"Name\",\"Age\",\"City\""); // No header
            result.Should().Contain("\"John\",\"30\",\"New York\""); // Data with default quoting
        }

        /// <summary>
        /// Verifies that WriteObjects handles empty collection gracefully.
        /// </summary>
        /// <remarks>
        /// Ensures empty collections don't cause errors.
        /// </remarks>
        [TestMethod]
        public void WriteObjects_WithEmptyCollection_ShouldNotWriteAnything()
        {
            // Arrange
            using var stringWriter = new StringWriter();
            using var writer = new CSVWriter(stringWriter);
            var objects = new SimplePerson[0];

            // Act
            writer.WriteObjects(objects);
            writer.Flush();

            // Assert
            var result = stringWriter.ToString();
            result.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that WriteObjects throws ArgumentNullException for null objects.
        /// </summary>
        /// <remarks>
        /// Ensures proper validation of objects parameter.
        /// </remarks>
        [TestMethod]
        public void WriteObjects_WithNullObjects_ShouldThrowArgumentNullException()
        {
            // Arrange
            using var stringWriter = new StringWriter();
            using var writer = new CSVWriter(stringWriter);

            // Act & Assert
            AssertionHelpers.AssertThrows<ArgumentNullException>(() => writer.WriteObjects<SimplePerson>(null));
        }

        /// <summary>
        /// Verifies that CSVWriter handles different delimiters correctly.
        /// </summary>
        /// <remarks>
        /// Ensures custom delimiters are properly used in output.
        /// </remarks>
        [TestMethod]
        public void WriteRow_WithCustomDelimiter_ShouldUseCorrectDelimiter()
        {
            // Arrange
            using var stringWriter = new StringWriter();
            var format = new CSVFormat { Delimiter = ';', HasHeader = false };
            using var writer = new CSVWriter(stringWriter, format);
            var values = new[] { "John", "30", "Engineer" };

            // Act
            writer.WriteRow(values);
            writer.Flush();

            // Assert
            var result = stringWriter.ToString();
            result.Should().Contain("John;30;Engineer");
        }

        /// <summary>
        /// Verifies that CSVWriter trims values when configured to do so.
        /// </summary>
        /// <remarks>
        /// Ensures TrimValues setting works correctly.
        /// </remarks>
        [TestMethod]
        public void WriteRow_WithTrimValues_ShouldTrimWhitespace()
        {
            // Arrange
            using var stringWriter = new StringWriter();
            var format = new CSVFormat { TrimValues = true, HasHeader = false };
            using var writer = new CSVWriter(stringWriter, format);
            var values = new[] { "  John  ", " 30 ", "Engineer   " };

            // Act
            writer.WriteRow(values);
            writer.Flush();

            // Assert
            var result = stringWriter.ToString();
            result.Should().Contain("John,30,Engineer");
            result.Should().NotContain("  John  ");
        }

        /// <summary>
        /// Verifies that CSVWriter handles null values in data correctly.
        /// </summary>
        /// <remarks>
        /// Ensures null values are converted to empty strings.
        /// </remarks>
        [TestMethod]
        public void WriteRow_WithNullValues_ShouldWriteEmptyStrings()
        {
            // Arrange
            using var stringWriter = new StringWriter();
            var format = new CSVFormat { HasHeader = false, DataQuoted = true, QuoteChar = '"' };
            using var writer = new CSVWriter(stringWriter, format);
            var values = new[] { "John", null, "Engineer" };

            // Act
            writer.WriteRow(values);
            writer.Flush();

            // Assert
            var result = stringWriter.ToString();
            result.Should().Contain("\"John\",\"\",\"Engineer\""); // Empty string is quoted too
        }

        /// <summary>
        /// Verifies that CSVWriter handles newlines in data correctly.
        /// </summary>
        /// <remarks>
        /// Ensures values with newlines are properly quoted.
        /// </remarks>
        [TestMethod]
        public void WriteRow_WithNewlines_ShouldQuoteValues()
        {
            // Arrange
            using var stringWriter = new StringWriter();
            var format = new CSVFormat { HasHeader = false, QuoteChar = '"' };
            using var writer = new CSVWriter(stringWriter, format);
            var values = new[] { "John\nDoe", "30", "Engineer" };

            // Act
            writer.WriteRow(values);
            writer.Flush();

            // Assert
            var result = stringWriter.ToString();
            result.Should().Contain("\"John\nDoe\"");
        }

        /// <summary>
        /// Verifies that Flush method works correctly.
        /// </summary>
        /// <remarks>
        /// Ensures data is properly flushed to the underlying stream.
        /// </remarks>
        [TestMethod]
        public void Flush_ShouldFlushUnderlyingWriter()
        {
            // Arrange
            using var stringWriter = new StringWriter();
            using var writer = new CSVWriter(stringWriter);
            var values = new[] { "John", "30", "Engineer" };

            // Act
            writer.WriteHeader(new[] { "Name", "Age", "Job" });
            writer.WriteRow(values);
            writer.Flush();

            // Assert
            var result = stringWriter.ToString();
            result.Should().NotBeEmpty();
            result.Should().Contain("\"Name\",\"Age\",\"Job\""); // Default format quotes headers
            result.Should().Contain("\"John\",\"30\",\"Engineer\""); // Default format quotes data
        }

        /// <summary>
        /// Verifies that Dispose method works correctly for file-based writers.
        /// </summary>
        /// <remarks>
        /// Ensures proper resource cleanup for file-based writers.
        /// </remarks>
        [TestMethod]
        public void Dispose_FileBasedWriter_ShouldCloseFile()
        {
            // Arrange
            var filePath = TempFileHelper.GetTempFilePath(".csv");
            var writer = new CSVWriter(filePath);

            // Act
            writer.WriteHeader(new[] { "Name", "Age" });
            writer.WriteRow(new[] { "John", "30" });
            writer.Dispose();

            // Assert
            // File should be created and contain data
            File.Exists(filePath).Should().BeTrue();
            var content = File.ReadAllText(filePath);
            content.Should().Contain("\"Name\",\"Age\""); // Default format quotes headers
            content.Should().Contain("\"John\",\"30\""); // Default format quotes data
        }

        /// <summary>
        /// Performance test for writing large CSV files.
        /// </summary>
        /// <remarks>
        /// Ensures CSVWriter can handle large datasets efficiently.
        /// </remarks>
        [TestMethod]
        public void WriteRecords_LargeDataset_ShouldPerformEfficiently()
        {
            // Arrange
            using var stringWriter = new StringWriter();
            using var writer = new CSVWriter(stringWriter);
            var records = Enumerable.Range(0, 1000)
                .Select(i => new TestRecord 
                { 
                    Name = $"Person{i}", 
                    Age = 20 + i % 50, 
                    Email = $"person{i}@example.com" 
                })
                .ToArray();

            var startTime = DateTime.UtcNow;

            // Act
            writer.WriteRecords(records);
            writer.Flush();

            // Assert
            var duration = DateTime.UtcNow - startTime;
            duration.Should().BeLessThan(TimeSpan.FromSeconds(5), "should write 1000 records quickly");
            
            var result = stringWriter.ToString();
            result.Should().NotBeEmpty();
            result.Should().Contain("\"Name\",\"Age\",\"Email\""); // Header with default quoting
        }

        /// <summary>
        /// Verifies that ProgressEventArgs works correctly.
        /// </summary>
        /// <remarks>
        /// Ensures progress event argument properties work as expected.
        /// </remarks>
        [TestMethod]
        public void ProgressEventArgs_ShouldWorkCorrectly()
        {
            // Arrange & Act
            var progressArgs = new CSVWriter.ProgressEventArgs(75.5f);

            // Assert
            progressArgs.Progress.Should().Be(75.5f);
        }
    }
}