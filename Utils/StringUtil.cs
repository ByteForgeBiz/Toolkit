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

    }
}
