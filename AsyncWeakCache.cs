using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Grammophone.Caching
{
	/// <summary>
	/// Thread-safe asynchronous cache of <typeparamref name="V"/> items indexed by 
	/// key type <typeparamref name="K"/>.
	/// All items are stored as weak references in order to be able to be garbage-collected
	/// while the program still holds on to the reference to the cache.
	/// </summary>
	/// <typeparam name="K">The type of item keys.</typeparam>
	/// <typeparam name="V">The type of items cached.</typeparam>
	/// <remarks>
	/// The methods of this cache are thread-safe, suitable for parallel algorithms.
	/// </remarks>
	public class AsyncWeakCache<K, V> : WeakCache<K, Task<V>>
		where K : class
	{
		#region Construction

		/// <summary>
		/// Create.
		/// </summary>
		/// <param name="asyncItemCreator">An asynchronous function to create an item corresponding to a key.</param>
		public AsyncWeakCache(Func<K, Task<V>> asyncItemCreator) : base(asyncItemCreator)
		{
		}

		#endregion
	}
}
