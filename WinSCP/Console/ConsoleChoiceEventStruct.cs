using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Represents the structure for console choice events.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal class ConsoleChoiceEventStruct
{
 /// <summary>
 /// Gets or sets the available options for the choice event.
 /// </summary>
 [MarshalAs(UnmanagedType.ByValTStr, SizeConst =64)]
 public string Options;

 /// <summary>
 /// Gets or sets the cancel value for the event.
 /// </summary>
 public int Cancel;

 /// <summary>
 /// Gets or sets the break value for the event.
 /// </summary>
 public int Break;

 /// <summary>
 /// Gets or sets the result value for the event.
 /// </summary>
 public int Result;

 /// <summary>
 /// Gets or sets the timeouted value for the event.
 /// </summary>
 public int Timeouted;

 /// <summary>
 /// Gets or sets the timer value for the event.
 /// </summary>
 public uint Timer;

 /// <summary>
 /// Gets or sets a value indicating whether the event is timing out.
 /// </summary>
 [MarshalAs(UnmanagedType.I1)]
 public bool Timeouting;

 /// <summary>
 /// Gets or sets the continue value for the event.
 /// </summary>
 public int Continue;

 /// <summary>
 /// Gets or sets the message associated with the choice event.
 /// </summary>
 [MarshalAs(UnmanagedType.ByValTStr, SizeConst =5120)]
 public string Message;
}
