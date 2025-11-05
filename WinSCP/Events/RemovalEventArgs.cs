using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[Guid("62FB0733-C24F-4DC2-8452-560148931927")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class RemovalEventArgs : FileOperationEventArgs
{
	internal const string Tag = "rm";

	private RemovalEventArgs()
	{
	}

	internal static RemovalEventArgs Read(CustomLogReader areader)
	{
		RemovalEventArgs e = new RemovalEventArgs();
		using ElementLogReader elementLogReader = new ElementLogReader(areader);
		while (elementLogReader.Read((LogReadFlags)0))
		{
			if (elementLogReader.GetEmptyElementValue("filename", out var value))
			{
				e.FileName = value;
			}
			else if (SessionRemoteException.IsResult(elementLogReader))
			{
				e.Error = SessionRemoteException.ReadResult(elementLogReader);
			}
		}
		return e;
	}
}
