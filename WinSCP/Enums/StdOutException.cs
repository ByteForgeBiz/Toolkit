using System;

namespace ByteForge.WinSCP;

internal class StdOutException : Exception
{
	public StdOutException()
		: base("Unexpected data")
	{
	}
}
