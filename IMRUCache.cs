using System.Collections.Generic;

namespace Grammophone.Caching
{
	/// <summary>
	/// Interface for a cache of <typeparamref name="V"/> items indexed by 
	/// key type <typeparamref name="K"/>. Items are kept up 
	/// to a <see cref="MaxCount"/>, above which the least recently 
	/// used ones are evicted.
	/// </summary>
	/// <typeparam name="K">The type of item keys.</typeparam>
	/// <typeparam name="V">The type of items cached.</typeparam>
	public interface IMRUCache<K, V>
	{
		/// <summary>
		/// The maximum items count in the cache.
		/// </summary>
		int MaxCount { get; set; }

		/// <summary>
		/// Evict all items from the cache.
		/// </summary>
		/// <returns>
		/// Returns the evicted cache entries.
		/// </returns>
		IEnumerable<KeyValuePair<K, V>> Clear();

		/// <summary>
		/// Get an item from the cache or return a newly created one.
		/// </summary>
		/// <param name="key">The key defining the item.</param>
		/// <returns>
		/// Returns the item corresponding to the supplied <paramref name="key"/>,
		/// either from the cache, in case of a cache miss, or newly created.
		/// In the latter case, the item is inserted in the cache as the most
		/// recently used.
		/// </returns>
		/// <remarks>
		/// If the cache is full and there is a cache miss, 
		/// the cache makes room form the new item until
		/// the total size reaches <see cref="MaxCount"/> by evicting
		/// the least recently used items.
		/// </remarks>
		V Get(K key);

		/// <summary>
		/// Removes an item from the cache.
		/// </summary>
		/// <param name="key">The key defining the item.</param>
		/// <returns>Returns true if the item was found in the cache, else false.</returns>
		bool Remove(K key);

		/// <summary>
		/// Removes an item from the cache.
		/// </summary>
		/// <param name="key">The key defining the item.</param>
		/// <param name="value">Set to the removed value, if found in the cache.</param>
		/// <returns>Returns true if the item was found in the cache, else false.</returns>
		bool Remove(K key, out V value);

		/// <summary>
		/// Remove the least recently used item.
		/// </summary>
		/// <returns>Returns the item entry found, else null.</returns>
		KeyValuePair<K, V>? RemoveLRU();
	}
}