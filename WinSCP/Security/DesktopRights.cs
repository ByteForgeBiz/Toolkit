using System;

namespace ByteForge.WinSCP;

[Flags]
internal enum DesktopRights
{
	ReadObjects = 1,
	CreateWindow = 2,
	CreateMenu = 4,
	HookControl = 8,
	JournalRecord = 0x10,
	JournalPlayback = 0x20,
	Enumerate = 0x40,
	WriteObjects = 0x80,
	SwitchDesktop = 0x100,
	AllAccess = 0xF01FF
}
