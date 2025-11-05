using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[Guid("B1DAE3A0-5E56-4001-88D8-786F68557E28")]
[ComVisible(true)]
public enum SynchronizationAction
{
	UploadNew = 1,
	DownloadNew,
	UploadUpdate,
	DownloadUpdate,
	DeleteRemote,
	DeleteLocal
}
