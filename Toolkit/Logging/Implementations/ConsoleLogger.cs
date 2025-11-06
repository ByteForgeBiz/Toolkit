using System;

namespace ByteForge.Toolkit.Logging
{
    /*
     *   ___                  _     _                           
     *  / __|___ _ _  ___ ___| |___| |   ___  __ _ __ _ ___ _ _ 
     * | (__/ _ \ ' \(_-</ _ \ / -_) |__/ _ \/ _` / _` / -_) '_|
     *  \___\___/_||_/__/\___/_\___|____\___/\__, \__, \___|_|  
     *                                       |___/|___/         
     */
    /// <summary>
    /// Console logger that logs messages to the console.
    /// </summary>
    public class ConsoleLogger : BaseLogger
    {
        private static readonly object _lock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleLogger"/> class.
        /// </summary>
        public ConsoleLogger() : base("Console") { }

        /// <summary>
        /// Gets or sets a value indicating whether to show only the message.
        /// </summary>
        public bool ShowMessageOnly { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show the timestamp.
        /// </summary>
        /// <remarks>
        /// If <see cref="ShowMessageOnly"/> is <see langword="true" />, this property is ignored.
        /// </remarks>
        public bool ShowTimestamp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show the stack trace.
        /// </summary>
        /// <remarks>
        /// If <see cref="ShowMessageOnly"/> is <see langword="true" />, this property is ignored.
        /// </remarks>
        public bool ShowStackTrace { get; set; }

        /// <summary>
        /// Gets the console color for the specified log level.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <returns>The console color for the log level.</returns>
        private static ConsoleColor GetColorForLogLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Verbose => ConsoleColor.Gray,
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Info => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Critical => ConsoleColor.DarkRed,
                _ => ConsoleColor.White,
            };
        }

        /// <summary>
        /// Records a log entry by writing it to the console with appropriate formatting and color.
        /// </summary>
        /// <param name="entry">The log entry to record.</param>
        protected internal override void RecordLogEntry(LogEntry entry)
        {
            if (!ConsoleUtil.IsConsoleAvailable || entry.Level < MinLogLevel)
                return;

            lock (_lock)
            {
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = GetColorForLogLevel(entry.Level);

                if (Console.CursorLeft != 0)
                    Console.WriteLine();

                if (!ShowMessageOnly && ShowTimestamp)
                    Console.Write($"[{entry.Timestamp:yyyy/MM/dd HH:mm:ss.fff}] ");

                if (!ShowMessageOnly)
                    Console.WriteLine($"[{entry.Level}] - {entry.Source}:");

                Console.WriteLine($"{entry.Message}");

                if (!ShowMessageOnly && entry.Exception != null)
                {
                    Console.WriteLine($"Exception: {{{entry.Exception.GetType()}}} {entry.Exception.Message}");
                    if (ShowStackTrace && string.IsNullOrEmpty(entry.Exception.StackTrace) == false)
                        Console.WriteLine("StackTrace: {0}", entry.Exception.StackTrace);
                }

                Console.ForegroundColor = originalColor;
            }
        }
    }
}