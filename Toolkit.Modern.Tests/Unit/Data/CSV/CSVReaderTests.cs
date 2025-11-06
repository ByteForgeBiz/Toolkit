using AwesomeAssertions;
using ByteForge.Toolkit.Tests.Helpers;
using System.Text;

namespace ByteForge.Toolkit.Tests.Unit.Data.CSV
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("Data")]
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

        /// <summary>
        /// Verifies that the CSVReader constructor creates an instance.
        /// </summary>
        /// <remarks>
        /// Ensures the CSVReader can be instantiated for parsing operations.
        /// </remarks>
        [TestMethod]
        public void Constructor_ShouldCreateInstance()
        {
            // Arrange & Act
            var reader = new CSVReader();

            // Assert
            reader.Should().NotBeNull();
            reader.RowHandler.Should().BeNull("row handler should be null by default");
        }

        /// <summary>
        /// Reads a valid CSV file and verifies correct parsing.
        /// </summary>
        /// <remarks>
        /// Ensures the reader parses standard CSV files and extracts data rows accurately.
        /// </remarks>
        [TestMethod]
        public void ReadFile_ValidCsvFile_ShouldParseCorrectly()
        {
            // Arrange
            var csvContent = "Name,Age,Email\nJohn Doe,30,john@example.com\nJane Smith,25,jane@example.com";
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader();
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

        /// <summary>
        /// Reads a CSV file with semicolon delimiter and verifies correct parsing.
        /// </summary>
        /// <remarks>
        /// Ensures delimiter detection and parsing flexibility for different CSV formats.
        /// </remarks>
        [TestMethod]
        public void ReadFile_CsvWithSemicolonDelimiter_ShouldParseCorrectly()
        {
            // Arrange
            var csvContent = "Name;Age;Email\nJohn Doe;30;john@example.com\nJane Smith;25;jane@example.com";
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader();
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

        /// <summary>
        /// Reads a CSV file with tab delimiter and verifies correct parsing.
        /// </summary>
        /// <remarks>
        /// Ensures support for tab-delimited files, common in data exports.
        /// </remarks>
        [TestMethod]
        public void ReadFile_CsvWithTabDelimiter_ShouldParseCorrectly()
        {
            // Arrange
            var csvContent = "Name\tAge\tEmail\nJohn Doe\t30\tjohn@example.com\nJane Smith\t25\tjane@example.com";
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader();
            var processedRows = new List<string[]>();

            reader.RowHandler = (row, status, rawLine) =>
            {
                if (status == CSVReader.CSVRowStatus.OK)
                    processedRows.Add([.. row.Values]);
                return true;
            };

            // Act
            reader.ReadFile(filePath);

            // Assert
            processedRows.Should().HaveCount(2);
            processedRows[0].Should().BeEquivalentTo(["John Doe", "30", "john@example.com"]);
        }

        /// <summary>
        /// Reads a CSV file with quoted fields and verifies correct parsing.
        /// </summary>
        /// <remarks>
        /// Ensures quoted values are handled, supporting embedded delimiters and special characters.
        /// </remarks>
        [TestMethod]
        public void ReadFile_CsvWithQuotedFields_ShouldParseCorrectly()
        {
            // Arrange
            var csvContent = "\"Name\",\"Age\",\"Description\"\n\"John Doe\",\"30\",\"Software Engineer\"\n\"Jane Smith\",\"25\",\"Product Manager\"";
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader();
            var processedRows = new List<string[]>();

            reader.RowHandler = (row, status, rawLine) =>
            {
                if (status == CSVReader.CSVRowStatus.OK)
                    processedRows.Add([.. row.Values]);
                return true;
            };

            // Act
            reader.ReadFile(filePath);

            // Assert
            processedRows.Should().HaveCount(2);
            processedRows[0].Should().BeEquivalentTo(["John Doe", "30", "Software Engineer"]);
            processedRows[1].Should().BeEquivalentTo(["Jane Smith", "25", "Product Manager"]);
        }

        /// <summary>
        /// Reads a CSV file with escaped quotes and verifies correct parsing.
        /// </summary>
        /// <remarks>
        /// Ensures escaped quotes are interpreted correctly, preventing data corruption.
        /// </remarks>
        [TestMethod]
        public void ReadFile_CsvWithEscapedQuotes_ShouldParseCorrectly()
        {
            // Arrange
            var csvContent = "\"Name\",\"Quote\"\n\"John \"\"Johnny\"\" Doe\",\"He said, \"\"Hello\"\"\"\n\"Jane O'Connor\",\"Single 'quote'\"";
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader();
            var processedRows = new List<string[]>();

            reader.RowHandler = (row, status, rawLine) =>
            {
                if (status == CSVReader.CSVRowStatus.OK)
                    processedRows.Add([.. row.Values]);
                return true;
            };

            // Act
            reader.ReadFile(filePath);

            // Assert
            processedRows.Should().HaveCount(2);
            processedRows[0].Should().BeEquivalentTo(["John \"Johnny\" Doe", "He said, \"Hello\""]);
            processedRows[1].Should().BeEquivalentTo(["Jane O'Connor", "Single 'quote'"]);
        }

        /// <summary>
        /// Reads a CSV file with newlines in quoted fields and verifies correct parsing.
        /// </summary>
        /// <remarks>
        /// Ensures multi-line fields are supported, common in real-world CSVs.
        /// </remarks>
        [TestMethod]
        public void ReadFile_CsvWithNewlinesInQuotedFields_ShouldParseCorrectly()
        {
            // Arrange
            var csvContent = "\"Name\",\"Description\"\n\"John Doe\",\"Line 1\nLine 2\"\n\"Jane Smith\",\"Single line\"";
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader();
            var processedRows = new List<string[]>();

            reader.RowHandler = (row, status, rawLine) =>
            {
                if (status == CSVReader.CSVRowStatus.OK)
                    processedRows.Add([.. row.Values]);
                return true;
            };

            // Act
            reader.ReadFile(filePath);

            // Assert
            processedRows.Should().HaveCount(2);
            processedRows[0].Should().BeEquivalentTo(["John Doe", "Line 1\nLine 2"]);
            processedRows[1].Should().BeEquivalentTo(["Jane Smith", "Single line"]);
        }

        /// <summary>
        /// Reads an empty CSV file and verifies graceful handling.
        /// </summary>
        /// <remarks>
        /// Ensures empty files do not cause errors or unexpected output.
        /// </remarks>
        [TestMethod]
        public void ReadFile_EmptyFile_ShouldHandleGracefully()
        {
            // Arrange
            var filePath = TempFileHelper.CreateTempCsvFile("");
            var reader = new CSVReader();
            var processedRows = new List<string[]>();

            reader.RowHandler = (row, status, rawLine) =>
            {
                if (status == CSVReader.CSVRowStatus.OK)
                    processedRows.Add([.. row.Values]);
                return true;
            };

            // Act
            reader.ReadFile(filePath);

            // Assert
            processedRows.Should().BeEmpty("empty file should produce no rows");
        }

        /// <summary>
        /// Reads a header-only CSV file and verifies graceful handling.
        /// </summary>
        /// <remarks>
        /// Ensures files with only headers do not produce data rows.
        /// </remarks>
        [TestMethod]
        public void ReadFile_HeaderOnlyFile_ShouldHandleGracefully()
        {
            // Arrange
            var csvContent = "Name,Age,Email";
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader();
            var processedRows = new List<string[]>();

            reader.RowHandler = (row, status, rawLine) =>
            {
                if (status == CSVReader.CSVRowStatus.OK)
                    processedRows.Add([.. row.Values]);
                return true;
            };

            // Act
            reader.ReadFile(filePath);

            // Assert
            processedRows.Should().BeEmpty("header-only file should produce no data rows");
        }

        /// <summary>
        /// Attempts to read a non-existent CSV file, expecting an exception.
        /// </summary>
        /// <remarks>
        /// Verifies error handling for missing files.
        /// </remarks>
        [TestMethod]
        public void ReadFile_NonExistentFile_ShouldThrowException()
        {
            // Arrange
            var reader = new CSVReader();
            var nonExistentPath = @"C:\NonExistent\File.csv";

            // Act & Assert
            AssertionHelpers.AssertThrows<FileNotFoundException>(() => reader.ReadFile(nonExistentPath));
        }

        /// <summary>
        /// Reads a CSV file with progress reporting and verifies progress events.
        /// </summary>
        /// <remarks>
        /// Ensures progress events are fired during file reading, supporting UI feedback.
        /// </remarks>
        [TestMethod]
        public void ReadFile_WithProgressReporting_ShouldReportProgress()
        {
            // Arrange
            var csvContent = "Name,Age\n" + string.Join("\n", Enumerable.Range(1, 100).Select(i => $"User{i},{20 + i}"));
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader();
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

        /// <summary>
        /// Reads a valid CSV stream and verifies correct parsing.
        /// </summary>
        /// <remarks>
        /// Ensures stream-based reading is supported for in-memory data.
        /// </remarks>
        [TestMethod]
        public void ReadStream_ValidCsvStream_ShouldParseCorrectly()
        {
            // Arrange
            var csvContent = "Name,Age,Email\nJohn Doe,30,john@example.com\nJane Smith,25,jane@example.com";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));
            var reader = new CSVReader();
            var processedRows = new List<string[]>();

            reader.RowHandler = (row, status, rawLine) =>
            {
                if (status == CSVReader.CSVRowStatus.OK)
                    processedRows.Add([.. row.Values]);
                return true;
            };

            // Act
            reader.ReadStream(stream);

            // Assert
            processedRows.Should().HaveCount(2);
            processedRows[0].Should().BeEquivalentTo(["John Doe", "30", "john@example.com"]);
        }

        /// <summary>
        /// Attempts to read a null stream, expecting an exception.
        /// </summary>
        /// <remarks>
        /// Verifies error handling for null input streams.
        /// </remarks>
        [TestMethod]
        public void ReadStream_NullStream_ShouldThrowException()
        {
            // Arrange
            var reader = new CSVReader();

            // Act & Assert
            AssertionHelpers.AssertThrows<ArgumentNullException>(() => reader.ReadStream(null));
        }

        /// <summary>
        /// Sets a custom CSV format and verifies parsing with the specified delimiter.
        /// </summary>
        /// <remarks>
        /// Ensures custom formats are respected, supporting non-standard CSVs.
        /// </remarks>
        [TestMethod]
        public void SetFormat_CustomFormat_ShouldUseSpecifiedFormat()
        {
            // Arrange
            var csvContent = "Name|Age|Email\nJohn Doe|30|john@example.com";
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader();
            var customFormat = new CSVFormat { Delimiter = '|' };
            var processedRows = new List<string[]>();

            reader.Format = customFormat;
            reader.RowHandler = (row, status, rawLine) =>
            {
                if (status == CSVReader.CSVRowStatus.OK)
                    processedRows.Add([.. row.Values]);
                return true;
            };

            // Act
            reader.ReadFile(filePath);

            // Assert
            processedRows.Should().HaveCount(1);
            processedRows[0].Should().BeEquivalentTo(["John Doe", "30", "john@example.com"]);
        }

        /// <summary>
        /// Reads a CSV file with Unicode content and verifies correct parsing.
        /// </summary>
        /// <remarks>
        /// Ensures Unicode characters are supported, preventing data loss.
        /// </remarks>
        [TestMethod]
        public void ReadFile_UnicodeContent_ShouldHandleCorrectly()
        {
            // Arrange
            var csvContent = "Name,Description\n\"José González\",\"Español\"\n\"李小明\",\"中文\"\n\"🌍 Earth\",\"Unicode emoji\"";
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader();
            var processedRows = new List<string[]>();

            reader.RowHandler = (row, status, rawLine) =>
            {
                if (status == CSVReader.CSVRowStatus.OK)
                    processedRows.Add([.. row.Values]);
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

        /// <summary>
        /// Reads a large CSV file and verifies efficient processing.
        /// </summary>
        /// <remarks>
        /// Ensures the reader can handle large files without performance issues.
        /// </remarks>
        [TestMethod]
        public void ReadFile_LargeFile_ShouldHandleEfficiently()
        {
            // Arrange
            var rows = 1000;
            var csvLines = new List<string> { "Name,Age,Email" };
            csvLines.AddRange(Enumerable.Range(1, rows).Select(i => $"User{i},{20 + i},user{i}@example.com"));
            var csvContent = string.Join("\n", csvLines);
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader();
            var processedRowCount = 0;
            var startTime = DateTime.UtcNow;

            reader.RowHandler = (row, status, rawLine) =>
            {
                if (status == CSVReader.CSVRowStatus.OK)
                    processedRowCount++;
                return true;
            };

            // Act
            reader.ReadFile(filePath);

            // Assert
            var duration = DateTime.UtcNow - startTime;
            processedRowCount.Should().Be(rows);
            duration.Should().BeLessThan(TimeSpan.FromSeconds(5), "should process large file efficiently");
        }

        /// <summary>
        /// Attempts to read a file without setting RowHandler, expecting an exception.
        /// </summary>
        /// <remarks>
        /// Verifies error handling for missing row processing logic.
        /// </remarks>
        [TestMethod]
        public void ReadFile_WithoutRowHandler_ShouldThrowException()
        {
            // Arrange
            var csvContent = "Name,Age\nJohn,30";
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader();

            // Act & Assert
            reader.Invoking(r => r.ReadFile(filePath))
                .Should().Throw<InvalidOperationException>("should throw if RowHandler is not set");
        }

        /// <summary>
        /// Reads a CSV file with empty fields and verifies correct parsing.
        /// </summary>
        /// <remarks>
        /// Ensures empty fields are handled, supporting sparse data.
        /// </remarks>
        [TestMethod]
        public void ReadFile_EmptyFields_ShouldHandleCorrectly()
        {
            // Arrange
            var csvContent = "Name,Age,Email\nJohn,,john@example.com\n,25,\n\"\",\"30\",\"\"";
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader();
            var processedRows = new List<string[]>();

            reader.RowHandler = (row, status, rawLine) =>
            {
                if (status == CSVReader.CSVRowStatus.OK)
                    processedRows.Add([.. row.Values]);
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

        /// <summary>
        /// Reads CSV files with different encodings and verifies correct parsing.
        /// </summary>
        /// <remarks>
        /// Ensures encoding flexibility for internationalization and compatibility.
        /// </remarks>
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
                var reader = new CSVReader();
                var processedRows = new List<string[]>();

                reader.RowHandler = (row, status, rawLine) =>
                {
                    if (status == CSVReader.CSVRowStatus.OK)
                        processedRows.Add([.. row.Values]);
                    return true;
                };

                // Act
                reader.Invoking(r => r.ReadFile(filePath))
                    .Should().NotThrow($"should handle {encoding.EncodingName} encoding");

                // Basic assertion - exact content may vary by encoding handling
                processedRows.Should().NotBeEmpty($"should process content with {encoding.EncodingName}");
            }
        }

        /// <summary>
        /// Auto-detects CSV format and verifies correct delimiter detection.
        /// </summary>
        /// <remarks>
        /// Ensures automatic format detection for user convenience.
        /// </remarks>
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
                var reader = new CSVReader();
                var detectedDelimiter = '\0';

                reader.RowHandler = (row, status, rawLine) =>
                {
                    // Check if format was detected correctly by ensuring proper parsing
                    if (status == CSVReader.CSVRowStatus.OK && row.Count == 3 && row.Values.First() == "John")
                        detectedDelimiter = expectedDelimiter; // Infer from successful parsing
                    return true;
                };

                // Act
                reader.ReadFile(filePath);

                // Assert
                detectedDelimiter.Should().Be(expectedDelimiter, $"should detect delimiter '{expectedDelimiter}'");
            }
        }

        /// <summary>
        /// Reads a malformed CSV file and verifies handling of malformed rows.
        /// </summary>
        /// <remarks>
        /// Ensures error rows are detected and reported, supporting robust parsing.
        /// </remarks>
        [TestMethod]
        public void ReadFile_MalformedCsvWithRowHandler_ShouldHandleMalformedRows()
        {
            // Arrange
            var csvContent = "Name,Age,Email\nJohn Doe,30,john@example.com\nJane Smith,25\nBob Wilson,35,bob@example.com,extra_field";
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader();
            var validRows = new List<IDictionary<string, string>>();
            var malformedRows = new List<string>();

            reader.RowHandler = (row, status, rawLine) =>
            {
                if (status == CSVReader.CSVRowStatus.OK)
                    validRows.Add(row);
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

        /// <summary>
        /// Reads a CSV file and verifies that RowHandler returning false stops processing.
        /// </summary>
        /// <remarks>
        /// Ensures row processing can be interrupted, supporting early termination scenarios.
        /// </remarks>
        [TestMethod]
        public void ReadFile_RowHandlerReturningFalse_ShouldStopProcessing()
        {
            // Arrange
            var csvContent = "Name,Age\nJohn,30\nJane,25\nBob,35";
            var filePath = TempFileHelper.CreateTempCsvFile(csvContent);
            var reader = new CSVReader();
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