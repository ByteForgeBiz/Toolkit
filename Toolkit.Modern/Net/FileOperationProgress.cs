namespace ByteForge.Toolkit.Net
{
    /// <summary>
    /// Progress information for file operations.
    /// </summary>
    public class FileOperationProgress
    {
        /// <summary>
        /// Gets or sets the number of files processed.
        /// </summary>
        public int ProcessedCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of files to process.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Gets or sets the percentage of completion (0-100).
        /// </summary>
        public int PercentComplete { get; set; }

        /// <summary>
        /// Gets or sets the current path being processed.
        /// </summary>
        public string CurrentPath { get; set; }
    }
}
