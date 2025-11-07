using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Represents the result of a directory synchronization operation.
/// </summary>
[Guid("D0ADB4F7-47AE-43AC-AA41-9114650EA51A")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class SynchronizationResult : OperationResultBase
{
	/// <summary>
	/// Gets the collection of files uploaded during synchronization.
	/// </summary>
	public TransferEventArgsCollection Uploads { get; private set; }

	/// <summary>
	/// Gets the collection of files downloaded during synchronization.
	/// </summary>
	public TransferEventArgsCollection Downloads { get; private set; }

	/// <summary>
	/// Gets the collection of files removed during synchronization.
	/// </summary>
	public RemovalEventArgsCollection Removals { get; private set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="SynchronizationResult"/> class.
	/// </summary>
	internal SynchronizationResult()
	{
		Uploads = new TransferEventArgsCollection();
		Downloads = new TransferEventArgsCollection();
		Removals = new RemovalEventArgsCollection();
	}

	/// <summary>
	/// Adds an upload transfer to the synchronization result.
	/// </summary>
	/// <param name="upload">The upload transfer to add.</param>
	internal void AddUpload(TransferEventArgs upload)
	{
		Uploads.InternalAdd(upload);
	}

	/// <summary>
	/// Adds a download transfer to the synchronization result.
	/// </summary>
	/// <param name="download">The download transfer to add.</param>
	internal void AddDownload(TransferEventArgs download)
	{
		Downloads.InternalAdd(download);
	}

	/// <summary>
	/// Adds a removal event to the synchronization result.
	/// </summary>
	/// <param name="removal">The removal event to add.</param>
	internal void AddRemoval(RemovalEventArgs removal)
	{
		Removals.InternalAdd(removal);
	}
}
