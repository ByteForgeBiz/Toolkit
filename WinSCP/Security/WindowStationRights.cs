using System;

namespace ByteForge.WinSCP;

/// <summary>
/// Specifies access rights for a window station.
/// </summary>
[Flags]
internal enum WindowStationRights
{
 /// <summary>
 /// The right to enumerate desktops.
 /// </summary>
 EnumDesktops =1,
 /// <summary>
 /// The right to read attributes.
 /// </summary>
 ReadAttributes =2,
 /// <summary>
 /// The right to access the clipboard.
 /// </summary>
 AccessClipboard =4,
 /// <summary>
 /// The right to create a desktop.
 /// </summary>
 CreateDesktop =8,
 /// <summary>
 /// The right to write attributes.
 /// </summary>
 WriteAttributes =0x10,
 /// <summary>
 /// The right to access global atoms.
 /// </summary>
 AccessGlobalAtoms =0x20,
 /// <summary>
 /// The right to exit Windows.
 /// </summary>
 ExitWindows =0x40,
 /// <summary>
 /// The right to enumerate window stations.
 /// </summary>
 Enumerate =0x100,
 /// <summary>
 /// The right to read the screen.
 /// </summary>
 ReadScreen =0x200,
 /// <summary>
 /// All possible access rights.
 /// </summary>
 AllAccess =0xF037F
}
