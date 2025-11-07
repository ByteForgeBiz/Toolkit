using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Specifies the protocol to use for remote file transfer operations.
/// </summary>
[Guid("F25C49A5-74A6-4E8F-AEB4-5B4E0DDF0EF9")]
[ComVisible(true)]
public enum Protocol
{
	/// <summary>
	/// SSH File Transfer Protocol (SFTP).
	/// </summary>
	Sftp,
	/// <summary>
	/// Secure Copy Protocol (SCP).
	/// </summary>
	Scp,
	/// <summary>
	/// File Transfer Protocol (FTP).
	/// </summary>
	Ftp,
	/// <summary>
	/// WebDAV protocol.
	/// </summary>
	Webdav,
	/// <summary>
	/// Amazon S3 protocol.
	/// </summary>
	S3
}
