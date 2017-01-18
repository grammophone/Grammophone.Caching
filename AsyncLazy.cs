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
		public AsyncLazy(Func<Task<T>> asyncValueFactory)
			: base(asyncValueFactory)
		{
		}

		/// <summary>
		/// Create.
		/// </summary>
		/// <param name="valueFactory">
		/// A function to produce the value when it is needed.
		/// </param>
		public AsyncLazy(Func<T> valueFactory)
			: base(() => Task.Run(valueFactory))
		{
		}
	}
}
