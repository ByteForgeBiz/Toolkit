namespace ByteForge.Toolkit;
/*
 *  ___ _                    ___                  _  _____             _________  
 * | _ )_)_ _  __ _ _ _ _  _/ __| ___ __ _ _ _ __| ||_   _| _ ___ ___ / /_   _\ \ 
 * | _ \ | ' \/ _` | '_| || \__ \/ -_) _` | '_/ _| ' \| || '_/ -_) -_< <  | |  > >
 * |___/_|_||_\__,_|_|  \_, |___/\___\__,_|_| \__|_||_|_||_| \___\___|\_\ |_| /_/ 
 *                      |__/                                                      
 */
/// <summary>
/// Represents a binary search tree.
/// </summary>
/// <typeparam name="T">The type of the values in the tree, which must implement IComparable.</typeparam>
public class BinarySearchTree<T> where T : IComparable<T?>
{
    /*
     *  _  _         _     
     * | \| |___  __| |___ 
     * | .` / _ \/ _` / -_)
     * |_|\_\___/\__,_\___|
     *                     
     */
    /// <summary>
    /// Represents a node in the binary search tree.
    /// </summary>
    public class Node
    {
        /// <summary>
        /// Initializes a new instance of the Node class.
        /// </summary>
        internal Node() { }

        /// <summary>
        /// Gets or sets the key stored in the node.
        /// </summary>
        public T Key { get; set; }

        /// <summary>
        /// Gets or sets the left child of the node.
        /// </summary>
        public Node? Left { get; set; }

        /// <summary>
        /// Gets or sets the right child of the node.
        /// </summary>
        public Node? Right { get; set; }

        /// <summary>
        /// Gets or sets the height of the node.
        /// </summary>
        public int Height { get; internal set; }

        /// <summary>
        /// Gets or sets the value of the node.
        /// </summary>
        public object? Value { get; set; }
    }

    private Node? root;

    /// <summary>
    /// Returns the height of a node.
    /// </summary>
    /// <param name="N">The node whose height is to be returned.</param>
    /// <returns>The height of the node.</returns>
    int Height(Node? N)
    {
        if (N == null)
            return 0;
        return N.Height;
    }

    /// <summary>
    /// Gets the number of nodes in the binary search tree.
    /// </summary>
    public int Count { get; private set; } = 0;

    /// <summary>
    /// Gets a value indicating whether the binary search tree is empty.
    /// </summary>
    public bool IsEmpty => Count == 0;

    /// <summary>
    /// Creates a new node with a specified key.
    /// </summary>
    /// <param name="key">The key to be stored in the node.</param>
    /// <returns>A new node with the specified key.</returns>
    Node NewNode(T key)
    {
        return (new Node
        {
            Key = key,
            Left = null,
            Right = null,
            Height = 1  // new node is initially added at leaf
        });
    }

    /// <summary>
    /// Performs a right rotation on a node.
    /// </summary>
    /// <param name="y">The node to be rotated.</param>
    /// <returns>The new root of the subtree after rotation.</returns>
    Node RightRotate(Node y)
    {
        var x = y.Left!;
        var T2 = x.Right!;
        x.Right = y;
        y.Left = T2;
        y.Height = Math.Max(Height(y.Left), Height(y.Right!)) + 1;
        x.Height = Math.Max(Height(x.Left!), Height(x.Right)) + 1;
        return x;
    }

    /// <summary>
    /// Performs a left rotation on a node.
    /// </summary>
    /// <param name="x">The node to be rotated.</param>
    /// <returns>The new root of the subtree after rotation.</returns>
    Node LeftRotate(Node x)
    {            
        var y = x.Right!;
        var T2 = y.Left!;
        y.Left = x;
        x.Right = T2;
        x.Height = Math.Max(Height(x.Left!), Height(x.Right)) + 1;
        y.Height = Math.Max(Height(y.Left), Height(y.Right!)) + 1;
        return y;
    }

    /// <summary>
    /// Returns the balance factor of a node.
    /// </summary>
    /// <param name="N">The node whose balance factor is to be returned.</param>
    /// <returns>The balance factor of the node.</returns>
    int GetBalance(Node? N)
    {
        if (N == null)
            return 0;
        return Height(N.Left!) - Height(N.Right!);
    }

    /// <summary>
    /// Inserts a key into the binary search tree.
    /// </summary>
    /// <param name="node">The root of the subtree where the key is to be inserted.</param>
    /// <param name="key">The key to be inserted.</param>
    /// <param name="wasInserted">Output parameter indicating whether a new node was inserted.</param>
    /// <returns>The new root of the subtree after insertion.</returns>
    Node Insert(Node? node, T key, ref bool wasInserted)
    {
        /* 1. Perform the normal BST rotation */
        if (node == null)
        {
            wasInserted = true;
            return (NewNode(key));
        }

        if (key.CompareTo(node.Key) < 0)
            node.Left = Insert(node.Left, key, ref wasInserted);
        else if (key.CompareTo(node.Key) > 0)
            node.Right = Insert(node.Right, key, ref wasInserted);
        else // Equal keys not allowed
        {
            wasInserted = false;
            return node;
        }

        /* 2. Update height of this ancestor node */
        node.Height = 1 + Math.Max(Height(node.Left), Height(node.Right));

        /* 3. Get the balance factor of this ancestor node to check whether this node became unbalanced */
        var balance = GetBalance(node);

        // If this node becomes unbalanced, then there are 4 cases

        // Left Left Case
        if (balance > 1 && key.CompareTo(node.Left!.Key) < 0)
            return RightRotate(node);

        // Right Right Case
        if (balance < -1 && key.CompareTo(node.Right!.Key) > 0)
            return LeftRotate(node);

        // Left Right Case
        if (balance > 1 && node.Left  != null && key.CompareTo(node.Left.Key) > 0)
        {
            node.Left = LeftRotate(node.Left);
            return RightRotate(node);
        }

        // Right Left Case
        if (balance < -1 && key.CompareTo(node.Right!.Key) < 0)
        {
            node.Right = RightRotate(node.Right);
            return LeftRotate(node);
        }

        /* return the (unchanged) node pointer */
        return node;
    }

    /// <summary>
    /// Inserts a key into the binary search tree.
    /// </summary>
    /// <param name="node">The root of the subtree where the key is to be inserted.</param>
    /// <param name="key">The key to be inserted.</param>
    /// <returns>The new root of the subtree after insertion.</returns>
    Node Insert(Node node, T key)
    {
        /* 1. Perform the normal BST rotation */
        if (node == null)
            return (NewNode(key));

        if (key.CompareTo(node.Key) < 0)
            node.Left = Insert(node.Left!, key);
        else if (key.CompareTo(node.Key) > 0)
            node.Right = Insert(node.Right!, key);
        else // Equal keys not allowed
            return node;

        /* 2. Update height of this ancestor node */
        node.Height = 1 + Math.Max(Height(node.Left), Height(node.Right));

        /* 3. Get the balance factor of this ancestor node to check whether this node became unbalanced */
        var balance = GetBalance(node);

        // If this node becomes unbalanced, then there are 4 cases

        // Left Left Case
        if (balance > 1 && node.Left  != null && key.CompareTo(node.Left.Key) < 0)
            return RightRotate(node);

        // Right Right Case
        if (balance < -1 && key.CompareTo(node.Right!.Key) > 0)
            return LeftRotate(node);

        // Left Right Case
        if (balance > 1 && key.CompareTo(node.Left!.Key) > 0)
        {
            node.Left = LeftRotate(node.Left);
            return RightRotate(node);
        }

        // Right Left Case
        if (balance < -1 && node.Right!= null && key.CompareTo(node.Right.Key) < 0)
        {
            node.Right = RightRotate(node.Right);
            return LeftRotate(node);
        }

        /* return the (unchanged) node pointer */
        return node;
    }

    /// <summary>
    /// Inserts a key into the binary search tree.
    /// </summary>
    /// <param name="key">The key to be inserted.</param>
    public void Insert(T key)
    {
        var wasInserted = false;
        root = Insert(root, key, ref wasInserted);
        if (wasInserted)
        {
            Count++;
        }
    }

    /// <summary>
    /// Removes a key from the binary search tree.
    /// </summary>
    /// <param name="node">The root of the subtree where the key is to be removed.</param>
    /// <param name="key">The key to be removed.</param>
    /// <param name="wasRemoved">Output parameter indicating whether a node was removed.</param>
    /// <returns>The new root of the subtree after removal.</returns>
    private Node? Remove(Node? node, T key, ref bool wasRemoved)
    {
        // STEP 1: PERFORM STANDARD BST DELETE

        if (node == null)
        {
            wasRemoved = false;
            return node;
        }

        // If the key to be deleted is smaller than the node's key,
        // then it lies in left subtree
        if (key.CompareTo(node.Key) < 0)
            node.Left = Remove(node.Left, key, ref wasRemoved);

        // If the key to be deleted is greater than the node's key,
        // then it lies in right subtree
        else if (key.CompareTo(node.Key) > 0)
            node.Right = Remove(node.Right, key, ref wasRemoved);

        // if key is same as node's key, then this is the node
        // to be deleted
        else
        {
            wasRemoved = true;
            
            // node with only one child or no child
            if ((node.Left == null) || (node.Right == null))
            {
                Node? temp = null;
                if (temp == node.Left)
                    temp = node.Right;
                else
                    temp = node.Left;

                // No child case
                if (temp == null)
                {
                    node = null;
                }
                else // One child case
                    node = temp; // Copy the contents of the non-empty child
            }
            else
            {
                // node with two children: Get the inorder
                // successor (smallest in the right subtree)
                var temp = MinValue(node.Right);

                // Copy the inorder successor's data to this node
                node.Key = temp.Key;

                // Delete the inorder successor
                var tempRemoved = false;
                node.Right = Remove(node.Right, temp.Key, ref tempRemoved);
            }
        }

        // If the tree had only one node then return
        if (node == null)
            return node;

        // STEP 2: UPDATE HEIGHT OF THE CURRENT NODE
        node.Height = Math.Max(Height(node.Left), Height(node.Right)) + 1;

        // STEP 3: GET THE BALANCE FACTOR OF THIS NODE (to check whether
        //  this node became unbalanced)
        var balance = GetBalance(node);

        // If this node becomes unbalanced, then there are 4 cases

        // Left Left Case
        if (balance > 1 && GetBalance(node.Left) >= 0)
            return RightRotate(node);

        // Left Right Case
        if (balance > 1 && GetBalance(node.Left) < 0)
        {
            node.Left = LeftRotate(node.Left!);
            return RightRotate(node);
        }

        // Right Right Case
        if (balance < -1 && GetBalance(node.Right) <= 0)
            return LeftRotate(node);

        // Right Left Case
        if (balance < -1 && GetBalance(node.Right) > 0)
        {
            node.Right = RightRotate(node.Right!);
            return LeftRotate(node);
        }

        return node;
    }

    /// <summary>
    /// Removes a key from the binary search tree.
    /// </summary>
    /// <param name="node">The root of the subtree where the key is to be removed.</param>
    /// <param name="key">The key to be removed.</param>
    /// <returns>The new root of the subtree after removal.</returns>
    private Node? Remove(Node? node, T key)
    {
        // STEP 1: PERFORM STANDARD BST DELETE

        if (node == null)
            return node;

        // If the key to be deleted is smaller than the node's key,
        // then it lies in left subtree
        if (key.CompareTo(node.Key) < 0)
            node.Left = Remove(node.Left, key);

        // If the key to be deleted is greater than the node's key,
        // then it lies in right subtree
        else if (key.CompareTo(node.Key) > 0)
            node.Right = Remove(node.Right, key);

        // if key is same as node's key, then this is the node
        // to be deleted
        else
        {
            // node with only one child or no child
            if ((node.Left == null) || (node.Right == null))
            {
                Node? temp = null;
                if (temp == node.Left)
                    temp = node.Right;
                else
                    temp = node.Left;

                // No child case
                if (temp == null)
                    node = null;
                else // One child case
                    node = temp; // Copy the contents of the non-empty child
            }
            else
            {
                // node with two children: Get the inorder
                // successor (smallest in the right subtree)
                var temp = MinValue(node.Right);

                // Copy the inorder successor's data to this node
                node.Key = temp.Key;

                // Delete the inorder successor
                node.Right = Remove(node.Right, temp.Key);
            }
        }

        // If the tree had only one node then return
        if (node == null)
            return node;

        // STEP 2: UPDATE HEIGHT OF THE CURRENT NODE
        node.Height = Math.Max(Height(node.Left), Height(node.Right)) + 1;

        // STEP 3: GET THE BALANCE FACTOR OF THIS NODE (to check whether
        //  this node became unbalanced)
        var balance = GetBalance(node);

        // If this node becomes unbalanced, then there are 4 cases

        // Left Left Case
        if (balance > 1 && GetBalance(node.Left) >= 0)
            return RightRotate(node);

        // Left Right Case
        if (balance > 1 && GetBalance(node.Left) < 0)
        {
            node.Left = LeftRotate(node.Left!);
            return RightRotate(node);
        }

        // Right Right Case
        if (balance < -1 && GetBalance(node.Right) <= 0)
            return LeftRotate(node);

        // Right Left Case
        if (balance < -1 && GetBalance(node.Right) > 0)
        {
            node.Right = RightRotate(node.Right!);
            return LeftRotate(node);
        }

        return node;
    }

    /// <summary>
    /// Removes a key from the binary search tree.
    /// </summary>
    /// <param name="key">The key to be removed.</param>
    /// <returns>True if the key was found and removed; otherwise, false.</returns>
    public bool Remove(T key)
    {
        var wasRemoved = false;
        root = Remove(root, key, ref wasRemoved);
        if (wasRemoved)
        {
            Count--;
        }
        return wasRemoved;
    }

    /// <summary>
    /// Get the node with minimum key value found in that tree. The tree is not empty.
    /// </summary>
    /// <param name="node">The root of the subtree to be searched.</param>
    /// <returns>The node with minimum key.</returns>
    Node MinValue(Node node)
    {
        var current = node;

        /* loop down to find the leftmost leaf */
        while (current.Left != null)
            current = current.Left;

        return current;
    }

    /// <summary>
    /// Searches for a key in the binary search tree.
    /// </summary>
    /// <param name="root">The root of the subtree to be searched.</param>
    /// <param name="key">The key to be searched for.</param>
    /// <returns>The node containing the key if it is found, otherwise null.</returns>
    private Node? Search(Node? root, T key)
    {
        // Base Cases: root is null or key is present at root
        if (root == null || root.Key.CompareTo(key) == 0)
            return root;

        // Key is greater than root's key
        if (root.Key.CompareTo(key) < 0)
            return Search(root.Right, key);

        // Key is smaller than root's key
        return Search(root.Left, key);
    }

    /// <summary>
    /// Determines whether the binary search tree contains a specific key.
    /// </summary>
    /// <param name="key">The key to locate in the binary search tree.</param>
    /// <returns>true if the binary search tree contains an element with the specified key; otherwise, false.</returns>
    public bool Contains(T key)
    {
        return Find(key) != null;
    }

    /// <summary>
    /// Locates a node in the binary search tree.
    /// </summary>
    /// <param name="key">The key to locate in the binary search tree.</param>
    /// <returns>The node containing the key if it is found, otherwise null.</returns>
    public Node? Find(T key)
    {
        return Search(root, key);
    }

    /// <summary>
    /// Converts the binary search tree to an array using in-order traversal.
    /// </summary>
    /// <returns>An array containing all the keys in the binary search tree in in-order.</returns>
    public T[] ToArray()
    {
        var array = new T[Count];
        var index = 0;
        ToArray(root!, array, ref index);
        return array;
    }

    /// <summary>
    /// Helper method to perform in-order traversal and add each node's key to the array.
    /// </summary>
    /// <param name="root">The root of the subtree to be traversed.</param>
    /// <param name="array">The array to which the keys are added.</param>
    /// <param name="index">The current index in the array.</param>
    private void ToArray(Node? root, T[] array, ref int index)
    {
        if (root == null)
            return;

        ToArray(root.Left, array, ref index);
        array[index++] = root.Key;
        ToArray(root.Right, array, ref index);
    }

    /// <summary>
    /// Removes all elements from the binary search tree.
    /// </summary>
    public void Clear()
    {
        root = null;
        Count = 0;
    }

    /// <summary>
    /// Finds the minimum value in the binary search tree.
    /// </summary>
    /// <returns>The minimum value in the tree.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the tree is empty.</exception>
    public T FindMin()
    {
        if (root == null)
            throw new InvalidOperationException("Tree is empty");
        
        return MinValue(root).Key;
    }

    /// <summary>
    /// Finds the maximum value in the binary search tree.
    /// </summary>
    /// <returns>The maximum value in the tree.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the tree is empty.</exception>
    public T FindMax()
    {
        if (root == null)
            throw new InvalidOperationException("Tree is empty");
        
        return MaxValue(root).Key;
    }

    /// <summary>
    /// Get the node with maximum key value found in that tree.
    /// </summary>
    /// <param name="node">The root of the subtree to be searched.</param>
    /// <returns>The node with maximum key.</returns>
    private Node MaxValue(Node node)
    {
        var current = node;

        /* loop down to find the rightmost leaf */
        while (current.Right != null)
            current = current.Right;

        return current;
    }

    /// <summary>
    /// Returns an enumerable collection of elements in in-order traversal.
    /// </summary>
    /// <returns>An enumerable collection of elements in in-order.</returns>
    public IEnumerable<T> GetInOrderTraversal()
    {
        return GetInOrderTraversal(root);
    }

    /// <summary>
    /// Helper method for in-order traversal.
    /// </summary>
    /// <param name="node">The root of the subtree to traverse.</param>
    /// <returns>An enumerable collection of elements in in-order.</returns>
    private IEnumerable<T> GetInOrderTraversal(Node? node)
    {
        if (node != null)
        {
            foreach (var value in GetInOrderTraversal(node.Left))
                yield return value;
            
            yield return node.Key;
            
            foreach (var value in GetInOrderTraversal(node.Right))
                yield return value;
        }
    }

    /// <summary>
    /// Returns an enumerable collection of elements in pre-order traversal.
    /// </summary>
    /// <returns>An enumerable collection of elements in pre-order.</returns>
    public IEnumerable<T> GetPreOrderTraversal()
    {
        return GetPreOrderTraversal(root);
    }

    /// <summary>
    /// Helper method for pre-order traversal.
    /// </summary>
    /// <param name="node">The root of the subtree to traverse.</param>
    /// <returns>An enumerable collection of elements in pre-order.</returns>
    private IEnumerable<T> GetPreOrderTraversal(Node? node)
    {
        if (node != null)
        {
            yield return node.Key;
            
            foreach (var value in GetPreOrderTraversal(node.Left))
                yield return value;
            
            foreach (var value in GetPreOrderTraversal(node.Right))
                yield return value;
        }
    }

    /// <summary>
    /// Returns an enumerable collection of elements in post-order traversal.
    /// </summary>
    /// <returns>An enumerable collection of elements in post-order.</returns>
    public IEnumerable<T> GetPostOrderTraversal()
    {
        return GetPostOrderTraversal(root);
    }

    /// <summary>
    /// Helper method for post-order traversal.
    /// </summary>
    /// <param name="node">The root of the subtree to traverse.</param>
    /// <returns>An enumerable collection of elements in post-order.</returns>
    private IEnumerable<T> GetPostOrderTraversal(Node? node)
    {
        if (node != null)
        {
            foreach (var value in GetPostOrderTraversal(node.Left))
                yield return value;
            
            foreach (var value in GetPostOrderTraversal(node.Right))
                yield return value;
            
            yield return node.Key;
        }
    }
}
