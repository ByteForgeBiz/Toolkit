using System;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[Guid("3F770EC1-35F5-4A7B-A000-46A2F7A213D8")]
[ComVisible(true)]
[Flags]
public enum SynchronizationCriteria
{
	None = 0,
	Time = 1,
	Size = 2,
	Checksum = 4,
	[Obsolete("Use Time | Size")]
	Either = 3
}
