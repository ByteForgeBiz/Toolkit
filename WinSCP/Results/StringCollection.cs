using System.Collections;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Represents a read-only collection of string objects.
/// </summary>
[Guid("E402CB1F-6219-4C79-9EDF-1914D9589909")]
[ClassInterface(ClassInterfaceType.None)]
[ComVisible(true)]
[ComDefaultInterface(typeof(IEnumerable))]
public class StringCollection : ReadOnlyInteropCollection<string>
{
}
