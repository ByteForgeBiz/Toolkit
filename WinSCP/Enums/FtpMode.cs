using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Specifies the FTP mode for file transfer operations.
/// </summary>
[Guid("D924FAB9-FCE7-47B8-9F23-5717698384D3")]
[ComVisible(true)]
public enum FtpMode
{
	/// <summary>
	/// Passive mode - the server initiates the data connection.
	/// </summary>
	Passive,
	/// <summary>
	/// Active mode - the client initiates the data connection.
	/// </summary>
	Active
}
