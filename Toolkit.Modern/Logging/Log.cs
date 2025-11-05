using System;
using System.IO;
using System.Linq;
#if NETFRAMEWORK
using System.Web;
#endif
using ByteForge.Toolkit.Logging;

namespace ByteForge.Toolkit
{
    /*
     *  _              
     * | |   ___  __ _ 
     * | |__/ _ \/ _` |
     * |____\___/\__, |
     *           |___/ 
     */
    /// <summary>
    /// Provides logging functionality for the application.
    /// </summary>
    public class Log : CompositeLogger
    {
        private LogLevel? _PreviousConsoleLoggerLevel = LogLevel.Verbose;
        private readonly FileLogger fileLogger;
        private readonly ConsoleLogger consoleLogger = new ConsoleLogger()
        {
            Name = "Console",
            MinLogLevel = LogLevel.Verbose,
            ShowMessageOnly = true,
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="Log"/> class.
        /// </summary>
        private Log() : base("ByteForge.Toolkit->Static Logger")
        {
            if (!Configuration.IsInitialized)
                Settings = new LogSettings();
            else
                Settings = Configuration.GetSection<LogSettings>("Logging");

            // Create the appropriate file logger based on configuration
            if (Settings.UseSessionLogging)
            {
                SessionFileLoggerOptions sessionOptions;
                if (!Configuration.IsInitialized)
                    sessionOptions = new SessionFileLoggerOptions();
                else
                    sessionOptions = Configuration.GetSection<SessionFileLoggerOptions>("FileLogger");

                fileLogger = new SessionFileLogger(Settings.LogFilePath, sessionOptions)
                {
                    Name = "SessionFile",
                    MinLogLevel = Settings.TraceLogLevel
                };
            }
            else
            {
                fileLogger = new FileLogger(Settings.LogFilePath)
                {
                    Name = "File",
                    MinLogLevel = Settings.TraceLogLevel
                };
            }

#if !DEBUG && NETFRAMEWORK
            if (HttpContext.Current == null && AppDomain.CurrentDomain.FriendlyName.Contains("W3SVC") == false)
#else
            if (AppDomain.CurrentDomain.FriendlyName.Contains("W3SVC") == false)
#endif
                AddLogger(consoleLogger);

            AddLogger(fileLogger);

            if (Settings.ClearLogOnStartup && !Settings.UseSessionLogging)
                fileLogger.Clear();

            this.MinLogLevel = Settings.TraceLogLevel;
        }

        /// <summary>
        /// Gets a value indicating whether the static Log instance has been initialized.
        /// </summary>
        public static bool IsInitialized => _instance != null;

        // Add these properties to access session logger features
        /// <summary>
        /// Gets the session file logger instance if session logging is enabled.
        /// </summary>
        public static SessionFileLogger SessionLogger => Instance?.fileLogger as SessionFileLogger;

        /// <summary>
        /// Gets the current session's log file path if using session logging.
        /// </summary>
        public static string CurrentLogFile => SessionLogger?.CurrentFilePath ?? ((FileLogger)Instance.fileLogger)?.CurrentFilePath;

        /// <summary>
        /// Ends the current logging session if using session logging.
        /// Call this during application shutdown.
        /// </summary>
        public static void EndSession()
        {
            SessionLogger?.EndSession();
        }

        /// <summary>
        /// Gets a value indicating whether console logging is enabled.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if console logging is enabled; otherwise, <see langword="false" />.
        /// </value>
        public static bool IsConsoleLoggingEnabled => Instance?.consoleLogger.MinLogLevel != LogLevel.None;

        /// <summary>
        /// Gets the alternate logger instance, which logs to a temporary file.
        /// </summary>
        public ILogger AlternateLogger =>
                                _alternateLogger ??= new FileLogger(Path.Combine(Path.GetTempPath(), $"LogError.{Guid.NewGuid()}.log"))
                                {
                                    Name = "Alternate",
                                    MinLogLevel = LogLevel.All,
                                };
        private ILogger _alternateLogger;

        /// <summary>
        /// Gets the singleton instance of the Log class.
        /// </summary>
        public static Log Instance => _instance ??= new Log();
        private static Log _instance;
        private bool _disposed;

        /// <summary>
        /// Enables console logging.
        /// </summary>
        public static void EnableConsoleLogging()
        {
            Instance.consoleLogger.MinLogLevel = Instance._PreviousConsoleLoggerLevel ?? LogLevel.Verbose;
            Instance._PreviousConsoleLoggerLevel = null;
        }

        /// <summary>
        /// Disables console logging.
        /// </summary>
        public static void DisableConsoleLogging()
        {
            if (!Instance._PreviousConsoleLoggerLevel.HasValue)
                Instance._PreviousConsoleLoggerLevel = Instance.consoleLogger.MinLogLevel;

            Instance.consoleLogger.MinLogLevel = LogLevel.None;
        }

        /// <summary>
        /// Gets or sets the log level for all loggers.
        /// </summary>
        public static LogLevel LogLevel
        {
            get => Instance.Loggers.Where(l => l.MinLogLevel != LogLevel.None).Max(l => l.MinLogLevel);
            set
            {
                Instance.LogVerbose($"Setting log level to {value}.");
                Instance.Loggers.ForEach(l => l.MinLogLevel = value);
            }
        }

        /// <summary>
        /// Gets the console logger instance.
        /// </summary>
        /// <value>
        /// The <see cref="ConsoleLogger"/> instance used for console logging.
        /// </value>
        public static ConsoleLogger Console => (ConsoleLogger)Instance.consoleLogger;

        /// <summary>
        /// Gets the logging configuration options.
        /// </summary>
        /// <remarks>
        /// This property retrieves the logging options from the application's configuration. <br/> 
        /// If the options have not been initialized, they are loaded from the "Logging" section of the
        /// configuration.</remarks>
        public LogSettings Settings { get; }

        /// <summary>
        /// Logs a trace message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Trace(string message) => WriteToLog(LogLevel.Trace, message, null);

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Debug(string message) => WriteToLog(LogLevel.Debug, message, null);

        /// <summary>
        /// Logs a verbose message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Verbose(string message) => WriteToLog(LogLevel.Verbose, message, null);

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Info(string message) => WriteToLog(LogLevel.Info, message, null);

        /// <summary>
        /// Logs a notice message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Notice(string message) => WriteToLog(LogLevel.Notice, message, null);

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Warning(string message) => WriteToLog(LogLevel.Warning, message, null);

        /// <summary>
        /// Logs a warning message with an exception.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">The exception to log.</param>
        public static void Warning(string message, Exception ex) => WriteToLog(LogLevel.Warning, message, ex);

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Error(string message) => WriteToLog(LogLevel.Error, message, null);

        /// <summary>
        /// Logs an error message with an exception.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        public static void Error(Exception ex) => WriteToLog(LogLevel.Error, ex.Message, ex);

        /// <summary>
        /// Logs an error message with an exception and a custom message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">The exception to log.</param>
        public static void Error(string message, Exception ex) => WriteToLog(LogLevel.Error, message, ex);

        /// <summary>
        /// Logs a critical error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Critical(string message) => WriteToLog(LogLevel.Critical, message, null);

        /// <summary>
        /// Logs a critical error message with an exception.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        public static void Critical(Exception ex) => WriteToLog(LogLevel.Critical, ex.Message, ex);

        /// <summary>
        /// Logs a critical error message with an exception and a custom message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">The exception to log.</param>
        public static void Critical(string message, Exception ex) => WriteToLog(LogLevel.Critical, message, ex);

        /// <summary>
        /// Logs a fatal error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Fatal(string message) => WriteToLog(LogLevel.Fatal, message, null);

        /// <summary>
        /// Logs a fatal error message with an exception.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        public static void Fatal(Exception ex) => WriteToLog(LogLevel.Fatal, ex.Message, ex);

        /// <summary>
        /// Logs a fatal error message with an exception and a custom message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">The exception to log.</param>
        public static void Fatal(string message, Exception ex) => WriteToLog(LogLevel.Fatal, message, ex);

        /// <summary>
        /// Writes a message to a specified log file.
        /// </summary>
        /// <param name="level">The trace level of the message.</param>
        /// <param name="message">The message to write.</param>
        /// <param name="ex"></param>
        /// 
        /// 
        private static void WriteToLog(LogLevel level, string message, Exception ex)
        {
            if (string.IsNullOrEmpty(message))
                return;
            Instance.WriteToLog(level, message.Split(Utils.arrCRLF, StringSplitOptions.RemoveEmptyEntries), ex);
        }

        /// <summary>
        /// Writes a set of messages to a specified log file.
        /// </summary>
        /// <param name="level">The trace level of the messages.</param>
        /// <param name="text">The messages to write.</param>
        /// <param name="ex"></param>
        /// 
        /// 
        private void WriteToLog(LogLevel level, string[] text, Exception ex)
        {
            if (text == null ||
                text.Length == 0 ||
                (text.Length == 1 && string.IsNullOrEmpty(text[0])))
                return;

            try
            {
                Instance.Log(level, string.Join(Environment.NewLine, text), ex);
            }
            catch (Exception ex2)
            {
                /*
                 * Generally, if an error occurs in this method, it is due to a permissions issue.
                 * So, we will attempt to write the error to a temporary file in the user's temp directory,
                 * as the user usually have write permissions there.
                 */
                AlternateLogger.LogCritical("An error occurred while writing to the log file.", ex2);
                AlternateLogger.Log(level, string.Join("\n", text), ex);
            }
        }

        /// <summary>
        /// Releases the resources used by the current instance of the class.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                SessionLogger?.EndSession();
                SessionLogger?.Dispose();
                fileLogger?.Dispose();
            }

            _disposed = true;
        }
   }
}