using System;

namespace ByteForge.WinSCP;

[Flags]
internal enum WindowStationRights
{
	EnumDesktops = 1,
	ReadAttributes = 2,
	AccessClipboard = 4,
	CreateDesktop = 8,
	WriteAttributes = 0x10,
	AccessGlobalAtoms = 0x20,
	ExitWindows = 0x40,
	Enumerate = 0x100,
	ReadScreen = 0x200,
	AllAccess = 0xF037F
}
