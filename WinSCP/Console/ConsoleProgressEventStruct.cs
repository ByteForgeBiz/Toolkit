using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal class ConsoleProgressEventStruct
{
	public enum ProgressOperation
	{
		Copy
	}

	public enum ProgressSide
	{
		Local,
		Remote
	}

	public ProgressOperation Operation;

	public ProgressSide Side;

	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
	public string FileName;

	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
	public string Directory;

	public uint OverallProgress;

	public uint FileProgress;

	public uint CPS;

	[MarshalAs(UnmanagedType.I1)]
	public bool Cancel;
}
