using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Specifies the security mode for FTP connections.
/// </summary>
[Guid("F2FC81EB-4761-4A4E-A3EC-4AFDD474C18C")]
[ComVisible(true)]
public enum FtpSecure
{
	/// <summary>
	/// No security - plain FTP.
	/// </summary>
	None = 0,
	/// <summary>
	/// Implicit SSL/TLS - secure connection from the start.
	/// </summary>
	Implicit = 1,
	/// <summary>
	/// Explicit SSL/TLS - security negotiated after initial connection.
	/// </summary>
	Explicit = 3
}
