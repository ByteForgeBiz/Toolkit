using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Represents the payload of a console print event, carrying a Unicode message
/// that the WinSCP executable wants written to the output stream.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal class ConsolePrintEventStruct
{
	/// <summary>
	/// The text to be printed. Holds up to 10 240 Unicode characters, including the null terminator.
	/// </summary>
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10240)]
	public string Message;

	/// <summary>
	/// When <see langword="true"/>, the message should overwrite the current output line
	/// from its beginning rather than being appended after existing content.
	/// </summary>
	[MarshalAs(UnmanagedType.I1)]
	public bool FromBeginning;

	/// <summary>
	/// When <see langword="true"/>, the message represents an error or warning
	/// and should be treated as error output.
	/// </summary>
	[MarshalAs(UnmanagedType.I1)]
	public bool Error;
}
