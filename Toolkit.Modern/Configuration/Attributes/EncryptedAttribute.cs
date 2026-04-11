using ByteForge.Toolkit.Security;

namespace ByteForge.Toolkit.Configuration;

/// <summary>
/// Marks a configuration property as encrypted.
/// The configuration system will automatically decrypt the value when loading
/// and encrypt it when saving, using <see cref="Encryptor.Default"/>.
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class EncryptedAttribute : System.Attribute
{
}
