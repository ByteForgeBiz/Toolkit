using System.Collections.Generic;
using System.Reflection;

namespace ByteForge.Toolkit.Configuration
{
    /// <summary>
    /// Supplies selectable editor values for a strongly typed configuration property.
    /// </summary>
    public interface IConfigOptionsProvider
    {
        /// <summary>
        /// Gets the selectable values for a reflected configuration property.
        /// </summary>
        /// <param name="property">The reflected configuration property requesting options.</param>
        /// <returns>The selectable configuration options.</returns>
        IReadOnlyCollection<ConfigItemOption> GetOptions(PropertyInfo property);
    }
}
