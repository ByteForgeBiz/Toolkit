using AwesomeAssertions;
using ByteForge.Toolkit.Tests.Helpers;
using System.Globalization;

namespace ByteForge.Toolkit.Tests.Unit.Utils
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("Utils")]
    public class ParserTests
    {
        /// <summary>
        /// Test enumeration for enum parsing tests.
        /// </summary>
        public enum TestEnum
        {
            Value1,
            Value2,
            Value3
        }

        /// <summary>
        /// Verifies that the default instance is properly initialized.
        /// </summary>
        /// <remarks>
        /// Ensures singleton pattern works correctly for default instance.
        /// </remarks>
        [TestMethod]
        public void Default_ShouldReturnSameInstance()
        {
            // Act
            var instance1 = Parser.Default;
            var instance2 = Parser.Default;

            // Assert
            instance1.Should().NotBeNull();
            instance2.Should().NotBeNull();
            instance1.Should().BeSameAs(instance2);
        }

        /// <summary>
        /// Verifies that Parse correctly parses primitive types.
        /// </summary>
        /// <remarks>
        /// Ensures all built-in primitive types are supported.
        /// </remarks>
        [TestMethod]
        public void Parse_PrimitiveTypes_ShouldParseCorrectly()
        {
            // Arrange
            IParser parser = new Parser();

            // Act & Assert
            parser.Parse(typeof(int), "42").Should().Be(42);
            parser.Parse(typeof(long), "9223372036854775807").Should().Be(9223372036854775807L);
            parser.Parse(typeof(short), "32767").Should().Be((short)32767);
            parser.Parse(typeof(uint), "42").Should().Be(42U);
            parser.Parse(typeof(ulong), "42").Should().Be(42UL);
            parser.Parse(typeof(ushort), "42").Should().Be((ushort)42);
            parser.Parse(typeof(byte), "255").Should().Be((byte)255);
            parser.Parse(typeof(double), "3.14159").Should().Be(3.14159);
            parser.Parse(typeof(float), "3.14").Should().Be(3.14f);
            parser.Parse(typeof(decimal), "123.45").Should().Be(123.45m);
            parser.Parse(typeof(bool), "true").Should().Be(true);
            parser.Parse(typeof(char), "A").Should().Be('A');
            parser.Parse(typeof(string), "Hello").Should().Be("Hello");
        }

        /// <summary>
        /// Verifies that Parse correctly parses complex types.
        /// </summary>
        /// <remarks>
        /// Ensures complex built-in types like DateTime, Guid, etc. are supported.
        /// </remarks>
        [TestMethod]
        public void Parse_ComplexTypes_ShouldParseCorrectly()
        {
            // Arrange
            IParser parser = new Parser();
            var guid = Guid.NewGuid();
            var dateTime = new DateTime(2024, 1, 15, 10, 30, 45);
            var timeSpan = new TimeSpan(1, 2, 3);
            var uri = new Uri("https://example.com");
            var version = new Version(1, 2, 3, 4);

            // Act & Assert
            parser.Parse(typeof(Guid), guid.ToString()).Should().Be(guid);
            parser.Parse(typeof(DateTime), "2024-01-15T10:30:45").Should().Be(dateTime);
            parser.Parse(typeof(TimeSpan), "01:02:03").Should().Be(timeSpan);
            parser.Parse(typeof(Uri), "https://example.com").Should().Be(uri);
            parser.Parse(typeof(Version), "1.2.3.4").Should().Be(version);
        }

        /// <summary>
        /// Verifies that Parse correctly parses enum types.
        /// </summary>
        /// <remarks>
        /// Ensures enum parsing works for custom enum types.
        /// </remarks>
        [TestMethod]
        public void Parse_EnumType_ShouldParseCorrectly()
        {
            // Arrange
            IParser parser = new Parser();

            // Act & Assert
            parser.Parse(typeof(TestEnum), "Value1").Should().Be(TestEnum.Value1);
            parser.Parse(typeof(TestEnum), "Value2").Should().Be(TestEnum.Value2);
            parser.Parse(typeof(TestEnum), "Value3").Should().Be(TestEnum.Value3);
        }

        /// <summary>
        /// Verifies that Parse correctly handles special types.
        /// </summary>
        /// <remarks>
        /// Ensures special types like byte arrays and char arrays work.
        /// </remarks>
        [TestMethod]
        public void Parse_SpecialTypes_ShouldParseCorrectly()
        {
            // Arrange
            IParser parser = new Parser();

            // Act & Assert
            var base64Input = Convert.ToBase64String([1, 2, 3, 4]);
            var byteArrayResult = (byte[])parser.Parse(typeof(byte[]), base64Input);
            byteArrayResult.Should().BeEquivalentTo(new byte[] { 1, 2, 3, 4 });

            var charArrayResult = (char[])parser.Parse(typeof(char[]), "Hello");
            charArrayResult.Should().BeEquivalentTo(['H', 'e', 'l', 'l', 'o']);

            var cultureResult = (CultureInfo)parser.Parse(typeof(CultureInfo), "en-US");
            cultureResult.Name.Should().Be("en-US");

            var typeResult = (Type)parser.Parse(typeof(Type), "System.String");
            typeResult.Should().Be(typeof(string));
        }

        /// <summary>
        /// Verifies that Parse generic method works correctly.
        /// </summary>
        /// <remarks>
        /// Ensures generic parsing provides type safety and convenience.
        /// </remarks>
        [TestMethod]
        public void Parse_Generic_ShouldParseCorrectly()
        {
            // Act & Assert - Using static generic methods
            Parser.Parse<int>("42").Should().Be(42);
            Parser.Parse<bool>("true").Should().BeTrue();
            Parser.Parse<string>("Hello").Should().Be("Hello");
            Parser.Parse<TestEnum>("Value1").Should().Be(TestEnum.Value1);
        }

        /// <summary>
        /// Verifies that Parse handles null type gracefully.
        /// </summary>
        /// <remarks>
        /// Ensures null type input returns null rather than throwing exception.
        /// </remarks>
        [TestMethod]
        public void Parse_NullType_ShouldReturnNull()
        {
            // Arrange
            IParser parser = new Parser();

            // Act
            var result = parser.Parse(null, "any_value");

            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// Verifies that TryParse returns true for valid input.
        /// </summary>
        /// <remarks>
        /// Ensures TryParse succeeds for valid parsing scenarios.
        /// </remarks>
        [TestMethod]
        public void TryParse_ValidInput_ShouldReturnTrueWithCorrectResult()
        {
            // Arrange
            IParser parser = new Parser();

            // Act & Assert
            var success1 = parser.TryParse(typeof(int), "42", out var result1);
            success1.Should().BeTrue();
            result1.Should().Be(42);

            var success2 = parser.TryParse(typeof(bool), "true", out var result2);
            success2.Should().BeTrue();
            result2.Should().Be(true);

            var success3 = parser.TryParse(typeof(string), "Hello", out var result3);
            success3.Should().BeTrue();
            result3.Should().Be("Hello");
        }

        /// <summary>
        /// Verifies that TryParse returns false for invalid input.
        /// </summary>
        /// <remarks>
        /// Ensures TryParse fails gracefully for invalid input.
        /// </remarks>
        [TestMethod]
        public void TryParse_InvalidInput_ShouldReturnFalseWithDefaultResult()
        {
            // Arrange
            IParser parser = new Parser();

            // Act & Assert
            var success1 = parser.TryParse(typeof(int), "invalid", out var result1);
            success1.Should().BeFalse();
            result1.Should().BeNull();

            var success2 = parser.TryParse(typeof(int), "invalid", out var result2);
            success2.Should().BeFalse();
            result2.Should().BeNull();

            var success3 = parser.TryParse(typeof(DateTime), "invalid_date", out var result3);
            success3.Should().BeFalse();
            result3.Should().BeNull();
        }

        /// <summary>
        /// Verifies that RegisterType adds custom parsers correctly.
        /// </summary>
        /// <remarks>
        /// Ensures custom type parsers can be registered and used.
        /// </remarks>
        [TestMethod]
        public void RegisterType_CustomType_ShouldAddAndParseCorrectly()
        {
            // Arrange
            IParser parser = new Parser();
            var customType = typeof(System.Drawing.Point);
            
            // Act
            parser.RegisterType(customType, 
                value =>
                {
                    var parts = value.Split(',');
                    return new System.Drawing.Point(int.Parse(parts[0]), int.Parse(parts[1]));
                },
                value =>
                {
                    var point = (System.Drawing.Point)value;
                    return $"{point.X},{point.Y}";
                });
            
            var result = parser.Parse(customType, "10,20");

            // Assert
            result.Should().BeOfType<System.Drawing.Point>();
            var point = (System.Drawing.Point)result;
            point.X.Should().Be(10);
            point.Y.Should().Be(20);
        }

        /// <summary>
        /// Verifies that RegisterType throws ArgumentNullException for null arguments.
        /// </summary>
        /// <remarks>
        /// Ensures proper validation for RegisterType method.
        /// </remarks>
        [TestMethod]
        public void RegisterType_NullArguments_ShouldThrowArgumentNullException()
        {
            // Arrange
            IParser parser = new Parser();

            // Act & Assert
            AssertionHelpers.AssertThrows<ArgumentNullException>(() => parser.RegisterType(null, _ => null, _ => ""));
            AssertionHelpers.AssertThrows<ArgumentNullException>(() => parser.RegisterType(typeof(int), null, _ => ""));
            AssertionHelpers.AssertThrows<ArgumentNullException>(() => parser.RegisterType(typeof(int), _ => null, null));
        }

        /// <summary>
        /// Verifies that IsKnownType correctly identifies known types.
        /// </summary>
        /// <remarks>
        /// Ensures IsKnownType provides accurate information about supported types.
        /// </remarks>
        [TestMethod]
        public void IsKnownType_BuiltInTypes_ShouldReturnTrue()
        {
            // Arrange & Act & Assert
            Parser.IsKnownType(typeof(int)).Should().BeTrue();
            Parser.IsKnownType(typeof(string)).Should().BeTrue();
            Parser.IsKnownType(typeof(DateTime)).Should().BeTrue();
            Parser.IsKnownType(typeof(Guid)).Should().BeTrue();
            Parser.IsKnownType(typeof(bool)).Should().BeTrue();
            Parser.IsKnownType(typeof(TimeSpan)).Should().BeTrue();
        }

        /// <summary>
        /// Verifies that IsKnownType returns false for unknown types.
        /// </summary>
        /// <remarks>
        /// Ensures IsKnownType correctly identifies unsupported types.
        /// </remarks>
        [TestMethod]
        public void IsKnownType_UnknownTypes_ShouldReturnFalse()
        {
            // Arrange & Act & Assert
            Parser.IsKnownType(typeof(System.Drawing.Point)).Should().BeFalse();
            Parser.IsKnownType(typeof(System.IO.FileInfo)).Should().BeFalse();
        }

        /// <summary>
        /// Verifies static Parse methods work correctly.
        /// </summary>
        /// <remarks>
        /// Ensures static methods delegate to default instance properly.
        /// </remarks>
        [TestMethod]
        public void Parse_StaticMethods_ShouldWorkCorrectly()
        {
            // Act & Assert
            Parser.Parse(typeof(int), "42").Should().Be(42);
            Parser.Parse<bool>("true").Should().BeTrue();
            Parser.Parse<string>("Hello").Should().Be("Hello");
        }

        /// <summary>
        /// Verifies static TryParse methods work correctly.
        /// </summary>
        /// <remarks>
        /// Ensures static TryParse methods delegate to default instance properly.
        /// </remarks>
        [TestMethod]
        public void TryParse_StaticMethods_ShouldWorkCorrectly()
        {
            // Act & Assert - Using Default instance since static TryParse doesn't exist
            var success1 = Parser.Default.TryParse(typeof(int), "42", out var result1);
            success1.Should().BeTrue();
            result1.Should().Be(42);

            var success2 = Parser.Default.TryParse(typeof(bool), "true", out var result2);
            success2.Should().BeTrue();
            result2.Should().Be(true);

            var success3 = Parser.Default.TryParse(typeof(int), "invalid", out var result3);
            success3.Should().BeFalse();
            result3.Should().BeNull();
        }

        /// <summary>
        /// Verifies static RegisterType method works correctly.
        /// </summary>
        /// <remarks>
        /// Ensures static registration affects the default instance.
        /// </remarks>
        [TestMethod]
        public void RegisterType_StaticMethod_ShouldWorkCorrectly()
        {
            // Arrange
            var customType = typeof(System.Drawing.Size);
            
            try
            {
                // Act
                Parser.RegisterType(customType, 
                    value =>
                    {
                        var parts = value.Split('x');
                        return new System.Drawing.Size(int.Parse(parts[0]), int.Parse(parts[1]));
                    },
                    value =>
                    {
                        var size = (System.Drawing.Size)value;
                        return $"{size.Width}x{size.Height}";
                    });
                
                var result = Parser.Parse(customType, "100x200");

                // Assert
                result.Should().BeOfType<System.Drawing.Size>();
                var size = (System.Drawing.Size)result;
                size.Width.Should().Be(100);
                size.Height.Should().Be(200);
            }
            finally
            {
                // Note: There's no unregister method, so the custom parser remains for other tests
                // This is acceptable for unit tests as they typically run in isolation
            }
        }

        /// <summary>
        /// Verifies that parser handles edge cases correctly.
        /// </summary>
        /// <remarks>
        /// Ensures robust handling of edge cases and boundary values.
        /// </remarks>
        [TestMethod]
        public void Parse_EdgeCases_ShouldHandleCorrectly()
        {
            // Arrange
            IParser parser = new Parser();

            // Act & Assert
            // Empty string for string type
            parser.Parse(typeof(string), "").Should().Be("");
            
            // Minimum and maximum values
            parser.Parse(typeof(int), int.MaxValue.ToString()).Should().Be(int.MaxValue);
            parser.Parse(typeof(int), int.MinValue.ToString()).Should().Be(int.MinValue);
            
            // Boolean edge cases
            parser.Parse(typeof(bool), "True").Should().Be(true);
            parser.Parse(typeof(bool), "False").Should().Be(false);
            
            // Whitespace handling for char
            parser.Parse(typeof(char), " ").Should().Be(' ');
        }

        /// <summary>
        /// Verifies that parser throws appropriate exceptions for invalid input.
        /// </summary>
        /// <remarks>
        /// Ensures proper error handling for invalid parsing scenarios.
        /// </remarks>
        [TestMethod]
        public void Parse_InvalidInput_ShouldThrowAppropriateExceptions()
        {
            // Arrange
            IParser parser = new Parser();

            // Act & Assert
            AssertionHelpers.AssertThrows<FormatException>(() => parser.Parse(typeof(int), "invalid"));
            AssertionHelpers.AssertThrows<FormatException>(() => parser.Parse(typeof(DateTime), "invalid_date"));
            AssertionHelpers.AssertThrows<FormatException>(() => parser.Parse(typeof(Guid), "invalid_guid"));
            AssertionHelpers.AssertThrows<ArgumentException>(() => parser.Parse(typeof(TestEnum), "InvalidValue"));
        }

        /// <summary>
        /// Verifies that Convert.ChangeType fallback works for unregistered types.
        /// </summary>
        /// <remarks>
        /// Ensures fallback mechanism works for types not explicitly registered.
        /// </remarks>
        [TestMethod]
        public void Parse_UnregisteredType_ShouldUseConvertChangeType()
        {
            // Arrange
            IParser parser = new Parser();

            // Act
            // sbyte is not explicitly registered, should fall back to Convert.ChangeType
            var result = parser.Parse(typeof(sbyte), "42");

            // Assert
            result.Should().Be((sbyte)42);
            result.Should().BeOfType<sbyte>();
        }

        /// <summary>
        /// Performance test for multiple type parses.
        /// </summary>
        /// <remarks>
        /// Ensures parsing is efficient for repeated use across different types.
        /// </remarks>
        [TestMethod]
        public void Parse_Performance_ShouldHandleMultipleParsesQuickly()
        {
            // Arrange
            IParser parser = new Parser();
            var iterations = 1000;
            var startTime = DateTime.UtcNow;
            var testCount = 0;

            // Act
            for (var i = 0; i < iterations; i++)
            {
                parser.Parse(typeof(int), "42").Should().NotBeNull();
                parser.Parse(typeof(bool), "true").Should().NotBeNull();
                parser.Parse(typeof(string), "Hello").Should().NotBeNull();
                parser.Parse(typeof(DateTime), "2024-01-15").Should().NotBeNull();
                parser.Parse(typeof(double), "3.14159").Should().NotBeNull();
                parser.Parse(typeof(Guid), Guid.NewGuid().ToString()).Should().NotBeNull();
                testCount += 6;
            }

            // Assert
            var duration = DateTime.UtcNow - startTime;
            duration.Should().BeLessThan(TimeSpan.FromSeconds(3), 
                $"should parse {testCount} values quickly");
        }

        /// <summary>
        /// Verifies thread safety for concurrent parsing.
        /// </summary>
        /// <remarks>
        /// Ensures the parser can be used safely in multi-threaded environments.
        /// </remarks>
        [TestMethod]
        public void Parse_ThreadSafety_ShouldHandleConcurrentAccess()
        {
            // Arrange
            IParser parser = new Parser();
            var tasks = new System.Threading.Tasks.Task[10];

            // Act
            for (var i = 0; i < tasks.Length; i++)
            {
                tasks[i] = System.Threading.Tasks.Task.Run(() =>
                {
                    for (var j = 0; j < 100; j++)
                    {
                        var intResult = (int)parser.Parse(typeof(int), "42");
                        var boolResult = (bool)parser.Parse(typeof(bool), "true");
                        var stringResult = (string)parser.Parse(typeof(string), "Hello");
                        
                        intResult.Should().Be(42, "concurrent parsing should produce consistent results");
                        boolResult.Should().BeTrue("concurrent parsing should produce consistent results");
                        stringResult.Should().Be("Hello", "concurrent parsing should produce consistent results");
                    }
                });
            }

            // Assert
            System.Threading.Tasks.Task.WaitAll(tasks, TimeSpan.FromSeconds(5))
                .Should().BeTrue("all concurrent parsing tasks should complete");
        }

        /// <summary>
        /// Verifies that custom parsers can override built-in parsers.
        /// </summary>
        /// <remarks>
        /// Ensures custom parsers take precedence over default implementations.
        /// </remarks>
        [TestMethod]
        public void RegisterType_OverrideBuiltIn_ShouldUseCustomParser()
        {
            // Arrange
            IParser parser = new Parser();
            var originalResult = (bool)parser.Parse(typeof(bool), "true");

            // Act
            // Register a custom bool parser that always returns false
            parser.RegisterType(typeof(bool), 
                _ => false,
                value => value.ToString().ToLower());
            var customResult = (bool)parser.Parse(typeof(bool), "true");

            // Assert
            originalResult.Should().BeTrue("original parser should work normally");
            customResult.Should().BeFalse("custom parser should override built-in behavior");
        }
    }
}