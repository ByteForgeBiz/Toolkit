using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Contains constants used throughout the WinSCP library.
/// </summary>
internal static class Constants
{
	/// <summary>
	/// The default COM class interface type for classes.
	/// </summary>
	public const ClassInterfaceType ClassInterface = ClassInterfaceType.AutoDispatch;

	/// <summary>
	/// The COM class interface type for collection classes.
	/// </summary>
	public const ClassInterfaceType CollectionClassInterface = ClassInterfaceType.None;
}
