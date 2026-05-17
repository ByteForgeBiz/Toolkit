using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace ByteForge.Toolkit.Configuration
{
    /// <summary>
    /// Supplies installed .NET cultures as selectable configuration values.
    /// </summary>
    public sealed class CultureInfoOptionsProvider : IConfigOptionsProvider
    {
        /// <summary>
        /// Gets installed culture names for a reflected configuration property.
        /// </summary>
        /// <param name="property">The reflected configuration property requesting options.</param>
        /// <returns>The selectable culture options.</returns>
        public IReadOnlyCollection<ConfigItemOption> GetOptions(PropertyInfo property)
        {
            var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures)
                .OrderBy(culture => culture.Name, System.StringComparer.OrdinalIgnoreCase)
                .Select(culture => new ConfigItemOption(culture.Name, GetCultureLabel(culture)))
                .ToList();

            return cultures;
        }

        /// <summary>
        /// Gets the display label for a culture option.
        /// </summary>
        /// <param name="culture">The culture to label.</param>
        /// <returns>The culture option label.</returns>
        private static string GetCultureLabel(CultureInfo culture)
        {
            return string.IsNullOrEmpty(culture.Name)
                ? "Invariant Culture"
                : $"{culture.Name} - {culture.EnglishName}";
        }
    }
}
