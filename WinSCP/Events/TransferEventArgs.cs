using System.Globalization;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Provides data for file transfer events.
/// </summary>
[Guid("772FACCC-0786-42E1-B1C8-F08D13C9CD07")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class TransferEventArgs : FileOperationEventArgs
{
	/// <summary>
	/// The XML tag name for upload operations.
	/// </summary>
	internal const string UploadTag = "upload";

	/// <summary>
	/// The XML tag name for download operations.
	/// </summary>
	internal const string DownloadTag = "download";

	/// <summary>
	/// The XML tag name for mkdir operations.
	/// </summary>
	internal const string MkDirTag = "mkdir";

	/// <summary>
	/// Gets the side (local or remote) of the transfer.
	/// </summary>
	public ProgressSide Side { get; internal set; }

	/// <summary>
	/// Gets the destination path of the transferred file.
	/// </summary>
	public string Destination { get; private set; }

	/// <summary>
	/// Gets the size of the transferred file in bytes.
	/// </summary>
	public long Length { get; private set; }

	/// <summary>
	/// Gets the touch event arguments associated with this transfer.
	/// </summary>
	public TouchEventArgs Touch { get; internal set; }

	/// <summary>
	/// Gets the chmod event arguments associated with this transfer.
	/// </summary>
	public ChmodEventArgs Chmod { get; internal set; }

	/// <summary>
	/// Gets the removal event arguments associated with this transfer.
	/// </summary>
	public RemovalEventArgs Removal { get; internal set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="TransferEventArgs"/> class.
	/// </summary>
	internal TransferEventArgs()
	{
	}

	/// <summary>
	/// Reads transfer event arguments from the log reader.
	/// </summary>
	/// <param name="side">The side of the transfer (local or remote).</param>
	/// <param name="areader">The log reader to read from.</param>
	/// <returns>A new <see cref="TransferEventArgs"/> instance.</returns>
	internal static TransferEventArgs Read(ProgressSide side, CustomLogReader areader)
	{
		TransferEventArgs e = new TransferEventArgs
		{
			Side = side
		};
		using ElementLogReader elementLogReader = new ElementLogReader(areader);
		while (elementLogReader.Read((LogReadFlags)0))
		{
			if (elementLogReader.GetEmptyElementValue("filename", out var value))
			{
				e.FileName = value;
			}
			else if (elementLogReader.GetEmptyElementValue("destination", out value))
			{
				e.Destination = value;
			}
			else if (elementLogReader.GetEmptyElementValue("size", out value))
			{
				e.Length = long.Parse(value, CultureInfo.InvariantCulture);
			}
			else if (SessionRemoteException.IsResult(elementLogReader))
			{
				e.Error = SessionRemoteException.ReadResult(elementLogReader);
			}
		}
		return e;
	}
}
