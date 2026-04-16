using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Represents the payload of a console initialization event, used to negotiate
/// standard-input/output modes and optional features between the WinSCP executable
/// and the .NET wrapper at session startup.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal class ConsoleInitEventStruct
{
	/// <summary>
	/// Describes the binary-transfer mode for a standard I/O stream.
	/// </summary>
	public enum StdInOut
	{
		/// <summary>
		/// Binary transfer is disabled; the stream operates in text mode.
		/// </summary>
		Off,

		/// <summary>
		/// Binary transfer is enabled; the stream operates in raw binary mode.
		/// </summary>
		Binary,

		/// <summary>
		/// Binary transfer is enabled using chunked framing.
		/// </summary>
		Chunked
	}

	/// <summary>
	/// A bitmask indicating which standard-input modes the WinSCP executable supports.
	/// </summary>
	public uint InputType;

	/// <summary>
	/// A bitmask indicating which standard-output modes the WinSCP executable supports.
	/// </summary>
	public uint OutputType;

	/// <summary>
	/// Indicates whether the .NET wrapper wants to receive file-transfer progress events.
	/// </summary>
	[MarshalAs(UnmanagedType.I1)]
	public bool WantsProgress;

	/// <summary>
	/// Indicates whether the WinSCP executable should direct error output to <c>stderr</c>
	/// rather than mixing it with regular output on <c>stdout</c>.
	/// </summary>
	[MarshalAs(UnmanagedType.I1)]
	public bool UseStdErr;

	/// <summary>
	/// The binary-transfer mode negotiated for the standard-output stream.
	/// </summary>
	public StdInOut BinaryOutput;

	/// <summary>
	/// The binary-transfer mode negotiated for the standard-input stream.
	/// </summary>
	public StdInOut BinaryInput;
}
