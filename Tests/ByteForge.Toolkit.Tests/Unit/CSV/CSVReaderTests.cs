using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ByteForge.Toolkit;
using ByteForge.Toolkit.Logging;
using ByteForge.Toolkit.Tests.Helpers;
using FluentAssertions;

namespace ByteForge.Toolkit.Tests.Unit.CSV
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("CSV")]
    public class CSVReaderTests
    {
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

        [TestMethod]
        public void Constructor_ShouldCreateInstance()
        {
            // Arrange & Act
            var reader = new CSVReader(new NullLogger());

            // Assert
            reader.Should().NotBeNull();
            reader.RowHandler.Should().BeNull("row handler should be null by default");
        }

        [TestMethod]
        public void Constructor_WithNullLogger_ShouldCreateInstanceWithDefaultLogger()
        {
            // Arrange & Act
            var reader = new CSVReader(new NullLogger());

            // Assert
            reader.Should().NotBeNull();
            reader.RowHandler.Should().BeNull("row handler should be null by default");
        }

        [TestMethod]
        public void ReadFile_ValidCsvFile_ShouldParseCorrectly()
        {
            // Arrange
            var csvContent = "Name,Age,Email\nJohn Doe,30,john@example.com\nJane Smith,25,jane@example.com";
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader(new NullLogger());
            var processedRows = new List<(string[] columns, string[] values, string rawLine)>();

            reader.RowHandler = (row, status, rawLine) =>
            {
                if (status == CSVReader.CSVRowStatus.OK)
                {
                    var columns = row.Keys.ToArray();
                    var values = row.Values.ToArray();
                    processedRows.Add((columns, values, rawLine));
                }
                return true;
            };

            // Act
            reader.ReadFile(filePath);

            // Assert
            processedRows.Should().HaveCount(2, "should process 2 data rows (excluding header)");
            
            var firstRow = processedRows[0];
            firstRow.columns.Should().BeEquivalentTo(["Name", "Age", "Email"]);
            firstRow.values.Should().BeEquivalentTo(["John Doe", "30", "john@example.com"]);
            
            var secondRow = processedRows[1];
            secondRow.values.Should().BeEquivalentTo(["Jane Smith", "25", "jane@example.com"]);
        }

        [TestMethod]
        public void ReadFile_CsvWithSemicolonDelimiter_ShouldParseCorrectly()
        {
            // Arrange
            var csvContent = "Name;Age;Email\nJohn Doe;30;john@example.com\nJane Smith;25;jane@example.com";
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader(new NullLogger());
            var processedRows = new List<(string[] columns, string[] values)>();

            reader.RowHandler = (row, status, rawLine) =>
            {
                if (status == CSVReader.CSVRowStatus.OK)
                {
                    var columns = row.Keys.ToArray();
                    var values = row.Values.ToArray();
                    processedRows.Add((columns, values));
                }
                return true;
            };

            // Act
            reader.ReadFile(filePath);

            // Assert
            processedRows.Should().HaveCount(2);
            processedRows[0].values.Should().BeEquivalentTo(["John Doe", "30", "john@example.com"]);
        }

        [TestMethod]
        public void ReadFile_CsvWithTabDelimiter_ShouldParseCorrectly()
        {
            // Arrange
            var csvContent = "Name\tAge\tEmail\nJohn Doe\t30\tjohn@example.com\nJane Smith\t25\tjane@example.com";
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader(new NullLogger());
            var processedRows = new List<string[]>();

            reader.RowHandler = (row, status, rawLine) =>
            {
                if (status == CSVReader.CSVRowStatus.OK)
                {
                    processedRows.Add([.. row.Values]);
                }
                return true;
            };

            // Act
            reader.ReadFile(filePath);

            // Assert
            processedRows.Should().HaveCount(2);
            processedRows[0].Should().BeEquivalentTo(["John Doe", "30", "john@example.com"]);
        }

        [TestMethod]
        public void ReadFile_CsvWithQuotedFields_ShouldParseCorrectly()
        {
            // Arrange
            var csvContent = "\"Name\",\"Age\",\"Description\"\n\"John Doe\",\"30\",\"Software Engineer\"\n\"Jane Smith\",\"25\",\"Product Manager\"";
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader(new NullLogger());
            var processedRows = new List<string[]>();

            reader.RowHandler = (row, status, rawLine) =>
            {
                if (status == CSVReader.CSVRowStatus.OK)
                {
                    processedRows.Add([.. row.Values]);
                }
                return true;
            };

            // Act
            reader.ReadFile(filePath);

            // Assert
            processedRows.Should().HaveCount(2);
            processedRows[0].Should().BeEquivalentTo(["John Doe", "30", "Software Engineer"]);
            processedRows[1].Should().BeEquivalentTo(["Jane Smith", "25", "Product Manager"]);
        }

        [TestMethod]
        public void ReadFile_CsvWithEscapedQuotes_ShouldParseCorrectly()
        {
            // Arrange
            var csvContent = "\"Name\",\"Quote\"\n\"John \"\"Johnny\"\" Doe\",\"He said, \"\"Hello\"\"\"\n\"Jane O'Connor\",\"Single 'quote'\"";
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader(new NullLogger());
            var processedRows = new List<string[]>();

            reader.RowHandler = (row, status, rawLine) =>
            {
                if (status == CSVReader.CSVRowStatus.OK)
                {
                    processedRows.Add([.. row.Values]);
                }
                return true;
            };

            // Act
            reader.ReadFile(filePath);

            // Assert
            processedRows.Should().HaveCount(2);
            processedRows[0].Should().BeEquivalentTo(["John \"Johnny\" Doe", "He said, \"Hello\""]);
            processedRows[1].Should().BeEquivalentTo(["Jane O'Connor", "Single 'quote'"]);
        }

        [TestMethod]
        public void ReadFile_CsvWithNewlinesInQuotedFields_ShouldParseCorrectly()
        {
            // Arrange
            var csvContent = "\"Name\",\"Description\"\n\"John Doe\",\"Line 1\nLine 2\"\n\"Jane Smith\",\"Single line\"";
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader(new NullLogger());
            var processedRows = new List<string[]>();

            reader.RowHandler = (row, status, rawLine) =>
            {
                if (status == CSVReader.CSVRowStatus.OK)
                {
                    processedRows.Add([.. row.Values]);
                }
                return true;
            };

            // Act
            reader.ReadFile(filePath);

            // Assert
            processedRows.Should().HaveCount(2);
            processedRows[0].Should().BeEquivalentTo(["John Doe", "Line 1\nLine 2"]);
            processedRows[1].Should().BeEquivalentTo(["Jane Smith", "Single line"]);
        }

        [TestMethod]
        public void ReadFile_EmptyFile_ShouldHandleGracefully()
        {
            // Arrange
            var filePath = TempFileHelper.CreateTempCsvFile("");
            var reader = new CSVReader(new NullLogger());
            var processedRows = new List<string[]>();

            reader.RowHandler = (row, status, rawLine) =>
            {
                if (status == CSVReader.CSVRowStatus.OK)
                {
                    processedRows.Add([.. row.Values]);
                }
                return true;
            };

            // Act
            reader.ReadFile(filePath);

            // Assert
            processedRows.Should().BeEmpty("empty file should produce no rows");
        }

        [TestMethod]
        public void ReadFile_HeaderOnlyFile_ShouldHandleGracefully()
        {
            // Arrange
            var csvContent = "Name,Age,Email";
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader(new NullLogger());
            var processedRows = new List<string[]>();

            reader.RowHandler = (row, status, rawLine) =>
            {
                if (status == CSVReader.CSVRowStatus.OK)
                {
                    processedRows.Add([.. row.Values]);
                }
                return true;
            };

            // Act
            reader.ReadFile(filePath);

            // Assert
            processedRows.Should().BeEmpty("header-only file should produce no data rows");
        }

        [TestMethod]
        public void ReadFile_NonExistentFile_ShouldThrowException()
        {
            // Arrange
            var reader = new CSVReader(new NullLogger());
            var nonExistentPath = @"C:\NonExistent\File.csv";

            // Act & Assert
            AssertionHelpers.AssertThrows<FileNotFoundException>(() => reader.ReadFile(nonExistentPath));
        }

        [TestMethod]
        public void ReadFile_WithProgressReporting_ShouldReportProgress()
        {
            // Arrange
            var csvContent = "Name,Age\n" + string.Join("\n", Enumerable.Range(1, 100).Select(i => $"User{i},{20 + i}"));
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader(new NullLogger());
            var progressReports = new List<double>();

            reader.Progress += (sender, args) =>
            {
                progressReports.Add(args.Progress);
            };

            reader.RowHandler = (row, status, rawLine) => true;

            // Act
            reader.ReadFile(filePath);

            // Assert
            progressReports.Should().NotBeEmpty("should report progress");
            progressReports.Should().OnlyContain(p => p >= 0 && p <= 100, "progress should be between 0 and 100");
            progressReports.Last().Should().Be(100, "final progress should be 100%");
        }

        [TestMethod]
        public void ReadStream_ValidCsvStream_ShouldParseCorrectly()
        {
            // Arrange
            var csvContent = "Name,Age,Email\nJohn Doe,30,john@example.com\nJane Smith,25,jane@example.com";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));
            var reader = new CSVReader(new NullLogger());
            var processedRows = new List<string[]>();

            reader.RowHandler = (row, status, rawLine) =>
            {
                if (status == CSVReader.CSVRowStatus.OK)
                {
                    processedRows.Add([.. row.Values]);
                }
                return true;
            };

            // Act
            reader.ReadStream(stream);

            // Assert
            processedRows.Should().HaveCount(2);
            processedRows[0].Should().BeEquivalentTo(["John Doe", "30", "john@example.com"]);
        }

        [TestMethod]
        public void ReadStream_NullStream_ShouldThrowException()
        {
            // Arrange
            var reader = new CSVReader(new NullLogger());

            // Act & Assert
            AssertionHelpers.AssertThrows<ArgumentNullException>(() => reader.ReadStream(null));
        }

        [TestMethod]
        public void SetFormat_CustomFormat_ShouldUseSpecifiedFormat()
        {
            // Arrange
            var csvContent = "Name|Age|Email\nJohn Doe|30|john@example.com";
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader(new NullLogger());
            var customFormat = new CSVFormat { Delimiter = '|' };
            var processedRows = new List<string[]>();

            reader.Format = customFormat;
            reader.RowHandler = (row, status, rawLine) =>
            {
                if (status == CSVReader.CSVRowStatus.OK)
                {
                    processedRows.Add([.. row.Values]);
                }
                return true;
            };

            // Act
            reader.ReadFile(filePath);

            // Assert
            processedRows.Should().HaveCount(1);
            processedRows[0].Should().BeEquivalentTo(["John Doe", "30", "john@example.com"]);
        }

        [TestMethod]
        public void ReadFile_MalformedCsv_ShouldThrowException()
        {
            // Arrange
            var csvContent = "Name,Age,Email\nJohn Doe,30\nJane Smith,25,jane@example.com,extra_field";
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader(new NullLogger());
            var processedRows = new List<string[]>();

            reader.RowHandler = (row, status, rawLine) =>
            {
                if (status == CSVReader.CSVRowStatus.OK)
                {
                    processedRows.Add([.. row.Values]);
                }
                return true;
            };

            // Act
            reader.Invoking(r => r.ReadFile(filePath))
                .Should().Throw<InvalidDataException>("malformed CSV should throw exception");

            // Assert
            processedRows.Should().NotBeEmpty("should still process valid rows");
        }

        [TestMethod]
        public void ReadFile_UnicodeContent_ShouldHandleCorrectly()
        {
            // Arrange
            var csvContent = "Name,Description\n\"José González\",\"Español\"\n\"李小明\",\"中文\"\n\"🌍 Earth\",\"Unicode emoji\"";
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader(new NullLogger());
            var processedRows = new List<string[]>();

            reader.RowHandler = (row, status, rawLine) =>
            {
                if (status == CSVReader.CSVRowStatus.OK)
                {
                    processedRows.Add([.. row.Values]);
                }
                return true;
            };

            // Act
            reader.ReadFile(filePath);

            // Assert
            processedRows.Should().HaveCount(3);
            processedRows[0].Should().BeEquivalentTo(["José González", "Español"]);
            processedRows[1].Should().BeEquivalentTo(["李小明", "中文"]);
            processedRows[2].Should().BeEquivalentTo(["🌍 Earth", "Unicode emoji"]);
        }

        [TestMethod]
        public void ReadFile_LargeFile_ShouldHandleEfficiently()
        {
            // Arrange
            var rows = 1000;
            var csvLines = new List<string> { "Name,Age,Email" };
            csvLines.AddRange(Enumerable.Range(1, rows).Select(i => $"User{i},{20 + i},user{i}@example.com"));
            var csvContent = string.Join("\n", csvLines);
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader(new NullLogger());
            var processedRowCount = 0;
            var startTime = DateTime.UtcNow;

            reader.RowHandler = (row, status, rawLine) =>
            {
                if (status == CSVReader.CSVRowStatus.OK)
                {
                    processedRowCount++;
                }
                return true;
            };

            // Act
            reader.ReadFile(filePath);

            // Assert
            var duration = DateTime.UtcNow - startTime;
            processedRowCount.Should().Be(rows);
            duration.Should().BeLessThan(TimeSpan.FromSeconds(5), "should process large file efficiently");
        }

        [TestMethod]
        public void ReadFile_WithoutRowHandler_ShouldThrowException()
        {
            // Arrange
            var csvContent = "Name,Age\nJohn,30";
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader(new NullLogger());

            // Act & Assert
            reader.Invoking(r => r.ReadFile(filePath))
                .Should().Throw<InvalidOperationException>("should throw if RowHandler is not set");
        }

        [TestMethod]
        public void ReadFile_EmptyFields_ShouldHandleCorrectly()
        {
            // Arrange
            var csvContent = "Name,Age,Email\nJohn,,john@example.com\n,25,\n\"\",\"30\",\"\"";
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader(new NullLogger());
            var processedRows = new List<string[]>();

            reader.RowHandler = (row, status, rawLine) =>
            {
                if (status == CSVReader.CSVRowStatus.OK)
                {
                    processedRows.Add([.. row.Values]);
                }
                return true;
            };

            // Act
            reader.ReadFile(filePath);

            // Assert
            processedRows.Should().HaveCount(3);
            processedRows[0].Should().BeEquivalentTo(["John", "", "john@example.com"]);
            processedRows[1].Should().BeEquivalentTo(["", "25", ""]);
            processedRows[2].Should().BeEquivalentTo(["", "30", ""]);
        }

        [TestMethod]
        public void ReadFile_DifferentEncodings_ShouldHandleCorrectly()
        {
            // Arrange
            var csvContent = "Name,Description\nCafé,Résumé\nNaïve,Élite";
            
            var encodings = new[]
            {
                Encoding.UTF8,
                Encoding.UTF32,
                Encoding.Unicode
            };

            foreach (var encoding in encodings)
            {
                var bytes = encoding.GetBytes(csvContent);
                var filePath = TempFileHelper.CreateTempFile(bytes, ".csv");
                var reader = new CSVReader(new NullLogger());
                var processedRows = new List<string[]>();

                reader.RowHandler = (row, status, rawLine) =>
                {
                    if (status == CSVReader.CSVRowStatus.OK)
                    {
                        processedRows.Add([.. row.Values]);
                    }
                    return true;
                };

                // Act
                reader.Invoking(r => r.ReadFile(filePath))
                    .Should().NotThrow($"should handle {encoding.EncodingName} encoding");

                // Basic assertion - exact content may vary by encoding handling
                processedRows.Should().NotBeEmpty($"should process content with {encoding.EncodingName}");
            }
        }

        [TestMethod]
        public void AutoDetectFormat_ShouldDetectCorrectDelimiter()
        {
            // Arrange
            var testCases = new[]
            {
                ("Name,Age,Email\nJohn,30,john@example.com", ','),
                ("Name;Age;Email\nJohn;30;john@example.com", ';'),
                ("Name\tAge\tEmail\nJohn\t30\tjohn@example.com", '\t')
            };

            foreach (var (content, expectedDelimiter) in testCases)
            {
                var filePath = TempFileHelper.CreateTempCsvFile(content);
                var reader = new CSVReader(new NullLogger());
                var detectedDelimiter = '\0';

                reader.RowHandler = (row, status, rawLine) =>
                {
                    // Check if format was detected correctly by ensuring proper parsing
                    if (status == CSVReader.CSVRowStatus.OK && row.Count == 3 && row.Values.First() == "John")
                    {
                        detectedDelimiter = expectedDelimiter; // Infer from successful parsing
                    }
                    return true;
                };

                // Act
                reader.ReadFile(filePath);

                // Assert
                detectedDelimiter.Should().Be(expectedDelimiter, $"should detect delimiter '{expectedDelimiter}'");
            }
        }

        [TestMethod]
        public void ReadFile_MalformedCsvWithRowHandler_ShouldHandleMalformedRows()
        {
            // Arrange
            var csvContent = "Name,Age,Email\nJohn Doe,30,john@example.com\nJane Smith,25\nBob Wilson,35,bob@example.com,extra_field";
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader(new NullLogger());
            var validRows = new List<IDictionary<string, string>>();
            var malformedRows = new List<string>();

            reader.RowHandler = (row, status, rawLine) =>
            {
                if (status == CSVReader.CSVRowStatus.OK)
                {
                    validRows.Add(row);
                }
                else if (status == CSVReader.CSVRowStatus.Malformed)
                {
                    malformedRows.Add(rawLine);
                }
                return true; // Continue processing
            };

            // Act
            reader.ReadStream(new FileStream(filePath, FileMode.Open));

            // Assert
            validRows.Should().HaveCount(1, "should process 1 valid row");
            validRows[0]["Name"].Should().Be("John Doe");
            
            malformedRows.Should().HaveCount(2, "should detect 2 malformed rows");
            malformedRows[0].Should().Contain("Jane Smith,25");
            malformedRows[1].Should().Contain("Bob Wilson,35,bob@example.com,extra_field");
        }

        [TestMethod]
        public void ReadFile_RowHandlerReturningFalse_ShouldStopProcessing()
        {
            // Arrange
            var csvContent = "Name,Age\nJohn,30\nJane,25\nBob,35";
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader(new NullLogger());
            var processedRows = new List<IDictionary<string, string>>();

            reader.RowHandler = (row, status, rawLine) =>
            {
                if (status == CSVReader.CSVRowStatus.OK)
                {
                    processedRows.Add(row);
                    return processedRows.Count < 2; // Stop after 2 rows
                }
                return true;
            };

            // Act
            reader.ReadFile(filePath);

            // Assert
            processedRows.Should().HaveCount(2, "should stop processing after 2 rows");
            processedRows[0]["Name"].Should().Be("John");
            processedRows[1]["Name"].Should().Be("Jane");
        }
    }
}