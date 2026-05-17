using System;

namespace ByteForge.Toolkit
{
    /// <summary>
    /// Represents an attribute that specifies the developer company for an assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class AssemblyDeveloperCompanyAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the company.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyDeveloperCompanyAttribute"/> class with the specified company name.
        /// </summary>
        /// <param name="companyName">The name of the company.</param>
        public AssemblyDeveloperCompanyAttribute(string companyName)
        {
            Name = companyName;
        }
    }

    /// <summary>
    /// Represents an attribute that specifies the developer for an assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class AssemblyDeveloperAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the developer.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyDeveloperAttribute"/> class with the specified developer name.
        /// </summary>
        /// <param name="developerName">The name of the developer.</param>
        public AssemblyDeveloperAttribute(string developerName)
        {
            Name = developerName;
        }
    }

    /// <summary>
    /// Represents an attribute that specifies the company URL for an assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class AssemblyCompanyUrlAttribute : Attribute
    {
        /// <summary>
        /// Gets the URL of the company.
        /// </summary>
        public string Url { get; }
        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyCompanyUrlAttribute"/> class with the specified company URL.
        /// </summary>
        /// <param name="companyUrl">The URL of the company.</param>
        public AssemblyCompanyUrlAttribute(string companyUrl)
        {
            Url = companyUrl;
        }
    }

    /// <summary>
    /// Represents an attribute that specifies the developer company URL for an assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class AssemblyDeveloperCompanyUrlAttribute : Attribute
    {
        /// <summary>
        /// Gets the URL of the developer company.
        /// </summary>
        public string Url { get; }
        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyDeveloperCompanyUrlAttribute"/> class with the specified developer company URL.
        /// </summary>
        /// <param name="developerUrl">The URL of the developer company.</param>
        public AssemblyDeveloperCompanyUrlAttribute(string developerUrl)
        {
            Url = developerUrl;
        }
    }
}
