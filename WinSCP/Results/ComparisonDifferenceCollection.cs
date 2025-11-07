using System.Collections;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Represents a read-only collection of <see cref="ComparisonDifference"/> objects.
/// </summary>
[Guid("28957CC8-DEBC-48D0-841B-48AD3CB3B49F")]
[ClassInterface(ClassInterfaceType.None)]
[ComVisible(true)]
[ComDefaultInterface(typeof(IEnumerable))]
public class ComparisonDifferenceCollection : ReadOnlyInteropCollection<ComparisonDifference>
{
}
