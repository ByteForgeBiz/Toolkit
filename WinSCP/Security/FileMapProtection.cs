using System;

namespace ByteForge.WinSCP;

[Flags]
internal enum FileMapProtection : uint
{
	PageReadonly = 2u,
	PageReadWrite = 4u,
	PageWriteCopy = 8u,
	PageExecuteRead = 0x20u,
	PageExecuteReadWrite = 0x40u,
	SectionCommit = 0x8000000u,
	SectionImage = 0x1000000u,
	SectionNoCache = 0x10000000u,
	SectionReserve = 0x4000000u
}
