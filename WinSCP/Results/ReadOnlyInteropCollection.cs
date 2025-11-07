using System;
using System.Collections;
using System.Collections.Generic;

namespace ByteForge.WinSCP;

/// <summary>
/// Represents a read-only collection for interop scenarios.
/// </summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
public class ReadOnlyInteropCollection<T> : ICollection<T>, IEnumerable<T>, IEnumerable
{
	private readonly List<T> _list = new List<T>();

	/// <summary>
	/// Gets the element at the specified index.
	/// </summary>
	/// <param name="index">The zero-based index of the element to get.</param>
	/// <returns>The element at the specified index.</returns>
	public T this[int index]
	{
		get
		{
			return _list[index];
		}
		set
		{
			throw CreateReadOnlyException();
		}
	}

	/// <summary>
	/// Gets the number of elements contained in the collection.
	/// </summary>
	public int Count => _list.Count;

	/// <summary>
	/// Gets a value indicating whether the collection is read-only.
	/// </summary>
	public bool IsReadOnly => true;

	/// <summary>
	/// Initializes a new instance of the <see cref="ReadOnlyInteropCollection{T}"/> class.
	/// </summary>
	internal ReadOnlyInteropCollection()
	{
	}

	/// <summary>
	/// Throws an exception because the collection is read-only.
	/// </summary>
	/// <param name="item">The item to add.</param>
	public void Add(T item)
	{
		throw CreateReadOnlyException();
	}

	/// <summary>
	/// Throws an exception because the collection is read-only.
	/// </summary>
	public void Clear()
	{
		throw CreateReadOnlyException();
	}

	/// <summary>
	/// Determines whether the collection contains a specific value.
	/// </summary>
	/// <param name="item">The object to locate in the collection.</param>
	/// <returns>True if item is found; otherwise, false.</returns>
	public bool Contains(T item)
	{
		return _list.Contains(item);
	}

	/// <summary>
	/// Copies the elements of the collection to an array, starting at a particular array index.
	/// </summary>
	/// <param name="array">The destination array.</param>
	/// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
	public void CopyTo(T[] array, int arrayIndex)
	{
		_list.CopyTo(array, arrayIndex);
	}

	/// <summary>
	/// Throws an exception because the collection is read-only.
	/// </summary>
	/// <param name="item">The item to remove.</param>
	public bool Remove(T item)
	{
		throw CreateReadOnlyException();
	}

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An enumerator for the collection.</returns>
	public IEnumerator<T> GetEnumerator()
	{
		return _list.GetEnumerator();
	}

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An enumerator for the collection.</returns>
	IEnumerator IEnumerable.GetEnumerator()
	{
		return _list.GetEnumerator();
	}

	/// <summary>
	/// Adds an item to the collection internally.
	/// </summary>
	/// <param name="item">The item to add.</param>
	internal void InternalAdd(T item)
	{
		_list.Add(item);
	}

	/// <summary>
	/// Removes the first item from the collection internally.
	/// </summary>
	internal void InternalRemoveFirst()
	{
		_list.RemoveAt(0);
	}

	/// <summary>
	/// Creates an exception indicating the collection is read-only.
	/// </summary>
	/// <returns>An <see cref="InvalidOperationException"/> indicating the collection is read-only.</returns>
	private static Exception CreateReadOnlyException()
	{
		return new InvalidOperationException("Collection is read-only.");
	}
}
