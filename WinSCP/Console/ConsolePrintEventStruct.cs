using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal class ConsolePrintEventStruct
{
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10240)]
	public string Message;

	[MarshalAs(UnmanagedType.I1)]
	public bool FromBeginning;

	[MarshalAs(UnmanagedType.I1)]
	public bool Error;
}
