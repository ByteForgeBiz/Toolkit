using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Represents the base class for operation results.
/// </summary>
[Guid("B4CC583A-B64E-4797-9967-0FCB2F07C977")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public class OperationResultBase
{
	/// <summary>
	/// Gets the collection of failures that occurred during the operation.
	/// </summary>
	public SessionRemoteExceptionCollection Failures { get; private set; }

	/// <summary>
	/// Gets a value indicating whether the operation was successful (no failures).
	/// </summary>
	public bool IsSuccess => Failures.Count == 0;

	/// <summary>
	/// Initializes a new instance of the <see cref="OperationResultBase"/> class.
	/// </summary>
	internal OperationResultBase()
	{
		Failures = new SessionRemoteExceptionCollection();
	}

	/// <summary>
	/// Checks if the operation was successful and throws the first failure if it was not.
	/// </summary>
	/// <exception cref="SessionRemoteException">Thrown if the operation failed.</exception>
	public void Check()
	{
		if (!IsSuccess)
		{
			throw Failures[0];
		}
	}

	/// <summary>
	/// Adds a failure to the collection of failures.
	/// </summary>
	/// <param name="failure">The failure to add.</param>
	internal void AddFailure(SessionRemoteException failure)
	{
		Failures.InternalAdd(failure);
	}
}
