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
    /// Contains utility methods for string manipulation, date/time conversion, and task execution.
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Represents the carriage return and line feed character array.
        /// </summary>
        public static readonly char[] arrCRLF = new char[] { '\r', '\n' };

        /// <summary>
        /// Formats a US phone number to the format (XXX) XXX-XXXX.
        /// Converts letters to their corresponding phone keypad numbers (A/B/C=2, D/E/F=3, etc.).
        /// </summary>
        /// <param name="phoneNumber">The phone number to format.</param>
        /// <returns>The formatted phone number, or the original input if it cannot be formatted.</returns>
        public static string FormatUSPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return string.Empty;

            // Convert letters to numbers based on phone keypad mapping
            var normalizedPhone = ConvertPhoneLettersToNumbers(phoneNumber);

            // Remove all non-numeric characters
            var digits = Regex.Replace(normalizedPhone, @"\D", "");

            if (digits.Length == 11 && digits[0] == '1')
                digits = digits.Substring(1);

            // Ensure the phone number has 10 digits
            if (digits.Length != 10)
                return phoneNumber; // Return the original if it doesn't have 10 digits

            // Format the phone number
            return Regex.Replace(digits, @"(\d{3})(\d{3})(\d{4})", "($1) $2-$3");
        }

        /// <summary>
        /// Converts letters in a phone number to their corresponding numbers based on the phone keypad.
        /// A/B/C=2, D/E/F=3, G/H/I=4, J/K/L=5, M/N/O=6, P/Q/R/S=7, T/U/V=8, W/X/Y/Z=9.
        /// </summary>
        /// <param name="phoneNumber">The phone number that may contain letters.</param>
        /// <returns>The phone number with letters converted to numbers.</returns>
        private static string ConvertPhoneLettersToNumbers(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return phoneNumber;

            var result = phoneNumber.ToUpperInvariant();
            
            // Phone keypad letter mappings
            result = result.Replace('A', '2').Replace('B', '2').Replace('C', '2');
            result = result.Replace('D', '3').Replace('E', '3').Replace('F', '3');
            result = result.Replace('G', '4').Replace('H', '4').Replace('I', '4');
            result = result.Replace('J', '5').Replace('K', '5').Replace('L', '5');
            result = result.Replace('M', '6').Replace('N', '6').Replace('O', '6');
            result = result.Replace('P', '7').Replace('Q', '7').Replace('R', '7').Replace('S', '7');
            result = result.Replace('T', '8').Replace('U', '8').Replace('V', '8');
            result = result.Replace('W', '9').Replace('X', '9').Replace('Y', '9').Replace('Z', '9');

            return result;
        }

        /// <summary>
        /// Returns <c>null</c> if the provided string is null, empty, or whitespace; otherwise, returns the string itself.
        /// </summary>
        /// <param name="value">The string to check.</param>
        /// <returns><c>null</c> if the string is null, empty, or whitespace; otherwise, the string itself.</returns>
        public static string NullIfEmpty(string value)
        {
            return string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value) ? null : value;
        }

        /// <summary>
        /// Runs an asynchronous function synchronously and returns its result.
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