using System;
using System.Reflection;

namespace ByteForge.Toolkit
{
    /// <summary>
    /// Provides utility methods for resolving the effective (non-nullable) type of a given property or type.
    /// </summary>
    /// <remarks>
    /// This class includes methods to handle nullable types, unwrapping them to their underlying types when
    /// applicable. It is designed to simplify type resolution in scenarios where nullable types may be
    /// encountered.
    /// </remarks>
    public static class TypeHelper
    {
        /// <summary>
        /// Resolves the effective (non-nullable) type represented by the provided property.
        /// </summary>
        /// <param name="prop">The property whose type is to be resolved.</param>
        /// <returns>The resolved non-nullable <see cref="Type"/>.</returns>
        public static Type ResolveType(PropertyInfo prop) => ResolveType(prop.PropertyType);

        /// <summary>
        /// Resolves the effective (non-nullable) type represented by the provided method parameter.
        /// </summary>
        /// <param name="param">The parameter whose type is to be resolved.</param>
        /// <returns>The resolved non-nullable <see cref="Type"/>.</returns>
        /// <seealso cref="ResolveType(Type)"/>
        public static Type ResolveType(ParameterInfo param) => ResolveType(param.ParameterType);

        /// <summary>
        /// Resolves the effective (non-nullable) type represented by the provided field.
        /// </summary>
        /// <param name="field">The field whose type is to be resolved.</param>
        /// <returns>The resolved non-nullable <see cref="Type"/>.</returns>
        /// <seealso cref="ResolveType(Type)"/>
        public static Type ResolveType(FieldInfo field) => ResolveType(field.FieldType);

        /// <summary>
        /// Resolves the effective (non-nullable) delegate type associated with the provided event.
        /// </summary>
        /// <param name="evt">The event whose handler (delegate) type is to be resolved.</param>
        /// <returns>The resolved non-nullable <see cref="Type"/> of the event handler.</returns>
        /// <seealso cref="ResolveType(Type)"/>
        public static Type ResolveType(EventInfo evt) => ResolveType(evt.EventHandlerType);

        /// <summary>
        /// Resolves the effective (non-nullable) return type for the supplied method.
        /// </summary>
        /// <param name="method">The method whose return type is to be resolved.</param>
        /// <returns>The resolved non-nullable <see cref="Type"/> representing the method's return type.</returns>
        /// <seealso cref="ResolveType(Type)"/>
        public static Type ResolveType(MethodInfo method) => ResolveType(method.ReturnType);

        /// <summary>
        /// Resolves the effective (non-nullable) type for the supplied <paramref name="type"/>.
        /// If the type is a nullable generic (e.g., <c>Nullable&lt;T&gt;</c>) its underlying type is returned.
        /// </summary>
        /// <param name="type">A type that may represent a nullable wrapper.</param>
        /// <returns>
        /// The unwrapped non-nullable <see cref="Type"/>, or the original type if no unwrapping is required;
        /// <c>null</c> if input was <c>null</c>.
        /// </returns>
        public static Type ResolveType(Type type) => Nullable.GetUnderlyingType(type) ?? type;

        /// <summary>
        /// Returns the default value for the specified type.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> for which to retrieve the default value. Must not be <see langword="null"/>.</param>
        /// <returns>An instance of the specified type initialized to its default value if it is a value type;  otherwise, <see langword="null"/> for reference types.</returns>
        public static object GetDefault(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}
