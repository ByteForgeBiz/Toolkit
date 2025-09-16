using System;

namespace ByteForge.Toolkit.Tests.Models
{
    /// <summary>
    /// Test entity class that maps to the BulkTestEntities table for bulk operation testing.
    /// </summary>
    /// <remarks>
    /// This class is specifically designed for testing bulk operations including insert, upsert,
    /// and delete scenarios. It includes a primary key, unique constraint, and various data types
    /// to validate comprehensive bulk processing capabilities.
    /// </remarks>
    public class BulkTestEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the bulk test entity.
        /// </summary>
        [DBColumn("Id", isPrimaryKey: true)]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the unique code for the bulk test entity.
        /// </summary>
        /// <remarks>
        /// This property has a unique constraint in the database and is used for upsert operations.
        /// </remarks>
        [DBColumn("Code", isUnique: true, MaxLength = 50)]
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the name of the bulk test entity.
        /// </summary>
        [DBColumn("Name", MaxLength = 200)]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the category of the bulk test entity.
        /// </summary>
        [DBColumn("Category", MaxLength = 100)]
        public string? Category { get; set; }

        /// <summary>
        /// Gets or sets the value associated with the bulk test entity.
        /// </summary>
        [DBColumn("Value")]
        public decimal Value { get; set; }

        /// <summary>
        /// Gets or sets the priority of the bulk test entity.
        /// </summary>
        [DBColumn("Priority")]
        public int Priority { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the bulk test entity is active.
        /// </summary>
        [DBColumn("IsActive")]
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the creation date and time of the bulk test entity.
        /// </summary>
        [DBColumn("CreatedDate")]
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the last modification date and time of the bulk test entity.
        /// </summary>
        [DBColumn("ModifiedDate")]
        public DateTime ModifiedDate { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkTestEntity"/> class.
        /// </summary>
        public BulkTestEntity()
        {
            Id = Guid.NewGuid();
            IsActive = true;
            CreatedDate = DateTime.Now;
            ModifiedDate = DateTime.Now;
            Priority = 1;
            Value = 0m;
        }

        /// <summary>
        /// Creates a bulk test entity with the specified code and name.
        /// </summary>
        /// <param name="code">The unique code for the entity.</param>
        /// <param name="name">The name of the entity.</param>
        /// <returns>A configured <see cref="BulkTestEntity"/> instance.</returns>
        public static BulkTestEntity Create(string code, string name)
        {
            return new BulkTestEntity
            {
                Code = code,
                Name = name
            };
        }

        /// <summary>
        /// Creates a bulk test entity with comprehensive properties.
        /// </summary>
        /// <param name="code">The unique code for the entity.</param>
        /// <param name="name">The name of the entity.</param>
        /// <param name="category">The category of the entity.</param>
        /// <param name="value">The value associated with the entity.</param>
        /// <param name="priority">The priority of the entity.</param>
        /// <param name="isActive">Whether the entity is active.</param>
        /// <returns>A configured <see cref="BulkTestEntity"/> instance.</returns>
        public static BulkTestEntity Create(string code, string name, string category, decimal value, int priority = 1, bool isActive = true)
        {
            return new BulkTestEntity
            {
                Code = code,
                Name = name,
                Category = category,
                Value = value,
                Priority = priority,
                IsActive = isActive
            };
        }

        /// <summary>
        /// Creates a collection of bulk test entities for batch testing.
        /// </summary>
        /// <param name="count">The number of entities to create.</param>
        /// <param name="prefix">The prefix for entity codes and names.</param>
        /// <returns>An array of configured <see cref="BulkTestEntity"/> instances.</returns>
        public static BulkTestEntity[] CreateBatch(int count, string prefix = "BULK")
        {
            var entities = new BulkTestEntity[count];
            var categories = new[] { "Category A", "Category B", "Category C", "Category D", "Category E" };
            var random = new Random();

            for (int i = 0; i < count; i++)
            {
                entities[i] = new BulkTestEntity
                {
                    Code = $"{prefix}{i + 1:D4}",
                    Name = $"{prefix} Test Entity {i + 1}",
                    Category = categories[i % categories.Length],
                    Value = (decimal)(random.NextDouble() * 1000),
                    Priority = (i % 10) + 1,
                    IsActive = i % 10 != 0 // Every 10th entity is inactive
                };
            }

            return entities;
        }

        /// <summary>
        /// Updates the modified date to the current time.
        /// </summary>
        public void Touch()
        {
            ModifiedDate = DateTime.Now;
        }

        /// <summary>
        /// Creates a copy of the current entity with updated values for upsert testing.
        /// </summary>
        /// <param name="newName">The new name for the entity.</param>
        /// <param name="newValue">The new value for the entity.</param>
        /// <returns>A new <see cref="BulkTestEntity"/> with updated values.</returns>
        public BulkTestEntity CreateUpdatedCopy(string newName, decimal newValue)
        {
            return new BulkTestEntity
            {
                Id = Id, // Keep the same ID
                Code = Code, // Keep the same unique code
                Name = newName,
                Category = Category,
                Value = newValue,
                Priority = Priority,
                IsActive = IsActive,
                CreatedDate = CreatedDate,
                ModifiedDate = DateTime.Now
            };
        }

        /// <summary>
        /// Returns a string representation of the bulk test entity.
        /// </summary>
        /// <returns>A string containing the entity's key properties.</returns>
        public override string ToString()
        {
            return $"BulkTestEntity {{ Id={Id}, Code='{Code}', Name='{Name}', Category='{Category}', Value={Value}, IsActive={IsActive} }}";
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current bulk test entity.
        /// </summary>
        /// <param name="obj">The object to compare with the current bulk test entity.</param>
        /// <returns>True if the specified object is equal to the current bulk test entity; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (obj is BulkTestEntity other)
            {
                return Id == other.Id && 
                       Code == other.Code && 
                       Name == other.Name && 
                       Category == other.Category &&
                       Value == other.Value &&
                       Priority == other.Priority &&
                       IsActive == other.IsActive;
            }
            return false;
        }

        /// <summary>
        /// Returns the hash code for this bulk test entity.
        /// </summary>
        /// <returns>A hash code for the current bulk test entity.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 23 + Id.GetHashCode();
                hash = hash * 23 + (Code?.GetHashCode() ?? 0);
                hash = hash * 23 + (Name?.GetHashCode() ?? 0);
                hash = hash * 23 + (Category?.GetHashCode() ?? 0);
                hash = hash * 23 + Value.GetHashCode();
                hash = hash * 23 + Priority.GetHashCode();
                hash = hash * 23 + IsActive.GetHashCode();
                return hash;
            }
        }
    }
}