namespace ByteForge.Toolkit
{
    /// <summary>
    /// Information about a file that was skipped during processing.
    /// </summary>
    public class SkippedFile
    {
        /// <summary>
        /// Gets or sets the file path of the skipped file.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the reason why the file was skipped.
        /// </summary>
        public string Reason { get; set; }
    }
}