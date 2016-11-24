using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Runtime.Serialization;
using System.Threading;
using System.Diagnostics;

namespace Grammophone.Caching
{
	/// <summary>
	/// Thread-safe cache of <typeparamref name="V"/> items indexed by 
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
	public class MRUCache<K, V> : IDeserializationCallback
	{
		#region Auxilliary types

		/// <summary>
		/// The type of items stored in the cache dictionary as well as the double linked usage list.
		/// </summary>
		[Serializable]
		private class Item
		{
			public K Key;
			public V Value;
			public Item MoreRecentItem; // Special case: When it points to self, the item is deleted.
			public Item LessRecentItem; // Special case: When it points to self, the item is not yet inserted.
			public bool IsCreated;
		}

		/// <summary>
		/// Holds a snapshot of statistics about the cache usage, such as cumulative cache hits,
		/// total hits and cached items count at a point in time.
		/// </summary>
		[Serializable]
		public class Statistics
		{
			#region Construction

			internal Statistics(int totalHitsCount, int cacheHitsCount, int cachedItemsCount)
			{
				this.TotalHitsCount = totalHitsCount;
				this.CacheHitsCount = cacheHitsCount;
				this.CachedItemsCount = cachedItemsCount;
			}

			#endregion

			#region Public properties

			/// <summary>
			/// The total amount of times <see cref="MRUCache{K, V}.Get"/> 
			/// was called since the cache was
			/// created or <see cref="MRUCache{K, V}.ResetStatistics"/> was invoked.
			/// </summary>
			public int TotalHitsCount { get; private set; }

			/// <summary>
			/// The amount of cache hits encountered during invokations of <see cref="MRUCache{K, V}.Get"/> 
			/// since the cache was
			/// created or <see cref="MRUCache{K, V}.ResetStatistics"/> was invoked.
			/// </summary>
			public int CacheHitsCount { get; private set; }

			/// <summary>
			/// The number of items held in the cache when this snapshot of statistics
			/// was requested.
			/// </summary>
			public int CachedItemsCount { get; private set; }

			#endregion

			#region Public methods

			/// <summary>
			/// Returns a string describing the statistics of the cache.
			/// </summary>
			public override string ToString()
			{
				return String.Format(
					"Cached items: {0}, total hits: {1}, cache hits: {2}, hit percentage: {3:F2}%.",
					this.CachedItemsCount,
					this.TotalHitsCount,
					this.CacheHitsCount,
					this.TotalHitsCount != 0 ? (double)(this.CacheHitsCount * 100) / (double)this.TotalHitsCount : 0.0);
			}

			#endregion
		}

		#endregion

		#region Private fields

		[NonSerialized]
		private SpinLock queueSpinLock = new SpinLock();

		private Func<K, V> itemCreator;

		private Item leastRecentItem;

		private Item mostRecentItem;

		private ConcurrentDictionary<K, Item> dictionary;

		private int totalHitsCount;

		private int cacheHitsCount;

		#endregion

		#region Construction

		/// <summary>
		/// Create.
		/// </summary>
		/// <param name="itemCreator">A function to create an item corresponding to a key.</param>
		/// <param name="maxCount">The maximum items count in the cache.</param>
		public MRUCache(Func<K, V> itemCreator, int maxCount = 1024)
		{
			if (itemCreator == null) throw new ArgumentNullException("itemCreator");

			this.itemCreator = itemCreator;
			this.MaxCount = maxCount;

			this.dictionary = new ConcurrentDictionary<K, Item>();

			this.totalHitsCount = 0;
			this.cacheHitsCount = 0;
		}

		#endregion

		#region Public properties

		/// <summary>
		/// The maximum items count in the cache.
		/// </summary>
		public int MaxCount { get; set; }

		#endregion

		#region Public methods

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
		public V Get(K key)
		{
			Item item = this.dictionary.GetOrAdd(key, CreateItemStub);

			while (dictionary.Count > this.MaxCount)
			{
				KeyValuePair<K, V>? lruEntry = this.RemoveLRU();

				if (!lruEntry.HasValue) break;
			}

			bool queueLockTaken = false;
			this.queueSpinLock.Enter(ref queueLockTaken);

			if (queueLockTaken)
			//lock (this.queueLock)
			{
				this.totalHitsCount++;

				if (item.MoreRecentItem != item) // Is this a non-deleted item?
				{
					if (item.LessRecentItem == item) // Is this a new item, not yet inserted?
					{
						if (this.mostRecentItem != null)
						{
							this.mostRecentItem.MoreRecentItem = item;
						}

						item.LessRecentItem = this.mostRecentItem;

						this.mostRecentItem = item;
					}
					else
					{
						this.cacheHitsCount++;

						if (item.MoreRecentItem == this.mostRecentItem) this.mostRecentItem = item;
						if (item == this.leastRecentItem) this.leastRecentItem = item.MoreRecentItem;

						if (item.MoreRecentItem != null)
						{
							if (item.LessRecentItem != null)
							{
								item.LessRecentItem.MoreRecentItem = item.MoreRecentItem;
							}

							item.MoreRecentItem.LessRecentItem = item.LessRecentItem;

							Item aheadItem = item.MoreRecentItem.MoreRecentItem;

							item.MoreRecentItem.MoreRecentItem = item;

							item.LessRecentItem = item.MoreRecentItem;

							item.MoreRecentItem = aheadItem;
							if (aheadItem != null) aheadItem.LessRecentItem = item;
						}

					}

					if (this.leastRecentItem == null)
					{
						this.leastRecentItem = item;
					}
				}

				queueSpinLock.Exit(false);
			}

			lock (item)
			{
				if (!item.IsCreated)
				{
					item.Value = this.itemCreator(key);
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
			V removedValue;

			return Remove(key, out removedValue);
		}

		/// <summary>
		/// Removes an item from the cache.
		/// </summary>
		/// <param name="key">The key defining the item.</param>
		/// <param name="value">Set to the removed value, if found in the cache.</param>
		/// <returns>Returns true if the item was found in the cache, else false.</returns>
		public bool Remove(K key, out V value)
		{
			Item removedItem;

			if (this.dictionary.TryRemove(key, out removedItem))
			{
				value = removedItem.Value;

				bool queueLockTaken = false;
				this.queueSpinLock.Enter(ref queueLockTaken);

				if (queueLockTaken)
				//lock (this.queueLock)
				{
					if (removedItem.LessRecentItem != removedItem) // Is the removed item not new?
					{
						if (removedItem.MoreRecentItem == null)
						{
							this.mostRecentItem = removedItem.LessRecentItem;
						}
						else
						{
							removedItem.MoreRecentItem.LessRecentItem = removedItem.LessRecentItem;
						}

						if (removedItem.LessRecentItem == null)
						{
							this.leastRecentItem = removedItem.MoreRecentItem;
						}
						else
						{
							removedItem.LessRecentItem.MoreRecentItem = removedItem.MoreRecentItem;
						}
					}

					removedItem.MoreRecentItem = removedItem; // Mark the item as deleted. Point to self.
					removedItem.LessRecentItem = null;

					queueSpinLock.Exit(false);

					return true;
				}
				else
				{
					return true;
				}

			}
			else
			{
				value = default(V);

				return false;
			}
		}

		/// <summary>
		/// Remove the least recently used item.
		/// </summary>
		/// <returns>Returns the item entry found, else null.</returns>
		public KeyValuePair<K, V>? RemoveLRU()
		{
			Item removedItem;

			do
			{
				bool queueLockTaken = false;
				this.queueSpinLock.Enter(ref queueLockTaken);

				if (queueLockTaken)
				//lock (this.queueLock)
				{
					if (this.leastRecentItem == null)
					{
						queueSpinLock.Exit(false);
						return null;
					}

					removedItem = this.leastRecentItem;
					queueSpinLock.Exit(false);
				}
				else
				{
					return null;
				}
			}
			while (!this.Remove(removedItem.Key));

			return new KeyValuePair<K, V>(removedItem.Key, removedItem.Value);
		}

		/// <summary>
		/// Evict all items from the cache.
		/// </summary>
		public void Clear()
		{
			bool queueLockTaken = false;
			this.queueSpinLock.Enter(ref queueLockTaken);

			if (queueLockTaken)
			//lock (this.queueLock)
			{
				this.leastRecentItem = null;
				this.mostRecentItem = null;

				this.dictionary.Clear();

				this.queueSpinLock.Exit(false);
			}
		}

		/// <summary>
		/// Get statistics of cache usage.
		/// </summary>
		/// <returns>
		/// Returns a snapshot of cache statistics such as cumulative cache hits,
		/// total hits and cache items count.
		/// </returns>
		public Statistics GetStatistics()
		{
			int currentCacheHitsCount = 0;
			int currentTotalHitsCount = 0;

			bool queueLockTaken = false;
			this.queueSpinLock.Enter(ref queueLockTaken);

			if (queueLockTaken)
			//lock (this.queueLock)
			{
				currentCacheHitsCount = this.cacheHitsCount;
				currentTotalHitsCount = this.totalHitsCount;

				this.queueSpinLock.Exit(false);
			}

			return new Statistics(currentTotalHitsCount, currentCacheHitsCount, this.dictionary.Count);
		}

		/// <summary>
		/// Reset statistics concerning cache hits and total hits count.
		/// </summary>
		public void ResetStatistics()
		{
			bool queueLockTaken = false;
			this.queueSpinLock.Enter(ref queueLockTaken);

			if (queueLockTaken)
			//lock (this.queueLock)
			{
				this.totalHitsCount = 0;
				this.cacheHitsCount = 0;

				this.queueSpinLock.Exit(false);
			}
		}

		#endregion

		#region Private methods

		/// <summary>
		/// Create a new item marked as not yet inserted.
		/// </summary>
		/// <remarks>
		/// Creates a blank entry, with its value not yet computed.
		/// </remarks>
		private Item CreateItemStub(K key)
		{
			var item = new Item();

			item.Key = key;

			item.LessRecentItem = item; // Mark the item as new, not yet inserted.

			return item;
		}

		/// <summary>
		/// Sanity check for an insane developer.
		/// </summary>
		/// <returns>
		/// Returns the number of items chained from least to most used.
		/// Should be the same as the dictionary's count when no other concurrent action takes place.
		/// </returns>
		internal int LinkedListSanityCheck()
		{
			int upCount, downCount, dictionaryCount;
			Item item;

			bool queueLockTaken = false;
			this.queueSpinLock.Enter(ref queueLockTaken);

			if (queueLockTaken)
			//lock (this.queueLock)
			{
				for (upCount = 0, item = this.leastRecentItem; item != null; item = item.MoreRecentItem, upCount++) ;

				for (downCount = 0, item = this.mostRecentItem; item != null; item = item.LessRecentItem, downCount++) ;

				dictionaryCount = this.dictionary.Count;

				this.queueSpinLock.Exit(false);
			}
			else
			{
				return 0;
			}

			if (dictionaryCount != upCount || dictionaryCount != downCount)
			{
				Trace.WriteLine(
					String.Format(
						"LRU list is inconsistent. Dictionary size is {0} items, LRU list forward traversing yields {1} items, backward yields {2} items.",
						dictionaryCount,
						upCount,
						downCount));
			}

			return upCount;
		}

		#endregion

		void IDeserializationCallback.OnDeserialization(object sender)
		{
			queueSpinLock = new SpinLock();
		}
	}
}
