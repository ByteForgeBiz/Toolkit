namespace ByteForge.Toolkit.Net;
/// <summary>
/// Result of a file transfer operation.
/// </summary>
public class FileTransferResult
{
    /// <summary>
    /// Gets or sets the path to the local file.
    /// </summary>
    public string? LocalPath { get; set; }

    /// <summary>
    /// Gets or sets the path to the remote file.
    /// </summary>
    public string? RemotePath { get; set; }

    /// <summary>
    /// Gets or sets whether the transfer was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the transfer failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
