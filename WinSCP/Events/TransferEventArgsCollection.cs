using System.Collections;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[Guid("0285917B-581A-4F6F-9A9D-1C34ABFB4E38")]
[ClassInterface(ClassInterfaceType.None)]
[ComVisible(true)]
[ComDefaultInterface(typeof(IEnumerable))]
public class TransferEventArgsCollection : ReadOnlyInteropCollection<TransferEventArgs>
{
}
