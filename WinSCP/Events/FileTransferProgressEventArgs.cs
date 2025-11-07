using System;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Provides data for the file transfer progress event.
/// </summary>
[Guid("E421924E-87F0-433E-AF38-CE034DC8E8CB")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class FileTransferProgressEventArgs : EventArgs
{
	/// <summary>
	/// Gets the type of operation being performed.
	/// </summary>
	public ProgressOperation Operation { get; internal set; }

	/// <summary>
	/// Gets the side (local or remote) of the transfer.
	/// </summary>
	public ProgressSide Side { get; internal set; }

	/// <summary>
	/// Gets the name of the file being transferred.
	/// </summary>
	public string FileName { get; internal set; }

	/// <summary>
	/// Gets the directory containing the file being transferred.
	/// </summary>
	public string Directory { get; internal set; }

	/// <summary>
	/// Gets the overall progress of the transfer operation as a percentage (0-100).
	/// </summary>
	public double OverallProgress { get; internal set; }

	/// <summary>
	/// Gets the progress of the current file transfer as a percentage (0-100).
	/// </summary>
	public double FileProgress { get; internal set; }

	/// <summary>
	/// Gets the transfer speed in characters per second (CPS).
	/// </summary>
	public int CPS { get; internal set; }

	/// <summary>
	/// Gets or sets a value indicating whether to cancel the transfer operation.
	/// </summary>
	public bool Cancel { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="FileTransferProgressEventArgs"/> class.
	/// </summary>
	internal FileTransferProgressEventArgs()
	{
	}
}
