using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Provides base data for file operation-related events.
/// </summary>
[Guid("FF8D5772-2653-4C9B-870E-4C5EF8F55673")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public class FileOperationEventArgs : OperationEventArgs
{
	/// <summary>
	/// Gets the name of the file involved in the operation.
	/// </summary>
	public string FileName { get; internal set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="FileOperationEventArgs"/> class.
	/// </summary>
	internal FileOperationEventArgs()
	{
	}
}
