using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ByteForge.Toolkit.Logging
{
    /*
     *  ___               _                           
     * | _ ) __ _ ___ ___| |   ___  __ _ __ _ ___ _ _ 
     * | _ \/ _` (_-</ -_) |__/ _ \/ _` / _` / -_) '_|
     * |___/\__,_/__/\___|____\___/\__, \__, \___|_|  
     *                             |___/|___/         
     */
    /// <summary>
    /// Abstract base class for logging functionality.
    /// </summary>
    public abstract class BaseLogger : ILogger
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseLogger"/> class with the specified name.
        /// </summary>
        /// <param name="name">The name of the logger.</param>
        protected BaseLogger(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets or sets the name of the logger.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the minimum log level for this logger.
        /// </summary>
        public LogLevel MinLogLevel { get; set; } = LogLevel.Info;

        /// <summary>
        /// Writes the log entry to the log destination.
        /// </summary>
        /// <param name="entry">The log entry to write.</param>
        protected internal abstract void RecordLogEntry(LogEntry entry);

        // Logging Methods

        /// <summary>
        /// Logs a message with the specified log level.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">An optional exception to log.</param>
        /// <param name="source">The source of the log message. Defaults to the caller member name.</param>
        public void Log(LogLevel level, string message, Exception ex = null, [CallerMemberName] string source = "")
        {
            if (level < MinLogLevel) return;

            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message,
                Source = source,
                Exception = ex,
                Properties = new Dictionary<string, object>
                    {
                        { "LoggerName", Name },
                        { "ThreadId", Thread.CurrentThread.ManagedThreadId }
                    }
            };

            RecordLogEntry(entry);
        }

        /// <summary>
        /// Logs a trace message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="source">The source of the log message. Defaults to the caller member name.</param>
        public void LogTrace(string message, [CallerMemberName] string source = "") => Log(LogLevel.Trace, message, null, source);

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="source">The source of the log message. Defaults to the caller member name.</param>
        public void LogDebug(string message, [CallerMemberName] string source = "") => Log(LogLevel.Debug, message, null, source);

        /// <summary>
        /// Logs a verbose message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="source">The source of the log message. Defaults to the caller member name.</param>
        public void LogVerbose(string message, [CallerMemberName] string source = "") => Log(LogLevel.Verbose, message, null, source);

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="source">The source of the log message. Defaults to the caller member name.</param>
        public void LogInfo(string message, [CallerMemberName] string source = "") => Log(LogLevel.Info, message, null, source);

        /// <summary>
        /// Logs a notice message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="source">The source of the log message. Defaults to the caller member name.</param>
        public void LogNotice(string message, [CallerMemberName] string source = "") => Log(LogLevel.Notice, message, null, source);

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="source">The source of the log message. Defaults to the caller member name.</param>
        public void LogWarning(string message, [CallerMemberName] string source = "") => Log(LogLevel.Warning, message, null, source);

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">An optional exception to log.</param>
        /// <param name="source">The source of the log message. Defaults to the caller member name.</param>
        public void LogError(string message, Exception ex = null, [CallerMemberName] string source = "") => Log(LogLevel.Error, message, ex, source);

        /// <summary>
        /// Logs a critical message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">An optional exception to log.</param>
        /// <param name="source">The source of the log message. Defaults to the caller member name.</param>
        public void LogCritical(string message, Exception ex = null, [CallerMemberName] string source = "") => Log(LogLevel.Critical, message, ex, source);

        /// <summary>
        /// Logs a fatal message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">An optional exception to log.</param>
        /// <param name="source">The source of the log message. Defaults to the caller member name.</param>
        public void LogFatal(string message, Exception ex = null, [CallerMemberName] string source = "") => Log(LogLevel.Fatal, message, ex, source);

        // Helper Methods

        // Add any additional helper methods here in the future.
    }
}