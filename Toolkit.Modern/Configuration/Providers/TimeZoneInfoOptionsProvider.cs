using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ByteForge.Toolkit.Configuration
{
    /// <summary>
    /// Supplies installed Windows time zones as selectable configuration values.
    /// </summary>
    public sealed class TimeZoneInfoOptionsProvider : IConfigOptionsProvider
    {
        /// <summary>
        /// Gets installed time-zone identifiers for a reflected configuration property.
        /// </summary>
        /// <param name="property">The reflected configuration property requesting options.</param>
        /// <returns>The selectable time-zone options.</returns>
        public IReadOnlyCollection<ConfigItemOption> GetOptions(PropertyInfo property)
        {
            return System.TimeZoneInfo.GetSystemTimeZones()
                .OrderBy(timeZone => timeZone.BaseUtcOffset)
                .ThenBy(timeZone => timeZone.DisplayName, System.StringComparer.OrdinalIgnoreCase)
                .Select(timeZone => new ConfigItemOption(timeZone.Id, timeZone.DisplayName))
                .ToList();
        }
    }
}
