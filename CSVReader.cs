using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace ByteForge.Toolkit;

/// <summary>
/// Provides utilities for reading and processing CSV files.
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
    /// Reads the CSV file and processes the data.
    /// </summary>
    /// <param name="filePath">The path to the CSV file.</param>
    /// <param name="dataProcessor">An action to process the data.</param>
    public static void ReadFile(string filePath, Action<string[], string[], string> dataProcessor)
    {
        var reader = new CSVReader() { DataProcessor = dataProcessor };
        reader.ReadFile(filePath);
    }

    /// <summary>
    /// Reads the provided stream and processes the CSV data.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="dataProcessor">An action to process the data.</param>
    public static void ReadStream(Stream stream, Action<string[], string[], string> dataProcessor)
    {
        var reader = new CSVReader() { DataProcessor = dataProcessor };
        reader.ReadStream(stream);
    }

    /// <summary>
    /// Read the CSV file and process the data.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when the filePath is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="InvalidDataException">Thrown when the CSV file is empty.</exception>
    /// <remarks>
    /// This method makes a couple of assumptions about the CSV file: <br/>
    /// <list type="number">
    ///   <item>All values are separated by commas.</item>
    ///   <item>All values are enclosed in double quotes and separated by a comma. Including the header row.</item>
    ///   <item>The first line contains the name of the fields.</item>
    /// </list>
    /// <br/>
    /// <b>ReadFile</b> takes two parameters: the file name and an action to perform for every line read.<br/>
    /// The action can be any method with the following signature:<br/>
    /// <code>anyMethod(string[] columns, string[] values, string line)</code><br/>
    /// <b>Algorithm</b><br/>
    /// When <c>ReadFile</c> is called, it opens the file and reads the first line.<br/>
    /// This line is parsed and it’s values are assumed to be the name of the fields for each line.<br/>
    /// Then it enters the main loop.<br/>
    /// It reads a line, if the number of quote chars is odd (that is there is an open quote, 
    /// but not a close quote), it reads the next line and test it again, until it has a line 
    /// with an even number of quotes. 
    /// (There might be some very edge cases where this algorithm breaks, but so far none was found.)<br/>
    /// Next it parses the lines, that is, breaks its fields by comma. (In actuality it breads by 
    /// the sequence of characters [<c>","</c>] [0x22 0x2C 0x22] ), storing the values in an array.<br/>
    /// The following step is call the <see cref="DataProcessor"/> action, passing the name of the fields, the values, and the original line read.<br/>
    /// </remarks>
    public void ReadFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentNullException(nameof(filePath));
        if (File.Exists(filePath) == false)
            throw new FileNotFoundException("File not found", filePath);
        if (DataProcessor == null)
            throw new ArgumentNullException(nameof(DataProcessor));

        Console.WriteLine($"Reading file: {filePath}");

        /*
         * Start reading the CSV file.
         */
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        ReadStream(stream);
    }

    /// <summary>
    /// Reads the provided stream and processes the CSV data.
    /// </summary>
    /// <returns>A StreamReader object used to read the stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the stream or dataProcessor is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when the CSV file is empty.</exception>
    /// <remarks>
    /// This method reads the CSV data from the provided stream and processes each row using the specified dataProcessor action.
    /// The first line of the stream is assumed to be the header row, containing the names of the fields.
    /// Each subsequent line is read and processed, with multi-line values handled by checking the number of double quotes.
    /// The <see cref="DataProcessor"/> action is called for each row, passing the column names, values, and the original line read.
    /// </remarks>
    public void ReadStream(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        if (DataProcessor == null)
            throw new ArgumentNullException(nameof(DataProcessor));

        using var reader = new StreamReader(stream);

        /*
         * Read the header row.
         */
        var pct = 0;
        var prevPct = 0;
        var header = reader.ReadLine();
        if (string.IsNullOrEmpty(header))
            throw new InvalidDataException("The CSV file is empty.");
        var columns = header.Split([@""","""], StringSplitOptions.None);
        columns[0] = columns[0].Substring(1);
        columns[columns.Length - 1] = columns[columns.Length - 1].Substring(0, columns[columns.Length - 1].Length - 1);

        /*
         * Read the data rows.
         */
        while (!reader.EndOfStream)
        {
            /*
             * Report progress 
             */
            pct = (int)(reader.BaseStream.Position * 1000 / reader.BaseStream.Length);
            if (pct != prevPct)
            {
                Progress?.Invoke(this, new ProgressEventArgs(pct / 10f));
                prevPct = pct;
            }

            /*
             * As there can be multi-line values in CSV, we handle that by checking the number of double quotes,
             * and then read the next line until the number of double quotes is even. 
             * Doing so will ensure that we have a complete row and also remove the line breaks.
             */
            var line = reader.ReadLine();
            while (line.Count(c => c == '"') % 2 != 0)
                line += reader.ReadLine();

            /*
             * Trim spaces, just in case.
             */
            line = line.Trim();

            /*
             * All values are enclosed in double quotes and separated by a comma.
             */
            var values = line.Substring(1, line.Length - 2).Split([@""","""], StringSplitOptions.None);

            DataProcessor(columns, values, line);
        }
    }

    /// <summary>
    /// Provides data for the Progress event.
    /// </summary>
    public class ProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressEventArgs"/> class with the specified progress.
        /// </summary>
        /// <param name="progress">The percentage of the process completed.</param>
        internal ProgressEventArgs(float progress)
        {
            Progress = progress;
        }

        /// <summary>
        /// Gets or sets the percentage of the process completed.
        /// </summary>
        public float Progress { get; private set; }
    }
}
