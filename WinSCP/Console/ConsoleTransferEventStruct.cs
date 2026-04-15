using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Represents the payload of a console transfer event, used to shuttle raw binary
/// data between the WinSCP executable and the .NET wrapper through the
/// shared-memory channel during stream-based file-transfer operations.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal class ConsoleTransferEventStruct
{
	/// <summary>
	/// The raw binary data buffer. The array is fixed at 20 480 bytes; only
	/// the first <see cref="Len"/> bytes contain meaningful content.
	/// </summary>
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 20480)]
	public byte[] Data;

	/// <summary>
	/// The number of valid bytes in <see cref="Data"/> for this transfer chunk.
	/// A value of zero on a <see cref="ConsoleEvent.TransferOut"/> event signals
	/// end-of-stream.
	/// </summary>
	public uint Len;

	/// <summary>
	/// When <see langword="true"/>, an I/O error occurred during the transfer
	/// and the operation should be treated as failed.
	/// </summary>
	[MarshalAs(UnmanagedType.I1)]
	public bool Error;
}
