using System;
using System.Collections.Generic;
using System.Linq;
using ByteForge.Toolkit;
using ByteForge.Toolkit.Tests.Helpers;
using FluentAssertions;

namespace ByteForge.Toolkit.Tests.Unit.DataStructures
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("DataStructures")]
    public class BinarySearchTreeTests
    {
        /// <summary>
        /// Verifies that the constructor creates an empty binary search tree.
        /// </summary>
        /// <remarks>
        /// Ensures the tree is initialized correctly for subsequent operations.
        /// </remarks>
        [TestMethod]
        public void Constructor_ShouldCreateEmptyTree()
        {
            // Arrange & Act
            var tree = new BinarySearchTree<int>();

            // Assert
            tree.Should().NotBeNull();
            tree.Count.Should().Be(0);
            tree.IsEmpty.Should().BeTrue();
        }

        /// <summary>
        /// Inserts a single value and verifies root node creation.
        /// </summary>
        /// <remarks>
        /// Ensures the first insert sets up the tree structure.
        /// </remarks>
        [TestMethod]
        public void Insert_SingleValue_ShouldCreateRootNode()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();

            // Act
            tree.Insert(10);

            // Assert
            tree.Count.Should().Be(1);
            tree.IsEmpty.Should().BeFalse();
            tree.Contains(10).Should().BeTrue();
        }

        /// <summary>
        /// Inserts multiple values and verifies BST property is maintained.
        /// </summary>
        /// <remarks>
        /// Ensures the tree maintains correct ordering for search efficiency.
        /// </remarks>
        [TestMethod]
        public void Insert_MultipleValues_ShouldMaintainBSTProperty()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();
            var values = new[] { 10, 5, 15, 3, 7, 12, 17 };

            // Act
            foreach (var value in values)
            {
                tree.Insert(value);
            }

            // Assert
            tree.Count.Should().Be(values.Length);
            AssertionHelpers.AssertBinarySearchTreeOrdering(tree, values);
        }

        /// <summary>
        /// Inserts duplicate values and verifies graceful handling.
        /// </summary>
        /// <remarks>
        /// Ensures duplicates do not corrupt the tree or inflate the count.
        /// </remarks>
        [TestMethod]
        public void Insert_DuplicateValues_ShouldHandleGracefully()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();
            tree.Insert(10);

            // Act
            tree.Insert(10); // Duplicate

            // Assert
            tree.Count.Should().Be(1, "duplicate values should not increase count");
            tree.Contains(10).Should().BeTrue();
        }

        /// <summary>
        /// Verifies that Contains returns true for existing values.
        /// </summary>
        /// <remarks>
        /// Ensures search functionality works for inserted values.
        /// </remarks>
        [TestMethod]
        public void Contains_ExistingValue_ShouldReturnTrue()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();
            var values = new[] { 10, 5, 15, 3, 7, 12, 17 };
            foreach (var value in values)
            {
                tree.Insert(value);
            }

            // Act & Assert
            foreach (var value in values)
            {
                tree.Contains(value).Should().BeTrue($"tree should contain {value}");
            }
        }

        /// <summary>
        /// Verifies that Contains returns false for non-existing values.
        /// </summary>
        /// <remarks>
        /// Ensures search functionality does not return false positives.
        /// </remarks>
        [TestMethod]
        public void Contains_NonExistingValue_ShouldReturnFalse()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();
            var values = new[] { 10, 5, 15 };
            foreach (var value in values)
            {
                tree.Insert(value);
            }

            // Act & Assert
            tree.Contains(99).Should().BeFalse();
            tree.Contains(1).Should().BeFalse();
            tree.Contains(0).Should().BeFalse();
        }

        /// <summary>
        /// Verifies that Contains returns false for an empty tree.
        /// </summary>
        /// <remarks>
        /// Ensures search functionality is robust for empty trees.
        /// </remarks>
        [TestMethod]
        public void Contains_EmptyTree_ShouldReturnFalse()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();

            // Act & Assert
            tree.Contains(10).Should().BeFalse();
        }

        /// <summary>
        /// Removes a leaf node and verifies successful removal.
        /// </summary>
        /// <remarks>
        /// Ensures leaf removal does not affect tree structure.
        /// </remarks>
        [TestMethod]
        public void Remove_LeafNode_ShouldRemoveSuccessfully()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();
            var values = new[] { 10, 5, 15, 3, 7 };
            foreach (var value in values)
            {
                tree.Insert(value);
            }

            // Act
            var result = tree.Remove(3); // Leaf node

            // Assert
            result.Should().BeTrue();
            tree.Contains(3).Should().BeFalse();
            tree.Count.Should().Be(values.Length - 1);
            
            // Verify remaining elements are still properly ordered
            var remaining = values.Where(v => v != 3);
            AssertionHelpers.AssertBinarySearchTreeOrdering(tree, remaining);
        }

        /// <summary>
        /// Removes a node with one child and verifies restructuring.
        /// </summary>
        /// <remarks>
        /// Ensures tree remains valid after removing nodes with one child.
        /// </remarks>
        [TestMethod]
        public void Remove_NodeWithOneChild_ShouldRemoveAndRestructure()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();
            var values = new[] { 10, 5, 15, 3 }; // 5 has only left child (3)
            foreach (var value in values)
            {
                tree.Insert(value);
            }

            // Act
            var result = tree.Remove(5);

            // Assert
            result.Should().BeTrue();
            tree.Contains(5).Should().BeFalse();
            tree.Count.Should().Be(values.Length - 1);
            
            // Verify structure is maintained
            tree.Contains(3).Should().BeTrue();
            tree.Contains(10).Should().BeTrue();
            tree.Contains(15).Should().BeTrue();
        }

        /// <summary>
        /// Removes a node with two children and verifies restructuring.
        /// </summary>
        /// <remarks>
        /// Ensures tree remains valid after removing nodes with two children.
        /// </remarks>
        [TestMethod]
        public void Remove_NodeWithTwoChildren_ShouldRemoveAndRestructure()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();
            var values = new[] { 10, 5, 15, 3, 7, 12, 17 };
            foreach (var value in values)
            {
                tree.Insert(value);
            }

            // Act
            var result = tree.Remove(10); // Root node with two children

            // Assert
            result.Should().BeTrue();
            tree.Contains(10).Should().BeFalse();
            tree.Count.Should().Be(values.Length - 1);
            
            // Verify remaining elements maintain BST property
            var remaining = values.Where(v => v != 10);
            AssertionHelpers.AssertBinarySearchTreeOrdering(tree, remaining);
        }

        /// <summary>
        /// Attempts to remove a non-existing value, expecting false.
        /// </summary>
        /// <remarks>
        /// Ensures removal does not affect tree for missing values.
        /// </remarks>
        [TestMethod]
        public void Remove_NonExistingValue_ShouldReturnFalse()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();
            var values = new[] { 10, 5, 15 };
            foreach (var value in values)
            {
                tree.Insert(value);
            }

            // Act
            var result = tree.Remove(99);

            // Assert
            result.Should().BeFalse();
            tree.Count.Should().Be(values.Length, "count should remain unchanged");
        }

        /// <summary>
        /// Attempts to remove from an empty tree, expecting false.
        /// </summary>
        /// <remarks>
        /// Ensures removal is safe for empty trees.
        /// </remarks>
        [TestMethod]
        public void Remove_FromEmptyTree_ShouldReturnFalse()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();

            // Act
            var result = tree.Remove(10);

            // Assert
            result.Should().BeFalse();
            tree.Count.Should().Be(0);
        }

        /// <summary>
        /// Verifies in-order traversal returns a sorted sequence.
        /// </summary>
        /// <remarks>
        /// Ensures traversal logic produces correct ordering for algorithms.
        /// </remarks>
        [TestMethod]
        public void InOrderTraversal_ShouldReturnSortedSequence()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();
            var values = new[] { 10, 5, 15, 3, 7, 12, 17, 1, 4, 6, 8 };
            foreach (var value in values)
            {
                tree.Insert(value);
            }

            // Act
            var inOrder = tree.GetInOrderTraversal().ToArray();

            // Assert
            var expected = values.OrderBy(x => x).ToArray();
            inOrder.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        }

        /// <summary>
        /// Verifies pre-order traversal returns the correct sequence.
        /// </summary>
        /// <remarks>
        /// Ensures traversal logic supports root-first algorithms.
        /// </remarks>
        [TestMethod]
        public void PreOrderTraversal_ShouldReturnCorrectSequence()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();
            var values = new[] { 10, 5, 15, 3, 7 };
            foreach (var value in values)
            {
                tree.Insert(value);
            }

            // Act
            var preOrder = tree.GetPreOrderTraversal().ToArray();

            // Assert
            preOrder[0].Should().Be(10, "root should be first in pre-order");
            preOrder.Should().Contain(values);
            preOrder.Length.Should().Be(values.Length);
        }

        /// <summary>
        /// Verifies post-order traversal returns the correct sequence.
        /// </summary>
        /// <remarks>
        /// Ensures traversal logic supports root-last algorithms.
        /// </remarks>
        [TestMethod]
        public void PostOrderTraversal_ShouldReturnCorrectSequence()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();
            var values = new[] { 10, 5, 15, 3, 7 };
            foreach (var value in values)
            {
                tree.Insert(value);
            }

            // Act
            var postOrder = tree.GetPostOrderTraversal().ToArray();

            // Assert
            postOrder[postOrder.Length - 1].Should().Be(10, "root should be last in post-order");
            postOrder.Should().Contain(values);
            postOrder.Length.Should().Be(values.Length);
        }

        /// <summary>
        /// Clears the tree and verifies all elements are removed.
        /// </summary>
        /// <remarks>
        /// Ensures the tree can be reset for reuse or disposal.
        /// </remarks>
        [TestMethod]
        public void Clear_ShouldRemoveAllElements()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();
            var values = new[] { 10, 5, 15, 3, 7, 12, 17 };
            foreach (var value in values)
            {
                tree.Insert(value);
            }

            // Act
            tree.Clear();

            // Assert
            tree.Count.Should().Be(0);
            tree.IsEmpty.Should().BeTrue();
            foreach (var value in values)
            {
                tree.Contains(value).Should().BeFalse();
            }
        }

        /// <summary>
        /// Finds the minimum value in the tree.
        /// </summary>
        /// <remarks>
        /// Ensures minimum search logic is correct for algorithms.
        /// </remarks>
        [TestMethod]
        public void FindMin_ShouldReturnSmallestValue()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();
            var values = new[] { 10, 5, 15, 3, 7, 12, 17, 1 };
            foreach (var value in values)
            {
                tree.Insert(value);
            }

            // Act
            var min = tree.FindMin();

            // Assert
            min.Should().Be(1);
        }

        /// <summary>
        /// Finds the maximum value in the tree.
        /// </summary>
        /// <remarks>
        /// Ensures maximum search logic is correct for algorithms.
        /// </remarks>
        [TestMethod]
        public void FindMax_ShouldReturnLargestValue()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();
            var values = new[] { 10, 5, 15, 3, 7, 12, 17, 20 };
            foreach (var value in values)
            {
                tree.Insert(value);
            }

            // Act
            var max = tree.FindMax();

            // Assert
            max.Should().Be(20);
        }

        /// <summary>
        /// Attempts to find the minimum value in an empty tree, expecting an exception.
        /// </summary>
        /// <remarks>
        /// Ensures error handling for empty trees.
        /// </remarks>
        [TestMethod]
        public void FindMin_EmptyTree_ShouldThrowException()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();

            // Act & Assert
            AssertionHelpers.AssertThrows<InvalidOperationException>(() => tree.FindMin());
        }

        /// <summary>
        /// Attempts to find the maximum value in an empty tree, expecting an exception.
        /// </summary>
        /// <remarks>
        /// Ensures error handling for empty trees.
        /// </remarks>
        [TestMethod]
        public void FindMax_EmptyTree_ShouldThrowException()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();

            // Act & Assert
            AssertionHelpers.AssertThrows<InvalidOperationException>(() => tree.FindMax());
        }

        /// <summary>
        /// Verifies binary search tree operations work correctly for string values.
        /// </summary>
        /// <remarks>
        /// Ensures generic support for non-numeric types.
        /// </remarks>
        [TestMethod]
        public void BinarySearchTree_WithStrings_ShouldWorkCorrectly()
        {
            // Arrange
            var tree = new BinarySearchTree<string>();
            var values = new[] { "dog", "cat", "elephant", "ant", "bear" };

            // Act
            foreach (var value in values)
            {
                tree.Insert(value);
            }

            // Assert
            tree.Count.Should().Be(values.Length);
            var inOrder = tree.GetInOrderTraversal().ToArray();
            var expected = values.OrderBy(x => x).ToArray();
            inOrder.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        }

        /// <summary>
        /// Verifies binary search tree operations for a large data set.
        /// </summary>
        /// <remarks>
        /// Ensures scalability and performance for large collections.
        /// </remarks>
        [TestMethod]
        public void BinarySearchTree_LargeDataSet_ShouldPerformCorrectly()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();
            var random = new Random(42); // Fixed seed for reproducibility
            var values = Enumerable.Range(1, 1000).OrderBy(x => random.Next()).ToArray();

            // Act
            foreach (var value in values)
            {
                tree.Insert(value);
            }

            // Assert
            tree.Count.Should().Be(1000);
            
            // Verify all values are present
            for (var i = 1; i <= 1000; i++)
            {
                tree.Contains(i).Should().BeTrue($"tree should contain {i}");
            }
            
            // Verify in-order traversal is sorted
            var inOrder = tree.GetInOrderTraversal().ToArray();
            inOrder.Should().BeInAscendingOrder();
            inOrder.Length.Should().Be(1000);
        }

        /// <summary>
        /// Stress test for insert and remove operations in the binary search tree.
        /// </summary>
        /// <remarks>
        /// Ensures robustness and correctness under random operations.
        /// </remarks>
        [TestMethod]
        public void BinarySearchTree_StressTest_InsertAndRemove()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();
            var random = new Random(42);
            var operations = 500;

            // Act & Assert
            for (var i = 0; i < operations; i++)
            {
                var value = random.Next(1, 100);
                
                if (random.NextDouble() > 0.3) // 70% chance to insert
                {
                    tree.Insert(value);
                }
                else // 30% chance to remove
                {
                    tree.Remove(value);
                }
                
                // Verify tree maintains BST property
                if (tree.Count > 0)
                {
                    var inOrder = tree.GetInOrderTraversal().ToArray();
                    inOrder.Should().BeInAscendingOrder("tree should maintain BST property after each operation");
                }
            }
        }

        /// <summary>
        /// Verifies that ToArray returns an empty array for an empty tree.
        /// </summary>
        /// <remarks>
        /// Ensures ToArray handles empty trees correctly.
        /// </remarks>
        [TestMethod]
        public void ToArray_EmptyTree_ShouldReturnEmptyArray()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();

            // Act
            var array = tree.ToArray();

            // Assert
            array.Should().NotBeNull();
            array.Should().BeEmpty();
            array.Length.Should().Be(0);
        }

        /// <summary>
        /// Verifies that ToArray returns a single-element array for a tree with one node.
        /// </summary>
        /// <remarks>
        /// Ensures ToArray correctly handles single-node trees.
        /// </remarks>
        [TestMethod]
        public void ToArray_SingleNode_ShouldReturnSingleElementArray()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();
            tree.Insert(42);

            // Act
            var array = tree.ToArray();

            // Assert
            array.Should().NotBeNull();
            array.Length.Should().Be(1);
            array[0].Should().Be(42);
        }

        /// <summary>
        /// Verifies that ToArray returns a sorted array for a balanced tree.
        /// </summary>
        /// <remarks>
        /// Ensures ToArray performs correct in-order traversal for balanced trees.
        /// </remarks>
        [TestMethod]
        public void ToArray_BalancedTree_ShouldReturnSortedArray()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();
            var values = new[] { 10, 5, 15, 3, 7, 12, 17 };
            foreach (var value in values)
            {
                tree.Insert(value);
            }

            // Act
            var array = tree.ToArray();

            // Assert
            array.Should().NotBeNull();
            array.Length.Should().Be(values.Length);
            array.Should().BeInAscendingOrder();
            array.Should().BeEquivalentTo(values.OrderBy(x => x));
        }

        /// <summary>
        /// Verifies that ToArray returns a sorted array for an unbalanced tree.
        /// </summary>
        /// <remarks>
        /// Ensures ToArray works correctly regardless of tree balance.
        /// </remarks>
        [TestMethod]
        public void ToArray_UnbalancedTree_ShouldReturnSortedArray()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();
            var values = new[] { 1, 2, 3, 4, 5, 6, 7 }; // Sequential insertion creates right-heavy tree
            foreach (var value in values)
            {
                tree.Insert(value);
            }

            // Act
            var array = tree.ToArray();

            // Assert
            array.Should().NotBeNull();
            array.Length.Should().Be(values.Length);
            array.Should().BeInAscendingOrder();
            array.Should().BeEquivalentTo(values);
        }

        /// <summary>
        /// Verifies that ToArray handles duplicate insertions correctly.
        /// </summary>
        /// <remarks>
        /// Ensures ToArray reflects the tree's duplicate handling policy.
        /// </remarks>
        [TestMethod]
        public void ToArray_WithDuplicateAttempts_ShouldReturnUniqueValues()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();
            tree.Insert(5);
            tree.Insert(3);
            tree.Insert(7);
            tree.Insert(5); // Duplicate - should not be added
            tree.Insert(3); // Duplicate - should not be added

            // Act
            var array = tree.ToArray();

            // Assert
            array.Should().NotBeNull();
            array.Length.Should().Be(3, "duplicates should not be added to tree");
            array.Should().BeInAscendingOrder();
            array.Should().BeEquivalentTo(new[] { 3, 5, 7 });
        }

        /// <summary>
        /// Verifies that ToArray works correctly with string values.
        /// </summary>
        /// <remarks>
        /// Ensures ToArray supports generic types and lexicographic ordering.
        /// </remarks>
        [TestMethod]
        public void ToArray_WithStrings_ShouldReturnSortedArray()
        {
            // Arrange
            var tree = new BinarySearchTree<string>();
            var values = new[] { "zebra", "apple", "banana", "cherry", "date" };
            foreach (var value in values)
            {
                tree.Insert(value);
            }

            // Act
            var array = tree.ToArray();

            // Assert
            array.Should().NotBeNull();
            array.Length.Should().Be(values.Length);
            array.Should().BeInAscendingOrder();
            array.Should().BeEquivalentTo(values.OrderBy(x => x));
        }

        /// <summary>
        /// Verifies that ToArray returns correct results after removals.
        /// </summary>
        /// <remarks>
        /// Ensures ToArray reflects the current state of the tree after modifications.
        /// </remarks>
        [TestMethod]
        public void ToArray_AfterRemovals_ShouldReturnUpdatedArray()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();
            var values = new[] { 10, 5, 15, 3, 7, 12, 17 };
            foreach (var value in values)
            {
                tree.Insert(value);
            }

            // Remove some values
            tree.Remove(3);
            tree.Remove(15);

            // Act
            var array = tree.ToArray();

            // Assert
            var expectedValues = values.Where(v => v != 3 && v != 15).OrderBy(x => x);
            array.Should().NotBeNull();
            array.Length.Should().Be(5);
            array.Should().BeInAscendingOrder();
            array.Should().BeEquivalentTo(expectedValues);
        }

        /// <summary>
        /// Verifies that ToArray performance is acceptable for large trees.
        /// </summary>
        /// <remarks>
        /// Ensures ToArray scales appropriately with tree size.
        /// </remarks>
        [TestMethod]
        public void ToArray_LargeTree_ShouldPerformEfficiently()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();
            var random = new Random(42);
            var values = Enumerable.Range(1, 1000).OrderBy(x => random.Next()).ToArray();
            
            foreach (var value in values)
            {
                tree.Insert(value);
            }

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var array = tree.ToArray();
            stopwatch.Stop();

            // Assert
            array.Should().NotBeNull();
            array.Length.Should().Be(1000);
            array.Should().BeInAscendingOrder();
            array.Should().BeEquivalentTo(Enumerable.Range(1, 1000));
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, "ToArray should complete quickly for 1000 elements");
        }
    }
}