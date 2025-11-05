using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[StructLayout(LayoutKind.Sequential)]
internal class ConsoleInitEventStruct
{
	public enum StdInOut
	{
		Off,
		Binary,
		Chunked
	}

	public uint InputType;

	public uint OutputType;

	[MarshalAs(UnmanagedType.I1)]
	public bool WantsProgress;

	[MarshalAs(UnmanagedType.I1)]
	public bool UseStdErr;

	public StdInOut BinaryOutput;

	public StdInOut BinaryInput;
}
