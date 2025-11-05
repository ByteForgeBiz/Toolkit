using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[StructLayout(LayoutKind.Sequential)]
internal class ConsoleCommHeader
{
	public uint Size;

	public int Version;

	public ConsoleEvent Event;
}
