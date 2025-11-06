using System;

namespace ByteForge.Toolkit.Net
{
    /// <summary>
    /// Information about a file or directory on the remote server.
    /// </summary>
    public class RemoteFileInfo
    {
        /// <summary>
        /// Gets or sets the name of the file or directory.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the full path of the file or directory on the server.
        /// </summary>
        public string? FullPath { get; set; }

        /// <summary>
        /// Gets or sets the size of the file in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Gets or sets the last modification date and time.
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Gets or sets whether this is a directory.
        /// </summary>
        public bool IsDirectory { get; set; }

        /// <summary>
        /// Gets or sets the file permissions as a string.
        /// </summary>
        public string? Permissions { get; set; }
    }
}
