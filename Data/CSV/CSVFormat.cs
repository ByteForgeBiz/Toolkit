using System;
using System.Linq;

namespace ByteForge.Toolkit
{
    /// <summary>
    /// Represents the format configuration for CSV parsing.
    /// </summary>
    public class CSVFormat
    {
        /// <summary>
        /// Gets or sets the delimiter character.
        /// </summary>
        public char Delimiter { get; set; }

        /// <summary>
        /// Gets or sets the quote character.
        /// </summary>
        public char? QuoteChar { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the CSV has a header row.
        /// </summary>
        public bool HasHeader { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the header is quoted.
        /// </summary>
        public bool HeaderQuoted { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the data is quoted.
        /// </summary>
        public bool DataQuoted { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to trim values.
        /// </summary>
        public bool TrimValues { get; set; }

        /// <summary>
        /// Creates a default CSV format (comma-delimited, double-quoted).
        /// </summary>
        public static CSVFormat Default => new CSVFormat
        {
            Delimiter = ',',
            QuoteChar = '"',
            HasHeader = true,
            HeaderQuoted = true,
            DataQuoted = true,
            TrimValues = true
        };

        /// <summary>
        /// Attempts to detect the format from a sample of lines.
        /// </summary>
        /// <param name="sampleLines">The sample lines to analyze.</param>
        /// <returns>The detected CSV format.</returns>
        /// <exception cref="ArgumentException">Thrown when the sample lines are null or empty.</exception>
        public static CSVFormat DetectFormat(string[] sampleLines)
        {
            if (sampleLines == null || sampleLines.Length == 0)
                throw new ArgumentException("Sample lines cannot be empty", nameof(sampleLines));

            var format = new CSVFormat();

            // Check potential delimiters
            var delimiters = new[] { ',', '|', '\t', ';' };
            var headerLine = sampleLines[0];
            var dataLines = sampleLines.Skip(1).ToArray();

            // Detect delimiter by consistency in field count
            format.Delimiter = DetectDelimiter(sampleLines, delimiters);

            // Detect quotes in header
            format.HeaderQuoted = IsQuoted(headerLine, '"') || IsQuoted(headerLine, '\'');
            format.QuoteChar = format.HeaderQuoted ? headerLine[0] : (char?)null;

            // Detect quotes in data
            if (dataLines.Length > 0)
            {
                format.DataQuoted = dataLines.All(line =>
                    IsQuoted(line, format.QuoteChar ?? '"') ||
                    IsQuoted(line, '\''));

                if (format.DataQuoted && !format.HeaderQuoted)
                    format.QuoteChar = dataLines[0][0];
            }

            format.HasHeader = true; // Assume headers by default
            format.TrimValues = true;

            return format;
        }

        /// <summary>
        /// Detects the delimiter from a set of sample lines.
        /// </summary>
        /// <param name="lines">The sample lines to analyze.</param>
        /// <param name="candidates">The candidate delimiters to consider.</param>
        /// <returns>The detected delimiter.</returns>
        private static char DetectDelimiter(string[] lines, char[] candidates)
        {
            var bestDelimiter = ','; // Default
            var bestConsistency = 0;

            foreach (var delimiter in candidates)
            {
                // Count fields in each line when split by this delimiter
                var fieldCounts = lines.Select(line =>
                    CountFields(line, delimiter)).ToArray();

                // Check consistency (all counts equal and > 1)
                if (fieldCounts.All(count => count == fieldCounts[0]) &&
                    fieldCounts[0] > 1)
                {
                    var consistency = fieldCounts[0];
                    if (consistency > bestConsistency)
                    {
                        bestConsistency = consistency;
                        bestDelimiter = delimiter;
                    }
                }
            }

            return bestDelimiter;
        }

        /// <summary>
        /// Counts the number of fields in a line when split by a delimiter.
        /// </summary>
        /// <param name="line">The line to analyze.</param>
        /// <param name="delimiter">The delimiter to use for splitting.</param>
        /// <returns>The number of fields in the line.</returns>
        private static int CountFields(string line, char delimiter)
        {
            var count = 1;
            var inQuotes = false;
            var quoteChar = line.FirstOrDefault(c => c == '"' || c == '\'');

            for (var i = 0; i < line.Length; i++)
            {
                if (line[i] == quoteChar)
                    inQuotes = !inQuotes;
                else if (line[i] == delimiter && !inQuotes)
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Determines whether a line is quoted with a specific quote character.
        /// </summary>
        /// <param name="line">The line to analyze.</param>
        /// <param name="quoteChar">The quote character to check for.</param>
        /// <returns>True if the line is quoted; otherwise, false.</returns>
        private static bool IsQuoted(string line, char quoteChar)
        {
            return line.Length >= 2 &&
                   line[0] == quoteChar &&
                   line[line.Length - 1] == quoteChar;
        }
    }
}