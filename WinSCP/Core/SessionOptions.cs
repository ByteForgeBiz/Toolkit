using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using System.Text.RegularExpressions;

namespace ByteForge.WinSCP;

/// <summary>
/// Specifies options for establishing a WinSCP session.
/// </summary>
[Guid("2D4EF368-EE80-4C15-AE77-D12AEAF4B00A")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class SessionOptions
{
	/// <summary>
	/// Backing store for the session password, held as a <see cref="SecureString"/> to minimize exposure in memory.
	/// </summary>
	private SecureString _securePassword;

	/// <summary>
	/// Backing store for the new password used during password-change authentication.
	/// </summary>
	private SecureString _secureNewPassword;

	/// <summary>
	/// Backing store for the SSH private key passphrase, held as a <see cref="SecureString"/>.
	/// </summary>
	private SecureString _securePrivateKeyPassphrase;

	/// <summary>
	/// Backing store for the validated SSH host key fingerprint string.
	/// </summary>
	private string _sshHostKeyFingerprint;

	/// <summary>
	/// Backing store for the validated TLS host certificate fingerprint string.
	/// </summary>
	private string _tlsHostCertificateFingerprint;

	/// <summary>
	/// Backing store for the operation timeout duration.
	/// </summary>
	private TimeSpan _timeout;

	/// <summary>
	/// Backing store for the validated port number (0–65535).
	/// </summary>
	private int _portNumber;

	/// <summary>
	/// Backing store for the root path, validated to start with a forward slash.
	/// </summary>
	private string _rootPath;

	/// <summary>
	/// Backing store for the selected transfer protocol.
	/// </summary>
	private Protocol _protocol;

	/// <summary>
	/// Backing store for the explicit session name; <c>null</c> causes the name to be derived from
	/// the username and hostname.
	/// </summary>
	private string _name;

	/// <summary>
	/// Format string used to build a semicolon-separated list regex from a single-item pattern.
	/// </summary>
	private const string _listPattern = "{0}(;{0})*";

	/// <summary>
	/// Regex pattern string matching a single SSH host key fingerprint in MD5 or SHA-256 format,
	/// optionally preceded by an algorithm identifier.
	/// </summary>
	private const string _sshHostKeyPattern = "((ssh-rsa|ssh-dss|ssh-ed25519|ecdsa-sha2-nistp(256|384|521))( |-))?(\\d+ )?(([0-9a-fA-F]{2}(:|-)){15}[0-9a-fA-F]{2}|[0-9a-zA-Z+/\\-_]{43}=?)";

	/// <summary>
	/// Compiled regex that matches a semicolon-separated list of SSH host key fingerprints.
	/// </summary>
	private static readonly Regex _sshHostKeyRegex = new Regex(string.Format(CultureInfo.InvariantCulture, "{0}(;{0})*", new object[1] { "((ssh-rsa|ssh-dss|ssh-ed25519|ecdsa-sha2-nistp(256|384|521))( |-))?(\\d+ )?(([0-9a-fA-F]{2}(:|-)){15}[0-9a-fA-F]{2}|[0-9a-zA-Z+/\\-_]{43}=?)" }));

	/// <summary>
	/// Regex pattern string matching a single TLS certificate fingerprint in SHA-1 or SHA-256 hex format.
	/// </summary>
	private const string _tlsCertificatePattern = "((([0-9a-fA-F]{2}[:\\-]){31})|(([0-9a-fA-F]{2}[:\\-]){19}))[0-9a-fA-F]{2}";

	/// <summary>
	/// Compiled regex that matches a semicolon-separated list of TLS certificate fingerprints.
	/// </summary>
	private static readonly Regex _tlsCertificateRegex = new Regex(string.Format(CultureInfo.InvariantCulture, "{0}(;{0})*", new object[1] { "((([0-9a-fA-F]{2}[:\\-]){31})|(([0-9a-fA-F]{2}[:\\-]){19}))[0-9a-fA-F]{2}" }));

	/// <summary>
	/// Gets or sets the name of the session.
	/// </summary>
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

	/// <summary>
	/// Gets or sets the protocol to use for the session.
	/// </summary>
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

	/// <summary>
	/// Gets or sets the hostname or IP address of the remote server.
	/// </summary>
	public string HostName { get; set; }

	/// <summary>
	/// Gets or sets the port number to connect to.
	/// </summary>
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

	/// <summary>
	/// Gets or sets the username for authentication.
	/// </summary>
	public string UserName { get; set; }

	/// <summary>
	/// Gets or sets the password for authentication.
	/// </summary>
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

	/// <summary>
	/// Gets or sets the password for authentication as a secure string.
	/// </summary>
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

	/// <summary>
	/// Gets or sets the new password for password change operations.
	/// </summary>
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

	/// <summary>
	/// Gets or sets the new password for password change operations as a secure string.
	/// </summary>
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

	/// <summary>
	/// Gets or sets the timeout duration for operations.
	/// </summary>
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

	/// <summary>
	/// Gets or sets the timeout in milliseconds.
	/// </summary>
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

	/// <summary>
	/// Gets or sets the passphrase for the SSH private key.
	/// </summary>
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

	/// <summary>
	/// Gets or sets the passphrase for the SSH private key as a secure string.
	/// </summary>
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

	/// <summary>
	/// Gets or sets the root path for WebDAV and S3 protocols.
	/// </summary>
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

	/// <summary>
	/// Gets or sets a value indicating whether to use a secure connection (HTTPS/FTPS/DAVS/S3).
	/// </summary>
	public bool Secure { get; set; }

	/// <summary>
	/// Gets or sets the SSH host key fingerprint for verification.
	/// </summary>
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

	/// <summary>
	/// Gets or sets the SSH host key policy.
	/// </summary>
	public SshHostKeyPolicy SshHostKeyPolicy { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether to give up security and accept any SSH host key. (Obsolete - use SshHostKeyPolicy)
	/// </summary>
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

	/// <summary>
	/// Gets or sets the path to the SSH private key file.
	/// </summary>
	public string SshPrivateKeyPath { get; set; }

	/// <summary>
	/// Gets or sets the SSH private key as a string.
	/// </summary>
	public string SshPrivateKey { get; set; }

	/// <summary>
	/// Gets or sets the SSH private key passphrase. (Obsolete - use PrivateKeyPassphrase)
	/// </summary>
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

	/// <summary>
	/// Gets or sets the FTP mode (passive or active).
	/// </summary>
	public FtpMode FtpMode { get; set; }

	/// <summary>
	/// Gets or sets the FTP security mode (implicit or explicit TLS/SSL).
	/// </summary>
	public FtpSecure FtpSecure { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether to use a secure WebDAV connection. (Obsolete - use Secure)
	/// </summary>
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

	/// <summary>
	/// Gets or sets the root path for WebDAV. (Obsolete - use RootPath)
	/// </summary>
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

	/// <summary>
	/// Gets or sets the TLS host certificate fingerprint for verification.
	/// </summary>
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

	/// <summary>
	/// Gets or sets a value indicating whether to give up security and accept any TLS host certificate.
	/// </summary>
	public bool GiveUpSecurityAndAcceptAnyTlsHostCertificate { get; set; }

	/// <summary>
	/// Gets or sets the path to the TLS client certificate file.
	/// </summary>
	public string TlsClientCertificatePath { get; set; }

	/// <summary>
	/// Gets the dictionary of raw settings for advanced configuration.
	/// </summary>
	internal Dictionary<string, string> RawSettings { get; private set; }

	/// <summary>
	/// Gets a value indicating whether the protocol is SSH-based (SFTP or SCP).
	/// </summary>
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

	/// <summary>
	/// Gets a value indicating whether the connection uses TLS/SSL encryption.
	/// </summary>
	internal bool IsTls => GetIsTls();

	/// <summary>
	/// Initializes a new instance of the <see cref="SessionOptions"/> class with default settings.
	/// </summary>
	public SessionOptions()
	{
		Timeout = new TimeSpan(0, 0, 15);
		RawSettings = new Dictionary<string, string>();
	}

	/// <summary>
	/// Adds a raw setting for advanced configuration.
	/// </summary>
	/// <param name="setting">The setting name.</param>
	/// <param name="value">The setting value.</param>
	public void AddRawSettings(string setting, string value)
	{
		RawSettings.Add(setting, value);
	}

	/// <summary>
	/// Parses a URL and configures the session options accordingly.
	/// </summary>
	/// <param name="url">The URL to parse in format: protocol://[username[:password]@]hostname[:port][/path][;fingerprint=...][;x-parameterName=...]</param>
	/// <exception cref="ArgumentNullException">Thrown when url is null.</exception>
	/// <exception cref="ArgumentException">Thrown when the URL format is invalid.</exception>
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

	/// <summary>
	/// Parses a protocol string and sets the corresponding <see cref="Protocol"/> and related properties.
	/// </summary>
	/// <param name="protocol">The protocol scheme string (e.g., "sftp", "ftp", "davs").</param>
	/// <returns><c>true</c> if the protocol was recognized; otherwise, <c>false</c>.</returns>
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

	/// <summary>
	/// Returns <c>null</c> if the string is null or empty; otherwise returns the original string.
	/// </summary>
	/// <param name="s">The string to evaluate.</param>
	/// <returns><c>null</c> if <paramref name="s"/> is null or empty; otherwise <paramref name="s"/>.</returns>
	private static string EmptyToNull(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return null;
		}
		return s;
	}

	/// <summary>
	/// Decodes a URI-encoded string.
	/// </summary>
	/// <param name="s">The URI-encoded string to decode.</param>
	/// <returns>The decoded string.</returns>
	private static string UriUnescape(string s)
	{
		return Uri.UnescapeDataString(s);
	}

	/// <summary>
	/// Splits <paramref name="s"/> at the first occurrence of <paramref name="c"/>, returning the portion
	/// before the delimiter and updating <paramref name="s"/> to the portion after it.
	/// </summary>
	/// <param name="s">The string to split; updated to the remainder after the delimiter on return.</param>
	/// <param name="c">The delimiter character.</param>
	/// <returns>The portion of the string before the first occurrence of <paramref name="c"/>, or the full
	/// string if the delimiter is not found.</returns>
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

	/// <summary>
	/// Determines whether the current protocol and security settings imply a TLS/SSL connection.
	/// </summary>
	/// <returns><c>true</c> if the connection uses TLS/SSL; otherwise, <c>false</c>.</returns>
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

	/// <summary>
	/// Validates and sets the SSH host key fingerprint field.
	/// </summary>
	/// <param name="s">The fingerprint string to validate and store, or <c>null</c> to clear it.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="s"/> does not match the expected SSH host key fingerprint pattern.</exception>
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

	/// <summary>
	/// Validates and sets the TLS host certificate fingerprint field.
	/// </summary>
	/// <param name="s">The fingerprint string to validate and store, or <c>null</c> to clear it.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="s"/> does not match the expected TLS certificate fingerprint pattern.</exception>
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

	/// <summary>
	/// Validates and sets the timeout value.
	/// </summary>
	/// <param name="value">The timeout duration to set. Must be a positive, non-zero value.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is zero or negative.</exception>
	private void SetTimeout(TimeSpan value)
	{
		if (value <= TimeSpan.Zero)
		{
			throw new ArgumentException("Timeout has to be positive non-zero value");
		}
		_timeout = value;
	}

	/// <summary>
	/// Validates and sets the port number field.
	/// </summary>
	/// <param name="value">The port number to set. Must be in the range 0 to 65535.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is outside the valid port range.</exception>
	private void SetPortNumber(int value)
	{
		if (value < 0 || value > 65535)
		{
			throw new ArgumentOutOfRangeException("Port number has to be in range from 0 to 65535");
		}
		_portNumber = value;
	}

	/// <summary>
	/// Sets the protocol and applies protocol-specific defaults (e.g., sets the S3 default hostname).
	/// </summary>
	/// <param name="value">The protocol to set.</param>
	private void SetProtocol(Protocol value)
	{
		_protocol = value;
		if (_protocol == Protocol.S3 && string.IsNullOrEmpty(HostName))
		{
			HostName = "s3.amazonaws.com";
			Secure = true;
		}
	}

	/// <summary>
	/// Validates and sets the root path field.
	/// </summary>
	/// <param name="value">The root path to set. If non-empty, must begin with a forward slash.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is non-empty and does not start with a slash.</exception>
	private void SetRootPath(string value)
	{
		if (!string.IsNullOrEmpty(value) && value[0] != '/')
		{
			throw new ArgumentException("Root path has to start with a slash");
		}
		_rootPath = value;
	}

	/// <summary>
	/// Converts a plain-text password string into a <see cref="SecureString"/> and stores it.
	/// </summary>
	/// <param name="securePassword">The <see cref="SecureString"/> field to update.</param>
	/// <param name="value">The plain-text password. If <c>null</c>, the field is cleared.</param>
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

	/// <summary>
	/// Extracts a plain-text password from a <see cref="SecureString"/>.
	/// </summary>
	/// <param name="securePassword">The <see cref="SecureString"/> containing the password.</param>
	/// <returns>The plain-text password, or <c>null</c> if <paramref name="securePassword"/> is <c>null</c>.</returns>
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

	/// <summary>
	/// Computes the display name for the session, deriving it from username and hostname when no explicit name is set.
	/// </summary>
	/// <returns>The session name.</returns>
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

	/// <summary>
	/// Returns the session name.
	/// </summary>
	/// <returns>The session name.</returns>
	public override string ToString()
	{
		return Name;
	}

	/// <summary>
	/// Maps the legacy <see cref="GiveUpSecurityAndAcceptAnySshHostKey"/> boolean setter to the
	/// <see cref="SshHostKeyPolicy"/> property.
	/// </summary>
	/// <param name="value">
	/// <c>true</c> to set the policy to <see cref="SshHostKeyPolicy.GiveUpSecurityAndAcceptAny"/>;
	/// <c>false</c> to restore the default <see cref="SshHostKeyPolicy.Check"/> policy.
	/// </param>
	private void SetGiveUpSecurityAndAcceptAnySshHostKey(bool value)
	{
		SshHostKeyPolicy = (value ? SshHostKeyPolicy.GiveUpSecurityAndAcceptAny : SshHostKeyPolicy.Check);
	}

	/// <summary>
	/// Returns whether the SSH host key policy is set to accept any key without verification.
	/// </summary>
	/// <returns><c>true</c> if <see cref="SshHostKeyPolicy"/> is <see cref="SshHostKeyPolicy.GiveUpSecurityAndAcceptAny"/>; otherwise, <c>false</c>.</returns>
	private bool GetGiveUpSecurityAndAcceptAnySshHostKey()
	{
		return SshHostKeyPolicy == SshHostKeyPolicy.GiveUpSecurityAndAcceptAny;
	}
}
