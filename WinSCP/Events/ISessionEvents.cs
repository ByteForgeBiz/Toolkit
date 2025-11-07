using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Defines events for WinSCP session operations.
/// </summary>
[ComVisible(true)]
[Guid("A1334E32-4EDF-4B51-A069-DA3FF1B19A5A")]
[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
public interface ISessionEvents
{
 /// <summary>
 /// Occurs when a file is transferred.
 /// </summary>
 /// <param name="sender">The source of the event.</param>
 /// <param name="e">The event data.</param>
 [DispId(1)]
 void FileTransferred(object sender, TransferEventArgs e);

 /// <summary>
 /// Occurs when a failure happens in the session.
 /// </summary>
 /// <param name="sender">The source of the event.</param>
 /// <param name="e">The event data.</param>
 [DispId(2)]
 void Failed(object sender, FailedEventArgs e);

 /// <summary>
 /// Occurs when output data is received.
 /// </summary>
 /// <param name="sender">The source of the event.</param>
 /// <param name="e">The event data.</param>
 [DispId(3)]
 void OutputDataReceived(object sender, OutputDataReceivedEventArgs e);

 /// <summary>
 /// Occurs when file transfer progress is reported.
 /// </summary>
 /// <param name="sender">The source of the event.</param>
 /// <param name="e">The event data.</param>
 [DispId(4)]
 void FileTransferProgress(object sender, FileTransferProgressEventArgs e);

 /// <summary>
 /// Occurs when a query is received.
 /// </summary>
 /// <param name="sender">The source of the event.</param>
 /// <param name="e">The event data.</param>
 [DispId(5)]
 void QueryReceived(object sender, QueryReceivedEventArgs e);
}
