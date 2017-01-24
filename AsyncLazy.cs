using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grammophone.Caching
{
	/// <summary>
	/// Provides support for asynchronous lazy initialization.
	/// </summary>
	/// <typeparam name="T">The type of object being lazily initialized.</typeparam>
	public class AsyncLazy<T> : Lazy<Task<T>>
	{
		/// <summary>
		/// Create.
		/// </summary>
		/// <param name="asyncValueFactory">
		/// An asynchronous function to produce the value when it is needed.
		/// </param>
		/// <param name="isThreadSafe">
		/// If true, makes the instance usable concurrently by multiple threads.
		/// Set to false when high performance is crucial and the instance is guaranteed to be
		/// accessed by one thread at a time.
		/// </param>
		public AsyncLazy(Func<Task<T>> asyncValueFactory, bool isThreadSafe = true)
			: base(asyncValueFactory, isThreadSafe)
		{
		}

		/// <summary>
		/// Create.
		/// </summary>
		/// <param name="valueFactory">
		/// A function to produce the value when it is needed.
		/// </param>
		/// <param name="isThreadSafe">
		/// If true, makes the instance usable concurrently by multiple threads.
		/// Set to false when high performance is crucial and the instance is guaranteed to be
		/// accessed by one thread at a time.
		/// </param>
		public AsyncLazy(Func<T> valueFactory, bool isThreadSafe = true)
			: base(() => Task.Run(valueFactory), isThreadSafe)
		{
		}
	}
}
