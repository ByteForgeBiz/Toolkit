using System;

namespace ByteForge.WinSCP;

[Flags]
internal enum StandardRights
{
	Delete = 0x10000,
	ReadPermissions = 0x20000,
	WritePermissions = 0x40000,
	TakeOwnership = 0x80000,
	Synchronize = 0x100000,
	Required = 0xF0000
}
