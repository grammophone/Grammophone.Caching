using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grammophone.Caching
{
	/// <summary>
	/// Disposable thread-safe cache of <typeparamref name="V"/> items which also
	/// implement <see cref="IDisposable"/>, indexed by 
	/// key type <typeparamref name="K"/>. Items are kept up 
	/// to a <see cref="MRUCache{K,V}.MaxCount"/>, above which the least recently 
	/// used ones are evicted.
	/// </summary>
	/// <typeparam name="K">The type of item keys.</typeparam>
	/// <typeparam name="V">the type of items cached, implementing <see cref="IDisposable"/>.</typeparam>
	/// <remarks>
	/// The methods of this cache are thread-safe, suitable for parallel algorithms.
	/// </remarks>
	public class DisposableMRUCache<K, V> : MRUCache<K, V>, IDisposable
		where V : class, IDisposable
	{
		#region Construction

		/// <summary>
		/// Create.
		/// </summary>
		/// <param name="itemCreator">A function to create an item corresponding to a key.</param>
		/// <param name="maxCount">The maximum items count in the cache.</param>
		public DisposableMRUCache(Func<K, V> itemCreator, int maxCount = 1024)
			: base(itemCreator, maxCount)
		{
		}

		#endregion

		#region Public methods

		/// <summary>
		/// Removes an item from the cache and disposes it.
		/// </summary>
		/// <param name="key">The key defining the item.</param>
		/// <param name="value">Set to the removed value, if found in the cache.</param>
		/// <returns>Returns true if the item was found in the cache, else false.</returns>
		public override bool Remove(K key, out V value)
		{
			bool isRemoved = base.Remove(key, out value);

			if (isRemoved) value?.Dispose();

			return isRemoved;
		}

		/// <summary>
		/// Evict all items from the cache and dispose them.
		/// </summary>
		/// <returns>
		/// Returns the evicted and disposed cache entries.
		/// </returns>
		public override IEnumerable<KeyValuePair<K, V>> Clear()
		{
			var evictedEntries = base.Clear();

			foreach (var entry in evictedEntries)
			{
				entry.Value?.Dispose();
			}

			return evictedEntries;
		}

		/// <summary>
		/// Calls <see cref="MRUCache{K, V}.Clear"/> to evict and dispose all items in the cache.
		/// </summary>
		void IDisposable.Dispose()
		{
			Clear();
		}

		#endregion
	}
}
