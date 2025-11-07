using System.Collections;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Represents a read-only collection of <see cref="RemovalEventArgs"/> objects.
/// </summary>
[Guid("574FF430-FD40-41F9-9A04-971D3CF844B7")]
[ClassInterface(ClassInterfaceType.None)]
[ComVisible(true)]
[ComDefaultInterface(typeof(IEnumerable))]
public class RemovalEventArgsCollection : ReadOnlyInteropCollection<RemovalEventArgs>
{
}
