using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Specifies the action to take on a file during synchronization.
/// </summary>
[Guid("B1DAE3A0-5E56-4001-88D8-786F68557E28")]
[ComVisible(true)]
public enum SynchronizationAction
{
	/// <summary>
	/// Upload a new file to the remote location.
	/// </summary>
	UploadNew = 1,
	/// <summary>
	/// Download a new file from the remote location.
	/// </summary>
	DownloadNew,
	/// <summary>
	/// Upload an updated file to the remote location.
	/// </summary>
	UploadUpdate,
	/// <summary>
	/// Download an updated file from the remote location.
	/// </summary>
	DownloadUpdate,
	/// <summary>
	/// Delete a file from the remote location.
	/// </summary>
	DeleteRemote,
	/// <summary>
	/// Delete a file from the local location.
	/// </summary>
	DeleteLocal
}
