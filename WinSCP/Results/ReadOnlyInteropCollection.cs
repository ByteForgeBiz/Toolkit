using System;
using System.Collections;
using System.Collections.Generic;

namespace ByteForge.WinSCP;

public class ReadOnlyInteropCollection<T> : ICollection<T>, IEnumerable<T>, IEnumerable
{
	private readonly List<T> _list = new List<T>();

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

	public int Count => _list.Count;

	public bool IsReadOnly => true;

	internal ReadOnlyInteropCollection()
	{
	}

	public void Add(T item)
	{
		throw CreateReadOnlyException();
	}

	public void Clear()
	{
		throw CreateReadOnlyException();
	}

	public bool Contains(T item)
	{
		return _list.Contains(item);
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		_list.CopyTo(array, arrayIndex);
	}

	public bool Remove(T item)
	{
		throw CreateReadOnlyException();
	}

	public IEnumerator<T> GetEnumerator()
	{
		return _list.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _list.GetEnumerator();
	}

	internal void InternalAdd(T item)
	{
		_list.Add(item);
	}

	internal void InternalRemoveFirst()
	{
		_list.RemoveAt(0);
	}

	private static Exception CreateReadOnlyException()
	{
		return new InvalidOperationException("Collection is read-only.");
	}
}
