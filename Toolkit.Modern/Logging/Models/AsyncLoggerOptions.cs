using System.ComponentModel;

namespace ByteForge.Toolkit.Logging
{
    /*
     *    _                    _                            ___       _   _             
     *   /_\   ____  _ _ _  __| |   ___  __ _ __ _ ___ _ _ / _ \ _ __| |_(_)___ _ _  ___
     *  / _ \ (_-< || | ' \/ _| |__/ _ \/ _` / _` / -_) '_| (_) | '_ \  _| / _ \ ' \(_-<
     * /_/ \_\/__/\_, |_||_\__|____\___/\__, \__, \___|_|  \___/| .__/\__|_\___/_||_/__/
     *            |__/                  |___/|___/              |_|                     
     */
    /// <summary>
    /// Provides base options for asynchronous logging configuration.
    /// </summary>
    public abstract class AsyncLoggerOptions
    {
        private const bool DefaultUseAsyncLogging = false;
        private const int DefaultAsyncQueueSize = 1000;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncLoggerOptions"/> class.
        /// </summary>
        protected AsyncLoggerOptions() { }

        /// <summary>
        /// Gets or sets a value indicating whether to use asynchronous logging.
        /// </summary>
        [DefaultValue(DefaultUseAsyncLogging)]
        public bool UseAsyncLogging { get; set; } = DefaultUseAsyncLogging;

        /// <summary>
        /// Gets or sets the size of the asynchronous logging queue.
        /// The default value is 1000.
        /// </summary>
        [DefaultValue(DefaultAsyncQueueSize)]
        public int AsyncQueueSize { get; set; } = DefaultAsyncQueueSize;
    }
}
