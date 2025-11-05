using System;

namespace ByteForge.WinSCP;

internal interface ISessionProcess : IDisposable
{
	bool HasExited { get; }

	int ExitCode { get; }

	event OutputDataReceivedEventHandler OutputDataReceived;

	void Start();

	void ExecuteCommand(string command);

	void Close();
}
