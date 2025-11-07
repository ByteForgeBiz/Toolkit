namespace ByteForge.WinSCP;

/// <summary>
/// Represents the method that will handle the failed event.
/// </summary>
/// <param name="sender">The source of the event.</param>
/// <param name="e">A <see cref="FailedEventArgs"/> that contains the event data.</param>
public delegate void FailedEventHandler(object sender, FailedEventArgs e);
