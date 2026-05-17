using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ByteForge.Toolkit.Configuration
{
    /// <summary>
    /// Supplies installed text encodings as selectable configuration values.
    /// </summary>
    public sealed class EncodingOptionsProvider : IConfigOptionsProvider
    {
        /// <summary>
        /// Gets installed text encodings for a reflected configuration property.
        /// </summary>
        /// <param name="property">The reflected configuration property requesting options.</param>
        /// <returns>The selectable encoding options.</returns>
        public IReadOnlyCollection<ConfigItemOption> GetOptions(PropertyInfo property)
        {
            return Encoding.GetEncodings()
                .Select(info => info.GetEncoding())
                .OrderBy(encoding => encoding.WebName, System.StringComparer.OrdinalIgnoreCase)
                .Select(encoding => new ConfigItemOption(encoding.WebName, GetEncodingLabel(encoding)))
                .ToList();
        }

        /// <summary>
        /// Gets the display label for an encoding option.
        /// </summary>
        /// <param name="encoding">The encoding to label.</param>
        /// <returns>The encoding option label.</returns>
        private static string GetEncodingLabel(Encoding encoding)
        {
            return $"{encoding.WebName} - {encoding.EncodingName}";
        }
    }
}
