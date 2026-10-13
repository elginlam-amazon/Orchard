using System;
using System.Collections.Generic;
using System.Linq;
using Orchard.Environment.Configuration;
using Orchard.OutputCache.Models;

namespace Orchard.OutputCache.Services
{
    public class DefaultCacheStorageProvider : IOutputCacheStorageProvider
    {
        private readonly string _tenantName;

        public DefaultCacheStorageProvider(IWorkContextAccessor workContextAccessor, ShellSettings shellSettings)
        {
            _tenantName = shellSettings.Name;
        }

        public void Set(string key, CacheItem cacheItem)
        {
            ApplicationCache.Instance.Set(key, cacheItem, new DateTimeOffset(cacheItem.StoredUntilUtc, TimeSpan.Zero));
        }

        public void Remove(string key)
        {
            ApplicationCache.Instance.Remove(key);
        }

        public void RemoveAll()
        {
            var items = GetCacheItems(0, 100).ToList();
            while (items.Any())
            {
                foreach (var item in items)
                {
                    Remove(item.CacheKey);
                }
                items = GetCacheItems(0, 100).ToList();
            }
        }

        public CacheItem GetCacheItem(string key)
        {
            return ApplicationCache.Instance.Get(key) as CacheItem;
        }

        public IEnumerable<CacheItem> GetCacheItems(int skip, int count)
        {
            // the ASP.NET cache can also contain other types of items
            return ApplicationCache.Instance.AsParallel()
                        .Select(x => x.Value)
                        .OfType<CacheItem>()
                        .Where(x => x.Tenant.Equals(_tenantName, StringComparison.OrdinalIgnoreCase))
                        .Skip(skip)
                        .Take(count);
        }

        public int GetCacheItemsCount()
        {
            return ApplicationCache.Instance.AsParallel()
                        .Select(x => x.Value)
                        .OfType<CacheItem>()
                        .Count(x => x.Tenant.Equals(_tenantName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
