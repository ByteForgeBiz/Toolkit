using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal class ConsoleChoiceEventStruct
{
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
	public string Options;

	public int Cancel;

	public int Break;

	public int Result;

	public int Timeouted;

	public uint Timer;

	[MarshalAs(UnmanagedType.I1)]
	public bool Timeouting;

	public int Continue;

	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 5120)]
	public string Message;
}
