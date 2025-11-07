using System.Collections;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Represents a read-only collection of <see cref="RemoteFileInfo"/> objects.
/// </summary>
[Guid("39AA3D00-578C-49AF-B3E4-16CE26C710C6")]
[ClassInterface(ClassInterfaceType.None)]
[ComVisible(true)]
[ComDefaultInterface(typeof(IEnumerable))]
public class RemoteFileInfoCollection : ReadOnlyInteropCollection<RemoteFileInfo>
{
}
