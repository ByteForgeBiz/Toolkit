namespace ByteForge.Toolkit.Net
{
    /// <summary>
    /// Result of a file operation.
    /// </summary>
    public class FileOperationResult
    {
        /// <summary>
        /// Gets or sets the path to the file.
        /// </summary>
        public string? Path  { get; set; }

        /// <summary>
        /// Gets or sets whether the operation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if the operation failed.
        /// </summary>
        public string? ErrorMessage  { get; set; }
    }
}
