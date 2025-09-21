using Newtonsoft.Json;

namespace ByteForge.Toolkit.Json
{
    /// <summary>
    /// Provides utility methods for serializing objects by comparing them to default objects
    /// and only including properties that have different values.
    /// </summary>
    public static class JsonDeltaSerializer
    {
        /// <summary>
        /// Serializes an object to JSON, including only properties that have different values
        /// compared to the provided default object.
        /// </summary>
        /// <typeparam name="T">The type of objects being compared and serialized.</typeparam>
        /// <param name="currentObject">The object to serialize.</param>
        /// <param name="defaultObject">The default object to compare against.</param>
        /// <returns>A JSON string containing only the properties that differ from the default object.</returns>
        public static string SerializeDelta<T>(T currentObject, T defaultObject)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DeltaContractResolver(defaultObject),
                NullValueHandling = NullValueHandling.Include
            };

            return JsonConvert.SerializeObject(currentObject, settings);
        }
    }
}