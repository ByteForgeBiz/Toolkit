using System;

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
        /// The width of the progress bar.
        /// </summary>
        const int wdt = 50;

        /// <summary>
        /// Draws a progress bar on the console.
        /// </summary>
        /// <param name="pct">The percentage of completion (0 to 100).</param>
        /// <param name="message">An optional message to display alongside the progress bar.</param>
        public static void DrawProgressBar(float pct, string message = null)
        {
            var pgb = "\r[" + new string('█', (int)(pct / 100f * wdt)).PadRight(wdt, '░') + $"] {pct: ##0.0}% {message}";
            if (pgb.Length > Console.WindowWidth)
                Console.Write(pgb.Substring(0, Console.WindowWidth + 1) + "\r\r");
            else
                Console.Write(pgb.PadRight(Console.WindowWidth));
        }
    }
}