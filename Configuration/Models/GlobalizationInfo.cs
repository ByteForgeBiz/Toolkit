using System.Globalization;

namespace ByteForge.Toolkit
{
    /// <summary>
    /// Provides globalization information such as culture, date format, and time format.
    /// </summary>
    public class GlobalizationInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalizationInfo"/> class.
        /// </summary>
        public GlobalizationInfo() { }

        /// <summary>
        /// Gets or sets the culture information.
        /// </summary>
        public CultureInfo CultureInfo { get; set; } = new CultureInfo("en-US");

        /// <summary>
        /// Gets or sets the date format string.
        /// </summary>
        public string DateFormat { get; set; } = "MM'/'dd'/'yyyy";

        /// <summary>
        /// Gets or sets the time format string.
        /// </summary>
        public string TimeFormat { get; set; } = "HH':'mm':'ss";

        /// <summary>
        /// Gets the combined date and time format string.
        /// </summary>
        public string DateTimeFormat => DateFormat + " " + TimeFormat;
    }
}
