using System;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Provides data for the output data received event.
/// </summary>
[Guid("42861F26-1ECA-43BA-8A43-ADF3291D8C81")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class OutputDataReceivedEventArgs : EventArgs
{
	/// <summary>
	/// Gets the output data received.
	/// </summary>
	public string Data { get; private set; }

	/// <summary>
	/// Gets a value indicating whether the data is error output.
	/// </summary>
	public bool Error { get; private set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="OutputDataReceivedEventArgs"/> class.
	/// </summary>
	/// <param name="data">The output data.</param>
	/// <param name="error">A value indicating whether this is error output.</param>
	internal OutputDataReceivedEventArgs(string data, bool error)
	{
		Data = data;
		Error = error;
	}

	/// <summary>
	/// Returns a string representation of the output data.
	/// </summary>
	/// <returns>The output data as a string.</returns>
	public override string ToString()
	{
		return Data;
	}
}
