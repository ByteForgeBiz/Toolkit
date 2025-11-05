using System;

namespace ByteForge.WinSCP;

[Flags]
internal enum FileMapAccess
{
	FileMapCopy = 1,
	FileMapWrite = 2,
	FileMapRead = 4,
	FileMapAllAccess = 0x1F,
	FileMapExecute = 0x20
}
