namespace ByteForge.Toolkit.Net
{
    /// <summary>
    /// Represents a file transfer item for batch operations.
    /// </summary>
    public class FileTransferItem
    {
        /// <summary>
        /// Gets or sets the path to the local file.
        /// </summary>
        public string LocalPath { get; set; }

        /// <summary>
        /// Gets or sets the path to the remote file.
        /// </summary>
        public string RemotePath { get; set; }

        /// <summary>
        /// Gets or sets whether to overwrite existing files.
        /// </summary>
        public bool Overwrite { get; set; }
    }
}
