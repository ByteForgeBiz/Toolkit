using System;

namespace ByteForge.Toolkit.Tests.Models
{
    /// <summary>
    /// Test entity class that maps to the TestDataTypes table for comprehensive data type conversion testing.
    /// </summary>
    /// <remarks>
    /// This class includes properties for all major SQL Server data types to validate the TypeConverter's
    /// ability to properly map database values to .NET types, including nullable types, boundary values,
    /// and special cases like Unicode strings and binary data.
    /// </remarks>
    public class TestDataTypeEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the test data type entity.
        /// </summary>
        [DBColumn("Id", isPrimaryKey: true, IsIdentity = true)]
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the string value for testing various string scenarios.
        /// </summary>
        [DBColumn("StringValue", MaxLength = 500)]
        public string StringValue { get; set; }

        /// <summary>
        /// Gets or sets the integer value for testing 32-bit integer conversion.
        /// </summary>
        [DBColumn("IntValue")]
        public int? IntValue { get; set; }

        /// <summary>
        /// Gets or sets the big integer value for testing 64-bit integer conversion.
        /// </summary>
        [DBColumn("BigIntValue")]
        public long? BigIntValue { get; set; }

        /// <summary>
        /// Gets or sets the small integer value for testing 16-bit integer conversion.
        /// </summary>
        [DBColumn("SmallIntValue")]
        public short? SmallIntValue { get; set; }

        /// <summary>
        /// Gets or sets the tiny integer value for testing byte conversion.
        /// </summary>
        [DBColumn("TinyIntValue")]
        public byte? TinyIntValue { get; set; }

        /// <summary>
        /// Gets or sets the decimal value for testing precise decimal conversion.
        /// </summary>
        [DBColumn("DecimalValue")]
        public decimal? DecimalValue { get; set; }

        /// <summary>
        /// Gets or sets the float value for testing double-precision floating point conversion.
        /// </summary>
        [DBColumn("FloatValue")]
        public double? FloatValue { get; set; }

        /// <summary>
        /// Gets or sets the real value for testing single-precision floating point conversion.
        /// </summary>
        [DBColumn("RealValue")]
        public float? RealValue { get; set; }

        /// <summary>
        /// Gets or sets the bit value for testing boolean conversion.
        /// </summary>
        [DBColumn("BitValue")]
        public bool? BitValue { get; set; }

        /// <summary>
        /// Gets or sets the datetime value for testing DateTime conversion.
        /// </summary>
        [DBColumn("DateTimeValue")]
        public DateTime? DateTimeValue { get; set; }

        /// <summary>
        /// Gets or sets the date value for testing date-only conversion.
        /// </summary>
        [DBColumn("DateValue")]
        public DateTime? DateValue { get; set; }

        /// <summary>
        /// Gets or sets the time value for testing time conversion.
        /// </summary>
        /// <remarks>
        /// SQL Server time type maps to TimeSpan in .NET, but for compatibility with older versions,
        /// we may need to handle this as DateTime or TimeSpan depending on the driver.
        /// </remarks>
        [DBColumn("TimeValue")]
        public TimeSpan? TimeValue { get; set; }

        /// <summary>
        /// Gets or sets the GUID value for testing uniqueidentifier conversion.
        /// </summary>
        [DBColumn("GuidValue")]
        public Guid? GuidValue { get; set; }

        /// <summary>
        /// Gets or sets the binary value for testing binary data conversion.
        /// </summary>
        [DBColumn("BinaryValue")]
        public byte[] BinaryValue { get; set; }

        /// <summary>
        /// Gets or sets the XML value for testing XML data conversion.
        /// </summary>
        /// <remarks>
        /// SQL Server XML type typically maps to string in .NET applications.
        /// </remarks>
        [DBColumn("XmlValue")]
        public string XmlValue { get; set; }

        /// <summary>
        /// Gets or sets the money value for testing currency conversion.
        /// </summary>
        [DBColumn("MoneyValue")]
        public decimal? MoneyValue { get; set; }

        /// <summary>
        /// Gets or sets the small money value for testing small currency conversion.
        /// </summary>
        [DBColumn("SmallMoneyValue")]
        public decimal? SmallMoneyValue { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestDataTypeEntity"/> class.
        /// </summary>
        public TestDataTypeEntity()
        {
        }

        /// <summary>
        /// Creates a test data type entity with basic values for testing.
        /// </summary>
        /// <returns>A configured <see cref="TestDataTypeEntity"/> instance with test values.</returns>
        public static TestDataTypeEntity CreateWithTestValues()
        {
            return new TestDataTypeEntity
            {
                StringValue = "Test String Value",
                IntValue = 12345,
                BigIntValue = 9876543210L,
                SmallIntValue = 32000,
                TinyIntValue = 255,
                DecimalValue = 12345.6789m,
                FloatValue = 123.456789,
                RealValue = 78.9f,
                BitValue = true,
                DateTimeValue = new DateTime(2023, 12, 1, 10, 30, 45),
                DateValue = new DateTime(2023, 12, 1),
                TimeValue = new TimeSpan(14, 30, 45),
                GuidValue = Guid.NewGuid(),
                BinaryValue = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }, // "Hello" in bytes
                XmlValue = "<test><value>123</value></test>",
                MoneyValue = 1234.56m,
                SmallMoneyValue = 567.89m
            };
        }

        /// <summary>
        /// Creates a test data type entity with null values for testing null handling.
        /// </summary>
        /// <returns>A <see cref="TestDataTypeEntity"/> instance with all nullable properties set to null.</returns>
        public static TestDataTypeEntity CreateWithNullValues()
        {
            return new TestDataTypeEntity
            {
                StringValue = null,
                IntValue = null,
                BigIntValue = null,
                SmallIntValue = null,
                TinyIntValue = null,
                DecimalValue = null,
                FloatValue = null,
                RealValue = null,
                BitValue = null,
                DateTimeValue = null,
                DateValue = null,
                TimeValue = null,
                GuidValue = null,
                BinaryValue = null,
                XmlValue = null,
                MoneyValue = null,
                SmallMoneyValue = null
            };
        }

        /// <summary>
        /// Creates a test data type entity with boundary values for testing edge cases.
        /// </summary>
        /// <returns>A <see cref="TestDataTypeEntity"/> instance with boundary/extreme values.</returns>
        public static TestDataTypeEntity CreateWithBoundaryValues()
        {
            return new TestDataTypeEntity
            {
                StringValue = new string('X', 500), // Maximum length string
                IntValue = int.MaxValue,
                BigIntValue = long.MaxValue,
                SmallIntValue = short.MaxValue,
                TinyIntValue = byte.MaxValue,
                DecimalValue = decimal.MaxValue,
                FloatValue = double.MaxValue,
                RealValue = float.MaxValue,
                BitValue = true,
                DateTimeValue = DateTime.MaxValue,
                DateValue = DateTime.MaxValue.Date,
                TimeValue = TimeSpan.MaxValue,
                GuidValue = Guid.Empty,
                BinaryValue = new byte[255], // Maximum practical binary size
                XmlValue = "<root>" + new string('x', 400) + "</root>",
                MoneyValue = 922337203685477.58m, // SQL Server money max value
                SmallMoneyValue = 214748.36m // SQL Server smallmoney max value
            };
        }

        /// <summary>
        /// Creates a test data type entity with minimum values for testing edge cases.
        /// </summary>
        /// <returns>A <see cref="TestDataTypeEntity"/> instance with minimum values.</returns>
        public static TestDataTypeEntity CreateWithMinimumValues()
        {
            return new TestDataTypeEntity
            {
                StringValue = "",
                IntValue = int.MinValue,
                BigIntValue = long.MinValue,
                SmallIntValue = short.MinValue,
                TinyIntValue = byte.MinValue,
                DecimalValue = decimal.MinValue,
                FloatValue = double.MinValue,
                RealValue = float.MinValue,
                BitValue = false,
                DateTimeValue = DateTime.MinValue,
                DateValue = DateTime.MinValue.Date,
                TimeValue = TimeSpan.MinValue,
                GuidValue = Guid.Empty,
                BinaryValue = new byte[0],
                XmlValue = "",
                MoneyValue = -922337203685477.58m, // SQL Server money min value
                SmallMoneyValue = -214748.36m // SQL Server smallmoney min value
            };
        }

        /// <summary>
        /// Creates a test data type entity with Unicode and special characters.
        /// </summary>
        /// <returns>A <see cref="TestDataTypeEntity"/> instance with Unicode test data.</returns>
        public static TestDataTypeEntity CreateWithUnicodeValues()
        {
            return new TestDataTypeEntity
            {
                StringValue = "Unicode: 🌟🔥💯 中文 العربية русский",
                IntValue = 42,
                BigIntValue = 987654321L,
                SmallIntValue = 12345,
                TinyIntValue = 128,
                DecimalValue = 3.14159m,
                FloatValue = 2.71828,
                RealValue = 1.414f,
                BitValue = true,
                DateTimeValue = new DateTime(2023, 6, 15, 12, 0, 0),
                DateValue = new DateTime(2023, 6, 15),
                TimeValue = new TimeSpan(12, 0, 0),
                GuidValue = Guid.NewGuid(),
                BinaryValue = new byte[] { 0x54, 0x65, 0x73, 0x74, 0xDA, 0xA1, 0xB2 }, // "Test" + some binary
                XmlValue = "<test>Unicode: 中文测试</test>",
                MoneyValue = 999.99m,
                SmallMoneyValue = 123.45m
            };
        }

        /// <summary>
        /// Returns a string representation of the test data type entity.
        /// </summary>
        /// <returns>A string containing the entity's key properties.</returns>
        public override string ToString()
        {
            return $"TestDataTypeEntity {{ Id={Id}, StringValue='{StringValue}', IntValue={IntValue}, DecimalValue={DecimalValue} }}";
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current test data type entity.
        /// </summary>
        /// <param name="obj">The object to compare with the current entity.</param>
        /// <returns>True if the specified object is equal to the current entity; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (obj is TestDataTypeEntity other)
            {
                return Id == other.Id && 
                       StringValue == other.StringValue && 
                       IntValue == other.IntValue && 
                       DecimalValue == other.DecimalValue &&
                       BitValue == other.BitValue &&
                       GuidValue == other.GuidValue;
            }
            return false;
        }

        /// <summary>
        /// Returns the hash code for this test data type entity.
        /// </summary>
        /// <returns>A hash code for the current entity.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 23 + Id.GetHashCode();
                hash = hash * 23 + (StringValue?.GetHashCode() ?? 0);
                hash = hash * 23 + (IntValue?.GetHashCode() ?? 0);
                hash = hash * 23 + (DecimalValue?.GetHashCode() ?? 0);
                hash = hash * 23 + (BitValue?.GetHashCode() ?? 0);
                hash = hash * 23 + (GuidValue?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}