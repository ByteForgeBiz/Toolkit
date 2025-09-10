using System;
using System.ComponentModel;

namespace ByteForge.Toolkit.Logging
{
    /*
     *  ___ _ _     _                            ___       _   _             
     * | __(_) |___| |   ___  __ _ __ _ ___ _ _ / _ \ _ __| |_(_)___ _ _  ___
     * | _|| | / -_) |__/ _ \/ _` / _` / -_) '_| (_) | '_ \  _| / _ \ ' \(_-<
     * |_| |_|_\___|____\___/\__, \__, \___|_|  \___/| .__/\__|_\___/_||_/__/
     *                       |___/|___/              |_|                     
     */
    /// <summary>
    /// Configuration options for the FileLogger.
    /// </summary>
    public class FileLoggerOptions : AsyncOptions
    {
        /// <summary>
        /// Gets or sets whether to create a new log file each day.
        /// </summary>
        [DefaultValue(false)]
        public bool UseDaily { get; set; }

        /// <summary>
        /// Gets or sets the maximum file size in megabytes before rolling over (0 for unlimited).
        /// </summary>
        [DefaultValue(0)]
        public int MaxFileSizeMB { get; set; }

        /// <summary>
        /// Gets or sets the file naming pattern for log files.
        /// Supports the following placeholders:
        /// <list type="bullet">
        /// <item>
        /// <description><c>{basename}</c> - Original filename without extension</description>
        /// </item>
        /// <item>
        /// <description><c>{date:format}</c> - Current date using specified format (e.g., {date:yyyy-MM-dd})</description>
        /// </item>
        /// <item>
        /// <description><c>{timestamp}</c> - Current timestamp (yyyyMMdd-HHmmss)</description>
        /// </item>
        /// <item>
        /// <description><c>{index}</c> - File index for size-based rotation</description>
        /// </item>
        /// <item>
        /// <description><c>{pid}</c> - Process ID</description>
        /// </item>
        /// <item>
        /// <description><c>{guid}</c> - Short GUID (8 characters)</description>
        /// </item>
        /// </list>
        /// The default is <c>"{basename}"</c> (no pattern).
        /// </summary>
        [DefaultValue("{basename}")]
        public string FileNamingPattern { get; set; } = "{basename}";

        /// <summary>
        /// Gets or sets a custom file name provider function.
        /// When set, this function is called to generate the file name and overrides FileNamingPattern.
        /// The function receives the base file path and current FileLoggerOptions as parameters.
        /// </summary>
        public Func<string, FileLoggerOptions, string> CustomFileNameProvider { get; set; }
    }
}