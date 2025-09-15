using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ByteForge.Toolkit.Logging;

namespace ByteForge.Toolkit
{
    /*
     *   ___ _____   _____             _         
     *  / __/ __\ \ / / _ \___ __ _ __| |___ _ _ 
     * | (__\__ \\ V /|   / -_) _` / _` / -_) '_|
     *  \___|___/ \_/ |_|_\___\__,_\__,_\___|_|  
     *                                           
     */
    /// <summary>
    /// Provides utilities for reading and processing CSV files with flexible format support.
    /// </summary>
    public class CSVReader
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CSVReader"/> class with default logging.
        /// </summary>
        public CSVReader() { }

        /// <summary>
        /// Occurs to report the progress of the CSV reading process.
        /// </summary>
        public event EventHandler<ProgressEventArgs> Progress;

        /// <summary>
        /// Represents the status of a CSV row during processing.
        /// </summary>
        public enum CSVRowStatus
        {
            /// <summary>
            /// The row was processed successfully.
            /// </summary>
            OK,

            /// <summary>
            /// The row is malformed (e.g., incorrect field count).
            /// </summary>
            Malformed,

            /// <summary>
            /// End of file reached.
            /// </summary>
            EOF
        }

        /// <summary>
        /// Delegate for handling CSV rows with status information.
        /// </summary>
        /// <param name="row">Dictionary mapping column names to field values.</param>
        /// <param name="status">The status of the row (OK, Malformed, EOF).</param>
        /// <param name="rawLine">The raw CSV line.</param>
        /// <returns>True to continue processing, false to stop.</returns>
        public delegate bool CSVRowProcessorDelegate(IDictionary<string, string> row, CSVRowStatus status, string rawLine);
        /// <summary>
        /// Gets or sets the delegate to handle CSV rows with status information.
        /// </summary>
        /// <remarks>
        /// This delegate receives each row as a dictionary mapping column names to field values,
        /// along with a status indicating if the row is OK, Malformed, or EOF, and the raw CSV line.<br/>
        /// It should return <see langword="true"/> to continue processing further rows, or <see langword="false"/> to stop.
        /// </remarks>
        public CSVRowProcessorDelegate RowHandler { get; set; }

        /// <summary>
        /// Gets or sets the format to use for parsing. If not set, format will be auto-detected.
        /// </summary>
        public CSVFormat Format { get; set; }

        /// <summary>
        /// Reads the CSV file and processes the data using the new RowHandler.
        /// </summary>
        /// <param name="filePath">The path to the CSV file.</param>
        /// <param name="rowHandler">The handler to process each row.</param>
        public static void ReadFile(string filePath, CSVRowProcessorDelegate rowHandler)
        {
            var reader = new CSVReader { RowHandler = rowHandler };
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            reader.ReadStream(stream);
        }

        /// <summary>
        /// Reads the provided stream and processes the CSV data using the new RowHandler.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="rowHandler">The handler to process each row.</param>
        public static void ReadStream(Stream stream, CSVRowProcessorDelegate rowHandler)
        {
            var reader = new CSVReader { RowHandler = rowHandler };
            reader.ReadStream(stream);
        }

        /// <summary>
        /// Reads the CSV file and processes the data.
        /// </summary>
        /// <param name="filePath">The path to the CSV file.</param>
        /// <exception cref="ArgumentNullException">Thrown when the file path or data processor is null.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the file is not found.</exception>
        public void ReadFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            Log.Debug($"Reading CSV file: {filePath}");

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            ReadStream(stream);
        }

        /// <summary>
        /// Reads the provided stream and processes the CSV data.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <exception cref="ArgumentNullException">Thrown when the stream or data processor is null.</exception>
        /// <exception cref="InvalidDataException">Thrown when the CSV file is empty or has inconsistent field counts (when throwExceptions is true).</exception>
        public void ReadStream(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (RowHandler == null)
                throw new InvalidOperationException("RowHandler delegate must be set before reading.");

            // If using RowHandler, we can choose to not throw on malformed rows
            var throwExceptions = RowHandler == null;
            using var streamReader = new StreamReader(stream);
            using var bufferedReader = new BufferedReader(streamReader);

            // Detect or validate format
            var sample = bufferedReader.ReadSample();
            if (sample.Length == 0)
            {
                Log.Warning("CSV file appears to be empty.");
                return;
            }

            var isFormatDetected = Format == null;
            Format ??= CSVFormat.DetectFormat(sample);

            Log.Debug($"CSV format: {Format}");

            // Process header
            var headerLine = bufferedReader.ReadLine() ?? throw new InvalidDataException("Failed to read header line.");
            var columns = ParseLine(headerLine, Format.Delimiter, Format.HeaderQuoted ? Format.QuoteChar : null);

            // Process data rows
            var totalBytes = stream.Length;
            long prevPct = 0;

            string line;
            while ((line = bufferedReader.ReadLine()) != null)
            {
                // Report progress
                var currentPct = stream.Position * 1000 / totalBytes;
                if (currentPct != prevPct)
                {
                    Progress?.Invoke(this, new ProgressEventArgs(currentPct / 10f));
                    prevPct = currentPct;
                }

                // Handle multi-line fields if quoted
                if (Format.DataQuoted && Format.QuoteChar.HasValue)
                {
                    var additionalLine = line;
                    while (CountQuotes(line, Format.QuoteChar.Value) % 2 != 0)
                    {
                        var hasCR = bufferedReader.Peek() == '\r';
                        additionalLine = bufferedReader.ReadLine();
                        if (additionalLine == null)
                            throw new InvalidDataException("Unexpected end of file within quoted value");
                        line += (hasCR ? "\r\n" : "\n") + additionalLine;
                    }
                }

                var values = ParseLine(line, Format.Delimiter, Format.DataQuoted ? Format.QuoteChar : null);

                // Handle field count mismatch
                if (values.Length != columns.Length)
                {
                    if (throwExceptions)
                        throw new InvalidDataException($"Inconsistent field count. Expected {columns.Length}, got {values.Length} at line: {line}");

                    // For non-throwing mode, call RowHandler with Malformed status
                    if (RowHandler != null)
                    {
                        var partialRow = BuildRowDictionary(columns, values);
                        if (!RowHandler(partialRow, CSVRowStatus.Malformed, line))
                            break; // Stop processing if handler returns false
                    }
                    continue; // Skip to next line
                }

                // Process valid row
                try
                {
                    var row = BuildRowDictionary(columns, values);
                    if (!RowHandler(row, CSVRowStatus.OK, line))
                        break; // Stop processing if handler returns false
                }
                catch (Exception ex)
                {
                    var error = new DataProcessingException($"Error processing data at line: {line}", ex);
                    error.Data["Format"] = Format;
                    error.Data["IsFormatDetected"] = isFormatDetected;
                    throw error;
                }
            }

            // Call RowHandler with EOF status if set
            RowHandler?.Invoke(new Dictionary<string, string>(), CSVRowStatus.EOF, string.Empty);
        }

        /// <summary>
        /// Builds a dictionary mapping column names to field values.
        /// For malformed rows with fewer values than columns, missing values are set to empty strings.
        /// </summary>
        /// <param name="columns">The column headers.</param>
        /// <param name="values">The field values.</param>
        /// <returns>A dictionary mapping column names to field values.</returns>
        private static IDictionary<string, string> BuildRowDictionary(string[] columns, string[] values)
        {
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < columns.Length; i++)
            {
                row[columns[i]] = i < values.Length ? values[i] : string.Empty;
            }
            return row;
        }

        /// <summary>
        /// Parses a line of CSV data into fields.
        /// </summary>
        /// <param name="line">The line to parse.</param>
        /// <param name="delimiter">The delimiter character.</param>
        /// <param name="quoteChar">The quote character, if any.</param>
        /// <returns>An array of parsed fields.</returns>
        private static string[] ParseLine(string line, char delimiter, char? quoteChar)
        {
            if (string.IsNullOrEmpty(line))
                return Array.Empty<string>();

            if (!quoteChar.HasValue)
                return line.Split(delimiter);

            var currValue = new StringBuilder();
            var result = new System.Collections.Generic.List<string>();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                if (line[i] == quoteChar)
                {
                    // Check for escaped quotes (both backslash and double-quote patterns)
                    if (i > 0 && line[i - 1] == '\\')
                    {
                        // Backslash-escaped quote - include both backslash and quote
                        currValue.Append(line[i]);
                    }
                    else if (i + 1 < line.Length && line[i + 1] == quoteChar)
                    {
                        // Double-quote escaped quote - include both quotes
                        currValue.Append(line[i]);
                        currValue.Append(line[i + 1]);
                        i++; // Skip the next quote
                    }
                    else
                    {
                        // Regular quote - toggle state and include in content
                        currValue.Append(line[i]);
                        inQuotes = !inQuotes;
                    }
                }
                else if (line[i] == delimiter && !inQuotes)
                {
                    result.Add(CleanField(currValue.ToString(), quoteChar.Value));
                    currValue.Clear();
                }
                else
                    currValue.Append(line[i]);
            }

            result.Add(CleanField(currValue.ToString(), quoteChar.Value));
            return result.ToArray();
        }

        /// <summary>
        /// Cleans a field by trimming and removing enclosing quotes.
        /// Automatically handles both double-quote repetition ("") and backslash escaping (\") patterns.
        /// </summary>
        /// <param name="field">The field to clean.</param>
        /// <param name="quoteChar">The quote character.</param>
        /// <returns>The cleaned field.</returns>
        private static string CleanField(string field, char quoteChar)
        {
            field = field.Trim();
            if (field.Length >= 2 && field[0] == quoteChar && field[field.Length - 1] == quoteChar)
                field = field.Substring(1, field.Length - 2);

            var quote = quoteChar.ToString();
            return field.Replace($"\\{quoteChar}", quote)           // Handle backslash-escaped quotes
                        .Replace(new string(quoteChar, 2), quote);  // Handle double-quote repetition
        }

        /// <summary>
        /// Counts the number of quotes in a text.
        /// </summary>
        /// <param name="text">The text to analyze.</param>
        /// <param name="quoteChar">The quote character to count.</param>
        /// <returns>The number of quotes in the text.</returns>
        private static int CountQuotes(string text, char quoteChar)
        {
            return text.Count(c => c == quoteChar);
        }

        /// <summary>
        /// Provides data for the Progress event.
        /// </summary>
        public class ProgressEventArgs : EventArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ProgressEventArgs"/> class.
            /// </summary>
            /// <param name="progress">The progress percentage.</param>
            internal ProgressEventArgs(float progress)
            {
                Progress = progress;
            }

            /// <summary>
            /// Gets the progress percentage.
            /// </summary>
            public float Progress { get; }
        }
    }
}