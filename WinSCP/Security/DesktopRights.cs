using System;

namespace ByteForge.WinSCP;

/// <summary>
/// Specifies access rights for a desktop object.
/// </summary>
[Flags]
internal enum DesktopRights
{
	/// <summary>
	/// The right to read objects on the desktop.
	/// </summary>
	ReadObjects = 1,
	/// <summary>
	/// The right to create a window on the desktop.
	/// </summary>
	CreateWindow = 2,
	/// <summary>
	/// The right to create a menu on the desktop.
	/// </summary>
	CreateMenu = 4,
	/// <summary>
	/// The right to establish any hook.
	/// </summary>
	HookControl = 8,
	/// <summary>
	/// The right to record a journal of all message and command ordering on the desktop.
	/// </summary>
	JournalRecord = 0x10,
	/// <summary>
	/// The right to play back a journal of message and commands on the desktop.
	/// </summary>
	JournalPlayback = 0x20,
	/// <summary>
	/// The right to enumerate the desktop.
	/// </summary>
	Enumerate = 0x40,
	/// <summary>
	/// The right to write objects on the desktop.
	/// </summary>
	WriteObjects = 0x80,
	/// <summary>
	/// The right to activate and switch to the desktop.
	/// </summary>
	SwitchDesktop = 0x100,
	/// <summary>
	/// All possible access rights for the desktop.
	/// </summary>
	AllAccess = 0xF01FF
}
