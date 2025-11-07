using System;

namespace ByteForge.WinSCP;

/// <summary>
/// Specifies access rights for file mapping operations.
/// </summary>
[Flags]
internal enum FileMapAccess
{
 /// <summary>
 /// Allows copy-on-write access to the file mapping.
 /// </summary>
 FileMapCopy =1,
 /// <summary>
 /// Allows write access to the file mapping.
 /// </summary>
 FileMapWrite =2,
 /// <summary>
 /// Allows read access to the file mapping.
 /// </summary>
 FileMapRead =4,
 /// <summary>
 /// Allows all possible access rights to the file mapping.
 /// </summary>
 FileMapAllAccess =0x1F,
 /// <summary>
 /// Allows execute access to the file mapping.
 /// </summary>
 FileMapExecute =0x20
}
