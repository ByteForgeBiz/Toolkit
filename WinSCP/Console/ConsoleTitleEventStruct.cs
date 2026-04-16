using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Represents the payload of a console title event, carrying the text that the
/// WinSCP executable wants set as the console window title.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal class ConsoleTitleEventStruct
{
	/// <summary>
	/// The new console window title. Holds up to 10 240 Unicode characters,
	/// including the null terminator.
	/// </summary>
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10240)]
	public string Title;
}
