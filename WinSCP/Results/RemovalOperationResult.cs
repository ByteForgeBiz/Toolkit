using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Represents the result of a file removal operation.
/// </summary>
[Guid("3BCB18EC-6D98-4BFB-A9C2-893CBD13CDAB")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class RemovalOperationResult : OperationResultBase
{
	/// <summary>
	/// Gets the collection of removal event arguments.
	/// </summary>
	public RemovalEventArgsCollection Removals { get; private set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="RemovalOperationResult"/> class.
	/// </summary>
	internal RemovalOperationResult()
	{
		Removals = new RemovalEventArgsCollection();
	}

	/// <summary>
	/// Adds a removal event argument to the collection.
	/// </summary>
	/// <param name="operation">The removal event argument to add.</param>
	internal void AddRemoval(RemovalEventArgs operation)
	{
		Removals.InternalAdd(operation);
	}
}
