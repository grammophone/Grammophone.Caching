using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Grammophone.Caching
{
	/// <summary>
	/// A cache that stores items created from repository instances, all held as weak references.
	/// </summary>
	/// <typeparam name="R">The type of the repository.</typeparam>
	/// <typeparam name="K">The type of item keys within a repository.</typeparam>
	/// <typeparam name="V">The type of items cached.</typeparam>
	public class AsyncWeakRepositoryCache<R, K, V> : WeakRepositoryCache<R, K, Task<V>>
		where R : class
	{
		#region Construction

		/// <summary>
		/// Create.
		/// </summary>
		/// <param name="itemCreator">Function to create an item from a repository and a key.</param>
		/// <param name="isThreadSafeWithinRepository">If true, the items in a repository can be accessed in a thread-safe manner. Default is true.</param>
		/// <param name="maxCount">Optional maximum number of items cached under a repository.</param>
		public AsyncWeakRepositoryCache(Func<R, K, Task<V>> itemCreator, bool isThreadSafeWithinRepository = true, int maxCount = 1024)
			: base(itemCreator, isThreadSafeWithinRepository, maxCount)
		{
		}

		/// <summary>
		/// Create.
		/// </summary>
		/// <param name="mruCacheCreator">A function to create an <see cref="IMRUCache{K,Task}"/> for a repository.</param>
		public AsyncWeakRepositoryCache(Func<R, IMRUCache<K, Task<V>>> mruCacheCreator) : base(mruCacheCreator)
		{
		}

		#endregion
	}
}
