using System;

namespace ByteForge.WinSCP;

/// <summary>
/// Provides a guard for operation results that ensures proper cleanup.
/// </summary>
internal class OperationResultGuard : IDisposable
{
	private readonly Session _session;

	private readonly OperationResultBase _operationResult;

	/// <summary>
	/// Initializes a new instance of the <see cref="OperationResultGuard"/> class.
	/// </summary>
	/// <param name="session">The session managing the operation result.</param>
	/// <param name="operationResult">The operation result to guard.</param>
	public OperationResultGuard(Session session, OperationResultBase operationResult)
	{
		_session = session;
		_operationResult = operationResult;
	}

	/// <summary>
	/// Unregisters the operation result from the session.
	/// </summary>
	public void Dispose()
	{
		_session.UnregisterOperationResult(_operationResult);
	}
}
