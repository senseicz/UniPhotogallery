using System;
using ServiceStack.CacheAccess;

namespace UniPhotoGallery.Globals
{
    public interface ICacheClientExtended : ICacheClient
    {
        T Get<T>(string cacheKey, Func<T> fallback) where T : class;
        T Get<T>(string cacheKey, Func<T> fallback, DateTime fixedExpiration) where T : class;
        T Get<T>(string cacheKey, Func<T> fallback, TimeSpan timeSpanExpiration) where T : class;
    }
}
