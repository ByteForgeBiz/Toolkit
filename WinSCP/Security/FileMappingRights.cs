using System;

namespace ByteForge.WinSCP;

[Flags]
internal enum FileMappingRights
{
	FileMapCopy = 1,
	FileMapWrite = 2,
	FileMapRead = 4,
	SectionMapExecute = 8,
	SectionExtendSize = 0x10,
	FileMapExecute = 0x20,
	AllAccess = 0xF001F
}
