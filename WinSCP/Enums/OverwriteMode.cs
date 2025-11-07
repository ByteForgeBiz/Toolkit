using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Specifies the overwrite mode for file transfers.
/// </summary>
[Guid("E0F3C3C2-C812-48F1-A711-E0BD0F703976")]
[ComVisible(true)]
public enum OverwriteMode
{
	/// <summary>
	/// Overwrite the target file if it exists.
	/// </summary>
	Overwrite,
	/// <summary>
	/// Resume transfer if the target file already exists.
	/// </summary>
	Resume,
	/// <summary>
	/// Append to the target file if it exists.
	/// </summary>
	Append
}
