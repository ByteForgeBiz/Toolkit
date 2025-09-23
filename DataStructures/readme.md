# DataStructures.md

## ByteForge Data Structures

Efficient, reusable structures and utilities for data organization and manipulation. 
Includes a self-balancing AVL tree and a URL utility class.

### 🚀 Features

#### BinarySearchTree<T>
- **AVL-based self-balancing**: Maintains O(log n) performance for all operations
- **Core operations**: Insert, Delete, Search, In-order traversal
- **Thread-safe count tracking**: Accurate node count with concurrent access
- **Array conversion**: Efficient sorted array generation via in-order traversal
- **Generic constraint**: Works with any `IComparable<T>` type

#### Url
- **Safe URL combination**: Intelligently combines multiple URL segments
- **Domain extraction**: Extracts domain from full URLs with protocol handling
- **Robust parsing**: Handles duplicate slashes, nulls, empty strings, and various protocols (HTTP/HTTPS/FTP)
- **Cross-platform**: Works with both forward and backward slashes

### 🧪 Tree Usage
```csharp
// Create and populate a binary search tree
var tree = new BinarySearchTree<int>();
tree.Insert(5);
tree.Insert(3);
tree.Insert(8);
tree.Insert(1);
tree.Insert(7);

// Search operations
bool exists = tree.Contains(3); // true
int count = tree.Count;         // 5

// Remove elements
bool removed = tree.Remove(3);  // true

// Get sorted array (in-order traversal)
int[] sorted = tree.ToArray();  // [1, 5, 7, 8]

// Works with strings too
var stringTree = new BinarySearchTree<string>();
stringTree.Insert("banana");
stringTree.Insert("apple");
stringTree.Insert("cherry");
string[] sortedFruits = stringTree.ToArray(); // ["apple", "banana", "cherry"]
```

### 🧪 URL Usage
```csharp
// Basic URL combination
string url = Url.Combine("http://example.com", "api", "v1");
// Result: "http://example.com/api/v1"

// Handle multiple segments
string apiUrl = Url.Combine("https://api.service.com/", "/users/", "123/", "/profile");
// Result: "https://api.service.com/users/123/profile"

// Extract domain from URLs
string domain1 = Url.GetDomain("https://www.example.com/path/page.html");
// Result: "www.example.com"

string domain2 = Url.GetDomain("ftp://files.company.org/documents/");
// Result: "files.company.org"

// Handle edge cases gracefully
string safe1 = Url.Combine("", "path/to/resource");      // "path/to/resource"
string safe2 = Url.Combine("http://base.com", "");       // "http://base.com"
string safe3 = Url.Combine(null, "relative/path");       // "relative/path"
```

### ✅ Best Practices

#### Binary Search Tree
- Ensure your type implements `IComparable<T>` for custom objects
- Use `ToArray()` when you need sorted output from the tree
- Consider the tree for scenarios requiring frequent insertions/deletions with sorted access
- The tree maintains balance automatically, so no manual rebalancing is needed

#### URL Utilities
- Always validate inputs when using `Url.GetDomain()` with user-provided URLs
- Use `Url.Combine()` instead of string concatenation for URL building
- The utilities handle null/empty inputs gracefully, but validate business logic requirements
- Test URL combinations with your specific use cases, especially with different protocols

---

## 📚 Modules

| Module | Description |
|--------|-------------|
| [🏠 Home](../readme.md) | ByteForge.Toolkit main documentation |
| [CommandLine](../CLI/readme.md) | Attribute-based CLI parsing with aliasing, typo correction, and plugin support |
| [Configuration](../Configuration/readme.md) | INI-based configuration system with typed section support |
| [Data](../Data/readme.md) | Comprehensive data processing with CSV, Database, Audio, and Exception handling |
| [DataStructures](../DataStructures/readme.md) | AVL tree and URL utility classes |
| [Logging](../Logging/readme.md) | Thread-safe logging system with async file/console output |
| [Mail](../Mail/readme.md) | Email utility with HTML support and attachment handling |
| [Net](../Net/readme.md) | FTP/FTPS/SFTP high-level transfer client |
| [Security](../Security/readme.md) | AES-based string encryption with key generation and Galois Field logic |
| [Utils](../Utils/readme.md) | Miscellaneous helpers: timing, path utilities, progress bar |
| [Core](../Core/readme.md) | Embedded resource deployment (WinSCP) |
| [HTML](../HTML/readme.md) | NPD UI framework components |
