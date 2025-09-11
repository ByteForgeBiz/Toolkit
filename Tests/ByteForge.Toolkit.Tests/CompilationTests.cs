using System;
using System.IO;
using System.Reflection;
using FluentAssertions;

namespace ByteForge.Toolkit.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("Compilation")]
    public class CompilationTests
    {
        [TestMethod]
        public void ByteForgeToolkitAssembly_ShouldLoadSuccessfully()
        {
            // Arrange & Act
            Assembly assembly = null;
            var loading = () => assembly = Assembly.LoadFrom(GetByteForgeToolkitPath());

            // Assert
            loading.Should().NotThrow("ByteForge.Toolkit assembly should load without errors");
            assembly.Should().NotBeNull();
        }

        [TestMethod]
        public void StringUtil_ShouldBeAccessible()
        {
            // Arrange & Act
            var type = typeof(StringUtil);

            // Assert
            type.Should().NotBeNull();
            type.Namespace.Should().Be("ByteForge.Toolkit");
        }

        [TestMethod]
        public void BinarySearchTree_ShouldBeAccessible()
        {
            // Arrange & Act
            var type = typeof(BinarySearchTree<int>);

            // Assert
            type.Should().NotBeNull();
            type.Namespace.Should().Be("ByteForge.Toolkit");
        }

        [TestMethod]
        public void Encryptor_ShouldBeAccessible()
        {
            // Arrange & Act
            var type = typeof(Encryptor);

            // Assert
            type.Should().NotBeNull();
            type.Namespace.Should().Be("ByteForge.Toolkit");
        }

        [TestMethod]
        public void CSVReader_ShouldBeAccessible()
        {
            // Arrange & Act
            var type = typeof(CSVReader);

            // Assert
            type.Should().NotBeNull();
            type.Namespace.Should().Be("ByteForge.Toolkit");
        }

        [TestMethod]
        public void Log_ShouldBeAccessible()
        {
            // Arrange & Act
            var type = typeof(Log);

            // Assert
            type.Should().NotBeNull();
            type.Namespace.Should().Be("ByteForge.Toolkit");
        }

        private string GetByteForgeToolkitPath()
        {
            // Try to find the ByteForge.Toolkit.dll in common locations
            var locations = new[]
            {
                @"..\..\bin\Debug\ByteForge.Toolkit.dll",
                @"..\..\bin\Release\ByteForge.Toolkit.dll",
                @"..\..\..\bin\Debug\ByteForge.Toolkit.dll",
                @"..\..\..\bin\Release\ByteForge.Toolkit.dll"
            };

            foreach (var location in locations)
            {
                var fullPath = Path.GetFullPath(location);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            // Fallback - try current directory
            return "ByteForge.Toolkit.dll";
        }
    }
}