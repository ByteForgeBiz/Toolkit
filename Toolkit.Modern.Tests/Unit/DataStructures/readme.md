# DataStructures Unit Tests

Tests for `ByteForge.Toolkit.DataStructures`.

**Test classes:** `BinarySearchTreeTests`, `UrlTests`
**Test categories:** `Unit`, `DataStructures`
**Source module:** `Toolkit.Modern/DataStructures/`

## Test Classes

### BinarySearchTreeTests

Validates `BinarySearchTree<T>`, a generic self-ordering binary search tree.

| Test area | Coverage |
|-----------|---------|
| Constructor | Empty tree: `Count = 0`, `IsEmpty = true` |
| `Insert` | Single value, duplicate values, ordered and unordered sequences |
| `Remove` | Leaf node, node with one child, node with two children |
| `Contains` | Found and not-found cases |
| `Count` / `IsEmpty` | Updated correctly after insertions and removals |
| In-order traversal | `GetInOrderTraversal()` returns values in sorted ascending order |
| Min / Max | Finding the minimum and maximum values in the tree |
| Large datasets | Performance with many values |
| Edge cases | Empty tree operations, single-node tree, all-duplicate values |

`AssertionHelpers.AssertBinarySearchTreeOrdering` is used to verify that in-order traversal produces a sorted sequence matching the inserted values.

### UrlTests

Validates the `Url` utility class, which provides URL combination and manipulation.

| Test area | Coverage |
|-----------|---------|
| `Url.Combine(url1, url2)` | Basic combination with correct slash handling |
| Trailing/leading slashes | Redundant slashes are normalized |
| Empty/null segments | Graceful handling without throwing |
| Multiple segments | `Url.Combine(base, seg1, seg2, ...)` |
| Query strings | Combined URL retains query parameters |
| Fragment handling | Fragment (`#anchor`) is preserved |
| Special characters | Encoded characters pass through unmodified |
| Edge cases | Both segments empty, one segment empty |

## Prerequisites

No external dependencies. All tests are fully in-memory.

## Running These Tests

```powershell
# All DataStructures tests
dotnet test --filter "TestCategory=DataStructures"

# By class
dotnet test --filter "FullyQualifiedName~BinarySearchTreeTests"
dotnet test --filter "FullyQualifiedName~UrlTests"
```

---

## Documentation Links

| Location | Description | Documentation |
|----------|-------------|---------------|
| **Tests root** | Test project overview | [../../README.md](../../README.md) |
| **Unit overview** | Unit test organization | [../readme.md](../readme.md) |
| **Helpers** | Test helper classes | [../../Helpers/README.md](../../Helpers/README.md) |
