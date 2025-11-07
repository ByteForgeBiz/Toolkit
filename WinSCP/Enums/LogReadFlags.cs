using System;

namespace ByteForge.WinSCP;

/// <summary>
/// Specifies flags for log reading behavior.
/// </summary>
[Flags]
internal enum LogReadFlags
{
	/// <summary>
	/// Throw exceptions for operation failures.
	/// </summary>
	ThrowFailures = 1
}
