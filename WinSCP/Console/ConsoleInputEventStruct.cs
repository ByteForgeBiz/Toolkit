using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Represents the structure for console input events.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal class ConsoleInputEventStruct
{
 /// <summary>
 /// Gets or sets a value indicating whether input should be echoed.
 /// </summary>
 [MarshalAs(UnmanagedType.I1)]
 public bool Echo;

 /// <summary>
 /// Gets or sets a value indicating the result of the input event.
 /// </summary>
 [MarshalAs(UnmanagedType.I1)]
 public bool Result;

 /// <summary>
 /// Gets or sets the input string for the event.
 /// </summary>
 [MarshalAs(UnmanagedType.ByValTStr, SizeConst =10240)]
 public string Str;

 /// <summary>
 /// Gets or sets the timer value for the event.
 /// </summary>
 public uint Timer;
}
