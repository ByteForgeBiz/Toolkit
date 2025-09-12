using System;

namespace ByteForge.Toolkit.Logging
{
    /*
     *  ___ _        _   _    _                            _      _           _           
     * / __| |_ __ _| |_(_)__| |   ___  __ _ __ _ ___ _ _ /_\  __| |__ _ _ __| |_ ___ _ _ 
     * \__ \  _/ _` |  _| / _| |__/ _ \/ _` / _` / -_) '_/ _ \/ _` / _` | '_ \  _/ -_) '_|
     * |___/\__\__,_|\__|_\__|____\___/\__, \__, \___|_|/_/ \_\__,_\__,_| .__/\__\___|_|  
     *                                 |___/|___/                       |_|               
     */
    /// <summary>
    /// Adapter that wraps the static Log class to implement ILogger interface.
    /// This allows dependency injection while maintaining backward compatibility.
    /// </summary>
    internal class StaticLoggerAdapter : ILogger
    {
        public StaticLoggerAdapter()
        {
            if (!ByteForge.Toolkit.Log.IsInitialized)
                throw new InvalidOperationException("The static Log class must be initialized before using StaticLoggerAdapter.");
        }

        /// <summary>
        /// Gets or sets the name of the logger.
        /// </summary>
        public string Name { get; set; } = "StaticLoggerAdapter";

        /// <summary>
        /// Gets or sets the minimum log level by delegating to the static Log class.
        /// </summary>
        public LogLevel MinLogLevel 
        { 
            get => ByteForge.Toolkit.Log.LogLevel; 
            set => ByteForge.Toolkit.Log.LogLevel = value; 
        }

        /// <summary>
        /// Logs a message with the specified log level by delegating to the static Log class.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">Optional exception associated with the log message.</param>
                public void Log(LogLevel level, string message, Exception ex = null)
        {
            // Delegate to the appropriate static Log method based on level
            switch (level)
            {
                case LogLevel.Trace:
                    ByteForge.Toolkit.Log.Trace(message);
                    break;
                case LogLevel.Debug:
                    ByteForge.Toolkit.Log.Debug(message);
                    break;
                case LogLevel.Verbose:
                    ByteForge.Toolkit.Log.Verbose(message);
                    break;
                case LogLevel.Info:
                    ByteForge.Toolkit.Log.Info(message);
                    break;
                case LogLevel.Notice:
                    ByteForge.Toolkit.Log.Notice(message);
                    break;
                case LogLevel.Warning:
                    if (ex != null)
                        ByteForge.Toolkit.Log.Warning(message, ex);
                    else
                        ByteForge.Toolkit.Log.Warning(message);
                    break;
                case LogLevel.Error:
                    if (ex != null)
                        ByteForge.Toolkit.Log.Error(message, ex);
                    else
                        ByteForge.Toolkit.Log.Error(message);
                    break;
                case LogLevel.Critical:
                    if (ex != null)
                        ByteForge.Toolkit.Log.Critical(message, ex);
                    else
                        ByteForge.Toolkit.Log.Critical(message);
                    break;
                case LogLevel.Fatal:
                    if (ex != null)
                        ByteForge.Toolkit.Log.Fatal(message, ex);
                    else
                        ByteForge.Toolkit.Log.Fatal(message);
                    break;
            }
        }

        /// <summary>
        /// Logs a trace message.
        /// </summary>
        /// <param name="message">The message to log.</param>
                public void LogTrace(string message)
        {
            ByteForge.Toolkit.Log.Trace(message);
        }

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The message to log.</param>
                public void LogDebug(string message)
        {
            ByteForge.Toolkit.Log.Debug(message);
        }

        /// <summary>
        /// Logs a verbose message.
        /// </summary>
        /// <param name="message">The message to log.</param>
                public void LogVerbose(string message)
        {
            ByteForge.Toolkit.Log.Verbose(message);
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message to log.</param>
                public void LogInfo(string message)
        {
            ByteForge.Toolkit.Log.Info(message);
        }

        /// <summary>
        /// Logs a notice message.
        /// </summary>
        /// <param name="message">The message to log.</param>
                public void LogNotice(string message)
        {
            ByteForge.Toolkit.Log.Notice(message);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
                public void LogWarning(string message)
        {
            ByteForge.Toolkit.Log.Warning(message);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">Optional exception associated with the error.</param>
                public void LogError(string message, Exception ex = null)
        {
            if (ex != null)
                ByteForge.Toolkit.Log.Error(message, ex);
            else
                ByteForge.Toolkit.Log.Error(message);
        }

        /// <summary>
        /// Logs a critical error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">Optional exception associated with the critical error.</param>
                public void LogCritical(string message, Exception ex = null)
        {
            if (ex != null)
                ByteForge.Toolkit.Log.Critical(message, ex);
            else
                ByteForge.Toolkit.Log.Critical(message);
        }

        /// <summary>
        /// Logs a fatal error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">Optional exception associated with the fatal error.</param>
                public void LogFatal(string message, Exception ex = null)
        {
            if (ex != null)
                ByteForge.Toolkit.Log.Fatal(message, ex);
            else
                ByteForge.Toolkit.Log.Fatal(message);
        }
    }
}