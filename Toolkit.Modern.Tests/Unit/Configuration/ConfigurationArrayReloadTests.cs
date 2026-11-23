using System;
using System.Collections.Generic;
using System.IO;
using ByteForge.Toolkit.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ConfigManager = ByteForge.Toolkit.Configuration.Configuration;

namespace ByteForge.Toolkit.Tests.Unit.Configuration
{
    /// <summary>Checks direct in-memory assignments and explicit INI reloads.</summary>
    [TestClass]
[TestCategory("Unit")]
    public class ConfigurationArrayReloadTests
    {
        /// <summary>Assignments leave other properties and the INI untouched until Save.</summary>
        [TestMethod]
        public void AssignmentDoesNotReloadOtherProperties()
        {
            var path = Path.Combine(Path.GetTempPath(), "array-assignment-" + Guid.NewGuid() + ".ini");
            try
            {
                var original = "[Owner]\nValues=Items\nCount=1\nRoutes=Mappings\n[Items]\n0000=first\n0001=second\n[Mappings]\nold=value\n";
                File.WriteAllText(path, original);
                IConfigurationManager configuration = new ConfigManager();
                configuration.Initialize(path);
                var settings = configuration.GetSection<ArraySettings>("Owner");
                settings.Count = 99;
                configuration.SetValue("Owner", "Values", new[] { "replacement", "added" });
                Assert.AreEqual(99, settings.Count);
                var assignedArray = settings.Values;
                configuration.SetValue("Owner", "Count", "7");
                Assert.AreSame(assignedArray, settings.Values);
                configuration.SetValue("Owner", "Routes", new Dictionary<string, string> { { "new", "route" } });
                Assert.AreSame(assignedArray, settings.Values);
                Assert.AreEqual(original, File.ReadAllText(path));
                Assert.AreEqual("first", configuration.GetSectionValues("Items")["0000"]);
                configuration.Save();
                configuration = new ConfigManager();
                configuration.Initialize(path);
                settings = configuration.GetSection<ArraySettings>("Owner");
                CollectionAssert.AreEqual(new[] { "replacement", "added" }, settings.Values);
                Assert.AreEqual(7, settings.Count);
                Assert.AreEqual("route", settings.Routes["new"]);
                Assert.IsFalse(settings.Routes.ContainsKey("old"));
                configuration.SetValue("Owner", "Values", new string[0]);
                configuration.SetValue("Owner", "Routes", new Dictionary<string, string>());
                configuration.Save();
                configuration = new ConfigManager();
                configuration.Initialize(path);
                settings = configuration.GetSection<ArraySettings>("Owner");
                Assert.AreEqual(0, settings.Values.Length);
                Assert.AreEqual(0, settings.Routes.Count);
                File.WriteAllText(path, original);
                configuration.Reload();
                settings = configuration.GetSection<ArraySettings>("Owner");
                CollectionAssert.AreEqual(new[] { "first", "second" }, settings.Values);
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }

        /// <summary>Minimal owner for isolated configuration checks.</summary>
        public class ArraySettings
        {
            /// <summary>Gets or sets array values.</summary>
            [ByteForge.Toolkit.Configuration.Array]
            public string[] Values { get; set; }
            /// <summary>Gets or sets a scalar value.</summary>
            public int Count { get; set; }
            /// <summary>Gets or sets mapping values.</summary>
            [ByteForge.Toolkit.Configuration.Dictionary]
            public Dictionary<string, string> Routes { get; set; }
        }
    }
}
