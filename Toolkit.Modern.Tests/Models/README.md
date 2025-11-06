# Test Models

This directory contains model classes used specifically for testing the ByteForge.Toolkit library.

## Overview

The test models in this directory provide data structures and entity definitions for use in test scenarios across the test suite. These models help ensure consistent test data and facilitate testing of complex scenarios involving serialization, data mapping, and other operations.

## Model Classes

### TestEntity

A general-purpose entity class used for testing database operations, data binding, and serialization:

```csharp
public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; }
    public decimal TestValue { get; set; }
    public DateTime CreatedDate { get; set; }
}
```

### BulkTestEntity

A specialized entity class used for testing bulk database operations and data mapping:

```csharp
public class BulkTestEntity
{
    [DBColumn("Id")]
    public int EntityId { get; set; }
    
    [DBColumn("EntityName")]
    public string Name { get; set; }
    
    [DBColumn("CreationDate")]
    public DateTime Created { get; set; }
    
    [DBColumn("IsEnabled")]
    public bool Active { get; set; }
    
    [DBColumn("DataValue", AllowNull = true)]
    public decimal? Value { get; set; }
}
```

### TestDataTypeEntity

An entity class with various data types for testing type conversion and handling:

```csharp
public class TestDataTypeEntity
{
    public int IntValue { get; set; }
    public long LongValue { get; set; }
    public decimal DecimalValue { get; set; }
    public double DoubleValue { get; set; }
    public float FloatValue { get; set; }
    public bool BoolValue { get; set; }
    public DateTime DateTimeValue { get; set; }
    public Guid GuidValue { get; set; }
    public string StringValue { get; set; }
    public byte[] ByteArrayValue { get; set; }
    public TestEnum EnumValue { get; set; }
    public TimeSpan TimeSpanValue { get; set; }
}
```

### ConfigurationTestModels

Contains various models for testing the Configuration module:

```csharp
public class BasicTestConfig : ConfigSection<BasicTestConfig>
{
    public string StringSetting { get; set; }
    public int IntSetting { get; set; }
    public bool BoolSetting { get; set; }
    public DateTime DateSetting { get; set; }
    public TimeSpan TimeSetting { get; set; }
}

public class ArrayTestConfig : ConfigSection<ArrayTestConfig>
{
    public string[] StringArray { get; set; }
    public int[] IntArray { get; set; }
    public List<string> StringList { get; set; }
    public List<CustomItem> ComplexList { get; set; }
}

public class CustomItem
{
    public string Name { get; set; }
    public int Value { get; set; }
}
```

## Usage Examples

### Using TestEntity in Database Tests

```csharp
[TestMethod]
public void DatabaseInsertTest()
{
    // Create a test entity
    var entity = new TestEntity
    {
        Name = "Test Entity",
        Description = "Created for testing",
        IsActive = true,
        TestValue = 42.5m,
        CreatedDate = DateTime.Now
    };
    
    // Use in database operation
    var dbAccess = DatabaseTestHelper.CreateTestDBAccess();
    bool success = dbAccess.ExecuteQuery(
        "INSERT INTO TestEntities (Name, Description, IsActive, TestValue, CreatedDate) " +
        "VALUES (@Name, @Description, @IsActive, @TestValue, @CreatedDate)",
        entity.Name, entity.Description, entity.IsActive, entity.TestValue, entity.CreatedDate);
        
    success.Should().BeTrue();
}
```

### Using Configuration Test Models

```csharp
[TestMethod]
public void ConfigurationSaveLoadTest()
{
    // Create a temp configuration file
    var configFile = TempFileHelper.CreateTempFile("", ".ini");
    Configuration.Initialize(configFile);
    
    // Create and configure a test section
    var config = Configuration.GetSection<BasicTestConfig>();
    config.StringSetting = "Test Value";
    config.IntSetting = 42;
    config.BoolSetting = true;
    config.DateSetting = new DateTime(2025, 1, 1);
    
    // Save configuration
    Configuration.Save();
    
    // Clear and reload
    Configuration.Reset();
    Configuration.Initialize(configFile);
    
    // Verify values were preserved
    var loadedConfig = Configuration.GetSection<BasicTestConfig>();
    loadedConfig.StringSetting.Should().Be("Test Value");
    loadedConfig.IntSetting.Should().Be(42);
    loadedConfig.BoolSetting.Should().BeTrue();
    loadedConfig.DateSetting.Should().Be(new DateTime(2025, 1, 1));
}
```

## Notes

- Test models are designed specifically for testing and may not reflect production data models
- Many models include attributes for testing attribute-based mapping and serialization
- These models are used consistently across test fixtures to ensure testing consistency
- Models may be extended or modified as needed for specific test scenarios
