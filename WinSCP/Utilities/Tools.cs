using System;
using System.Collections.Generic;
using System.Globalization;

namespace ByteForge.WinSCP;

/// <summary>
/// Provides utility methods for the WinSCP library.
/// </summary>
internal static class Tools
{
	/// <summary>
	/// Converts a TimeSpan to milliseconds as an integer.
	/// </summary>
	/// <param name="value">The TimeSpan to convert.</param>
	/// <returns>The number of milliseconds.</returns>
	/// <exception cref="InvalidCastException">Thrown when the value is too large to fit in an integer.</exception>
	public static int TimeSpanToMilliseconds(TimeSpan value)
	{
		if (value.TotalMilliseconds > 2147483647.0 || value.TotalMilliseconds < -2147483648.0)
		{
			throw new InvalidCastException(string.Format(CultureInfo.CurrentCulture, "Cannot convert {0} to integer", new object[1] { value }));
		}
		return (int)value.TotalMilliseconds;
	}

	/// <summary>
	/// Converts milliseconds to a TimeSpan.
	/// </summary>
	/// <param name="value">The number of milliseconds.</param>
	/// <returns>A TimeSpan representing the specified milliseconds.</returns>
	public static TimeSpan MillisecondsToTimeSpan(int value)
	{
		return TimeSpan.FromMilliseconds(value);
	}

	/// <summary>
	/// Escapes double quotes in a string for command-line arguments.
	/// </summary>
	/// <param name="value">The string to escape.</param>
	/// <returns>The escaped string.</returns>
	public static string ArgumentEscape(string value)
	{
		for (int i = 0; i < value.Length; i++)
		{
			if (value[i] == '"')
			{
				value = value.Insert(i, "\"");
				i++;
			}
		}
		return value;
	}

	/// <summary>
	/// Adds raw parameters to a command-line argument string.
	/// </summary>
	/// <param name="arguments">The command-line arguments to append to.</param>
	/// <param name="parameters">The parameters to add.</param>
	/// <param name="switchName">The switch name to use.</param>
	/// <param name="count">Whether to include the parameter count in the switch.</param>
	public static void AddRawParameters(ref string arguments, Dictionary<string, string> parameters, string switchName, bool count)
	{
		if (parameters.Count <= 0)
		{
			return;
		}
		if (!string.IsNullOrEmpty(arguments))
		{
			arguments += " ";
		}
		arguments += switchName;
		if (count)
		{
			arguments += string.Format(CultureInfo.InvariantCulture, "[{0}]", new object[1] { parameters.Count });
		}
		foreach (KeyValuePair<string, string> parameter in parameters)
		{
			arguments += string.Format(CultureInfo.InvariantCulture, " {0}=\"{1}\"", new object[2]
			{
				parameter.Key,
				ArgumentEscape(parameter.Value)
			});
		}
	}

	/// <summary>
	/// Converts a 64-bit length to a 32-bit integer.
	/// </summary>
	/// <param name="length">The length to convert.</param>
	/// <returns>The length as a 32-bit integer.</returns>
	/// <exception cref="OverflowException">Thrown when the length is too large to fit in a 32-bit integer.</exception>
	public static int LengthTo32Bit(long length)
	{
		if (length < int.MinValue || length > int.MaxValue)
		{
			throw new OverflowException(string.Format(CultureInfo.CurrentCulture, "Size {0} cannot be represented using 32-bit value", new object[1] { length }));
		}
		return (int)length;
	}
}
