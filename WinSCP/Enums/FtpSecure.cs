using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[Guid("F2FC81EB-4761-4A4E-A3EC-4AFDD474C18C")]
[ComVisible(true)]
public enum FtpSecure
{
	None = 0,
	Implicit = 1,
	Explicit = 3
}
