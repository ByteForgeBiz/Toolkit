using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ByteForge.Toolkit
{
    /*
     *   ___ _____   ____      __   _ _           
     *  / __/ __\ \ / /\ \    / / _(_) |_ ___ _ _ 
     * | (__\__ \\ V /  \ \/\/ / '_| |  _/ -_) '_|
     *  \___|___/ \_/    \_/\_/|_| |_|\__\___|_|  
     *                                            
     */
    /// <summary>
    /// Provides utilities for writing CSV files with flexible format support.
    /// </summary>
    public class CSVWriter : IDisposable
    {
        private readonly TextWriter _writer;
        private readonly bool _ownsWriter;
        private bool _headerWritten;
        private string[] _columns;

        /// <summary>
        /// Occurs to report the progress of the CSV writing process.
        /// </summary>
        public event EventHandler<ProgressEventArgs> Progress;

        /// <summary>
        /// Gets or sets the format to use for writing. If not set, uses default format.
        /// </summary>
        public CSVFormat Format { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CSVWriter"/> class with a file path.
        /// </summary>
        /// <param name="filePath">The path to the CSV file to write.</param>
        /// <param name="format">The CSV format to use. If null, uses default format.</param>
        /// <exception cref="ArgumentNullException">Thrown when the file path is null or empty.</exception>
        public CSVWriter(string filePath, CSVFormat format = null)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            Format = format ?? CSVFormat.Default;
            _writer = new StreamWriter(filePath, false, Encoding.UTF8);
            _ownsWriter = true;
            _headerWritten = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CSVWriter"/> class with a stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="format">The CSV format to use. If null, uses default format.</param>
        /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
        public CSVWriter(Stream stream, CSVFormat format = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            Format = format ?? CSVFormat.Default;
            _writer = new StreamWriter(stream, Encoding.UTF8);
            _ownsWriter = false;
            _headerWritten = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CSVWriter"/> class with a TextWriter.
        /// </summary>
        /// <param name="writer">The TextWriter to write to.</param>
        /// <param name="format">The CSV format to use. If null, uses default format.</param>
        /// <exception cref="ArgumentNullException">Thrown when the writer is null.</exception>
        public CSVWriter(TextWriter writer, CSVFormat format = null)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            Format = format ?? CSVFormat.Default;
            _ownsWriter = false;
            _headerWritten = false;
        }

        /// <summary>
        /// Writes a collection of CSV records to the file.
        /// </summary>
        /// <typeparam name="T">The type of CSV record.</typeparam>
        /// <param name="records">The records to write.</param>
        /// <exception cref="ArgumentNullException">Thrown when records is null.</exception>
        public void WriteRecords<T>(IEnumerable<T> records) where T : ICSVRecord
        {
            if (records == null)
                throw new ArgumentNullException(nameof(records));

            var recordList = records.ToList();
            if (recordList.Count == 0)
                return;

            // Extract column information from the first record
            var firstRecord = recordList[0];
            var properties = GetCSVProperties(firstRecord.GetType());
            var columns = properties.Select(p => GetColumnName(p)).ToArray();

            // Write header if configured to do so
            if (Format.HasHeader)
                WriteHeader(columns);

            // Write data rows
            var totalRecords = recordList.Count;
            for (var i = 0; i < totalRecords; i++)
            {
                var record = recordList[i];
                var values = properties.Select(p => GetPropertyValue(p, record)).ToArray();
                WriteRow(values);

                // Report progress
                if (i % 100 == 0 || i == totalRecords - 1)
                    Progress?.Invoke(this, new ProgressEventArgs((float)(i + 1) / totalRecords * 100));
            }
        }

        /// <summary>
        /// Writes the header row to the CSV file.
        /// </summary>
        /// <param name="columns">The column names to write.</param>
        /// <exception cref="ArgumentNullException">Thrown when columns is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when header has already been written.</exception>
        public void WriteHeader(string[] columns)
        {
            if (_headerWritten)
                throw new InvalidOperationException("Header has already been written.");

            _columns = columns ?? throw new ArgumentNullException(nameof(columns));
            var line = FormatLine(columns, Format.HeaderQuoted);
            _writer.WriteLine(line);
            _headerWritten = true;

            Log.Debug($"CSV header written: {string.Join(", ", columns)}");
        }

        /// <summary>
        /// Writes a single row of data to the CSV file.
        /// </summary>
        /// <param name="values">The values to write.</param>
        /// <exception cref="ArgumentNullException">Thrown when values is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when header is required but not written, or field count mismatch.</exception>
        public void WriteRow(string[] values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            if (Format.HasHeader && !_headerWritten)
                throw new InvalidOperationException("Header must be written before data rows when HasHeader is true.");

            if (_columns != null && values.Length != _columns.Length)
                throw new InvalidOperationException($"Field count mismatch. Expected {_columns.Length}, got {values.Length}.");

            var line = FormatLine(values, Format.DataQuoted);
            _writer.WriteLine(line);
        }

        /// <summary>
        /// Writes multiple rows of data to the CSV file.
        /// </summary>
        /// <param name="rows">The rows to write.</param>
        /// <exception cref="ArgumentNullException">Thrown when rows is null.</exception>
        public void WriteRows(IEnumerable<string[]> rows)
        {
            if (rows == null)
                throw new ArgumentNullException(nameof(rows));

            foreach (var row in rows)
                WriteRow(row);
        }

        /// <summary>
        /// Writes a collection of objects as CSV records using reflection.
        /// </summary>
        /// <typeparam name="T">The type of objects to write.</typeparam>
        /// <param name="objects">The objects to write.</param>
        /// <param name="includeHeader">Whether to include a header row with property names.</param>
        /// <exception cref="ArgumentNullException">Thrown when objects is null.</exception>
        public void WriteObjects<T>(IEnumerable<T> objects, bool includeHeader = true)
        {
            if (objects == null)
                throw new ArgumentNullException(nameof(objects));

            var objectList = objects.ToList();
            if (objectList.Count == 0)
                return;

            var properties = GetPublicProperties(typeof(T));
            var columns = properties.Select(p => p.Name).ToArray();

            // Write header if requested
            if (includeHeader && Format.HasHeader)
                WriteHeader(columns);

            // Write data rows
            foreach (var obj in objectList)
            {
                var values = properties.Select(p => GetPropertyValue(p, obj)?.ToString() ?? string.Empty).ToArray();
                WriteRow(values);
            }
        }

        /// <summary>
        /// Formats a line of CSV data with proper quoting and escaping.
        /// </summary>
        /// <param name="values">The values to format.</param>
        /// <param name="shouldQuote">Whether values should be quoted.</param>
        /// <returns>The formatted CSV line.</returns>
        private string FormatLine(string[] values, bool shouldQuote)
        {
            var formattedValues = new string[values.Length];

            for (var i = 0; i < values.Length; i++)
            {
                var value = values[i] ?? string.Empty;

                if (Format.TrimValues)
                    value = value.Trim();

                // Determine if this value needs quoting
                var needsQuoting = shouldQuote ||
                                 value.Contains(Format.Delimiter) ||
                                 (Format.QuoteChar.HasValue && value.Contains(Format.QuoteChar.Value)) ||
                                 value.Contains('\n') ||
                                 value.Contains('\r');

                if (needsQuoting && Format.QuoteChar.HasValue)
                {
                    // Escape any quote characters within the value
                    var escapedValue = value.Replace(Format.QuoteChar.Value.ToString(),
                                                   new string(Format.QuoteChar.Value, 2));
                    formattedValues[i] = Format.QuoteChar.Value + escapedValue + Format.QuoteChar.Value;
                }
                else
                    formattedValues[i] = value;
            }

            return string.Join(Format.Delimiter.ToString(), formattedValues);
        }

        /// <summary>
        /// Gets properties marked with CSVColumnAttribute from a type.
        /// </summary>
        /// <param name="type">The type to analyze.</param>
        /// <returns>An array of properties with CSV column attributes.</returns>
        private static PropertyInfo[] GetCSVProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
                .Where(p => p.GetCustomAttributes(typeof(CSVColumnAttribute)).Any())
                .OrderBy(p => p.GetCustomAttribute<CSVColumnAttribute>()?.Index ?? int.MaxValue)
                .ToArray();
        }

        /// <summary>
        /// Gets public properties from a type.
        /// </summary>
        /// <param name="type">The type to analyze.</param>
        /// <returns>An array of public properties.</returns>
        private static PropertyInfo[] GetPublicProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead)
                .ToArray();
        }

        /// <summary>
        /// Gets the column name for a property based on CSVColumnAttribute or property name.
        /// </summary>
        /// <param name="property">The property to get the column name for.</param>
        /// <returns>The column name.</returns>
        private static string GetColumnName(PropertyInfo property)
        {
            var attribute = property.GetCustomAttribute<CSVColumnAttribute>();
            return attribute?.Name ?? property.Name;
        }

        /// <summary>
        /// Gets the value of a property from an object.
        /// </summary>
        /// <param name="property">The property to get the value from.</param>
        /// <param name="obj">The object to get the value from.</param>
        /// <returns>The property value as a string.</returns>
        private static string GetPropertyValue(PropertyInfo property, object obj)
        {
            try
            {
                var value = property.GetValue(obj);
                return value?.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Flushes any buffered data to the underlying stream.
        /// </summary>
        public void Flush()
        {
            _writer.Flush();
        }

        /// <summary>
        /// Releases all resources used by the CSVWriter.
        /// </summary>
        public void Dispose()
        {
            if (_ownsWriter)
                _writer?.Dispose();
            else
                _writer?.Flush();
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