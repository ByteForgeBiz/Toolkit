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
        /// Verifies that ParseValue correctly parses primitive types.
        /// </summary>
        /// <remarks>
        /// Ensures all built-in primitive types are supported.
        /// </remarks>
        [TestMethod]
        public void ParseValue_PrimitiveTypes_ShouldParseCorrectly()
        {
            // Arrange
            var parser = new Parser();

            // Act & Assert
            parser.ParseValue(typeof(int), "42").Should().Be(42);
            parser.ParseValue(typeof(long), "9223372036854775807").Should().Be(9223372036854775807L);
            parser.ParseValue(typeof(short), "32767").Should().Be((short)32767);
            parser.ParseValue(typeof(uint), "42").Should().Be(42U);
            parser.ParseValue(typeof(ulong), "42").Should().Be(42UL);
            parser.ParseValue(typeof(ushort), "42").Should().Be((ushort)42);
            parser.ParseValue(typeof(byte), "255").Should().Be((byte)255);
            parser.ParseValue(typeof(double), "3.14159").Should().Be(3.14159);
            parser.ParseValue(typeof(float), "3.14").Should().Be(3.14f);
            parser.ParseValue(typeof(decimal), "123.45").Should().Be(123.45m);
            parser.ParseValue(typeof(bool), "true").Should().Be(true);
            parser.ParseValue(typeof(char), "A").Should().Be('A');
            parser.ParseValue(typeof(string), "Hello").Should().Be("Hello");
        }

        /// <summary>
        /// Verifies that ParseValue correctly parses complex types.
        /// </summary>
        /// <remarks>
        /// Ensures complex built-in types like DateTime, Guid, etc. are supported.
        /// </remarks>
        [TestMethod]
        public void ParseValue_ComplexTypes_ShouldParseCorrectly()
        {
            // Arrange
            var parser = new Parser();
            var guid = Guid.NewGuid();
            var dateTime = new DateTime(2024, 1, 15, 10, 30, 45);
            var timeSpan = new TimeSpan(1, 2, 3);
            var uri = new Uri("https://example.com");
            var version = new Version(1, 2, 3, 4);

            // Act & Assert
            parser.ParseValue(typeof(Guid), guid.ToString()).Should().Be(guid);
            parser.ParseValue(typeof(DateTime), "2024-01-15T10:30:45").Should().Be(dateTime);
            parser.ParseValue(typeof(TimeSpan), "01:02:03").Should().Be(timeSpan);
            parser.ParseValue(typeof(Uri), "https://example.com").Should().Be(uri);
            parser.ParseValue(typeof(Version), "1.2.3.4").Should().Be(version);
        }

        /// <summary>
        /// Verifies that ParseValue correctly parses enum types.
        /// </summary>
        /// <remarks>
        /// Ensures enum parsing works for custom enum types.
        /// </remarks>
        [TestMethod]
        public void ParseValue_EnumType_ShouldParseCorrectly()
        {
            // Arrange
            var parser = new Parser();

            // Act & Assert
            parser.ParseValue(typeof(TestEnum), "Value1").Should().Be(TestEnum.Value1);
            parser.ParseValue(typeof(TestEnum), "Value2").Should().Be(TestEnum.Value2);
            parser.ParseValue(typeof(TestEnum), "Value3").Should().Be(TestEnum.Value3);
        }

        /// <summary>
        /// Verifies that ParseValue correctly handles special types.
        /// </summary>
        /// <remarks>
        /// Ensures special types like byte arrays and char arrays work.
        /// </remarks>
        [TestMethod]
        public void ParseValue_SpecialTypes_ShouldParseCorrectly()
        {
            // Arrange
            var parser = new Parser();

            // Act & Assert
            var base64Input = Convert.ToBase64String([1, 2, 3, 4]);
            var byteArrayResult = (byte[])parser.ParseValue(typeof(byte[]), base64Input);
            byteArrayResult.Should().BeEquivalentTo(new byte[] { 1, 2, 3, 4 });

            var charArrayResult = (char[])parser.ParseValue(typeof(char[]), "Hello");
            charArrayResult.Should().BeEquivalentTo(['H', 'e', 'l', 'l', 'o']);

            var cultureResult = (CultureInfo)parser.ParseValue(typeof(CultureInfo), "en-US");
            cultureResult.Name.Should().Be("en-US");

            var typeResult = (Type)parser.ParseValue(typeof(Type), "System.String");
            typeResult.Should().Be(typeof(string));
        }

        /// <summary>
        /// Verifies that ParseValue generic method works correctly.
        /// </summary>
        /// <remarks>
        /// Ensures generic parsing provides type safety and convenience.
        /// </remarks>
        [TestMethod]
        public void ParseValue_Generic_ShouldParseCorrectly()
        {
            // Arrange
            var parser = new Parser();

            // Act & Assert
            parser.ParseValue<int>("42").Should().Be(42);
            parser.ParseValue<bool>("true").Should().BeTrue();
            parser.ParseValue<string>("Hello").Should().Be("Hello");
            parser.ParseValue<TestEnum>("Value1").Should().Be(TestEnum.Value1);
        }

        /// <summary>
        /// Verifies that ParseValue handles null type gracefully.
        /// </summary>
        /// <remarks>
        /// Ensures null type input returns null rather than throwing exception.
        /// </remarks>
        [TestMethod]
        public void ParseValue_NullType_ShouldReturnNull()
        {
            // Arrange
            var parser = new Parser();

            // Act
            var result = parser.ParseValue(null, "any_value");

            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// Verifies that TryParseValue returns true for valid input.
        /// </summary>
        /// <remarks>
        /// Ensures TryParseValue succeeds for valid parsing scenarios.
        /// </remarks>
        [TestMethod]
        public void TryParseValue_ValidInput_ShouldReturnTrueWithCorrectResult()
        {
            // Arrange
            var parser = new Parser();

            // Act & Assert
            var success1 = parser.TryParseValue(typeof(int), "42", out var result1);
            success1.Should().BeTrue();
            result1.Should().Be(42);

            var success2 = parser.TryParseValue<bool>("true", out var result2);
            success2.Should().BeTrue();
            result2.Should().BeTrue();

            var success3 = parser.TryParseValue<string>("Hello", out var result3);
            success3.Should().BeTrue();
            result3.Should().Be("Hello");
        }

        /// <summary>
        /// Verifies that TryParseValue returns false for invalid input.
        /// </summary>
        /// <remarks>
        /// Ensures TryParseValue fails gracefully for invalid input.
        /// </remarks>
        [TestMethod]
        public void TryParseValue_InvalidInput_ShouldReturnFalseWithDefaultResult()
        {
            // Arrange
            var parser = new Parser();

            // Act & Assert
            var success1 = parser.TryParseValue(typeof(int), "invalid", out var result1);
            success1.Should().BeFalse();
            result1.Should().BeNull();

            var success2 = parser.TryParseValue<int>("invalid", out var result2);
            success2.Should().BeFalse();
            result2.Should().Be(default);

            var success3 = parser.TryParseValue<DateTime>("invalid_date", out var result3);
            success3.Should().BeFalse();
            result3.Should().Be(default);
        }

        /// <summary>
        /// Verifies that RegisterTypeParser adds custom parsers correctly.
        /// </summary>
        /// <remarks>
        /// Ensures custom type parsers can be registered and used.
        /// </remarks>
        [TestMethod]
        public void RegisterTypeParser_CustomType_ShouldAddAndParseCorrectly()
        {
            // Arrange
            var parser = new Parser();
            var customType = typeof(System.Drawing.Point);
            
            // Act
            parser.RegisterTypeParser(customType, value =>
            {
                var parts = value.Split(',');
                return new System.Drawing.Point(int.Parse(parts[0]), int.Parse(parts[1]));
            });
            
            var result = parser.ParseValue(customType, "10,20");

            // Assert
            result.Should().BeOfType<System.Drawing.Point>();
            var point = (System.Drawing.Point)result;
            point.X.Should().Be(10);
            point.Y.Should().Be(20);
        }

        /// <summary>
        /// Verifies that RegisterTypeParser throws ArgumentNullException for null arguments.
        /// </summary>
        /// <remarks>
        /// Ensures proper validation for RegisterTypeParser method.
        /// </remarks>
        [TestMethod]
        public void RegisterTypeParser_NullArguments_ShouldThrowArgumentNullException()
        {
            // Arrange
            var parser = new Parser();

            // Act & Assert
            AssertionHelpers.AssertThrows<ArgumentNullException>(() => parser.RegisterTypeParser(null, _ => null));
            AssertionHelpers.AssertThrows<ArgumentNullException>(() => parser.RegisterTypeParser(typeof(int), null));
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
            // Act & Assert
            var success1 = Parser.TryParse(typeof(int), "42", out var result1);
            success1.Should().BeTrue();
            result1.Should().Be(42);

            var success2 = Parser.TryParse<bool>("true", out var result2);
            success2.Should().BeTrue();
            result2.Should().BeTrue();

            var success3 = Parser.TryParse<int>("invalid", out var result3);
            success3.Should().BeFalse();
            result3.Should().Be(default);
        }

        /// <summary>
        /// Verifies static RegisterParser method works correctly.
        /// </summary>
        /// <remarks>
        /// Ensures static registration affects the default instance.
        /// </remarks>
        [TestMethod]
        public void RegisterParser_StaticMethod_ShouldWorkCorrectly()
        {
            // Arrange
            var customType = typeof(System.Drawing.Size);
            
            try
            {
                // Act
                Parser.RegisterParser(customType, value =>
                {
                    var parts = value.Split('x');
                    return new System.Drawing.Size(int.Parse(parts[0]), int.Parse(parts[1]));
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
        public void ParseValue_EdgeCases_ShouldHandleCorrectly()
        {
            // Arrange
            var parser = new Parser();

            // Act & Assert
            // Empty string for string type
            parser.ParseValue<string>("").Should().Be("");
            
            // Minimum and maximum values
            parser.ParseValue<int>(int.MaxValue.ToString()).Should().Be(int.MaxValue);
            parser.ParseValue<int>(int.MinValue.ToString()).Should().Be(int.MinValue);
            
            // Boolean edge cases
            parser.ParseValue<bool>("True").Should().BeTrue();
            parser.ParseValue<bool>("False").Should().BeFalse();
            
            // Whitespace handling for char
            parser.ParseValue<char>(" ").Should().Be(' ');
        }

        /// <summary>
        /// Verifies that parser throws appropriate exceptions for invalid input.
        /// </summary>
        /// <remarks>
        /// Ensures proper error handling for invalid parsing scenarios.
        /// </remarks>
        [TestMethod]
        public void ParseValue_InvalidInput_ShouldThrowAppropriateExceptions()
        {
            // Arrange
            var parser = new Parser();

            // Act & Assert
            AssertionHelpers.AssertThrows<FormatException>(() => parser.ParseValue<int>("invalid"));
            AssertionHelpers.AssertThrows<FormatException>(() => parser.ParseValue<DateTime>("invalid_date"));
            AssertionHelpers.AssertThrows<FormatException>(() => parser.ParseValue<Guid>("invalid_guid"));
            AssertionHelpers.AssertThrows<ArgumentException>(() => parser.ParseValue<TestEnum>("InvalidValue"));
        }

        /// <summary>
        /// Verifies that Convert.ChangeType fallback works for unregistered types.
        /// </summary>
        /// <remarks>
        /// Ensures fallback mechanism works for types not explicitly registered.
        /// </remarks>
        [TestMethod]
        public void ParseValue_UnregisteredType_ShouldUseConvertChangeType()
        {
            // Arrange
            var parser = new Parser();

            // Act
            // sbyte is not explicitly registered, should fall back to Convert.ChangeType
            var result = parser.ParseValue(typeof(sbyte), "42");

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
        public void ParseValue_Performance_ShouldHandleMultipleParsesQuickly()
        {
            // Arrange
            var parser = new Parser();
            var iterations = 1000;
            var startTime = DateTime.UtcNow;
            var testCount = 0;

            // Act
            for (var i = 0; i < iterations; i++)
            {
                parser.ParseValue(typeof(int), "42").Should().NotBeNull();
                parser.ParseValue(typeof(bool), "true").Should().NotBeNull();
                parser.ParseValue(typeof(string), "Hello").Should().NotBeNull();
                parser.ParseValue(typeof(DateTime), "2024-01-15").Should().NotBeNull();
                parser.ParseValue(typeof(double), "3.14159").Should().NotBeNull();
                parser.ParseValue(typeof(Guid), Guid.NewGuid().ToString()).Should().NotBeNull();
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
        public void ParseValue_ThreadSafety_ShouldHandleConcurrentAccess()
        {
            // Arrange
            var parser = new Parser();
            var tasks = new System.Threading.Tasks.Task[10];

            // Act
            for (var i = 0; i < tasks.Length; i++)
            {
                tasks[i] = System.Threading.Tasks.Task.Run(() =>
                {
                    for (var j = 0; j < 100; j++)
                    {
                        var intResult = parser.ParseValue<int>("42");
                        var boolResult = parser.ParseValue<bool>("true");
                        var stringResult = parser.ParseValue<string>("Hello");
                        
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
        public void RegisterTypeParser_OverrideBuiltIn_ShouldUseCustomParser()
        {
            // Arrange
            var parser = new Parser();
            var originalResult = parser.ParseValue<bool>("true");

            // Act
            // Register a custom bool parser that always returns false
            parser.RegisterTypeParser(typeof(bool), _ => false);
            var customResult = parser.ParseValue<bool>("true");

            // Assert
            originalResult.Should().BeTrue("original parser should work normally");
            customResult.Should().BeFalse("custom parser should override built-in behavior");
        }
    }
}