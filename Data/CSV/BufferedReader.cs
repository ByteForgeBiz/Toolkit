using System;
using System.Collections.Generic;
using System.IO;

namespace ByteForge.Toolkit
{
    /// <summary>
    /// Provides buffered reading capabilities for CSV processing.
    /// </summary>
    internal class BufferedReader : IDisposable
    {
        private readonly TextReader _reader;
        private readonly Queue<string> _buffer;
        private bool _isEndOfInput;
        private readonly int _sampleSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferedReader"/> class.
        /// </summary>
        /// <param name="reader">The text reader to read from.</param>
        /// <param name="sampleSize">The number of lines to read into the buffer at a time.</param>
        /// <exception cref="ArgumentNullException">Thrown when the reader is null.</exception>
        public BufferedReader(TextReader reader, int sampleSize = 10)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _buffer = new Queue<string>();
            _sampleSize = sampleSize;
            _isEndOfInput = false;
        }

        /// <summary>
        /// Reads lines into the buffer up to the sample size or end of input.
        /// </summary>
        /// <returns>An array of lines read for format detection.</returns>
        public string[] ReadSample()
        {
            var sampleLines = new List<string>();

            while (sampleLines.Count < _sampleSize && !_isEndOfInput)
            {
                var line = _reader.ReadLine();
                if (line == null)
                {
                    _isEndOfInput = true;
                    break;
                }

                _buffer.Enqueue(line);
                sampleLines.Add(line);
            }

            return sampleLines.ToArray();
        }

        /// <summary>
        /// Reads the next line from either the buffer or the underlying reader.
        /// </summary>
        /// <returns>The next line or null if at end of input.</returns>
        public string ReadLine()
        {
            if (_buffer.Count > 0)
                return _buffer.Dequeue();

            if (_isEndOfInput)
                return null;

            var line = _reader.ReadLine();
            if (line == null)
                _isEndOfInput = true;

            return line;
        }

        /// <summary>
        /// Returns all currently buffered lines without removing them.
        /// </summary>
        /// <returns>An array of currently buffered lines.</returns>
        public string[] PeekBuffered() => _buffer.ToArray();

        /// <summary>
        /// Checks if there are more lines to read.
        /// </summary>
        /// <returns>True if there are more lines to read; otherwise, false.</returns>
        public bool HasMoreData => _buffer.Count > 0 || !_isEndOfInput;

        /// <summary>
        /// Releases all resources used by the <see cref="BufferedReader"/>.
        /// </summary>
        public void Dispose()
        {
            _reader.Dispose();
        }
    }
}