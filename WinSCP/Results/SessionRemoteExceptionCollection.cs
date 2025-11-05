using System.Collections;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[Guid("2309282F-B89B-4F6B-AEB1-D3E1629B7033")]
[ClassInterface(ClassInterfaceType.None)]
[ComVisible(true)]
[ComDefaultInterface(typeof(IEnumerable))]
public class SessionRemoteExceptionCollection : ReadOnlyInteropCollection<SessionRemoteException>
{
}
