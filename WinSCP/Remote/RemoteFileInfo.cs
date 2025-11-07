using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Provides information about a remote file.
/// </summary>
[Guid("17FF9C92-B8B6-4506-A7BA-8482D9B0AB07")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class RemoteFileInfo
{
	/// <summary>
	/// Gets or sets the name of the file.
	/// </summary>
	public string Name { get; internal set; }

	/// <summary>
	/// Gets or sets the full path of the file.
	/// </summary>
	public string FullName { get; internal set; }

	/// <summary>
	/// Gets or sets the file type character ('d' for directory, '-' for file, etc.).
	/// </summary>
	public char FileType { get; internal set; }

	/// <summary>
	/// Gets or sets the file size in bytes.
	/// </summary>
	public long Length { get; internal set; }

	/// <summary>
	/// Gets or sets the file size in bytes as a 32-bit integer.
	/// </summary>
	public int Length32
	{
		get
		{
			return GetLength32();
		}
		set
		{
			SetLength32(value);
		}
	}

	/// <summary>
	/// Gets or sets the last write time (modification time) of the file.
	/// </summary>
	public DateTime LastWriteTime { get; internal set; }

	/// <summary>
	/// Gets or sets the file permissions.
	/// </summary>
	public FilePermissions FilePermissions { get; internal set; }

	/// <summary>
	/// Gets or sets the owner of the file.
	/// </summary>
	public string Owner { get; internal set; }

	/// <summary>
	/// Gets or sets the group of the file.
	/// </summary>
	public string Group { get; internal set; }

	/// <summary>
	/// Gets a value indicating whether the file is a directory.
	/// </summary>
	public bool IsDirectory => IsDirectoryFileType(FileType);

	/// <summary>
	/// Gets a value indicating whether the file is the current directory (.).
	/// </summary>
	public bool IsThisDirectory
	{
		get
		{
			if (IsDirectory)
			{
				return Name == ".";
			}
			return false;
		}
	}

	/// <summary>
	/// Gets a value indicating whether the file is the parent directory (..).
	/// </summary>
	public bool IsParentDirectory
	{
		get
		{
			if (IsDirectory)
			{
				return Name == "..";
			}
			return false;
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RemoteFileInfo"/> class.
	/// </summary>
	internal RemoteFileInfo()
	{
	}

	/// <summary>
	/// Returns the file name.
	/// </summary>
	/// <returns>The name of the file.</returns>
	public override string ToString()
	{
		return Name;
	}

	private int GetLength32()
	{
		return Tools.LengthTo32Bit(Length);
	}

	private void SetLength32(int value)
	{
		Length = value;
	}

	/// <summary>
	/// Determines whether the specified file type character represents a directory.
	/// </summary>
	/// <param name="fileType">The file type character.</param>
	/// <returns>True if the file type represents a directory; otherwise, false.</returns>
	internal static bool IsDirectoryFileType(char fileType)
	{
		return char.ToUpper(fileType, CultureInfo.InvariantCulture) == 'D';
	}
}
