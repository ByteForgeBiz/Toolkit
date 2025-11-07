using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Specifies the mode for directory synchronization operations.
/// </summary>
[Guid("38649D44-B839-4F2C-A9DC-5D45EEA4B5E9")]
[ComVisible(true)]
public enum SynchronizationMode
{
 /// <summary>
 /// Synchronize from remote to local.
 /// </summary>
 Local,
 /// <summary>
 /// Synchronize from local to remote.
 /// </summary>
 Remote,
 /// <summary>
 /// Synchronize both directions.
 /// </summary>
 Both
}
