using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal class ConsoleInputEventStruct
{
	[MarshalAs(UnmanagedType.I1)]
	public bool Echo;

	[MarshalAs(UnmanagedType.I1)]
	public bool Result;

	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10240)]
	public string Str;

	public uint Timer;
}
