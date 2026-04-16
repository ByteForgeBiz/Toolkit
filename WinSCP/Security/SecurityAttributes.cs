using System;

namespace ByteForge.WinSCP;

/// <summary>
/// Represents the Win32 SECURITY_ATTRIBUTES structure, which contains the security descriptor for an object and specifies whether the handle retrieved by specifying this structure is inheritable.
/// </summary>
internal struct SecurityAttributes
{
	/// <summary>
	/// The size, in bytes, of this structure.
	/// </summary>
	public uint nLength;

	/// <summary>
	/// A pointer to a SECURITY_DESCRIPTOR structure that controls access to the object. If this value is <see cref="IntPtr.Zero"/>, the object is assigned the default security descriptor of the calling process.
	/// </summary>
	public IntPtr lpSecurityDescriptor;

	/// <summary>
	/// A value that specifies whether the returned handle is inherited when a new process is created. A non-zero value indicates the handle can be inherited.
	/// </summary>
	public int bInheritHandle;
}
