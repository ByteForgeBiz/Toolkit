using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal class ConsoleTransferEventStruct
{
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 20480)]
	public byte[] Data;

	public uint Len;

	[MarshalAs(UnmanagedType.I1)]
	public bool Error;
}
