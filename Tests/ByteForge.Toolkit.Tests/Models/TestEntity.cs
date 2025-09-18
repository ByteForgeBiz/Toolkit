#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace ByteForge.Toolkit.Tests.Models
{
    /// <summary>
    /// Test entity class that maps to the TestEntities table in the TestUnitDB database.
    /// </summary>
    /// <remarks>
    /// This class demonstrates the use of <see cref="DBColumnAttribute"/> for property-to-column mapping
    /// and provides a strongly-typed representation of test data for unit testing scenarios.
    /// </remarks>
    public class TestEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the test entity.
        /// </summary>
        [DBColumn("Id", isPrimaryKey: true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the test entity.
        /// </summary>
        [DBColumn("Name", MaxLength = 100)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the test entity.
        /// </summary>
        [DBColumn("Description", MaxLength = 255)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the creation date and time of the test entity.
        /// </summary>
        [DBColumn("CreatedDate")]
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the test entity is active.
        /// </summary>
        [DBColumn("IsActive")]
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the test value for the entity.
        /// </summary>
        [DBColumn("TestValue")]
        public decimal TestValue { get; set; }

        /// <summary>
        /// Gets or sets the test GUID for the entity.
        /// </summary>
        [DBColumn("TestGuid")]
        public Guid TestGuid { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestEntity"/> class.
        /// </summary>
        public TestEntity()
        {
            CreatedDate = DateTime.Now;
            IsActive = true;
            TestGuid = Guid.NewGuid();
        }

        /// <summary>
        /// Creates a test entity with the specified name and description.
        /// </summary>
        /// <param name="name">The name of the test entity.</param>
        /// <param name="description">The description of the test entity.</param>
        /// <returns>A configured <see cref="TestEntity"/> instance.</returns>
        public static TestEntity Create(string name, string description)
        {
            return new TestEntity
            {
                Name = name,
                Description = description,
                TestValue = 0m
            };
        }

        /// <summary>
        /// Creates a test entity with the specified properties.
        /// </summary>
        /// <param name="name">The name of the test entity.</param>
        /// <param name="description">The description of the test entity.</param>
        /// <param name="testValue">The test value for the entity.</param>
        /// <param name="isActive">Whether the entity is active.</param>
        /// <returns>A configured <see cref="TestEntity"/> instance.</returns>
        public static TestEntity Create(string name, string description, decimal testValue, bool isActive = true)
        {
            return new TestEntity
            {
                Name = name,
                Description = description,
                TestValue = testValue,
                IsActive = isActive
            };
        }

        /// <summary>
        /// Returns a string representation of the test entity.
        /// </summary>
        /// <returns>A string containing the entity's key properties.</returns>
        public override string ToString()
        {
            return $"TestEntity {{ Id={Id}, Name='{Name}', IsActive={IsActive}, TestValue={TestValue} }}";
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current test entity.
        /// </summary>
        /// <param name="obj">The object to compare with the current test entity.</param>
        /// <returns>True if the specified object is equal to the current test entity; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (obj is TestEntity other)
            {
                return Id == other.Id && 
                       Name == other.Name && 
                       Description == other.Description &&
                       IsActive == other.IsActive &&
                       TestValue == other.TestValue;
            }
            return false;
        }

        /// <summary>
        /// Returns the hash code for this test entity.
        /// </summary>
        /// <returns>A hash code for the current test entity.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 23 + Id.GetHashCode();
                hash = hash * 23 + (Name?.GetHashCode() ?? 0);
                hash = hash * 23 + (Description?.GetHashCode() ?? 0);
                hash = hash * 23 + IsActive.GetHashCode();
                hash = hash * 23 + TestValue.GetHashCode();
                return hash;
            }
        }
    }
}