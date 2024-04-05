using Microsoft.Extensions.Caching.Memory;

namespace CardServicesProcessor.Shared
{
    public static class CacheManager
    {
        // Static property to set or retrieve the cache instance
        public static IMemoryCache? Cache { get; private set; }

        public static void Initialize(IMemoryCache memoryCache)
        {
            Cache = memoryCache;
        }
    }
}
