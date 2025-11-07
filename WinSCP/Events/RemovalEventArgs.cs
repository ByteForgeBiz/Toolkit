using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Provides data for file removal events.
/// </summary>
[Guid("62FB0733-C24F-4DC2-8452-560148931927")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class RemovalEventArgs : FileOperationEventArgs
{
	/// <summary>
	/// The XML tag name for removal operations.
	/// </summary>
	internal const string Tag = "rm";

	/// <summary>
	/// Initializes a new instance of the <see cref="RemovalEventArgs"/> class.
	/// </summary>
	private RemovalEventArgs()
	{
	}

	/// <summary>
	/// Reads removal event arguments from the log reader.
	/// </summary>
	/// <param name="areader">The log reader to read from.</param>
	/// <returns>A new <see cref="RemovalEventArgs"/> instance.</returns>
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
