namespace ByteForge.Toolkit.Logging
{
    /*
     *  _              _                _ 
     * | |   ___  __ _| |   _____ _____| |
     * | |__/ _ \/ _` | |__/ -_) V / -_) |
     * |____\___/\__, |____\___|\_/\___|_|
     *           |___/                    
     */
    /// <summary>
    /// Specifies the log level for logging events.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Logs that are used for interactive investigation during development. These logs should primarily contain information useful for debugging.
        /// </summary>
        Debug,

        /// <summary>
        /// Logs that contain the most detailed messages. These messages may contain sensitive application data.
        /// </summary>
        Verbose,

        /// <summary>
        /// Logs that track the general flow of the application. These logs should have long-term value.
        /// </summary>
        Info,

        /// <summary>
        /// Logs that highlight an abnormal or unexpected event in the application flow, but do not otherwise cause the application to stop.
        /// </summary>
        Warning,

        /// <summary>
        /// Logs that highlight when the current flow of execution is stopped due to a failure. These should indicate a failure in the current activity, not an application-wide failure.
        /// </summary>
        Error,

        /// <summary>
        /// Logs that describe an unrecoverable application or system crash, or a catastrophic failure that requires immediate attention.
        /// </summary>
        Critical,

        /// <summary>
        /// Specifies that no logging should be done.
        /// </summary>
        None = int.MaxValue
    }
}