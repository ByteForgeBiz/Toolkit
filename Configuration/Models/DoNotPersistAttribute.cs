using System;

namespace ByteForge.Toolkit
{
    /// <summary>
    /// An attribute to indicate that a property should not be persisted.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DoNotPersistAttribute : Attribute { }
}