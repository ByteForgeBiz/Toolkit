using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace ByteForge.Toolkit.Json;
/// <summary>
/// A custom JSON contract resolver that only serializes properties that have different values
/// compared to a default object.
/// </summary>
public class DeltaContractResolver : DefaultContractResolver
{
    /// <summary>
    /// The default object used as a baseline for comparison.
    /// </summary>
    private readonly object _defaultObject;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeltaContractResolver"/> class.
    /// </summary>
    /// <param name="defaultObject">The default object to compare against when determining which properties to serialize.</param>
    public DeltaContractResolver(object defaultObject)
    {
        _defaultObject = defaultObject;
    }

    /// <summary>
    /// Creates a <see cref="JsonProperty"/> for the given <see cref="MemberInfo"/>.
    /// </summary>
    /// <param name="member">The member to create a <see cref="JsonProperty"/> for.</param>
    /// <param name="memberSerialization">The member serialization mode.</param>
    /// <returns>A <see cref="JsonProperty"/> configured to only serialize when the value differs from the default object.</returns>
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);

        property.ShouldSerialize = instance =>
        {
            try
            {
                var currentValue = property.ValueProvider?.GetValue(instance);
                var defaultValue = property.ValueProvider?.GetValue(_defaultObject);

                return !Equals(currentValue, defaultValue);
            }
            catch
            {
                return true; // If comparison fails, include the property
            }
        };

        return property;
    }
}
