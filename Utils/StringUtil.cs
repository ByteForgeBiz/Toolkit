using System.Collections.Generic;

namespace ByteForge.Toolkit
{
    /// <summary>
    /// Provides utility methods for working with strings.
    /// </summary>
    public static class StringUtil
    {
        /// <summary>
        /// Escapes a string for safe use in JavaScript.
        /// </summary>
        /// <param name="text">The text to escape.</param>
        /// <returns>The escaped text.</returns>
        public static string EscapeForJavaScript(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            return text
                .Replace("\\", "\\\\")  // Escape backslashes first
                .Replace("'", "\\'")    // Escape single quotes  
                .Replace("\"", "\\\"")  // Escape double quotes
                .Replace("\r", "\\r")   // Escape carriage returns
                .Replace("\n", "\\n")   // Escape newlines
                .Replace("\t", "\\t")   // Escape tabs
                .Replace("\b", "\\b")   // Escape backspace
                .Replace("\f", "\\f");  // Escape form feed
        }

        /// <summary>
        /// Wraps a comma-separated list of items to a specified maximum line length,
        /// ensuring no individual items are split across lines.
        /// </summary>
        /// <param name="items">The collection of items to join and wrap.</param>
        /// <param name="separator">The separator to use between items (default is ", ").</param>
        /// <param name="maxLineLength">The maximum length of each line (default is 200).</param>
        /// <returns>A string with the items joined and wrapped at appropriate points.</returns>
        public static string WrapList<T>(IEnumerable<T> items, string separator = ", ", int maxLineLength = 200)
        {
            if (items == null)
                return string.Empty;

            var result = new System.Text.StringBuilder();
            var currentLine = new System.Text.StringBuilder();
            var isFirstItem = true;

            foreach (var item in items)
            {
                var itemString = item.ToString();

                // Skip empty items
                if (string.IsNullOrEmpty(itemString))
                    continue;

                // Calculate what would be added to the current line
                var textToAdd = isFirstItem ? itemString : separator + itemString;

                // If adding this item would exceed the line length and it's not the first item on the line
                if (!isFirstItem && (currentLine.Length + textToAdd.Length > maxLineLength))
                {
                    // Add the current line to the result and start a new line
                    result.AppendLine(currentLine.ToString());
                    currentLine.Clear();
                    currentLine.Append(itemString);
                    isFirstItem = false;
                }
                else
                {
                    // Add the item to the current line
                    currentLine.Append(textToAdd);
                    isFirstItem = false;
                }
            }

            // Add the last line if it has content
            if (currentLine.Length > 0)
                result.Append(currentLine.ToString());

            return result.ToString();
        }
    }
}
