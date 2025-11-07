using System;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Specifies options for remote file enumeration operations.
/// </summary>
[Guid("6C441F60-26AA-44FC-9B93-08884768507B")]
[ComVisible(true)]
[Flags]
public enum EnumerationOptions
{
	/// <summary>
	/// No enumeration options.
	/// </summary>
	None = 0,
	/// <summary>
	/// Recursively enumerate all subdirectories.
	/// </summary>
	AllDirectories = 1,
	/// <summary>
	/// Include directories in the match results.
	/// </summary>
	MatchDirectories = 2,
	/// <summary>
	/// Enumerate directories separately from files.
	/// </summary>
	EnumerateDirectories = 4
}
