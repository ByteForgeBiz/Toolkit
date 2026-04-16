using System;

namespace ByteForge.WinSCP;

/// <summary>
/// Specifies the page protection flags for a file mapping object.
/// </summary>
[Flags]
internal enum FileMapProtection : uint
{
	/// <summary>
	/// Allows read-only views to be mapped for the file mapping object.
	/// </summary>
	PageReadonly = 2u,
	/// <summary>
	/// Allows read and write views to be mapped for the file mapping object.
	/// </summary>
	PageReadWrite = 4u,
	/// <summary>
	/// Allows copy-on-write views to be mapped for the file mapping object.
	/// </summary>
	PageWriteCopy = 8u,
	/// <summary>
	/// Allows read and execute views to be mapped for the file mapping object.
	/// </summary>
	PageExecuteRead = 0x20u,
	/// <summary>
	/// Allows read, write, and execute views to be mapped for the file mapping object.
	/// </summary>
	PageExecuteReadWrite = 0x40u,
	/// <summary>
	/// Causes all modified pages in the file mapping to be written to disk when the view is unmapped.
	/// </summary>
	SectionCommit = 0x8000000u,
	/// <summary>
	/// Specifies that the file mapping object is mapped as an executable image.
	/// </summary>
	SectionImage = 0x1000000u,
	/// <summary>
	/// Specifies that the file mapping object is not cached.
	/// </summary>
	SectionNoCache = 0x10000000u,
	/// <summary>
	/// Reserves address space in the virtual address space of the calling process without allocating physical storage.
	/// </summary>
	SectionReserve = 0x4000000u
}
