using System.Globalization;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[Guid("772FACCC-0786-42E1-B1C8-F08D13C9CD07")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class TransferEventArgs : FileOperationEventArgs
{
	internal const string UploadTag = "upload";

	internal const string DownloadTag = "download";

	internal const string MkDirTag = "mkdir";

	public ProgressSide Side { get; internal set; }

	public string Destination { get; private set; }

	public long Length { get; private set; }

	public TouchEventArgs Touch { get; internal set; }

	public ChmodEventArgs Chmod { get; internal set; }

	public RemovalEventArgs Removal { get; internal set; }

	internal TransferEventArgs()
	{
	}

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
