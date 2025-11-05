using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[Guid("6CED4579-0DF2-4E46-93E9-18780546B421")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class TransferResumeSupport
{
	private int _threshold;

	public TransferResumeSupportState State { get; set; }

	public int Threshold
	{
		get
		{
			return GetThreshold();
		}
		set
		{
			SetThreshold(value);
		}
	}

	public TransferResumeSupport()
	{
		State = TransferResumeSupportState.Default;
		_threshold = 100;
	}

	public override string ToString()
	{
		return State switch
		{
			TransferResumeSupportState.Default => "default", 
			TransferResumeSupportState.Off => "off", 
			TransferResumeSupportState.On => "on", 
			TransferResumeSupportState.Smart => Threshold.ToString(CultureInfo.InvariantCulture), 
			_ => "unknown", 
		};
	}

	private int GetThreshold()
	{
		if (State != TransferResumeSupportState.Smart)
		{
			throw new InvalidOperationException("Threshold is undefined when state is not Smart");
		}
		return _threshold;
	}

	private void SetThreshold(int threshold)
	{
		if (_threshold != threshold)
		{
			if (threshold <= 0)
			{
				throw new ArgumentOutOfRangeException("threshold", "Threshold must be positive");
			}
			State = TransferResumeSupportState.Smart;
			_threshold = threshold;
		}
	}
}
