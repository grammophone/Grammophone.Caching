using System;
using System.Collections.Generic;
using System.Text;

namespace Grammophone.Caching
{
	/// <summary>
	/// A cache that stores items created from repository instances, all held as weak references.
	/// </summary>
	/// <typeparam name="R">The type of the repository.</typeparam>
	/// <typeparam name="K">The type of item keys within a repository.</typeparam>
	/// <typeparam name="V">The type of items cached.</typeparam>
	public class WeakRepositoryCache<R, K, V> : IWeakRepositoryCache<R, K, V>
		where R : class
	{
		#region Private fields

		private readonly WeakCache<R, IMRUCache<K, V>> repositoryCache;

		#endregion

		#region Construction

		/// <summary>
		/// Create.
		/// </summary>
		/// <param name="itemCreator">Function to create an item from a repository and a key.</param>
		/// <param name="isThreadSafeWithinRepository">If true, the items in a repository can be accessed in a thread-safe manner. Default is true.</param>
		/// <param name="maxCount">Optional maximum number of items cached under a repository.</param>
		public WeakRepositoryCache(Func<R, K, V> itemCreator, bool isThreadSafeWithinRepository = true, int maxCount = 1024)
		{
			if (itemCreator == null) throw new ArgumentNullException(nameof(itemCreator));

			if (isThreadSafeWithinRepository)
				repositoryCache = new WeakCache<R, IMRUCache<K, V>>(r => new MRUCache<K, V>(k => itemCreator(r, k), maxCount));
			else
				repositoryCache = new WeakCache<R, IMRUCache<K, V>>(r => new SequentialMRUCache<K, V>(k => itemCreator(r, k), maxCount));
		}

		/// <summary>
		/// Create.
		/// </summary>
		/// <param name="mruCacheCreator">A function to create an <see cref="IMRUCache{K,V}"/> for a repository.</param>
		public WeakRepositoryCache(Func<R, IMRUCache<K, V>> mruCacheCreator)
		{
			if (mruCacheCreator == null) throw new ArgumentNullException(nameof(mruCacheCreator));

			repositoryCache = new WeakCache<R, IMRUCache<K, V>>(mruCacheCreator);
		}

		#endregion

		#region Public methods

		/// <inheritdoc/>
		public V Get(R repository, K key)
		{
			var itemsCache = repositoryCache.Get(repository);

			return itemsCache.Get(key);
		}

		/// <inheritdoc/>
		public bool RemoveRepository(R repository) => repositoryCache.Remove(repository);

		#endregion
	}
}
