using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[Guid("6B19CBFA-0D81-4B36-A587-E11AA6A06214")]
[ComVisible(true)]
public enum TransferMode
{
	Binary,
	Ascii,
	Automatic
}
