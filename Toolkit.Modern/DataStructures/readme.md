# DataStructures Module

## Overview

The **DataStructures** module provides efficient, well-tested data structures for storing and retrieving data in organized ways. It includes a self-balancing binary search tree and URL parsing utilities.

---

## Purpose

Complex applications need specialized data structures that:
1. **Maintain order** - Keep data sorted for efficient searching
2. **Balance automatically** - Prevent tree skewing and degradation
3. **Support traversal** - Iterate in different orders (in-order, pre-order, post-order)
4. **Handle URLs** - Parse and manipulate URLs reliably
5. **Perform efficiently** - O(log n) operations for balanced trees

---

## Key Classes

### `BinarySearchTree<T>`
**Purpose:** Self-balancing AVL (Adelson-Velsky and Landis) binary search tree.

**Generic Type Parameter:** 
- `T` - Element type, must implement `IComparable<T?>`

**Key Characteristics:**
- **Self-balancing** - Maintains height balance through rotations
- **Generic** - Works with any comparable type
- **O(log n)** - Efficient insert, delete, search
- **Complete traversal support** - In-order, pre-order, post-order

#### Core Methods

**Basic Operations:**
```csharp
// Insert element
void Insert(T key);

// Remove element
bool Remove(T key);

// Search for element
bool Contains(T key);

// Find specific node
BinarySearchTree<T>.Node Find(T key);

// Clear all elements
void Clear();
```

**Tree Navigation:**
```csharp
// Find extremes
T FindMin();  // Smallest element
T FindMax();  // Largest element

// Get count and status
int Count { get; }
bool IsEmpty { get; }
```

**Traversals:**
```csharp
// Convert to array (in-order)
T[] ToArray();

// Enumerable traversals
IEnumerable<T> GetInOrderTraversal();  // Left, Node, Right (sorted)
IEnumerable<T> GetPreOrderTraversal(); // Node, Left, Right (prefix)
IEnumerable<T> GetPostOrderTraversal();  // Left, Right, Node (postfix)
```

#### Inner Class: `Node`
**Purpose:** Represents a single tree node.

**Properties:**
```csharp
T Key { get; set; }          // Element stored in node
Node? Left { get; set; }      // Left subtree
Node? Right { get; set; }     // Right subtree
int Height { get; internal set; }  // Node height (for balance)
object? Value { get; set; }   // Associated metadata
```

---

### Usage Patterns

#### Creating and Populating

```csharp
// Create tree for integers
var intTree = new BinarySearchTree<int>();
intTree.Insert(50);
intTree.Insert(30);
intTree.Insert(70);
intTree.Insert(20);
intTree.Insert(40);
intTree.Insert(60);
intTree.Insert(80);

// Tree structure (automatically balanced):
//        50
//       /  \
//      30   70
//     / \   / \
//    20 40 60 80
```

#### Searching

```csharp
var tree = new BinarySearchTree<string>();
tree.Insert("Apple");
tree.Insert("Zebra");
tree.Insert("Mango");

// Check if element exists
if (tree.Contains("Mango"))
{
    Console.WriteLine("Found!");
}

// Find exact node
var node = tree.Find("Apple");
if (node != null)
{
    Console.WriteLine($"Found: {node.Key}");
}
```

#### Removing Elements

```csharp
var tree = new BinarySearchTree<int>();
// ... populate tree ...

// Remove element
bool removed = tree.Remove(30);  // Returns true if found and removed

Console.WriteLine($"Tree size: {tree.Count}");
```

#### Traversing

```csharp
var tree = new BinarySearchTree<int>();
var numbers = new[] { 50, 30, 70, 20, 40, 60, 80 };
foreach (var num in numbers)
    tree.Insert(num);

// In-order traversal (sorted)
Console.WriteLine("In-order (sorted):");
foreach (var value in tree.GetInOrderTraversal())
    Console.Write($"{value} ");  // Output: 20 30 40 50 60 70 80

// Pre-order traversal (prefix)
Console.WriteLine("\nPre-order (prefix):");
foreach (var value in tree.GetPreOrderTraversal())
    Console.Write($"{value} ");  // Output: 50 30 20 40 70 60 80

// Post-order traversal (postfix)
Console.WriteLine("\nPost-order (postfix):");
foreach (var value in tree.GetPostOrderTraversal())
    Console.Write($"{value} ");  // Output: 20 40 30 60 80 70 50
```

#### Finding Min/Max

```csharp
var tree = new BinarySearchTree<int>();
foreach (var num in new[] { 50, 30, 70, 20, 40, 60, 80 })
    tree.Insert(num);

int min = tree.FindMin();  // 20
int max = tree.FindMax();  // 80
```

---

### AVL Balancing Algorithm

The tree maintains balance through automatic rotations:

#### Balance Factor
**Definition:** Height(left subtree) - Height(right subtree)
- **+1, 0, -1:** Balanced
- **< -1:** Right-heavy (right rotation needed)
- **> +1:** Left-heavy (left rotation needed)

#### Rotation Types

**Left Rotation** (fix right-heavy trees):
```
    x           y
     \         / \
      y       x   z
       \
        z
```

**Right Rotation** (fix left-heavy trees):
```
        z         x
       /         / \
      x    =>   w   z
     /
    w
```

**Left-Right Rotation** (fix left-heavy with right-heavy child):
```
Perform left rotation on left child, then right rotation on parent
```

**Right-Left Rotation** (fix right-heavy with left-heavy child):
```
Perform right rotation on right child, then left rotation on parent
```

#### Automatic Rebalancing

The tree automatically rebalances after each insert/delete:

```csharp
var tree = new BinarySearchTree<int>();
tree.Insert(1);
tree.Insert(2);
tree.Insert(3);
tree.Insert(4);
tree.Insert(5);

// Without rebalancing, would be: 1 -> 2 -> 3 -> 4 -> 5 (linked list)
// With AVL balancing:
//      3
//    /   \
//   2     4
//  /       \
// 1         5
```

---

### Performance Characteristics

| Operation   | Best     | Average   | Worst    |
|-------------|----------|-----------|----------|
| Insert      | O(1)     | O(log n)  | O(log n) |
| Delete      | O(log n) | O(log n)  | O(log n) |
| Search      | O(log n) | O(log n)  | O(log n) |
| Min/Max     | O(log n) | O(log n)  | O(log n) |
| Traversal   | O(n)     | O(n)      | O(n)     |

**Comparison with Other Structures:**
- **Unordered List:** Insert O(1), Search O(n), Delete O(n)
- **Sorted List:** Insert O(n), Search O(log n), Delete O(n)
- **Hash Table:** Insert O(1), Search O(1), No ordering
- **AVL Tree:** Insert O(log n), Search O(log n), Delete O(log n), Maintains order

---

### Practical Applications

#### 1. Sorted Data Queries
```csharp
// Build database index
var index = new BinarySearchTree<(string lastName, string firstName, int id)>();
foreach (var person in persons)
    index.Insert((person.LastName, person.FirstName, person.Id));

// Query: Get all people in last name range
var results = index.GetInOrderTraversal()
    .Where(p => p.lastName >= "Smith" && p.lastName <= "Wilson")
    .ToList();
```

#### 2. Event Scheduling
```csharp
// Events sorted by time
var events = new BinarySearchTree<(DateTime time, string description)>();
events.Insert((DateTime.Now, "Meeting"));
events.Insert((DateTime.Now.AddHours(1), "Lunch"));
events.Insert((DateTime.Now.AddMinutes(30), "Break"));

// Get chronological order
foreach (var (@event, description) in events.GetInOrderTraversal())
    Console.WriteLine($"{@event}: {description}");
```

#### 3. Range Queries
```csharp
var tree = new BinarySearchTree<int>();
// ... populate with scores ...

// Get scores in range 80-90
var inRange = tree.GetInOrderTraversal()
    .Where(score => score >= 80 && score <= 90)
    .ToList();
```

---

### Exception Handling

```csharp
var tree = new BinarySearchTree<int>();

try
{
    // Empty tree operations
    int min = tree.FindMin();  // Throws InvalidOperationException
}
catch (InvalidOperationException ex)
{
    Console.WriteLine("Tree is empty");
}

try
{
    // Type constraint violation
    var stringTree = new BinarySearchTree<string>();
    // Must implement IComparable<string?>
}
catch (Exception ex)
{
    // Type must be comparable
}
```

---

### Testing

```csharp
[TestMethod]
public void BinarySearchTree_InsertAndFind_ReturnsElement()
{
    var tree = new BinarySearchTree<int>();
    tree.Insert(50);
    tree.Insert(30);
    tree.Insert(70);

    Assert.IsTrue(tree.Contains(30));
    Assert.IsTrue(tree.Contains(70));
 Assert.IsFalse(tree.Contains(40));
}

[TestMethod]
public void BinarySearchTree_Balanced_MaintainsBalance()
{
    var tree = new BinarySearchTree<int>();
    // Insert in ascending order - would be skewed without balancing
    for (int i = 1; i <= 100; i++)
        tree.Insert(i);
  
    // Tree should still have ~log2(100) ≈ 7 height, not 100
    Assert.IsTrue(tree.Count == 100);
}

[TestMethod]
public void BinarySearchTree_InOrderTraversal_ReturnsSorted()
{
    var tree = new BinarySearchTree<int>();
    foreach (var num in new[] { 50, 30, 70, 20, 40, 60, 80 })
 tree.Insert(num);
 
    var sorted = tree.GetInOrderTraversal().ToArray();
    Assert.IsTrue(sorted.SequenceEqual(new[] { 20, 30, 40, 50, 60, 70, 80 }));
}
```

---

### Limitations

- **Not thread-safe** - Requires external synchronization for multi-threaded use
- **Generic only** - Must implement IComparable<T?>
- **Memory overhead** - Each node stores pointers and height
- **No duplicate handling** - Silently ignores duplicate inserts

---

### Url Class

**Purpose:** Parse and manipulate URLs.

**Key Methods:**
```csharp
// Parse URL
Url url = new Url("https://api.example.com/users/123?sort=name&limit=10");

// Component access
string protocol = url.Protocol;  // "https"
string host = url.Host;          // "api.example.com"
string path = url.Path;   // "/users/123"
string query = url.Query;  // "sort=name&limit=10"

// Query parameter access
string? sort = url.GetQueryParameter("sort");  // "name"
string? limit = url.GetQueryParameter("limit");  // "10"

// URL manipulation
url.SetQueryParameter("sort", "date");
url.RemoveQueryParameter("limit");
```

**Features:**
- URL parsing and validation
- Query parameter manipulation
- URL encoding/decoding
- Component extraction
- Fragment handling

---

## File Organization

### `BinarySearchTree.cs`
Complete AVL tree implementation.

**Contains:**
- Generic tree class
- Node inner class
- Insertion with balancing
- Deletion with rebalancing
- Traversal methods
- Search and min/max operations

### `Url.cs`
URL parsing and manipulation.

---

## Summary

The DataStructures module provides:

**Key Strengths:**
- ✓ Self-balancing AVL tree (O(log n) operations)
- ✓ Generic, type-safe implementation
- ✓ Multiple traversal orders
- ✓ Automatic balance maintenance
- ✓ Efficient for sorted data operations
- ✓ URL parsing and manipulation

**Best For:**
- Maintaining sorted datasets
- Implementing indexes
- Range queries
- Event scheduling
- Efficient searching

**Not Ideal For:**
- Unsorted data operations (use List<T>)
- Highly frequent insertions/deletions (consider hash tables)
- Multi-threaded scenarios (add synchronization)
- Duplicate-heavy datasets (not supported)

---

## 📖 Documentation Links

### 🏗️ Related Modules
| Module                                          | Description                |
|-------------------------------------------------|----------------------------|
| **[CLI](../CommandLine/readme.md)**             | Command-line parsing       |
| **[Configuration](../Configuration/readme.md)** | INI-based configuration    |
| **[Core](../Core/readme.md)**                   | Core utilities             |
| **[Data](../Data/readme.md)**                   | Database & file processing |
| **[JSON](../Json/readme.md)**                   | Delta serialization        |
| **[Logging](../Logging/readme.md)**             | Structured logging         |
| **[Mail](../Mail/readme.md)**                   | Email processing           |
| **[Net](../Net/readme.md)**                     | Network file transfers     |
| **[Security](../Security/readme.md)**           | Encryption & security      |
| **[Utils](../Utilities/readme.md)**             | General utilities          |
