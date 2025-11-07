using System;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Provides data for operation-related events, including any associated error.
/// </summary>
[Guid("4D79C4F7-0FE2-428D-9908-AB2D38E96C53")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public class OperationEventArgs : EventArgs
{
	/// <summary>
	/// Gets the error associated with the operation, if any.
	/// </summary>
	public SessionRemoteException Error { get; internal set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="OperationEventArgs"/> class.
	/// </summary>
	internal OperationEventArgs()
	{
	}
}
