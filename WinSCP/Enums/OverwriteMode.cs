using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[Guid("E0F3C3C2-C812-48F1-A711-E0BD0F703976")]
[ComVisible(true)]
public enum OverwriteMode
{
	Overwrite,
	Resume,
	Append
}
