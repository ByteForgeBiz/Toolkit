namespace ByteForge.WinSCP;

/// <summary>
/// Represents the method that will handle the query received event.
/// </summary>
/// <param name="sender">The source of the event.</param>
/// <param name="e">A <see cref="QueryReceivedEventArgs"/> that contains the event data.</param>
public delegate void QueryReceivedEventHandler(object sender, QueryReceivedEventArgs e);
