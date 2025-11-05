using System;

namespace ByteForge.Toolkit.Logging
{
    /*
     *  _  _      _ _ _                           
     * | \| |_  _| | | |   ___  __ _ __ _ ___ _ _ 
     * | .` | || | | | |__/ _ \/ _` / _` / -_) '_|
     * |_|\_|\_,_|_|_|____\___/\__, \__, \___|_|  
     *                         |___/|___/         
     */
    /// <summary>
    /// A logger implementation that discards all log messages. 
    /// Useful for testing or when logging is not required.
    /// </summary>
    public class NullLogger : ILogger
    {
        /// <summary>
        /// Gets or sets the name of the logger.
        /// </summary>
        public string Name { get; set; } = "NullLogger";

        /// <summary>
        /// Gets or sets the minimum log level. Always returns None for NullLogger.
        /// </summary>
        public LogLevel MinLogLevel { get; set; } = LogLevel.None;

        /// <summary>
        /// Logs a message with the specified log level. This implementation does nothing.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">Optional exception associated with the log message.</param>
                public void Log(LogLevel level, string message, Exception ex = null)
        {
            // Do nothing - this is a null logger
        }

        /// <summary>
        /// Logs a trace message. This implementation does nothing.
        /// </summary>
        /// <param name="message">The message to log.</param>
                public void LogTrace(string message)
        {
            // Do nothing - this is a null logger
        }

        /// <summary>
        /// Logs a debug message. This implementation does nothing.
        /// </summary>
        /// <param name="message">The message to log.</param>
                public void LogDebug(string message)
        {
            // Do nothing - this is a null logger
        }

        /// <summary>
        /// Logs a verbose message. This implementation does nothing.
        /// </summary>
        /// <param name="message">The message to log.</param>
                public void LogVerbose(string message)
        {
            // Do nothing - this is a null logger
        }

        /// <summary>
        /// Logs an informational message. This implementation does nothing.
        /// </summary>
        /// <param name="message">The message to log.</param>
                public void LogInfo(string message)
        {
            // Do nothing - this is a null logger
        }

        /// <summary>
        /// Logs a notice message. This implementation does nothing.
        /// </summary>
        /// <param name="message">The message to log.</param>
                public void LogNotice(string message)
        {
            // Do nothing - this is a null logger
        }

        /// <summary>
        /// Logs a warning message. This implementation does nothing.
        /// </summary>
        /// <param name="message">The message to log.</param>
                public void LogWarning(string message)
        {
            // Do nothing - this is a null logger
        }

        /// <summary>
        /// Logs an error message. This implementation does nothing.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">Optional exception associated with the error.</param>
                public void LogError(string message, Exception ex = null)
        {
            // Do nothing - this is a null logger
        }

        /// <summary>
        /// Logs a critical error message. This implementation does nothing.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">Optional exception associated with the critical error.</param>
                public void LogCritical(string message, Exception ex = null)
        {
            // Do nothing - this is a null logger
        }

        /// <summary>
        /// Logs a fatal error message. This implementation does nothing.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">Optional exception associated with the fatal error.</param>
                public void LogFatal(string message, Exception ex = null)
        {
            // Do nothing - this is a null logger
        }
    }
}