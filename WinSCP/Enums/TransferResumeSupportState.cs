using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Specifies the state of transfer resume support.
/// </summary>
[Guid("0ADAAEBC-4A15-4A9C-8ED4-D85F5630035C")]
[ComVisible(true)]
public enum TransferResumeSupportState
{
	/// <summary>
	/// Use the default resume support setting.
	/// </summary>
	Default,
	/// <summary>
	/// Resume support is enabled.
	/// </summary>
	On,
	/// <summary>
	/// Resume support is disabled.
	/// </summary>
	Off,
	/// <summary>
	/// Resume support uses a smart threshold.
	/// </summary>
	Smart
}
