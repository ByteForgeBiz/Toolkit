using System;

namespace ByteForge.WinSCP;

internal class OperationResultGuard : IDisposable
{
	private readonly Session _session;

	private readonly OperationResultBase _operationResult;

	public OperationResultGuard(Session session, OperationResultBase operationResult)
	{
		_session = session;
		_operationResult = operationResult;
	}

	public void Dispose()
	{
		_session.UnregisterOperationResult(_operationResult);
	}
}
