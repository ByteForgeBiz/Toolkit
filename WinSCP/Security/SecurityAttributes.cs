using System;

namespace ByteForge.WinSCP;

internal struct SecurityAttributes
{
	public uint nLength;

	public IntPtr lpSecurityDescriptor;

	public int bInheritHandle;
}
