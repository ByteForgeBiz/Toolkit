using System;
using System.Runtime.InteropServices;
using System.Xml;

namespace ByteForge.WinSCP;

[Guid("802FCEF7-E1D3-4205-B171-87A3724E85FA")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class TouchEventArgs : FileOperationEventArgs
{
	internal const string Tag = "touch";

	public DateTime LastWriteTime { get; private set; }

	private TouchEventArgs()
	{
	}

	internal static TouchEventArgs Read(CustomLogReader areader)
	{
		TouchEventArgs e = new TouchEventArgs();
		using ElementLogReader elementLogReader = new ElementLogReader(areader);
		while (elementLogReader.Read((LogReadFlags)0))
		{
			if (elementLogReader.GetEmptyElementValue("filename", out var value))
			{
				e.FileName = value;
			}
			else if (elementLogReader.GetEmptyElementValue("modification", out value))
			{
				e.LastWriteTime = XmlConvert.ToDateTime(value, XmlDateTimeSerializationMode.Local);
			}
			else if (SessionRemoteException.IsResult(elementLogReader))
			{
				e.Error = SessionRemoteException.ReadResult(elementLogReader);
			}
		}
		return e;
	}
}
