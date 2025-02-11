using ByteForge.Toolkit.Logging;
using System.IO;
using System.Reflection;

namespace ByteForge.Toolkit
{
    /// <summary>
    /// Represents the options for logging configuration.
    /// </summary>
    public class LogOptions
    {
        /// <summary>
        /// Gets or sets the path to the log file.
        /// </summary>
        /// <remarks>
        /// The default log file path is determined by the <see cref="GetDefaultLogPath"/> method.
        /// </remarks>
        [PropertyName("sLogFile", "GetDefaultLogPath")]
        public string LogFilePath { get; set; }

        /// <summary>
        /// Gets or sets the log level for tracing.
        /// </summary>
        public LogLevel TraceLogLevel { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the log should be cleared on startup.
        /// </summary>
        /// <value>
        /// <c>true</c> if the log should be cleared on startup; otherwise, <c>false</c>.
        /// </value>
        public bool ClearLogOnStartup { get; set; }

        /// <summary>
        /// Gets the default log file path.
        /// </summary>
        /// <returns>The default log file path.</returns>
        private static string GetDefaultLogPath(string paramName)
        {
            // paramName is ignored because the default log file path is determined by the calling assembly.

            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly() ?? Assembly.GetExecutingAssembly();
            return Path.Combine(Path.GetDirectoryName(asm.Location), $"{asm.GetName().Name}.log");
        }
    }
}