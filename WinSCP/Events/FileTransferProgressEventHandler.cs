namespace ByteForge.WinSCP;

/// <summary>
/// Represents the method that will handle the file transfer progress event.
/// </summary>
/// <param name="sender">The source of the event.</param>
/// <param name="e">A <see cref="FileTransferProgressEventArgs"/> that contains the event data.</param>
public delegate void FileTransferProgressEventHandler(object sender, FileTransferProgressEventArgs e);
