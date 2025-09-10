using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ByteForge.Toolkit.Logging
{
    /*
     *  ___            _          ___ _ _     _                           
     * / __| ___ _____(_)___ _ _ | __(_) |___| |   ___  __ _ __ _ ___ _ _ 
     * \__ \/ -_)_-<_-< / _ \ ' \| _|| | / -_) |__/ _ \/ _` / _` / -_) '_|
     * |___/\___/__/__/_\___/_||_|_| |_|_\___|____\___/\__, \__, \___|_|  
     *                                                 |___/|___/         
     */
    /// <summary>
    /// A specialized file logger that creates a unique log file for each application session/run.
    /// Inherits all the advanced features of FileLogger (rotation, async, etc.) while adding session management.
    /// </summary>
    public class SessionFileLogger : FileLogger
    {
        /// <summary>
        /// Gets the unique session ID for this application run.
        /// </summary>
        public string SessionId => _sessionId = _sessionId ?? GenerateSessionId(Settings);
        private string _sessionId = null;

        /// <summary>
        /// Gets the timestamp when this logging session started.
        /// </summary>
        public DateTime SessionStartTime { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionFileLogger"/> class.
        /// </summary>
        /// <param name="baseFilePath">The base path for log files. Session-specific suffix will be added.</param>
        /// <param name="sessionOptions">Configuration options specific to session management.</param>
        public SessionFileLogger(string baseFilePath, SessionFileLoggerOptions sessionOptions = null) : base(baseFilePath, sessionOptions, true)
        {
            SessionStartTime = DateTime.Now;
            Settings = sessionOptions ?? Configuration.GetSection<SessionFileLoggerOptions>("SessionFileLogger") ?? new SessionFileLoggerOptions();

            // Override the logger name to indicate it's a session logger
            this.Name = $"SessionFile-{SessionId}";

            // Write session header if configured
            if (Settings.WriteSessionHeader)
                WriteSessionHeader();

            // Clean up old sessions if configured
            if (Settings.CleanupOldSessions)
                CleanupOldSessions();

            InitializeFileLogger();
        }

        /// <summary>
        /// Gets the configuration options for the session file logger.
        /// </summary>
        public new SessionFileLoggerOptions Settings { get; }

        /// <summary>
        /// Generates a unique session ID based on the configured format.
        /// </summary>
        private string GenerateSessionId(SessionFileLoggerOptions options)
        {
            var now = DateTime.Now;

            switch (options.SessionIdFormat)
            {
                case SessionIdFormat.Timestamp:
                    return now.ToString("yyyyMMdd-HHmmss");

                case SessionIdFormat.TimestampWithMilliseconds:
                    return now.ToString("yyyyMMdd-HHmmss-fff");

                case SessionIdFormat.Guid:
                    return Guid.NewGuid().ToString("N").Substring(0, 8); // First 8 chars of GUID

                case SessionIdFormat.ProcessId:
                    return $"{now:yyyyMMdd-HHmmss}-P{System.Diagnostics.Process.GetCurrentProcess().Id}";

                case SessionIdFormat.Custom:
                    if (!string.IsNullOrEmpty(options.CustomSessionIdProvider?.Invoke()))
                        return options.CustomSessionIdProvider();
                    goto default;

                default:
                    return now.ToString("yyyyMMdd-HHmmss");
            }
        }

        /// <summary>
        /// Processes additional placeholders specific to session logging.
        /// </summary>
        protected override string ProcessAdditionalPlaceholders(string fileName, FileLoggerOptions options)
        {
            // Process session-specific placeholders
            fileName = fileName.Replace("{sessionid}", SessionId);

            return base.ProcessAdditionalPlaceholders(fileName, options);
        }

        /// <summary>
        /// Writes the session header to the log file.
        /// </summary>
        private void WriteSessionHeader()
        {
            var header = new StringBuilder();
            header.AppendLine("".PadRight(80, '='));
            header.AppendLine($"SESSION STARTED: {SessionStartTime:yyyy-MM-dd HH:mm:ss.fff}");
            header.AppendLine($"SESSION ID: {SessionId}");
            header.AppendLine($"APPLICATION: {Assembly.GetEntryAssembly()?.GetName().Name ?? "Unknown"}");
            header.AppendLine($"VERSION: {Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown"}");
            header.AppendLine($"MACHINE: {Environment.MachineName}");
            header.AppendLine($"USER: {Environment.UserName}");
            header.AppendLine($"PROCESS ID: {System.Diagnostics.Process.GetCurrentProcess().Id}");
            header.AppendLine("".PadRight(80, '='));

            // Use the parent's logging mechanism to write the header
            var headerEntry = new LogEntry
            {
                Timestamp = SessionStartTime,
                Level = LogLevel.Info,
                Message = header.ToString().TrimEnd(),
                Source = "SessionStart"
            };

            base.RecordLogEntry(headerEntry);
        }

        /// <summary>
        /// Cleans up old session log files based on retention policy.
        /// </summary>
        private void CleanupOldSessions()
        {
            if (Settings.RetentionDays <= 0) return;

            try
            {
                var currentFilePath = this.CurrentFilePath;
                var directory = Path.GetDirectoryName(currentFilePath);

                // Create a search pattern based on the naming pattern
                var searchPattern = Settings.FileNamingPattern
                    .Replace("{basename}", "*")
                    .Replace("{sessionid}", "*")
                    .Replace("{timestamp}", "*")
                    .Replace("{pid}", "*")
                    .Replace("{index}", "*")
                    .Replace("{guid}", "*");

                // Handle date patterns
                searchPattern = System.Text.RegularExpressions.Regex.Replace(
                    searchPattern, @"\{date(?::[^}]+)?\}", "*");

                searchPattern += "*.log";

                var cutoffDate = DateTime.Now.AddDays(-Settings.RetentionDays);

                var oldFiles = Directory.GetFiles(directory, searchPattern)
                    .Select(f => new FileInfo(f))
                    .Where(f => f.LastWriteTime < cutoffDate && f.FullName != currentFilePath);

                foreach (var file in oldFiles)
                {
                    try
                    {
                        file.Delete();
                    }
                    catch
                    {
                        // Log cleanup failure if needed, but don't throw
                    }
                }
            }
            catch
            {
                // Don't let cleanup failures affect logging
            }
        }

        /// <summary>
        /// Writes a session footer and closes the current session.
        /// Call this when the application is shutting down.
        /// </summary>
        public void EndSession()
        {
            if (Settings.WriteSessionFooter)
            {
                var footer = new StringBuilder();
                footer.AppendLine("".PadRight(80, '='));
                footer.AppendLine($"SESSION ENDED: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                footer.AppendLine($"DURATION: {DateTime.Now - SessionStartTime}");
                footer.AppendLine("".PadRight(80, '='));

                // Use the parent's logging mechanism to write the footer
                var footerEntry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Level = LogLevel.Info,
                    Message = footer.ToString().TrimEnd(),
                    Source = "SessionEnd"
                };

                base.RecordLogEntry(footerEntry);
            }
        }

        /// <summary>
        /// Releases the resources used by the current instance of the class.
        /// </summary>
        /// <param name="disposing">A value indicating whether to release both managed and unmanaged resources  (<see langword="true"/>) or only unmanaged resources (<see langword="false"/>).</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                EndSession();

            base.Dispose(disposing);
        }
    }
}