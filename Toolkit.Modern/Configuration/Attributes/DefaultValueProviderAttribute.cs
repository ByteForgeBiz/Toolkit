using System;

namespace ByteForge.Toolkit;
/*
 *  ___       __           _ _ __   __    _          ___             _    _          _  _   _       _ _         _       
 * |   \ ___ / _|__ _ _  _| | |\ \ / /_ _| |_  _ ___| _ \_ _ _____ _(_)__| |___ _ _ /_\| |_| |_ _ _(_) |__ _  _| |_ ___ 
 * | |) / -_)  _/ _` | || | |  _\ V / _` | | || / -_)  _/ '_/ _ \ V / / _` / -_) '_/ _ \  _|  _| '_| | '_ \ || |  _/ -_)
 * |___/\___|_| \__,_|\_,_|_|\__|\_/\__,_|_|\_,_\___|_| |_| \___/\_/|_\__,_\___|_|/_/ \_\__|\__|_| |_|_.__/\_,_|\__\___|
 *                                                                                                                      
 */
/// <summary>
/// Specifies a method that provides the default value for a property.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public class DefaultValueProviderAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultValueProviderAttribute"/> class.
    /// </summary>
    /// <param name="providerType">The type that contains the static method providing the default value.</param>
    /// <param name="methodName">The name of the static method that provides the default value.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="providerType"/> or <paramref name="methodName"/> is <c>null</c>.
    /// </exception>
    public DefaultValueProviderAttribute(Type providerType, string methodName)
    {
        ProviderType = providerType ?? throw new ArgumentNullException(nameof(providerType));
        MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
    }

    /// <summary>
    /// Gets the type that contains the static method providing the default value.
    /// </summary>
    public Type ProviderType { get; }

    /// <summary>
    /// Gets the name of the static method that provides the default value.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// Invokes the specified static method to obtain the default value.
    /// </summary>
    /// <returns>The default value returned by the provider method.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the method cannot be found, is not static, or has parameters.
    /// </exception>
    public object? GetDefaultValue()
    {
        var method = ProviderType.GetMethod(MethodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                        ?? throw new InvalidOperationException($"Method '{MethodName}' not found in type '{ProviderType.FullName}'.");

        if (method.GetParameters().Length != 0)
            throw new InvalidOperationException($"Method '{MethodName}' in type '{ProviderType.FullName}' must have no parameters.");

        return method.Invoke(null, null);
    }
}
