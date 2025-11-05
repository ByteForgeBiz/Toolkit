using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[Guid("0ADAAEBC-4A15-4A9C-8ED4-D85F5630035C")]
[ComVisible(true)]
public enum TransferResumeSupportState
{
	Default,
	On,
	Off,
	Smart
}
