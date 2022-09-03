namespace Grammophone.Caching
{
	/// <summary>
	/// Interface for a cache that stores items created from repository instances, all held as weak references.
	/// </summary>
	/// <typeparam name="R">The type of the repository.</typeparam>
	/// <typeparam name="K">The type of item keys within a repository.</typeparam>
	/// <typeparam name="V">The type of items cached.</typeparam>
	public interface IWeakRepositoryCache<R, K, V>
		where R : class
	{
		/// <summary>
		/// Get an item from the cache or return a newly created one.
		/// Items are indexed by <paramref name="repository"/> and <paramref name="key"/>.
		/// </summary>
		/// <param name="repository">The repository from which to create the item.</param>
		/// <param name="key">The key defining the item in the <paramref name="repository"/>.</param>
		/// <returns>
		/// Returns the item corresponding to the supplied <paramref name="key"/> within the <paramref name="repository"/>,
		/// either from the cache, in case of a cache hit, or newly created.
		/// In the latter case, the item is inserted in the cache as the most
		/// recently used item for the repository.
		/// </returns>
		V Get(R repository, K key);

		/// <summary>
		/// Attempt to remove all items created from a repository.
		/// </summary>
		/// <param name="repository">The repository.</param>
		/// <returns>Returns true when items created from the <paramref name="repository"/> were existing in the cache, else false.</returns>
		bool RemoveRepository(R repository);
	}
}