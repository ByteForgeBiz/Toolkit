using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using System.Text.RegularExpressions;

namespace ByteForge.WinSCP;

[Guid("2D4EF368-EE80-4C15-AE77-D12AEAF4B00A")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class SessionOptions
{
	private SecureString _securePassword;

	private SecureString _secureNewPassword;

	private SecureString _securePrivateKeyPassphrase;

	private string _sshHostKeyFingerprint;

	private string _tlsHostCertificateFingerprint;

	private TimeSpan _timeout;

	private int _portNumber;

	private string _rootPath;

	private Protocol _protocol;

	private string _name;

	private const string _listPattern = "{0}(;{0})*";

	private const string _sshHostKeyPattern = "((ssh-rsa|ssh-dss|ssh-ed25519|ecdsa-sha2-nistp(256|384|521))( |-))?(\\d+ )?(([0-9a-fA-F]{2}(:|-)){15}[0-9a-fA-F]{2}|[0-9a-zA-Z+/\\-_]{43}=?)";

	private static readonly Regex _sshHostKeyRegex = new Regex(string.Format(CultureInfo.InvariantCulture, "{0}(;{0})*", new object[1] { "((ssh-rsa|ssh-dss|ssh-ed25519|ecdsa-sha2-nistp(256|384|521))( |-))?(\\d+ )?(([0-9a-fA-F]{2}(:|-)){15}[0-9a-fA-F]{2}|[0-9a-zA-Z+/\\-_]{43}=?)" }));

	private const string _tlsCertificatePattern = "((([0-9a-fA-F]{2}[:\\-]){31})|(([0-9a-fA-F]{2}[:\\-]){19}))[0-9a-fA-F]{2}";

	private static readonly Regex _tlsCertificateRegex = new Regex(string.Format(CultureInfo.InvariantCulture, "{0}(;{0})*", new object[1] { "((([0-9a-fA-F]{2}[:\\-]){31})|(([0-9a-fA-F]{2}[:\\-]){19}))[0-9a-fA-F]{2}" }));

	public string Name
	{
		get
		{
			return GetName();
		}
		set
		{
			_name = value;
		}
	}

	public Protocol Protocol
	{
		get
		{
			return _protocol;
		}
		set
		{
			SetProtocol(value);
		}
	}

	public string HostName { get; set; }

	public int PortNumber
	{
		get
		{
			return _portNumber;
		}
		set
		{
			SetPortNumber(value);
		}
	}

	public string UserName { get; set; }

	public string Password
	{
		get
		{
			return GetPassword(_securePassword);
		}
		set
		{
			SetPassword(ref _securePassword, value);
		}
	}

	public SecureString SecurePassword
	{
		get
		{
			return _securePassword;
		}
		set
		{
			_securePassword = value;
		}
	}

	public string NewPassword
	{
		get
		{
			return GetPassword(_secureNewPassword);
		}
		set
		{
			SetPassword(ref _secureNewPassword, value);
		}
	}

	public SecureString SecureNewPassword
	{
		get
		{
			return _secureNewPassword;
		}
		set
		{
			_secureNewPassword = value;
		}
	}

	public TimeSpan Timeout
	{
		get
		{
			return _timeout;
		}
		set
		{
			SetTimeout(value);
		}
	}

	public int TimeoutInMilliseconds
	{
		get
		{
			return Tools.TimeSpanToMilliseconds(Timeout);
		}
		set
		{
			Timeout = Tools.MillisecondsToTimeSpan(value);
		}
	}

	public string PrivateKeyPassphrase
	{
		get
		{
			return GetPassword(_securePrivateKeyPassphrase);
		}
		set
		{
			SetPassword(ref _securePrivateKeyPassphrase, value);
		}
	}

	public SecureString SecurePrivateKeyPassphrase
	{
		get
		{
			return _securePrivateKeyPassphrase;
		}
		set
		{
			_securePrivateKeyPassphrase = value;
		}
	}

	public string RootPath
	{
		get
		{
			return _rootPath;
		}
		set
		{
			SetRootPath(value);
		}
	}

	public bool Secure { get; set; }

	public string SshHostKeyFingerprint
	{
		get
		{
			return _sshHostKeyFingerprint;
		}
		set
		{
			SetSshHostKeyFingerprint(value);
		}
	}

	public SshHostKeyPolicy SshHostKeyPolicy { get; set; }

	[Obsolete("Use SshHostKeyPolicy")]
	public bool GiveUpSecurityAndAcceptAnySshHostKey
	{
		get
		{
			return GetGiveUpSecurityAndAcceptAnySshHostKey();
		}
		set
		{
			SetGiveUpSecurityAndAcceptAnySshHostKey(value);
		}
	}

	public string SshPrivateKeyPath { get; set; }

	public string SshPrivateKey { get; set; }

	[Obsolete("Use PrivateKeyPassphrase")]
	public string SshPrivateKeyPassphrase
	{
		get
		{
			return PrivateKeyPassphrase;
		}
		set
		{
			PrivateKeyPassphrase = value;
		}
	}

	public FtpMode FtpMode { get; set; }

	public FtpSecure FtpSecure { get; set; }

	[Obsolete("Use Secure")]
	public bool WebdavSecure
	{
		get
		{
			return Secure;
		}
		set
		{
			Secure = value;
		}
	}

	[Obsolete("Use RootPath")]
	public string WebdavRoot
	{
		get
		{
			return RootPath;
		}
		set
		{
			RootPath = value;
		}
	}

	public string TlsHostCertificateFingerprint
	{
		get
		{
			return _tlsHostCertificateFingerprint;
		}
		set
		{
			SetHostTlsCertificateFingerprint(value);
		}
	}

	public bool GiveUpSecurityAndAcceptAnyTlsHostCertificate { get; set; }

	public string TlsClientCertificatePath { get; set; }

	internal Dictionary<string, string> RawSettings { get; private set; }

	internal bool IsSsh
	{
		get
		{
			if (Protocol != Protocol.Sftp)
			{
				return Protocol == Protocol.Scp;
			}
			return true;
		}
	}

	internal bool IsTls => GetIsTls();

	public SessionOptions()
	{
		Timeout = new TimeSpan(0, 0, 15);
		RawSettings = new Dictionary<string, string>();
	}

	public void AddRawSettings(string setting, string value)
	{
		RawSettings.Add(setting, value);
	}

	public void ParseUrl(string url)
	{
		if (url == null)
		{
			throw new ArgumentNullException("url");
		}
		url = url.Trim();
		int num = url.IndexOf("://", StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			throw new ArgumentException("Protocol not specified", "url");
		}
		string text = url.Substring(0, num).Trim();
		if (!ParseProtocol(text))
		{
			throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Unknown protocol {0}", new object[1] { text }), "url");
		}
		url = url.Substring(num + "://".Length).Trim();
		num = url.IndexOf('/');
		RootPath = null;
		if (num >= 0)
		{
			string text2 = url.Substring(num).Trim();
			url = url.Substring(0, num).Trim();
			string s = text2;
			text2 = CutToChar(ref s, ';');
			if (!string.IsNullOrEmpty(text2) && text2 != "/")
			{
				if (Protocol != Protocol.Webdav && Protocol != Protocol.S3)
				{
					throw new ArgumentException("Root path can be specified for WebDAV and S3 protocols only", "url");
				}
				RootPath = text2;
			}
			if (!string.IsNullOrEmpty(s))
			{
				throw new ArgumentException("No session parameters are supported", "url");
			}
		}
		num = url.LastIndexOf('@');
		string text3 = null;
		string s2;
		if (num >= 0)
		{
			text3 = url.Substring(0, num).Trim();
			s2 = url.Substring(num + 1).Trim();
		}
		else
		{
			s2 = url;
		}
		PortNumber = 0;
		string text4 = null;
		if (s2.Length >= 2 && s2[0] == '[' && (num = s2.IndexOf(']')) > 0)
		{
			HostName = s2.Substring(1, num - 1).Trim();
			s2 = s2.Substring(num + 1).Trim();
			if (s2.Length > 0)
			{
				if (s2[0] != ':')
				{
					throw new ArgumentException("Unexpected syntax after ]", "url");
				}
				text4 = s2.Substring(1);
			}
		}
		else
		{
			HostName = UriUnescape(CutToChar(ref s2, ':'));
			text4 = s2;
		}
		if (string.IsNullOrEmpty(HostName))
		{
			throw new ArgumentException("No host name", "url");
		}
		if (string.IsNullOrEmpty(text4))
		{
			PortNumber = 0;
		}
		else
		{
			text4 = UriUnescape(text4);
			if (!int.TryParse(text4, NumberStyles.None, CultureInfo.InvariantCulture, out var result))
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0} is not a valid port number", new object[1] { text4 }), "url");
			}
			PortNumber = result;
		}
		UserName = null;
		Password = null;
		SshHostKeyFingerprint = null;
		SshHostKeyPolicy = SshHostKeyPolicy.Check;
		TlsHostCertificateFingerprint = null;
		GiveUpSecurityAndAcceptAnyTlsHostCertificate = false;
		if (string.IsNullOrEmpty(text3))
		{
			return;
		}
		string s3 = text3;
		text3 = CutToChar(ref s3, ';');
		bool flag = text3.IndexOf(':') >= 0;
		UserName = EmptyToNull(UriUnescape(CutToChar(ref text3, ':')));
		Password = (flag ? UriUnescape(text3) : null);
		while (!string.IsNullOrEmpty(s3))
		{
			string s4 = CutToChar(ref s3, ';');
			string text5 = CutToChar(ref s4, '=');
			s4 = UriUnescape(s4);
			if (text5.Equals("fingerprint", StringComparison.OrdinalIgnoreCase))
			{
				switch (Protocol)
				{
				case Protocol.Sftp:
				case Protocol.Scp:
					SshHostKeyFingerprint = s4;
					break;
				case Protocol.Ftp:
				case Protocol.Webdav:
				case Protocol.S3:
					TlsHostCertificateFingerprint = s4;
					break;
				default:
					throw new ArgumentException();
				}
			}
			else
			{
				if (!text5.StartsWith("x-", StringComparison.OrdinalIgnoreCase))
				{
					throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Unsupported connection parameter {0}", new object[1] { text5 }), "url");
				}
				text5 = UriUnescape(text5.Substring("x-".Length));
				if (text5.Equals("name", StringComparison.OrdinalIgnoreCase))
				{
					Name = s4;
				}
				else
				{
					AddRawSettings(text5, s4);
				}
			}
		}
	}

	private bool ParseProtocol(string protocol)
	{
		bool result = true;
		FtpSecure = FtpSecure.None;
		if (protocol.Equals("sftp", StringComparison.OrdinalIgnoreCase))
		{
			Protocol = Protocol.Sftp;
		}
		else if (protocol.Equals("scp", StringComparison.OrdinalIgnoreCase))
		{
			Protocol = Protocol.Scp;
		}
		else if (protocol.Equals("ftp", StringComparison.OrdinalIgnoreCase))
		{
			Protocol = Protocol.Ftp;
		}
		else if (protocol.Equals("ftps", StringComparison.OrdinalIgnoreCase))
		{
			Protocol = Protocol.Ftp;
			FtpSecure = FtpSecure.Implicit;
		}
		else if (protocol.Equals("ftpes", StringComparison.OrdinalIgnoreCase))
		{
			Protocol = Protocol.Ftp;
			FtpSecure = FtpSecure.Explicit;
		}
		else if (protocol.Equals("dav", StringComparison.OrdinalIgnoreCase) || protocol.Equals("http", StringComparison.OrdinalIgnoreCase))
		{
			Protocol = Protocol.Webdav;
		}
		else if (protocol.Equals("davs", StringComparison.OrdinalIgnoreCase) || protocol.Equals("https", StringComparison.OrdinalIgnoreCase))
		{
			Protocol = Protocol.Webdav;
			Secure = true;
		}
		else if (protocol.Equals("s3plain", StringComparison.OrdinalIgnoreCase))
		{
			Protocol = Protocol.S3;
			Secure = false;
		}
		else if (protocol.Equals("s3", StringComparison.OrdinalIgnoreCase))
		{
			Protocol = Protocol.S3;
		}
		else
		{
			result = false;
		}
		return result;
	}

	private static string EmptyToNull(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return null;
		}
		return s;
	}

	private static string UriUnescape(string s)
	{
		return Uri.UnescapeDataString(s);
	}

	private static string CutToChar(ref string s, char c)
	{
		int num = s.IndexOf(c);
		string result;
		if (num >= 0)
		{
			result = s.Substring(0, num).Trim();
			s = s.Substring(num + 1).Trim();
		}
		else
		{
			result = s;
			s = string.Empty;
		}
		return result;
	}

	private bool GetIsTls()
	{
		if (Protocol != Protocol.Ftp || FtpSecure == FtpSecure.None)
		{
			if (Protocol == Protocol.Webdav || Protocol == Protocol.S3)
			{
				return Secure;
			}
			return false;
		}
		return true;
	}

	private void SetSshHostKeyFingerprint(string s)
	{
		if (s != null)
		{
			Match match = _sshHostKeyRegex.Match(s);
			if (!match.Success || match.Length != s.Length)
			{
				throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "SSH host key fingerprint \"{0}\" does not match pattern /{1}/", new object[2] { s, _sshHostKeyRegex }));
			}
		}
		_sshHostKeyFingerprint = s;
	}

	private void SetHostTlsCertificateFingerprint(string s)
	{
		if (s != null)
		{
			Match match = _tlsCertificateRegex.Match(s);
			if (!match.Success || match.Length != s.Length)
			{
				throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "TLS host certificate fingerprint \"{0}\" does not match pattern /{1}/", new object[2] { s, _tlsCertificateRegex }));
			}
		}
		_tlsHostCertificateFingerprint = s;
	}

	private void SetTimeout(TimeSpan value)
	{
		if (value <= TimeSpan.Zero)
		{
			throw new ArgumentException("Timeout has to be positive non-zero value");
		}
		_timeout = value;
	}

	private void SetPortNumber(int value)
	{
		if (value < 0 || value > 65535)
		{
			throw new ArgumentOutOfRangeException("Port number has to be in range from 0 to 65535");
		}
		_portNumber = value;
	}

	private void SetProtocol(Protocol value)
	{
		_protocol = value;
		if (_protocol == Protocol.S3 && string.IsNullOrEmpty(HostName))
		{
			HostName = "s3.amazonaws.com";
			Secure = true;
		}
	}

	private void SetRootPath(string value)
	{
		if (!string.IsNullOrEmpty(value) && value[0] != '/')
		{
			throw new ArgumentException("Root path has to start with a slash");
		}
		_rootPath = value;
	}

	private static void SetPassword(ref SecureString securePassword, string value)
	{
		if (value == null)
		{
			securePassword = null;
			return;
		}
		securePassword = new SecureString();
		foreach (char c in value)
		{
			securePassword.AppendChar(c);
		}
	}

	private static string GetPassword(SecureString securePassword)
	{
		if (securePassword == null)
		{
			return null;
		}
		IntPtr intPtr = IntPtr.Zero;
		try
		{
			intPtr = Marshal.SecureStringToGlobalAllocUnicode(securePassword);
			return Marshal.PtrToStringUni(intPtr);
		}
		finally
		{
			Marshal.ZeroFreeGlobalAllocUnicode(intPtr);
		}
	}

	private string GetName()
	{
		if (_name != null)
		{
			return _name;
		}
		if (!string.IsNullOrEmpty(HostName) && !string.IsNullOrEmpty(UserName))
		{
			return UserName + "@" + HostName;
		}
		if (!string.IsNullOrEmpty(HostName))
		{
			return HostName;
		}
		return "session";
	}

	public override string ToString()
	{
		return Name;
	}

	private void SetGiveUpSecurityAndAcceptAnySshHostKey(bool value)
	{
		SshHostKeyPolicy = (value ? SshHostKeyPolicy.GiveUpSecurityAndAcceptAny : SshHostKeyPolicy.Check);
	}

	private bool GetGiveUpSecurityAndAcceptAnySshHostKey()
	{
		return SshHostKeyPolicy == SshHostKeyPolicy.GiveUpSecurityAndAcceptAny;
	}
}
