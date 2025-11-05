using System.Collections;
using System.Collections.Generic;

namespace ByteForge.WinSCP;

internal class ImplicitEnumerable<T> : IEnumerable<T>, IEnumerable
{
	private readonly IEnumerable<T> _enumerable;

	public ImplicitEnumerable(IEnumerable<T> enumerable)
	{
		_enumerable = enumerable;
	}

	public IEnumerator<T> GetEnumerator()
	{
		return _enumerable.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
