namespace ByteForge.Toolkit.Logging;

/*
 *  _                   _           ___                    ___         _           _   
 * | |   ___  __ _ __ _(_)_ _  __ _/ __| __ ___ _ __  ___ / __|___ _ _| |_ _____ _| |_ 
 * | |__/ _ \/ _` / _` | | ' \/ _` \__ \/ _/ _ \ '_ \/ -_) (__/ _ \ ' \  _/ -_) \ /  _|
 * |____\___/\__, \__, |_|_||_\__, |___/\__\___/ .__/\___|\___\___/_||_\__\___/_\_\\__|
 *           |___/|___/       |___/            |_|                                     
 */
/// <summary>
/// Maintains ambient logging execution state for the current async flow.
/// </summary>
internal static class LoggingScopeContext
{
    private static readonly AsyncLocal<int> _suppressionDepth = new AsyncLocal<int>();

    /// <summary>
    /// Gets a value indicating whether logging is suppressed for the current async flow.
    /// </summary>
    internal static bool IsSuppressed => _suppressionDepth.Value > 0;

    /// <summary>
    /// Begins a suppression scope for static log dispatch.
    /// </summary>
    /// <returns>A disposable that restores the previous suppression depth.</returns>
    internal static IDisposable Suppress()
    {
        _suppressionDepth.Value++;
        return new SuppressionScope();
    }

    private sealed class SuppressionScope : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _suppressionDepth.Value = Math.Max(0, _suppressionDepth.Value - 1);
            _disposed = true;
        }
    }
}
