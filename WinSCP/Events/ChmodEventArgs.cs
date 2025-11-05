using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[Guid("DFCA88E2-6A47-4290-AD66-A39C5682D610")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class ChmodEventArgs : FileOperationEventArgs
{
	internal const string Tag = "chmod";

	public FilePermissions FilePermissions { get; private set; }

	private ChmodEventArgs()
	{
	}

	internal static ChmodEventArgs Read(CustomLogReader areader)
	{
		ChmodEventArgs e = new ChmodEventArgs();
		using ElementLogReader elementLogReader = new ElementLogReader(areader);
		while (elementLogReader.Read((LogReadFlags)0))
		{
			if (elementLogReader.GetEmptyElementValue("filename", out var value))
			{
				e.FileName = value;
			}
			else if (elementLogReader.GetEmptyElementValue("permissions", out value))
			{
				e.FilePermissions = FilePermissions.CreateReadOnlyFromText(value);
			}
			else if (SessionRemoteException.IsResult(elementLogReader))
			{
				e.Error = SessionRemoteException.ReadResult(elementLogReader);
			}
		}
		return e;
	}
}
