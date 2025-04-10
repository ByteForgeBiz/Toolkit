using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ByteForge.Toolkit
{
    /*
     *  _   _ _   _ _    
     * | | | | |_(_) |___
     * | |_| |  _| | (_-<
     *  \___/ \__|_|_/__/
     *                   
     */
    /// <summary>
    /// Contains utility methods.
    /// </summary>
    public static class Utils
    {

        /// <summary>
        /// Represents the carriage return and line feed character array.
        /// </summary>
        public static readonly char[] arrCRLF = new char[] { '\r', '\n' };

        /// <summary>
        /// Formats a US phone number to (XXX) XXX-XXXX.
        /// </summary>
        /// <param name="phoneNumber">The phone number to format.</param>
        /// <returns>The formatted phone number.</returns>
        public static string FormatUSPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return string.Empty;

            // Remove all non-numeric characters
            var digits = Regex.Replace(phoneNumber, @"\D", "");

            if (digits.Length == 11 && digits[0] == '1')
                digits = digits.Substring(1);

            // Ensure the phone number has 10 digits
            if (digits.Length != 10)
                return phoneNumber; // Return the original if it doesn't have 10 digits

            // Format the phone number
            return Regex.Replace(digits, @"(\d{3})(\d{3})(\d{4})", "($1) $2-$3");
        }

        /// <summary>
        /// Returns null if the provided string is null or empty; otherwise, returns the string itself.
        /// </summary>
        /// <param name="value">The string to check.</param>
        /// <returns>Null if the string is null or empty; otherwise, the string itself.</returns>
        public static string NullIfEmpty(string value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }

        /// <summary>
        /// Removes all occurrences of a specified string from the current string.
        /// </summary>
        /// <param name="text">The string to remove from.</param>
        /// <param name="value">The string to remove.</param>
        /// <returns>A new string with all occurrences of the specified string removed.</returns>
        public static string Remove(this string text, string value) => text.Replace(value, string.Empty);

        /// <summary>
        /// Removes all occurrences of a specified character from the current string.
        /// </summary>
        /// <param name="text">The string to remove from.</param>
        /// <param name="value">The character to remove.</param>
        /// <returns>A new string with all occurrences of the specified character removed.</returns>
        public static string Remove(this string text, char value) => text.Remove(value.ToString());

        /// <summary>
        /// Removes all occurrences of the specified characters from the current string.
        /// </summary>
        /// <param name="text">The string to remove from.</param>
        /// <param name="values">The array of characters to remove.</param>
        /// <returns>A new string with all occurrences of the specified characters removed.</returns>
        public static string Remove(this string text, char[] values)
        {
            foreach (var value in values)
                text = text.Remove(value);
            return text;
        }

        /// <summary>
        /// Runs an asynchronous function synchronously.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="asyncFunc">The asynchronous function to run.</param>
        /// <returns>The result of the asynchronous function.</returns>
        public static T RunSync<T>(Func<CancellationToken, Task<T>> asyncFunc)
        {
            var task = Task.Run(() => asyncFunc(CancellationToken.None));
            task.Wait();
            return task.Result;
        }
    }
}