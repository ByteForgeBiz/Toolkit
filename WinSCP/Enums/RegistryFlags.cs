namespace ByteForge.WinSCP;

/// <summary>
/// Specifies flags for registry operations.
/// </summary>
internal enum RegistryFlags
{
 /// <summary>
 /// Indicates a string value in the registry.
 /// </summary>
 RegSz =2,
 /// <summary>
 /// Indicates a subkey for WOW6432 (32-bit) registry view.
 /// </summary>
 SubKeyWow6432Key =0x20000
}
