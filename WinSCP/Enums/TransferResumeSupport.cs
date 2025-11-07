using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Specifies resume support options for file transfers.
/// </summary>
[Guid("6CED4579-0DF2-4E46-93E9-18780546B421")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class TransferResumeSupport
{
	private int _threshold;

	/// <summary>
	/// Gets or sets the state of transfer resume support.
	/// </summary>
	public TransferResumeSupportState State { get; set; }

	/// <summary>
	/// Gets or sets the threshold for smart resume support (in KB).
	/// </summary>
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

	/// <summary>
	/// Initializes a new instance of the <see cref="TransferResumeSupport"/> class with default settings.
	/// </summary>
	public TransferResumeSupport()
	{
		State = TransferResumeSupportState.Default;
		_threshold = 100;
	}

	/// <summary>
	/// Returns a string representation of the resume support configuration.
	/// </summary>
	/// <returns>A string representing the resume support state.</returns>
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
