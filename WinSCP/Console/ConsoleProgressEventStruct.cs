using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Represents the structure for console progress events during file operations.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal class ConsoleProgressEventStruct
{
	/// <summary>
	/// Specifies the type of progress operation.
	/// </summary>
	public enum ProgressOperation
	{
		/// <summary>
		/// Copy operation.
		/// </summary>
		Copy
	}

	/// <summary>
	/// Specifies the side (local or remote) for progress reporting.
	/// </summary>
	public enum ProgressSide
	{
		/// <summary>
		/// Local side.
		/// </summary>
		Local,
		/// <summary>
		/// Remote side.
		/// </summary>
		Remote
	}

	/// <summary>
	/// Gets or sets the type of progress operation.
	/// </summary>
	public ProgressOperation Operation;

	/// <summary>
	/// Gets or sets the side for progress reporting.
	/// </summary>
	public ProgressSide Side;

	/// <summary>
	/// Gets or sets the file name involved in the operation.
	/// </summary>
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
	public string FileName;

	/// <summary>
	/// Gets or sets the directory involved in the operation.
	/// </summary>
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
	public string Directory;

	/// <summary>
	/// Gets or sets the overall progress value.
	/// </summary>
	public uint OverallProgress;

	/// <summary>
	/// Gets or sets the file progress value.
	/// </summary>
	public uint FileProgress;

	/// <summary>
	/// Gets or sets the current bytes per second (CPS).
	/// </summary>
	public uint CPS;

	/// <summary>
	/// Gets or sets a value indicating whether the operation should be canceled.
	/// </summary>
	[MarshalAs(UnmanagedType.I1)]
	public bool Cancel;
}
