# DataStructures Module

## Overview

The **DataStructures** module provides two utility types: a self-balancing AVL binary search tree for ordered, efficient data storage and retrieval, and a static URL helper for combining URL path fragments.

---

## Key Types

| Type | Kind | Namespace | Description |
|------|------|-----------|-------------|
| `BinarySearchTree<T>` | Class | `ByteForge.Toolkit.DataStructures` | Generic self-balancing AVL tree with O(log n) insert, delete, and search |
| `Url` | Static class | `ByteForge.Toolkit.DataStructures` | Combines URL path fragments safely, handling slashes and protocol prefixes |

---

## BinarySearchTree\<T\>

A generic AVL (Adelson-Velsky and Landis) binary search tree. It automatically rebalances after every insert and delete, guaranteeing O(log n) worst-case performance regardless of insertion order.

**Type constraint:** `T : IComparable<T?>`

### Inner class: `Node`

Each node stores a key, optional metadata, left/right child pointers, and an AVL height counter.

| Property | Type | Description |
|----------|------|-------------|
| `Key` | `T` | The element stored in the node |
| `Left` | `Node?` | Left child |
| `Right` | `Node?` | Right child |
| `Height` | `int` | Node height used for AVL balance factor |
| `Value` | `object?` | Optional associated metadata |

### Core methods

| Method | Returns | Description |
|--------|---------|-------------|
| `Insert(T key)` | `void` | Inserts a key and rebalances. Duplicate keys are silently ignored. |
| `Remove(T key)` | `bool` | Removes a key and rebalances. Returns `true` if found. |
| `Contains(T key)` | `bool` | Returns `true` if the key exists in the tree |
| `Find(T key)` | `Node?` | Returns the node for the given key, or `null` |
| `FindMin()` | `T` | Returns the smallest key. Throws `InvalidOperationException` if empty. |
| `FindMax()` | `T` | Returns the largest key. Throws `InvalidOperationException` if empty. |
| `Clear()` | `void` | Removes all nodes |
| `ToArray()` | `T[]` | Returns all keys as an array in sorted (in-order) sequence |
| `Count` | `int` | Number of elements in the tree |
| `IsEmpty` | `bool` | `true` when `Count == 0` |

### Traversal methods

| Method | Order | Description |
|--------|-------|-------------|
| `GetInOrderTraversal()` | Left → Node → Right | Yields keys in ascending sorted order |
| `GetPreOrderTraversal()` | Node → Left → Right | Prefix order; useful for tree serialisation |
| `GetPostOrderTraversal()` | Left → Right → Node | Postfix order |

### Usage examples

```csharp
var tree = new BinarySearchTree<int>();

// Insertions — balanced automatically even in ascending order
foreach (var n in new[] { 50, 30, 70, 20, 40, 60, 80 })
    tree.Insert(n);

// Searching
bool found = tree.Contains(40);   // true
var node   = tree.Find(70);       // Node with Key = 70

// Extremes
int min = tree.FindMin();  // 20
int max = tree.FindMax();  // 80

// Sorted traversal
int[] sorted = tree.GetInOrderTraversal().ToArray();
// [20, 30, 40, 50, 60, 70, 80]

// Deletion
bool removed = tree.Remove(30);   // true; tree rebalances
Console.WriteLine(tree.Count);    // 6
```

### String tree

```csharp
var names = new BinarySearchTree<string>();
names.Insert("Charlie");
names.Insert("Alice");
names.Insert("Bob");

foreach (var name in names.GetInOrderTraversal())
    Console.WriteLine(name);
// Alice
// Bob
// Charlie
```

### Practical patterns

#### Range query

```csharp
var scores = new BinarySearchTree<int>();
// … populate …

var inRange = scores.GetInOrderTraversal()
    .Where(s => s >= 80 && s <= 90)
    .ToList();
```

#### Sorted deduplication

```csharp
var distinct = new BinarySearchTree<string>();
foreach (var item in rawList)
    distinct.Insert(item);   // duplicates are silently dropped

var deduped = distinct.ToArray();
```

### AVL rebalancing

The tree maintains a balance factor (left height − right height) for each node. Whenever an insert or delete makes the factor exceed ±1, one of four rotation types is applied:

| Condition | Rotation |
|-----------|----------|
| Right-heavy subtree | Left rotation |
| Left-heavy subtree | Right rotation |
| Left-heavy with right-heavy child | Left-Right double rotation |
| Right-heavy with left-heavy child | Right-Left double rotation |

### Performance

| Operation | Complexity |
|-----------|------------|
| Insert | O(log n) |
| Delete | O(log n) |
| Search / Find | O(log n) |
| FindMin / FindMax | O(log n) |
| Traversal | O(n) |

### Limitations

- **Not thread-safe.** Use external locking for concurrent access.
- **No duplicates.** A second `Insert` of an existing key is a no-op.
- **Memory overhead.** Each node stores two child pointers plus a height integer in addition to the key.

---

## Url (static utility class)

`Url` provides `Combine` overloads that join URL path fragments the same way `Path.Combine` joins file-system paths, handling protocol prefixes, trailing/leading slashes, and backslash normalisation.

### Methods

| Method | Description |
|--------|-------------|
| `Combine(string url1, string url2)` | Joins two URL fragments |
| `Combine(string url1, string url2, string url3)` | Joins three fragments |
| `Combine(string url1, string url2, string url3, string url4)` | Joins four fragments |
| `Combine(params string[] urls)` | Joins any number of fragments |

### Behaviour

- Backslashes are normalised to forward slashes.
- Duplicate slashes are collapsed to a single slash.
- If `url2` begins with a protocol (`http://`, `https://`, `ftp://`) or is an absolute path (`/`), it takes precedence and `url1` is discarded (same as `Path.Combine` on Windows with an absolute second argument).
- The result is not percent-encoded; inputs are expected to be already valid URL-encoded strings.

### Usage

```csharp
// Basic joining
Url.Combine("https://api.example.com", "users", "123");
// → "https://api.example.com/users/123"

// Trailing/leading slashes are handled
Url.Combine("https://api.example.com/", "/users/", "/profile");
// → "https://api.example.com/users/profile"

// Absolute second segment resets the result
Url.Combine("https://api.example.com/v1", "https://cdn.example.com/assets");
// → "https://cdn.example.com/assets"

// Arbitrary number of segments
Url.Combine("base", "api", "v2", "orders", "detail");
// → "base/api/v2/orders/detail"
```

---

## 📖 Documentation Links

| Module | Description |
|--------|-------------|
| **[CLI](../CommandLine/readme.md)** | Command-line parsing |
| **[Configuration](../Configuration/readme.md)** | INI-based configuration |
| **[Core](../Core/readme.md)** | Core utilities |
| **[Data](../Data/readme.md)** | Database and file processing |
| **[JSON](../Json/readme.md)** | Delta serialisation |
| **[Logging](../Logging/readme.md)** | Structured logging |
| **[Mail](../Mail/readme.md)** | Email processing |
| **[Net](../Net/readme.md)** | Network file transfers |
| **[Security](../Security/readme.md)** | Encryption and security |
| **[Utils](../Utilities/readme.md)** | General utilities |
