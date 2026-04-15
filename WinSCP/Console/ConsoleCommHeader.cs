using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Represents the header portion of the shared-memory communication structure used
/// to exchange console events between the WinSCP executable and the .NET wrapper.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal class ConsoleCommHeader
{
	/// <summary>
	/// The total size, in bytes, of the communication structure (header + payload).
	/// </summary>
	public uint Size;

	/// <summary>
	/// The version number of the console communication protocol.
	/// </summary>
	public int Version;

	/// <summary>
	/// The type of console event currently stored in the communication structure.
	/// </summary>
	public ConsoleEvent Event;
}
