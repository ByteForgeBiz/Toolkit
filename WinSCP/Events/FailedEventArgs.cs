using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[Guid("2C29B0BD-4F77-4743-A72A-B91F6D0EAD16")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class FailedEventArgs : OperationEventArgs
{
	internal FailedEventArgs()
	{
	}

	public override string ToString()
	{
		if (base.Error == null)
		{
			return "<no error>";
		}
		return base.Error.ToString();
	}
}
