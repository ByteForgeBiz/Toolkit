using System;
using System.Runtime.InteropServices;
using System.Xml;

namespace ByteForge.WinSCP;

/// <summary>
/// Provides data for file touch (timestamp modification) events.
/// </summary>
[Guid("802FCEF7-E1D3-4205-B171-87A3724E85FA")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class TouchEventArgs : FileOperationEventArgs
{
	/// <summary>
	/// The XML tag name for touch operations.
	/// </summary>
	internal const string Tag = "touch";

	/// <summary>
	/// Gets the last write time (modification time) of the file.
	/// </summary>
	public DateTime LastWriteTime { get; private set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="TouchEventArgs"/> class.
	/// </summary>
	private TouchEventArgs()
	{
	}

	/// <summary>
	/// Reads touch event arguments from the log reader.
	/// </summary>
	/// <param name="areader">The log reader to read from.</param>
	/// <returns>A new <see cref="TouchEventArgs"/> instance.</returns>
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
