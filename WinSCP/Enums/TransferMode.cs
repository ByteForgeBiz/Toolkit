using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Specifies the transfer mode for file transfers.
/// </summary>
[Guid("6B19CBFA-0D81-4B36-A587-E11AA6A06214")]
[ComVisible(true)]
public enum TransferMode
{
	/// <summary>
	/// Binary mode - transfer files without modification.
	/// </summary>
	Binary,
	/// <summary>
	/// ASCII mode - convert line endings for text files.
	/// </summary>
	Ascii,
	/// <summary>
	/// Automatic mode - automatically select the appropriate transfer mode.
	/// </summary>
	Automatic
}
