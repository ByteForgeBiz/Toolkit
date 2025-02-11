// ILogger.cs
using System;
using System.Runtime.CompilerServices;

namespace ByteForge.Toolkit.Logging
{
    /*
     *  ___ _                           
     * |_ _| |   ___  __ _ __ _ ___ _ _ 
     *  | || |__/ _ \/ _` / _` / -_) '_|
     * |___|____\___/\__, \__, \___|_|  
     *               |___/|___/         
     */
    /// <summary>
    /// Defines methods for logging messages at various levels.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Gets the name of the logger.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets the minimum log level for the logger.
        /// </summary>
        LogLevel MinLogLevel { get; set; }

        /// <summary>
        /// Logs a verbose message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="source">The source of the message. If ommitted uses the calling method's name.</param>
        /// 
        void LogVerbose(string message, [CallerMemberName] string source = "");

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="source">The source of the message. If ommitted uses the calling method's name.</param>
        /// 
        void LogDebug(string message, [CallerMemberName] string source = "");

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="source">The source of the message. If ommitted uses the calling method's name.</param>
        /// 
        void LogInfo(string message, [CallerMemberName] string source = "");

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="source">The source of the message. If ommitted uses the calling method's name.</param>
        /// 
        void LogWarning(string message, [CallerMemberName] string source = "");

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">Optional exception associated with the error.</param>
        /// <param name="source">The source of the message. If ommitted uses the calling method's name.</param>
        /// 
        void LogError(string message, Exception ex = null, [CallerMemberName] string source = "");

        /// <summary>
        /// Logs a critical error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">Optional exception associated with the critical error.</param>
        /// <param name="source">The source of the message. If ommitted uses the calling method's name.</param>
        /// 
        void LogCritical(string message, Exception ex = null, [CallerMemberName] string source = "");

        /// <summary>
        /// Logs a message with the specified log level.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">Optional exception associated with the log message.</param>
        /// <param name="source">The source of the message. If ommitted uses the calling method's name.</param>
        /// 
        void Log(LogLevel level, string message, Exception ex = null, [CallerMemberName] string source = "");
    }
}