using System.Runtime.InteropServices;

namespace ByteForge.Toolkit;
/*
 *   ___                  _     _   _ _   _ _ 
 *  / __|___ _ _  ___ ___| |___| | | | |_(_) |
 * | (__/ _ \ ' \(_-</ _ \ / -_) |_| |  _| | |
 *  \___\___/_||_/__/\___/_\___|\___/ \__|_|_|

 */

/// <summary>
/// Utility class for console-related functionalities.
/// Provides methods for formatted console output, progress visualization, and console availability detection.
/// </summary>
public static class ConsoleUtil
{
    /// <summary>
    /// Tracks the current animation frame for indeterminate progress bars.
    /// </summary>
    private static int _animationFrame = 0;


    /// <summary>
    /// Draws a horizontal line in the console using the specified character.
    /// </summary>
    /// <param name="ch">The character to use for drawing the line. Defaults to '-'.</param>
    /// <param name="length">The length of the line to draw. If 0 or negative, uses console width minus 1.</param>
    /// <remarks>
    /// This method does nothing if the console is not available.
    /// </remarks>
    public static void DrawLine(char ch = '-', int length = 0)
    {
        if (!ConsoleUtil.IsConsoleAvailable) return;
        if (length <= 0) length = Console.WindowWidth - 1;
        Console.WriteLine(new string(ch, length));
    }

    /// <summary>
    /// Centers text in the console window.
    /// </summary>
    /// <param name="text">The text to center.</param>
    /// <remarks>
    /// If the text is longer than the console width, it will be displayed without centering.
    /// This method does nothing if the console is not available.
    /// </remarks>
    public static void CenterText(string text)
    {
        if (!ConsoleUtil.IsConsoleAvailable) return;
        var wdt = Console.WindowWidth;
        if (text.Length >= wdt) { Console.WriteLine(text); return; }
        var leftPadding = (wdt - text.Length) / 2;
        Console.WriteLine(new string(' ', leftPadding) + text);
    }

    /// <summary>
    /// Right-aligns text in the console window.
    /// </summary>
    /// <param name="text">The text to right-align.</param>
    /// <remarks>
    /// If the text is longer than the console width, it will be displayed without alignment.
    /// This method does nothing if the console is not available.
    /// </remarks>
    public static void RightAlignText(string text)
    {
        if (!ConsoleUtil.IsConsoleAvailable) return;
        var wdt = Console.WindowWidth;
        if (text.Length >= wdt) { Console.WriteLine(text); return; }
        var leftPadding = wdt - text.Length - 1;
        Console.WriteLine(new string(' ', leftPadding) + text);
    }

    /// <summary>
    /// Draws a progress bar on the console.
    /// </summary>
    /// <param name="pct">The percentage of completion (0 to 100). Use negative values for indeterminate progress.</param>
    /// <param name="message">An optional message to display alongside the progress bar.</param>
    /// <remarks>
    /// This method draws a filled progress bar representing the percentage of completion.
    /// When <paramref name="pct"/> is negative, an indeterminate/animated progress bar is shown instead.
    /// The progress bar occupies approximately 30% of the console width.
    /// This method does nothing if the console is not available.
    /// </remarks>
    /// <testing>
    /// <b>Note:</b>
    /// Due to the nature of console output, automated testing of this method is not feasible.
    /// It would require capturing and validating console output, which is not practical in a unit test environment.
    /// </testing>
    public static void DrawProgressBar(double pct, string message  = "")
    {
        if (!ConsoleUtil.IsConsoleAvailable) return;

        var wdt = (int)(Console.WindowWidth * 0.3);
        string progressBar;

        if (pct < 0) // Indeterminate progress
            progressBar = BuildIndeterminateProgressBar(message, wdt);
        else // Normal percentage progress
            progressBar = BuildProgressBar(ref pct, message, wdt);

        progressBar = progressBar.Substring(0, Math.Min(progressBar.Length, Console.WindowWidth - 1));
        Console.Write(progressBar.PadRight(Console.WindowWidth - 1) + "\r");
    }

    /// <summary>
    /// Builds a determinate progress bar string based on the given percentage.
    /// </summary>
    /// <param name="pct">Reference to the percentage value, which will be clamped between 0 and 100.</param>
    /// <param name="message">Optional message to display with the progress bar.</param>
    /// <param name="wdt">Width of the progress bar in characters.</param>
    /// <returns>A formatted string representing the progress bar.</returns>
    static string BuildProgressBar(ref double pct, string message, int wdt)
    {
        pct = Math.Min(100, Math.Max(0, pct));
        var filledWidth = (int)(pct / 100f * wdt);
        return $"\r [{new string('█', filledWidth).PadRight(wdt, '░')}] {pct:##0.00}% {message}";
    }

    /// <summary>
    /// Builds an indeterminate progress bar string with an animated block.
    /// </summary>
    /// <param name="message">Optional message to display with the progress bar.</param>
    /// <param name="wdt">Width of the progress bar in characters.</param>
    /// <returns>A formatted string representing the animated progress bar.</returns>
    static string BuildIndeterminateProgressBar(string message, int wdt)
    {
        // Advance animation frame on each call
        var animFrame = _animationFrame % (wdt + 2);
        _animationFrame += (wdt / 10);

        var filled = new string('░', wdt);

        // Place a moving block of activity
        var blockSize = Math.Min(3, wdt / 4);
        var startPos = Math.Max(0, animFrame - blockSize);
        var endPos = Math.Min(wdt, animFrame);

        if (endPos > startPos)
        {
            filled = filled.Substring(0, startPos) +
                    new string('█', endPos - startPos) +
                    filled.Substring(endPos);
        }

        return $"\r [{filled}] {message}";
    }

    /// <summary>
    /// Gets a value indicating whether the console is available for input and output operations.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the console is available; otherwise, <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// This property can be used to determine if the application has access to a functional console environment.<br/>
    /// It returns <see langword="false"/> in scenarios such as when the application is running in a non-console 
    /// context  (e.g., a GUI application) or when output streams are redirected.
    /// </remarks>
    public static bool IsConsoleAvailable => (GetConsoleWindow() != IntPtr.Zero) && !Console.IsOutputRedirected && !Console.IsErrorRedirected;

    /// <summary>
    /// Retrieves the handle to the console window associated with the calling process.
    /// </summary>
    /// <returns>A handle to the console window, or <see cref="IntPtr.Zero"/> if the calling process does not have a console window.</returns>
    /// <remarks>
    /// This method is a platform invocation (P/Invoke) call to the Windows API function <c>GetConsoleWindow</c>.<br/>
    /// It is only supported on Windows operating systems.
    /// </remarks>
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();
}
