using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Represents the result of a file transfer operation.
/// </summary>
[Guid("74F668E6-8EF2-4D01-84D8-DA2FE619C062")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class TransferOperationResult : OperationResultBase
{
	/// <summary>
	/// Gets the collection of transfers that occurred during the operation.
	/// </summary>
	public TransferEventArgsCollection Transfers { get; private set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="TransferOperationResult"/> class.
	/// </summary>
	internal TransferOperationResult()
	{
		Transfers = new TransferEventArgsCollection();
	}

	/// <summary>
	/// Adds a transfer to the collection of transfers.
	/// </summary>
	/// <param name="operation">The transfer to add.</param>
	internal void AddTransfer(TransferEventArgs operation)
	{
		Transfers.InternalAdd(operation);
	}
}
