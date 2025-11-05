using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[Guid("155B841F-39D4-40C8-BA87-C79675E14CE3")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class TransferOptions
{
	public bool PreserveTimestamp { get; set; }

	public FilePermissions FilePermissions { get; set; }

	public TransferMode TransferMode { get; set; }

	public string FileMask { get; set; }

	public TransferResumeSupport ResumeSupport { get; set; }

	public int SpeedLimit { get; set; }

	public OverwriteMode OverwriteMode { get; set; }

	internal Dictionary<string, string> RawSettings { get; private set; }

	public TransferOptions()
	{
		PreserveTimestamp = true;
		TransferMode = TransferMode.Binary;
		ResumeSupport = new TransferResumeSupport();
		OverwriteMode = OverwriteMode.Overwrite;
		RawSettings = new Dictionary<string, string>();
	}

	public void AddRawSettings(string setting, string value)
	{
		RawSettings.Add(setting, value);
	}

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
