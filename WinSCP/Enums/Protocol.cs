using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[Guid("F25C49A5-74A6-4E8F-AEB4-5B4E0DDF0EF9")]
[ComVisible(true)]
public enum Protocol
{
	Sftp,
	Scp,
	Ftp,
	Webdav,
	S3
}
