using System;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Specifies criteria for directory synchronization comparisons.
/// </summary>
[Guid("3F770EC1-35F5-4A7B-A000-46A2F7A213D8")]
[ComVisible(true)]
[Flags]
public enum SynchronizationCriteria
{
	/// <summary>
	/// No criteria - no comparison will be done.
	/// </summary>
	None = 0,
	/// <summary>
	/// Compare files by modification time.
	/// </summary>
	Time = 1,
	/// <summary>
	/// Compare files by size.
	/// </summary>
	Size = 2,
	/// <summary>
	/// Compare files by checksum.
	/// </summary>
	Checksum = 4,
	/// <summary>
	/// Compare files by either time or size. (Obsolete - use Time | Size instead)
	/// </summary>
	[Obsolete("Use Time | Size")]
	Either = 3
}
