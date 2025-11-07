using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Provides data for the failed event.
/// </summary>
[Guid("2C29B0BD-4F77-4743-A72A-B91F6D0EAD16")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class FailedEventArgs : OperationEventArgs
{
	/// <summary>
	/// Initializes a new instance of the <see cref="FailedEventArgs"/> class.
	/// </summary>
	internal FailedEventArgs()
	{
	}

	/// <summary>
	/// Returns a string representation of the failed event arguments.
	/// </summary>
	/// <returns>The error message or "&lt;no error&gt;" if no error occurred.</returns>
	public override string ToString()
	{
		if (base.Error == null)
		{
			return "<no error>";
		}
		return base.Error.ToString();
	}
}
