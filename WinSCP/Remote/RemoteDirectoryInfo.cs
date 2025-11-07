using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Provides information about a remote directory.
/// </summary>
[Guid("FBE2FACF-F1D5-493D-9E41-4B9B7243A676")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class RemoteDirectoryInfo
{
	/// <summary>
	/// Gets the collection of files in the directory.
	/// </summary>
	public RemoteFileInfoCollection Files { get; private set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="RemoteDirectoryInfo"/> class.
	/// </summary>
	internal RemoteDirectoryInfo()
	{
		Files = new RemoteFileInfoCollection();
	}

	/// <summary>
	/// Adds a file to the directory.
	/// </summary>
	/// <param name="file">The <see cref="RemoteFileInfo"/> to add.</param>
	internal void AddFile(RemoteFileInfo file)
	{
		Files.InternalAdd(file);
	}
}
