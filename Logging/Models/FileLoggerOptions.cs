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
    public class FileLoggerOptions
    {
        /// <summary>
        /// Gets or sets whether to create a new log file each day.
        /// </summary>
        public bool UseDaily { get; set; }

        /// <summary>
        /// Gets or sets the custom date format for file names (default: "yyyy-MM-dd").
        /// </summary>
        public string DateFormat { get; set; } = "yyyy-MM-dd";

        /// <summary>
        /// Gets or sets the maximum file size in megabytes before rolling over (0 for unlimited).
        /// </summary>
        public int MaxFileSizeMB { get; set; }

        /// <summary>
        /// Gets or sets the number of days to retain log files (0 for unlimited).
        /// </summary>
        public int RetentionDays { get; set; }

        /// <summary>
        /// Gets or sets whether to use async logging.
        /// </summary>
        public bool UseAsyncLogging { get; set; } = false;

        /// <summary>
        /// Gets or sets the size of the async logging queue (default: 1000).
        /// </summary>
        public int AsyncQueueSize { get; set; } = 1000;
    }
}