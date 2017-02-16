using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grammophone.Caching
{
	/// <summary>
	/// Thread-safe asynchronous cache of <typeparamref name="V"/> items indexed by 
	/// key type <typeparamref name="K"/>. Items are kept up 
	/// to a <see cref="MRUCache{K,V}.MaxCount"/>, above which the least recently 
	/// used ones are evicted.
	/// </summary>
	/// <typeparam name="K">The type of item keys.</typeparam>
	/// <typeparam name="V">the type of items cached.</typeparam>
	/// <remarks>
	/// The methods of this cache are thread-safe, suitable for parallel algorithms.
	/// </remarks>
	[Serializable]
	public class AsyncMRUCache<K, V> : MRUCache<K, Task<V>>
	{
		/// <summary>
		/// Create.
		/// </summary>
		/// <param name="asyncItemCreator">An asynchronous function to create an item corresponding to a key.</param>
		/// <param name="maxCount">The maximum items count in the cache.</param>
		public AsyncMRUCache(Func<K, Task<V>> asyncItemCreator, int maxCount = 1024)
			: base(asyncItemCreator, maxCount)
		{
		}
	}
}
