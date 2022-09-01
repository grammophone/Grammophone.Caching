using System;
using System.Collections.Generic;
using System.Text;

namespace Grammophone.Caching
{
	/// <summary>
	/// Thread-safe cache of <typeparamref name="V"/> items indexed by 
	/// key type <typeparamref name="K"/>, all stored as weak references in order to be able to be garbage-collected
	/// while the program still holds on to the reference to the cache.
	/// </summary>
	/// <typeparam name="K">The type of item keys.</typeparam>
	/// <typeparam name="V">The type of items cached.</typeparam>
	/// <remarks>
	/// The methods of this cache are thread-safe, suitable for parallel algorithms.
	/// </remarks>
	public class WeakCache<K, V>
		where K : class
	{
		#region Auxilliary classes

		private class Item
		{
			public bool IsCreated;

			public V Value;
		}

		#endregion

		#region Private fields

		private readonly Func<K, V> itemCreator;

		private readonly System.Runtime.CompilerServices.ConditionalWeakTable<K, Item> conditionalWeakTable;

		#endregion

		#region Construction

		/// <summary>
		/// Create.
		/// </summary>
		/// <param name="itemCreator">A function to create an item corresponding to a key.</param>
		public WeakCache(Func<K, V> itemCreator)
		{
			if (itemCreator == null) throw new ArgumentNullException(nameof(itemCreator));

			this.itemCreator = itemCreator;

			this.conditionalWeakTable = new System.Runtime.CompilerServices.ConditionalWeakTable<K, Item>();
		}

		#endregion

		#region Public methods

		/// <summary>
		/// Get an item from the cache or return a newly created one.
		/// </summary>
		/// <param name="key">The key defining the item.</param>
		/// <returns>
		/// Returns the item corresponding to the supplied <paramref name="key"/>,
		/// either from the cache, in case of a cache miss, or newly created.
		/// </returns>
		public V Get(K key)
		{
			if (key == null) throw new ArgumentNullException(nameof(key));

			Item item = conditionalWeakTable.GetOrCreateValue(key);

			lock (item)
			{
				if (!item.IsCreated)
				{
					item.Value = itemCreator(key);
					item.IsCreated = true;
				}
			}

			return item.Value;
		}

		/// <summary>
		/// Removes an item from the cache.
		/// </summary>
		/// <param name="key">The key defining the item.</param>
		/// <returns>Returns true if the item was found in the cache, else false.</returns>
		public bool Remove(K key)
		{
			if (key == null) throw new ArgumentNullException(nameof(key));

			return conditionalWeakTable.Remove(key);
		}

		#endregion
	}
}
