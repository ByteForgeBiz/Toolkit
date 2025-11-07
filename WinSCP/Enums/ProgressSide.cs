using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Specifies the side (local or remote) for progress reporting.
/// </summary>
[Guid("16B6D8F6-C0B4-487D-9546-A25BBF582ED6")]
[ComVisible(true)]
public enum ProgressSide
{
	/// <summary>
	/// Indicates the local side.
	/// </summary>
	Local,

	/// <summary>
	/// Indicates the remote side.
	/// </summary>
	Remote
}
