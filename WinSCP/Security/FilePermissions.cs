using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[Guid("90A290B2-C8CE-4900-8C42-7736F9E435C6")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class FilePermissions
{
	private const string BasicSymbols = "rwxrwxrwx";

	private const string CombinedSymbols = "--s--s--t";

	private const string ExtendedSymbols = "--S--S--T";

	private const char UnsetSymbol = '-';

	private int _numeric;

	private bool _readOnly;

	public int Numeric
	{
		get
		{
			return _numeric;
		}
		set
		{
			if (_readOnly)
			{
				throw new InvalidOperationException("Cannot change read-only permissions");
			}
			if (value < 0 || value > 4095)
			{
				throw new ArgumentOutOfRangeException(string.Format(CultureInfo.CurrentCulture, "{0} is not valid numerical permissions", new object[1] { value }));
			}
			_numeric = value;
		}
	}

	public string Text
	{
		get
		{
			return NumericToText(Numeric);
		}
		set
		{
			Numeric = TextToNumeric(value);
		}
	}

	public string Octal
	{
		get
		{
			string text = Convert.ToString(Numeric, 8);
			return new string('0', Math.Max(3 - text.Length, 0)) + text;
		}
		set
		{
			if (value == null || (value.Length != 3 && value.Length != 4))
			{
				throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "{0} is not valid octal permissions", new object[1] { value }));
			}
			Numeric = Convert.ToInt16(value, 8);
		}
	}

	public bool OtherExecute
	{
		get
		{
			return GetBit(0);
		}
		set
		{
			SetBit(0, value);
		}
	}

	public bool OtherWrite
	{
		get
		{
			return GetBit(1);
		}
		set
		{
			SetBit(1, value);
		}
	}

	public bool OtherRead
	{
		get
		{
			return GetBit(2);
		}
		set
		{
			SetBit(2, value);
		}
	}

	public bool GroupExecute
	{
		get
		{
			return GetBit(3);
		}
		set
		{
			SetBit(3, value);
		}
	}

	public bool GroupWrite
	{
		get
		{
			return GetBit(4);
		}
		set
		{
			SetBit(4, value);
		}
	}

	public bool GroupRead
	{
		get
		{
			return GetBit(5);
		}
		set
		{
			SetBit(5, value);
		}
	}

	public bool UserExecute
	{
		get
		{
			return GetBit(6);
		}
		set
		{
			SetBit(6, value);
		}
	}

	public bool UserWrite
	{
		get
		{
			return GetBit(7);
		}
		set
		{
			SetBit(7, value);
		}
	}

	public bool UserRead
	{
		get
		{
			return GetBit(8);
		}
		set
		{
			SetBit(8, value);
		}
	}

	public bool Sticky
	{
		get
		{
			return GetBit(9);
		}
		set
		{
			SetBit(9, value);
		}
	}

	public bool SetGid
	{
		get
		{
			return GetBit(10);
		}
		set
		{
			SetBit(10, value);
		}
	}

	public bool SetUid
	{
		get
		{
			return GetBit(11);
		}
		set
		{
			SetBit(11, value);
		}
	}

	public FilePermissions()
		: this(0)
	{
	}

	public FilePermissions(int numeric)
	{
		Numeric = numeric;
	}

	public override string ToString()
	{
		return Text;
	}

	internal static FilePermissions CreateReadOnlyFromText(string text)
	{
		return new FilePermissions
		{
			Numeric = TextToNumeric(text),
			_readOnly = true
		};
	}

	internal static int TextToNumeric(string text)
	{
		if (text.Length != "rwxrwxrwx".Length)
		{
			throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "{0} is not valid permissions string", new object[1] { text }));
		}
		int num = 0;
		int num2 = 1;
		int num3 = 512;
		for (int num4 = text.Length - 1; num4 >= 0; num4--)
		{
			if (text[num4] != '-')
			{
				num = ((text[num4] == "--s--s--t"[num4]) ? (num | (num2 | num3)) : ((text[num4] != "--S--S--T"[num4]) ? (num | num2) : (num | num3)));
			}
			num2 <<= 1;
			if (num4 % 3 == 0)
			{
				num3 <<= 1;
			}
		}
		return num;
	}

	private static string NumericToText(int numeric)
	{
		char[] array = new char["rwxrwxrwx".Length];
		int num = 1;
		int num2 = 512;
		bool flag = true;
		int num3 = "rwxrwxrwx".Length - 1;
		while (num3 >= 0)
		{
			char c = ((flag && (numeric & (num | num2)) == (num | num2)) ? "--s--s--t"[num3] : (((numeric & num) != 0) ? "rwxrwxrwx"[num3] : ((!flag || (numeric & num2) == 0) ? '-' : "--S--S--T"[num3])));
			array[num3] = c;
			num <<= 1;
			num3--;
			flag = num3 % 3 == 2;
			if (flag)
			{
				num2 <<= 1;
			}
		}
		return new string(array);
	}

	private bool GetBit(int bit)
	{
		return (Numeric & (1 << bit)) != 0;
	}

	private void SetBit(int bit, bool value)
	{
		if (value)
		{
			Numeric |= 1 << bit;
		}
		else
		{
			Numeric &= ~(1 << bit);
		}
	}
}
