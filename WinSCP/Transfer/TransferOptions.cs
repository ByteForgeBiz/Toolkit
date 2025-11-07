using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Specifies options for file transfer operations.
/// </summary>
[Guid("155B841F-39D4-40C8-BA87-C79675E14CE3")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class TransferOptions
{
	/// <summary>
	/// Gets or sets a value indicating whether to preserve the timestamp of transferred files.
	/// </summary>
	public bool PreserveTimestamp { get; set; }

	/// <summary>
	/// Gets or sets the file permissions to apply to transferred files.
	/// </summary>
	public FilePermissions FilePermissions { get; set; }

	/// <summary>
	/// Gets or sets the transfer mode (binary, ASCII, automatic).
	/// </summary>
	public TransferMode TransferMode { get; set; }

	/// <summary>
	/// Gets or sets the file mask for selecting files to transfer.
	/// </summary>
	public string FileMask { get; set; }

	/// <summary>
	/// Gets or sets the resume support options for file transfers.
	/// </summary>
	public TransferResumeSupport ResumeSupport { get; set; }

	/// <summary>
	/// Gets or sets the speed limit for file transfers in KB/s.
	/// </summary>
	public int SpeedLimit { get; set; }

	/// <summary>
	/// Gets or sets the overwrite mode for file transfers.
	/// </summary>
	public OverwriteMode OverwriteMode { get; set; }

	/// <summary>
	/// Gets the raw settings for advanced transfer options.
	/// </summary>
	internal Dictionary<string, string> RawSettings { get; private set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="TransferOptions"/> class.
	/// </summary>
	public TransferOptions()
	{
		PreserveTimestamp = true;
		TransferMode = TransferMode.Binary;
		ResumeSupport = new TransferResumeSupport();
		OverwriteMode = OverwriteMode.Overwrite;
		RawSettings = new Dictionary<string, string>();
	}

	/// <summary>
	/// Adds a raw setting for advanced transfer options.
	/// </summary>
	/// <param name="setting">The name of the setting.</param>
	/// <param name="value">The value of the setting.</param>
	public void AddRawSettings(string setting, string value)
	{
		RawSettings.Add(setting, value);
	}

	/// <summary>
	/// Converts the transfer options to command-line switches.
	/// </summary>
	/// <returns>A string containing the command-line switches.</returns>
	internal string ToSwitches()
	{
		List<string> list = new List<string>();
		if (FilePermissions != null)
		{
			list.Add(Session.FormatSwitch("permissions", FilePermissions.Octal));
		}
		else
		{
			list.Add("-nopermissions");
		}
		list.Add(Session.BooleanSwitch(PreserveTimestamp, "preservetime", "nopreservetime"));
		list.Add(Session.FormatSwitch("transfer", TransferMode switch
		{
			TransferMode.Binary => "binary", 
			TransferMode.Ascii => "ascii", 
			TransferMode.Automatic => "automatic", 
			_ => throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "{0} is not supported", new object[1] { TransferMode })), 
		}));
		if (!string.IsNullOrEmpty(FileMask))
		{
			list.Add(Session.FormatSwitch("filemask", FileMask));
		}
		if (ResumeSupport.State != TransferResumeSupportState.Default)
		{
			list.Add(Session.FormatSwitch("resumesupport", ResumeSupport.ToString()));
		}
		if (SpeedLimit > 0)
		{
			list.Add(Session.FormatSwitch("speed", SpeedLimit.ToString(CultureInfo.InvariantCulture)));
		}
		switch (OverwriteMode)
		{
		case OverwriteMode.Resume:
			list.Add(Session.FormatSwitch("resume"));
			break;
		case OverwriteMode.Append:
			list.Add(Session.FormatSwitch("append"));
			break;
		default:
			throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "{0} is not supported", new object[1] { OverwriteMode }));
		case OverwriteMode.Overwrite:
			break;
		}
		string arguments = string.Join(" ", list.ToArray());
		Tools.AddRawParameters(ref arguments, RawSettings, "-rawtransfersettings", count: true);
		return arguments;
	}
}
