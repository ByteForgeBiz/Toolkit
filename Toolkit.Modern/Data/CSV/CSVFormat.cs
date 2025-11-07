namespace ByteForge.Toolkit.Data;
/*
 *   ___ _____   _____                   _   
 *  / __/ __\ \ / / __|__ _ _ _ __  __ _| |_ 
 * | (__\__ \\ V /| _/ _ \ '_| '  \/ _` |  _|
 *  \___|___/ \_/ |_|\___/_| |_|_|_\__,_|\__|
 *                                           
 */
/// <summary>
/// Represents the format configuration for CSV parsing.
/// </summary>
[Serializable]
public class CSVFormat
{
    /// <summary>
    /// Gets or sets the delimiter character.
    /// </summary>
    public char Delimiter { get; set; } = ',';

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
        return $"Delimiter: '{Delimiter}', QuoteChar: '{QuoteChar}', HasHeader: {HasHeader}, HeaderQuoted: {HeaderQuoted}, DataQuoted: {DataQuoted}, TrimValues: {TrimValues}";
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

        // Check if quotes are actually used as field delimiters in the header
        var headerQuoteChar = default(char);
        foreach (var q in possibleQuoteChars)
        {
            if (headerLine.Contains(q) && IsLikelyQuoteChar(headerLine, q, format.Delimiter))
            {
                headerQuoteChar = q;
                break;
            }
        }

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
                    // Check if this character is likely being used as a quote character
                    if (dataLines.Any(line => line.Contains(quoteChar) &&
                                              IsLikelyQuoteChar(line, quoteChar, format.Delimiter)))
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
                    var fieldsInLine = CountFields(line, format.Delimiter, format.QuoteChar);
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
    /// Determines if a character is likely being used as a quote character in the line.
    /// </summary>
    /// <param name="line">The line to analyze.</param>
    /// <param name="quoteChar">The potential quote character.</param>
    /// <param name="delimiter">The delimiter character.</param>
    /// <returns>True if the character is likely a quote character; otherwise, false.</returns>
    private static bool IsLikelyQuoteChar(string line, char quoteChar, char delimiter)
    {
        // Check if the line has an even number of the quote character
        var quoteCount = line.Count(c => c == quoteChar);
        if (quoteCount % 2 != 0)
            return false;

        // Check if fields start and end with the quote character
        var fields = line.Split(delimiter);
        var quotedFieldCount = fields.Count(f =>
        {
            var trimmed = f.Trim();
            return trimmed.Length >= 2 &&
                   trimmed[0] == quoteChar &&
                   trimmed[trimmed.Length - 1] == quoteChar;
        });

        // It's likely a quote char if at least one field is properly quoted
        return quotedFieldCount > 0;
    }

    /// <summary>
    /// Counts the number of fields in a line when split by a delimiter.
    /// </summary>
    /// <param name="line">The line to analyze.</param>
    /// <param name="delimiter">The delimiter to use for splitting.</param>
    /// <param name="quoteChar">The quote character, if any.</param>
    /// <returns>The number of fields in the line.</returns>
    private static int CountFields(string line, char delimiter, char? quoteChar = null)
    {
        var count = 1;
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            // Only toggle quote state if the character matches our expected quote character
            if (quoteChar.HasValue && line[i] == quoteChar.Value)
            {
                // Handle escaped quotes (double quotes)
                if (i + 1 < line.Length && line[i + 1] == quoteChar.Value)
                    i++; // Skip next quote character
                else
                    inQuotes = !inQuotes;
            }
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
        var bestScore = 0;

        foreach (var delimiter in candidates)
        {
            // Count occurrences of each delimiter
            var delimiterCount = lines.Sum(line => line.Count(c => c == delimiter));
            if (delimiterCount == 0)
                continue;

            // Count fields in each line when split by this delimiter
            // Don't consider quotes for initial delimiter detection to avoid circular dependency
            var fieldCounts = lines.Select(line => CountFields(line, delimiter)).ToArray();

            // Calculate consistency score
            var avgFieldCount = fieldCounts.Average();
            var stdDev = Math.Sqrt(fieldCounts.Average(fc => Math.Pow(fc - avgFieldCount, 2)));

            // Lower standard deviation means more consistent field counts
            var consistencyScore = stdDev == 0 ? int.MaxValue : (int)(1.0 / stdDev * 1000);

            // Check if all field counts are greater than 1
            var allFieldsValid = fieldCounts.All(count => count > 1);

            // Calculate overall score based on consistency and delimiter occurrence
            var longScore = allFieldsValid ? ((long)consistencyScore * delimiterCount) : 0;
            var score = longScore > int.MaxValue ? int.MaxValue : (int)longScore;

            if (score > bestScore)
            {
                bestScore = score;
                bestDelimiter = delimiter;
                bestConsistency = (int)avgFieldCount;
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
    /// <returns><see langword="true" /> if the specified object is equal to the current object; otherwise, <see langword="false" />.</returns>
    override public bool Equals(object? obj)
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
               (QuoteChar?.GetHashCode() ?? 0) ^
               HasHeader.GetHashCode() ^
               HeaderQuoted.GetHashCode() ^
               DataQuoted.GetHashCode() ^
               TrimValues.GetHashCode();
    }
}
