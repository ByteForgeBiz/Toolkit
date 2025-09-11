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

        [TestMethod]
        public void Contains_EmptyTree_ShouldReturnFalse()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();

            // Act & Assert
            tree.Contains(10).Should().BeFalse();
        }

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

        [TestMethod]
        public void FindMin_EmptyTree_ShouldThrowException()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();

            // Act & Assert
            AssertionHelpers.AssertThrows<InvalidOperationException>(() => tree.FindMin());
        }

        [TestMethod]
        public void FindMax_EmptyTree_ShouldThrowException()
        {
            // Arrange
            var tree = new BinarySearchTree<int>();

            // Act & Assert
            AssertionHelpers.AssertThrows<InvalidOperationException>(() => tree.FindMax());
        }

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
    }
}