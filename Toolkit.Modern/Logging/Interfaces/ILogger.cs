using System;

namespace ByteForge.Toolkit.Logging;
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
    // General Logging Methods
    /// <summary>
    /// Logs a message with the specified log level.
    /// </summary>
    /// <param name="level">The log level.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="ex">Optional exception associated with the log message.</param>
    void Log(LogLevel level, string message, Exception? ex = null);

    // Level-Specific Logging Methods
    /// <summary>
    /// Logs a trace message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void LogTrace(string message);

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void LogDebug(string message);

    /// <summary>
    /// Logs a verbose message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void LogVerbose(string message);

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void LogInfo(string message);

    /// <summary>
    /// Logs a notice message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void LogNotice(string message);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void LogWarning(string message);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="ex">Optional exception associated with the error.</param>
    void LogError(string message, Exception? ex = null);

    /// <summary>
    /// Logs a critical error message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="ex">Optional exception associated with the critical error.</param>
    void LogCritical(string message, Exception? ex = null);

    /// <summary>
    /// Logs a fatal error message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="ex">Optional exception associated with the fatal error.</param>
    void LogFatal(string message, Exception? ex = null);

    // Utility Methods
    /// <summary>
    /// Gets the name of the logger.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Gets or sets the minimum log level for the logger.
    /// </summary>
    LogLevel MinLogLevel { get; set; }
}
