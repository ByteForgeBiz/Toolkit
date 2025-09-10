using System.ComponentModel;

namespace ByteForge.Toolkit.Logging
{
    /*
     *    _                     ___       _   _             
     *   /_\   ____  _ _ _  __ / _ \ _ __| |_(_)___ _ _  ___
     *  / _ \ (_-< || | ' \/ _| (_) | '_ \  _| / _ \ ' \(_-<
     * /_/ \_\/__/\_, |_||_\__|\___/| .__/\__|_\___/_||_/__/
     *            |__/              |_|                     
     */
    /// <summary>
    /// Provides base options for asynchronous logging configuration.
    /// </summary>
    public abstract class AsyncOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncOptions"/> class.
        /// </summary>
        protected AsyncOptions() { }

        /// <summary>
        /// Gets or sets the number of days to retain log files.
        /// A value of 0 indicates unlimited retention.
        /// </summary>
        [DefaultValue(0)]
        public int RetentionDays { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use asynchronous logging.
        /// </summary>
        [DefaultValue(false)]
        public bool UseAsyncLogging { get; set; }

        /// <summary>
        /// Gets or sets the size of the asynchronous logging queue.
        /// The default value is 1000.
        /// </summary>
        [DefaultValue(1000)]
        public int AsyncQueueSize { get; set; } = 1000;
    }
}
