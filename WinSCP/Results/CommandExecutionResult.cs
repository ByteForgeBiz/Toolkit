using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Represents the result of a command execution, including output, error output, and exit code.
/// </summary>
[Guid("70C312F8-9A09-4D9B-B8EC-FB6ED753892B")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class CommandExecutionResult : OperationResultBase
{
	/// <summary>
	/// Gets the standard output produced by the command.
	/// </summary>
	public string Output { get; internal set; }

	/// <summary>
	/// Gets the error output produced by the command.
	/// </summary>
	public string ErrorOutput { get; internal set; }

	/// <summary>
	/// Gets the exit code returned by the command.
	/// </summary>
	public int ExitCode { get; internal set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="CommandExecutionResult"/> class.
	/// </summary>
	internal CommandExecutionResult()
	{
	}
}
