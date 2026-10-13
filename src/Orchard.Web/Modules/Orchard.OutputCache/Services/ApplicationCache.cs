using System.Runtime.Caching;

namespace Orchard.OutputCache.Services
{
    /// <summary>
    /// The application wide in-memory cache of the output cache items (formerly the ASP.NET HttpRuntime.Cache).
    /// </summary>
    public static class ApplicationCache
    {
        public static MemoryCache Instance { get; } = new MemoryCache("Orchard.OutputCache");
    }
}
