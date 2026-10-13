using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Orchard.Environment.Configuration;
using Orchard.Utility.Extensions;

namespace Orchard.OutputCache.Services
{
    /// <summary>
    /// Tenant wide case insensitive reverse index for <see cref="CacheItem"/> tags.
    /// </summary>
    public class DefaultTagCache : ITagCache
    {
        private ConcurrentDictionary<string, HashSet<string>> _dictionary;
        private readonly string _cacheKey;
        private readonly IWorkContextAccessor _workContextAccessor;

        public DefaultTagCache(IWorkContextAccessor workContextAccessor, ShellSettings shellSettings)
        {
            _cacheKey = $"{shellSettings.Name}:TagCache";
            _workContextAccessor = workContextAccessor;
        }

        public void Tag(string tag, params string[] keys)
        {
            EnsureInitialized();

            var set = _dictionary.GetOrAdd(tag, x => new HashSet<string>());

            lock (set)
            {
                foreach (var key in keys)
                {
                    set.Add(key);
                }
            }
        }

        public IEnumerable<string> GetTaggedItems(string tag)
        {
            EnsureInitialized();

            HashSet<string> set;
            if (_dictionary.TryGetValue(tag, out set))
            {
                lock (set)
                {
                    return set.ToReadOnlyCollection();
                }
            }

            return Enumerable.Empty<string>();
        }

        public void RemoveTag(string tag)
        {
            EnsureInitialized();

            HashSet<string> set;
            _dictionary.TryRemove(tag, out set);
        }

        private void EnsureInitialized()
        {
            if (_dictionary == null)
            {
                lock (this)
                {
                    if (_dictionary == null)
                    {
                        _dictionary = (ConcurrentDictionary<string, HashSet<string>>)ApplicationCache.Instance.AddOrGetExisting(
                            _cacheKey,
                            new ConcurrentDictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase),
                            System.Runtime.Caching.ObjectCache.InfiniteAbsoluteExpiration)
                            ?? (ConcurrentDictionary<string, HashSet<string>>)ApplicationCache.Instance.Get(_cacheKey);
                    }
                }
            }
        }
    }
}
