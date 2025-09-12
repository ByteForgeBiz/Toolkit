using System;
using System.Runtime.InteropServices;

namespace ByteForge.Toolkit
{
    /*
     *   ___                  _     _   _ _   _ _ 
     *  / __|___ _ _  ___ ___| |___| | | | |_(_) |
     * | (__/ _ \ ' \(_-</ _ \ / -_) |_| |  _| | |
     *  \___\___/_||_/__/\___/_\___|\___/ \__|_|_|
     *                                            
     */

    /// <summary>
    /// Utility class for console-related functionalities.
    /// </summary>
    public static class ConsoleUtil
    {
        /// <summary>
        /// Draws a progress bar on the console.
        /// </summary>
        /// <param name="pct">The percentage of completion (0 to 100).</param>
        /// <param name="message">An optional message to display alongside the progress bar.</param>
        /// <testing>
        /// <b>Note:</b>
        /// Due to the nature of console output, automated testing of this method is not feasible.
        /// It would require capturing and validating console output, which is not practical in a unit test environment.
        /// </testing>
        public static void DrawProgressBar(float pct, string message = null)
        {
            if (!ConsoleUtil.HasConsole() || Console.IsOutputRedirected) return;

            pct = Math.Min(100, Math.Max(0, pct));
            var wdt = (int)(Console.WindowWidth * 0.3);
            var pgb = "\r [" + new string('█', (int)(pct / 100f * wdt)).PadRight(wdt, '░') + $"] {pct: ##0.00}% {message}";
            pgb = pgb.Substring(0, Math.Min(pgb.Length, Console.WindowWidth - 1));
            Console.Write(pgb.PadRight(Console.WindowWidth - 1) + "\r");
        }

        /// <summary>
        /// Determines whether the current process is associated with a console window.
        /// </summary>
        /// <returns><see langword="true"/> if the current process has an associated console window; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// This method checks if the current process is attached to a console window by verifying the 
        /// presence of a console handle.<br/>
        /// It can be useful for determining whether console-specific operations, such as writing to 
        /// standard output, are applicable.
        /// </remarks>
        public static bool HasConsole()
        {
            return GetConsoleWindow() != IntPtr.Zero;
        }

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
}