using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Provides data for file chmod (permission modification) events.
/// </summary>
[Guid("DFCA88E2-6A47-4290-AD66-A39C5682D610")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class ChmodEventArgs : FileOperationEventArgs
{
	/// <summary>
	/// The XML tag name for chmod operations.
	/// </summary>
	internal const string Tag = "chmod";

	/// <summary>
	/// Gets the file permissions set for the file.
	/// </summary>
	public FilePermissions FilePermissions { get; private set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ChmodEventArgs"/> class.
	/// </summary>
	private ChmodEventArgs()
	{
	}

	/// <summary>
	/// Reads chmod event arguments from the log reader.
	/// </summary>
	/// <param name="areader">The log reader to read from.</param>
	/// <returns>A new <see cref="ChmodEventArgs"/> instance.</returns>
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
