using System;

namespace ByteForge.WinSCP;

/// <summary>
/// Represents an exception thrown when unexpected data is received on standard output.
/// </summary>
internal class StdOutException : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="StdOutException"/> class.
	/// </summary>
	public StdOutException()
		: base("Unexpected data")
	{
	}
}
