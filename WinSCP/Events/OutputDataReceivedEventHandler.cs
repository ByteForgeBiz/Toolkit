namespace ByteForge.WinSCP;

/// <summary>
/// Represents the method that will handle the output data received event.
/// </summary>
/// <param name="sender">The source of the event.</param>
/// <param name="e">An <see cref="OutputDataReceivedEventArgs"/> that contains the event data.</param>
public delegate void OutputDataReceivedEventHandler(object sender, OutputDataReceivedEventArgs e);
