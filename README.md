# Gramma.Caching
This .NET library contains in-memory cache implementations for most-recently-used items. The items don't expire automatically. There are two implementations, one is thread safe called `MRUCache<K, V>` where `K` and `V` are the types of the keys and the values, and one non-thread safe `SequentialMRUCache<K, V>` which has the same API.

Usage is very simple. Create the cache by supplying an item creation function `Func<K, V>` which will be used upon cache miss to create an item, followed by the maximum number of cached items. The number of cached items can later be changed by setting the `MaxCount` property, which will cause the cache to drop the least recently used items if it is less than the number of currently retained items. Use the `Get` method supplying the item's key to fetch an item. During a cache miss, the item will be created automatically using the creator function which was supplied in the constructor. Use `Remove` to flush an item, `Clear` to flush the all items. You can get the hit rate of the cache via the `GetStatistics` method and reset it with `ClearStatistics`.

This library has no dependencies.
