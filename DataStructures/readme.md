# DataStructures.md

## ByteForge Data Structures

Efficient, reusable structures and utilities for data organization and manipulation. 
Includes a self-balancing AVL tree and a URL utility class.

### 🚀 Features

#### BinarySearchTree<T>
- AVL-based self-balancing tree
- Insert, Delete, Search, In-order traversal
- Thread-safe count tracking
- Conversion to array

#### Url
- Safe combination of multiple URL segments
- Extract domain from full URL
- Handles duplicate slashes, nulls, and FTP/HTTP

### 🧪 Tree Usage
```csharp
var tree = new BinarySearchTree<int>();
tree.Insert(5);
tree.Insert(3);
tree.Contains(3); // true
tree.Remove(3);
int[] sorted = tree.ToArray();
```

### 🧪 URL Usage
```csharp
string url = Url.Combine("http://example.com", "api", "v1");
string domain = Url.GetDomain(url);
```

### ✅ Best Practices
- Use `ToArray()` for sorted output
- Validate inputs when using `Url.GetDomain`
- Ensure `T : IComparable<T>` for BST

### 🔗 Related Modules
- [Utils](../Utils/readme.md)
- [CSV](../Data/CSV/readme.md)
