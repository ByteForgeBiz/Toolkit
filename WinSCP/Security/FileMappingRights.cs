using System;

namespace ByteForge.WinSCP;

/// <summary>
/// Specifies access rights for file mapping section objects.
/// </summary>
[Flags]
internal enum FileMappingRights
{
	/// <summary>
	/// The right to copy data from a file-mapping object into a process's address space.
	/// </summary>
	FileMapCopy = 1,
	/// <summary>
	/// The right to map a view of a file-mapping object as writable.
	/// </summary>
	FileMapWrite = 2,
	/// <summary>
	/// The right to map a view of a file-mapping object as readable.
	/// </summary>
	FileMapRead = 4,
	/// <summary>
	/// The right to map views of the section as executable.
	/// </summary>
	SectionMapExecute = 8,
	/// <summary>
	/// The right to extend the size of a file-mapping section.
	/// </summary>
	SectionExtendSize = 0x10,
	/// <summary>
	/// The right to map a view of a file-mapping object as executable.
	/// </summary>
	FileMapExecute = 0x20,
	/// <summary>
	/// All possible access rights for a file-mapping object.
	/// </summary>
	AllAccess = 0xF001F
}
