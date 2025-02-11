using System;
using System.IO;
using System.Linq;
using System.Text;

namespace ByteForge.Toolkit
{
    /// <summary>
    /// Provides utilities for reading and processing CSV files with flexible format support.
    /// </summary>
    public class CSVReader
    {
        /// <summary>
        /// Occurs to report the progress of the CSV reading process.
        /// </summary>
        public event EventHandler<ProgressEventArgs> Progress;

        /// <summary>
        /// Gets or sets the action to process the data.
        /// </summary>
        public Action<string[], string[], string> DataProcessor { get; set; }

        /// <summary>
        /// Gets or sets the format to use for parsing. If not set, format will be auto-detected.
        /// </summary>
        public CSVFormat Format { get; set; }

        /// <summary>
        /// Reads the CSV file and processes the data.
        /// </summary>
        /// <param name="filePath">The path to the CSV file.</param>
        /// <param name="dataProcessor">The action to process the data.</param>
        public static void ReadFile(string filePath, Action<string[], string[], string> dataProcessor)
        {
            var reader = new CSVReader { DataProcessor = dataProcessor };
            reader.ReadFile(filePath);
        }

        /// <summary>
        /// Reads the provided stream and processes the CSV data.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="dataProcessor">The action to process the data.</param>
        public static void ReadStream(Stream stream, Action<string[], string[], string> dataProcessor)
        {
            var reader = new CSVReader { DataProcessor = dataProcessor };
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
            if (DataProcessor == null)
                throw new ArgumentNullException(nameof(DataProcessor));

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                ReadStream(stream);
        }

        /// <summary>
        /// Reads the provided stream and processes the CSV data.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <exception cref="ArgumentNullException">Thrown when the stream or data processor is null.</exception>
        /// <exception cref="InvalidDataException">Thrown when the CSV file is empty or has inconsistent field counts.</exception>
        public void ReadStream(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (DataProcessor == null)
                throw new ArgumentNullException(nameof(DataProcessor));

            using (var streamReader = new StreamReader(stream))
            using (var bufferedReader = new BufferedReader(streamReader))
            {
                // Detect or validate format
                var sample = bufferedReader.ReadSample();
                if (sample.Length == 0)
                    throw new InvalidDataException("The CSV file is empty.");

                Format = Format ?? CSVFormat.DetectFormat(sample);

                // Process header
                var headerLine = bufferedReader.ReadLine();
                if (headerLine == null)
                    throw new InvalidDataException("Failed to read header line.");

                var columns = ParseLine(headerLine, Format.Delimiter, Format.HeaderQuoted ? Format.QuoteChar : null);

                // Process data rows
                long totalBytes = stream.Length;
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
                        while (CountQuotes(additionalLine, Format.QuoteChar.Value) % 2 != 0)
                        {
                            additionalLine = bufferedReader.ReadLine();
                            if (additionalLine == null)
                                throw new InvalidDataException("Unexpected end of file within quoted value");
                            line += Environment.NewLine + additionalLine;
                        }
                    }

                    var values = ParseLine(line, Format.Delimiter, Format.DataQuoted ? Format.QuoteChar : null);

                    if (values.Length != columns.Length)
                        throw new InvalidDataException($"Inconsistent field count. Expected {columns.Length}, got {values.Length} at line: {line}");

                    DataProcessor(columns, values, line);
                }
            }
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

            var fields = new StringBuilder();
            var result = new System.Collections.Generic.List<string>();
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == quoteChar)
                    inQuotes = !inQuotes;
                else if (line[i] == delimiter && !inQuotes)
                {
                    result.Add(CleanField(fields.ToString(), quoteChar.Value));
                    fields.Clear();
                }
                else
                    fields.Append(line[i]);
            }

            result.Add(CleanField(fields.ToString(), quoteChar.Value));
            return result.ToArray();
        }

        /// <summary>
        /// Cleans a field by trimming and removing enclosing quotes.
        /// </summary>
        /// <param name="field">The field to clean.</param>
        /// <param name="quoteChar">The quote character.</param>
        /// <returns>The cleaned field.</returns>
        private static string CleanField(string field, char quoteChar)
        {
            field = field.Trim();
            if (field.Length >= 2 && field[0] == quoteChar && field[field.Length - 1] == quoteChar)
                field = field.Substring(1, field.Length - 2);
            return field.Replace(new string(quoteChar, 2), quoteChar.ToString());
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