using System.Collections;
using System.Collections.Generic;

namespace ByteForge.WinSCP;

/// <summary>
/// Provides an enumerable wrapper for deferred enumeration of generic collections.
/// </summary>
/// <typeparam name="T">The type of elements in the enumerable.</typeparam>
internal class ImplicitEnumerable<T> : IEnumerable<T>, IEnumerable
{
	/// <summary>
	/// The underlying enumerable source that this wrapper delegates enumeration to.
	/// </summary>
	private readonly IEnumerable<T> _enumerable;

	/// <summary>
	/// Initializes a new instance of the <see cref="ImplicitEnumerable{T}"/> class.
	/// </summary>
	/// <param name="enumerable">The underlying enumerable to wrap.</param>
	public ImplicitEnumerable(IEnumerable<T> enumerable)
	{
		_enumerable = enumerable;
	}

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An enumerator for the collection.</returns>
	public IEnumerator<T> GetEnumerator()
	{
		return _enumerable.GetEnumerator();
	}

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An enumerator for the collection.</returns>
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
