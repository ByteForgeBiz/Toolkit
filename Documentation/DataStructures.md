# ByteForge.Toolkit Data Structures

ByteForge.Toolkit is a .NET library targeting .NET Framework 4.8 that provides essential data structures and utility classes for common programming tasks.

## Components

### BinarySearchTree<T>

A generic implementation of a self-balancing binary search tree (AVL Tree) that provides efficient operations for storing and retrieving ordered data.

#### Features

- Generic implementation supporting any comparable type
- Self-balancing using AVL tree rotations
- Supports standard tree operations:
  - Insertion
  - Deletion
  - Search
  - In-order traversal
- Maintains tree height balance for optimal performance
- Thread-safe node count tracking
- Conversion to array functionality

#### Usage

```csharp
var tree = new BinarySearchTree<int>();

// Insert elements
tree.Insert(5);
tree.Insert(3);
tree.Insert(7);

// Check if element exists
bool contains = tree.Contains(3); // returns true

// Remove element
tree.Remove(3);

// Convert to sorted array
int[] sorted = tree.ToArray();
```

#### Performance

All major operations (insert, delete, search) have an average and worst-case time complexity of O(log n), where n is the number of nodes in the tree.

### Url

A static utility class providing URL manipulation and parsing functionality.

#### Features

- URL combination with proper slash handling
- Support for multiple URL fragment combinations
- Domain extraction
- Handles HTTP, HTTPS, and FTP protocols
- Proper handling of duplicate slashes
- Null-safe operations

#### Usage

```csharp
// Combine URLs
string url = Url.Combine("http://example.com", "api", "v1", "users");
// Result: "http://example.com/api/v1/users"

// Get domain
string domain = Url.GetDomain("https://api.example.com/v1/users");
// Result: "api.example.com"

// Combine multiple URL fragments
string[] fragments = new[] { "http://example.com", "api", "v1", "users" };
string combined = Url.Combine(fragments);
```

## Requirements

- .NET Framework 4.8
- Compatible with SQL Server 2000 when used in database applications

## Best Practices

When using these components:

1. For BinarySearchTree<T>:
   - Ensure that type T implements IComparable<T>
   - Consider using the ToArray() method for efficient sorted data extraction
   - Track the Count property for efficient size operations

2. For Url:
   - Use the Combine methods instead of manual string concatenation
   - Always validate URLs before processing
   - Handle potential URI format exceptions when using GetDomain
