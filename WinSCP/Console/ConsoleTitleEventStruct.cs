using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal class ConsoleTitleEventStruct
{
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10240)]
	public string Title;
}
