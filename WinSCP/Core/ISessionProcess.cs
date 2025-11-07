using System;

namespace ByteForge.WinSCP;

/// <summary>
/// Defines the interface for a WinSCP session process.
/// </summary>
internal interface ISessionProcess : IDisposable
{
	/// <summary>
	/// Gets a value indicating whether the process has exited.
	/// </summary>
	bool HasExited { get; }

	/// <summary>
	/// Gets the exit code of the process.
	/// </summary>
	int ExitCode { get; }

	/// <summary>
	/// Occurs when output data is received from the process.
	/// </summary>
	event OutputDataReceivedEventHandler OutputDataReceived;

	/// <summary>
	/// Starts the process.
	/// </summary>
	void Start();

	/// <summary>
	/// Executes a command in the process.
	/// </summary>
	/// <param name="command">The command to execute.</param>
	void ExecuteCommand(string command);

	/// <summary>
	/// Closes the process.
	/// </summary>
	void Close();
}
