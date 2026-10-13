using NHibernate.Cfg.Loquacious;
using Orchard.Data;
using Orchard.Environment.Configuration;
using Orchard.Environment.Extensions;

namespace SysCache.Services
{
    [OrchardSuppressDependency("Orchard.Data.DefaultDatabaseCacheConfiguration")]
    public class SysCacheDatabaseCacheConfiguration : IDatabaseCacheConfiguration
    {
        private readonly ShellSettings _shellSettings;

        public SysCacheDatabaseCacheConfiguration(ShellSettings shellSettings)
        {
            _shellSettings = shellSettings;
        }

        public void Configure(CacheConfigurationProperties cache)
        {
            // SysCache2 depends on the ASP.NET (System.Web) cache: the memory cache provider of NHibernate.Caches is used on .NET
            cache.Provider<NHibernate.Caches.CoreMemoryCache.CoreMemoryCacheProvider>();
            cache.UseQueryCache = true;
            cache.RegionsPrefix = _shellSettings.Name;
        }
    }
}