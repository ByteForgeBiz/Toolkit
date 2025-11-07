using System;

namespace ByteForge.WinSCP;

/// <summary>
/// Specifies standard access rights for security objects.
/// </summary>
[Flags]
internal enum StandardRights
{
	/// <summary>
	/// The right to delete the object.
	/// </summary>
	Delete = 0x10000,
	/// <summary>
	/// The right to read permissions.
	/// </summary>
	ReadPermissions = 0x20000,
	/// <summary>
	/// The right to write permissions.
	/// </summary>
	WritePermissions = 0x40000,
	/// <summary>
	/// The right to take ownership.
	/// </summary>
	TakeOwnership = 0x80000,
	/// <summary>
	/// The right to synchronize access.
	/// </summary>
	Synchronize = 0x100000,
	/// <summary>
	/// The required rights for the object.
	/// </summary>
	Required = 0xF0000
}
