using System;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[Guid("6C441F60-26AA-44FC-9B93-08884768507B")]
[ComVisible(true)]
[Flags]
public enum EnumerationOptions
{
	None = 0,
	AllDirectories = 1,
	MatchDirectories = 2,
	EnumerateDirectories = 4
}
