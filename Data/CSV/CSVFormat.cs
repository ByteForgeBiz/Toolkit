using System;
using System.Linq;

namespace ByteForge.Toolkit
{
    /// <summary>
    /// Represents the format configuration for CSV parsing.
    /// </summary>
    [Serializable]
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

        /// <inheritdoc />
        override public string ToString()
        {
            return $"Delimiter: ‘{Delimiter}’, QuoteChar: ‘{QuoteChar}’, HasHeader: {HasHeader}, HeaderQuoted: {HeaderQuoted}, DataQuoted: {DataQuoted}, TrimValues: {TrimValues}";
        }

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
            var possibleQuoteChars = new[] { '"', '\'' };
            var headerQuoteChar = possibleQuoteChars.FirstOrDefault(q => headerLine.Contains(q));

            if (headerQuoteChar != default(char))
            {
                format.HeaderQuoted = CountQuotedFields(headerLine, format.Delimiter, headerQuoteChar) > 0;
                format.QuoteChar = format.HeaderQuoted ? headerQuoteChar : (char?)null;
            }
            else
            {
                format.HeaderQuoted = false;
                format.QuoteChar = null;
            }

            // Detect quotes in data
            if (dataLines.Length > 0)
            {
                // If no quote char found in header, try to find one in data
                if (!format.QuoteChar.HasValue)
                {
                    foreach (var quoteChar in possibleQuoteChars)
                    {
                        if (dataLines.Any(line => line.Contains(quoteChar)))
                        {
                            format.QuoteChar = quoteChar;
                            break;
                        }
                    }
                }

                if (format.QuoteChar.HasValue)
                {
                    // Count total and quoted fields to determine if data is mostly quoted
                    var totalFields = 0;
                    var quotedFields = 0;

                    foreach (var line in dataLines)
                    {
                        var fieldsInLine = CountFields(line, format.Delimiter);
                        totalFields += fieldsInLine;
                        quotedFields += CountQuotedFields(line, format.Delimiter, format.QuoteChar.Value);
                    }

                    // Consider data quoted if more than 25% of fields are quoted
                    // This threshold handles mixed quoting patterns but avoids false positives
                    format.DataQuoted = quotedFields > (totalFields * 0.25);
                }
                else
                {
                    format.DataQuoted = false;
                }
            }

            format.HasHeader = true; // Assume headers by default
            format.TrimValues = true;

            return format;
        }

        /// <summary>
        /// Counts the number of fields that appear to be quoted in a line.
        /// </summary>
        /// <param name="line">The line to analyze.</param>
        /// <param name="delimiter">The delimiter character.</param>
        /// <param name="quoteChar">The quote character.</param>
        /// <returns>The number of quoted fields in the line.</returns>
        private static int CountQuotedFields(string line, char delimiter, char quoteChar)
        {
            var count = 0;
            var inQuotes = false;
            var fieldStart = 0;

            for (var i = 0; i <= line.Length; i++)
            {
                // At delimiter or end of line - evaluate field
                if (i == line.Length || (line[i] == delimiter && !inQuotes))
                {
                    // Get the field content (trimmed)
                    var field = i == fieldStart ? string.Empty :
                                   line.Substring(fieldStart, i - fieldStart).Trim();

                    // Check if field is quoted
                    if (field.Length >= 2 &&
                        field[0] == quoteChar &&
                        field[field.Length - 1] == quoteChar)
                    {
                        count++;
                    }

                    fieldStart = i + 1;
                }
                // Toggle quote state when encountering a quote character
                else if (line[i] == quoteChar)
                {
                    // Handle escaped quotes (double quotes)
                    if (i + 1 < line.Length && line[i + 1] == quoteChar)
                        i++; // Skip next quote character
                    else
                        inQuotes = !inQuotes;
                }
            }

            return count;
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

            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '"' || line[i] == '\'')
                    inQuotes = !inQuotes;
                else if (line[i] == delimiter && !inQuotes)
                    count++;
            }

            return count;
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

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c>.</returns>
        override public bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            var other = (CSVFormat)obj;
            return Delimiter == other.Delimiter &&
                   QuoteChar == other.QuoteChar &&
                   HasHeader == other.HasHeader &&
                   HeaderQuoted == other.HeaderQuoted &&
                   DataQuoted == other.DataQuoted &&
                   TrimValues == other.TrimValues;
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        override public int GetHashCode()
        {
            return Delimiter.GetHashCode() ^
                   QuoteChar.GetHashCode() ^
                   HasHeader.GetHashCode() ^
                   HeaderQuoted.GetHashCode() ^
                   DataQuoted.GetHashCode() ^
                   TrimValues.GetHashCode();
        }
    }
}