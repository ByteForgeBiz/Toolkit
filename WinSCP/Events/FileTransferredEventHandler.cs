namespace ByteForge.WinSCP;

/// <summary>
/// Represents the method that will handle the file transferred event.
/// </summary>
/// <param name="sender">The source of the event.</param>
/// <param name="e">A <see cref="TransferEventArgs"/> that contains the event data.</param>
public delegate void FileTransferredEventHandler(object sender, TransferEventArgs e);
