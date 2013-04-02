using System;
using ServiceStack.CacheAccess.Providers;

namespace UniPhotoGallery.Globals
{
    public class MemoryCacheClientExtended : MemoryCacheClient, ICacheClientExtended
    {
        public T Get<T>(string cacheKey, Func<T> fallback) where T : class
        {
            return Get(cacheKey, fallback, DateTime.Now.AddHours(1));
        }

        public T Get<T>(string cacheKey, Func<T> fallback, DateTime fixedExpiration) where T : class
        {
            var obj = Get<T>(cacheKey);
            if (obj == null)
            {
                obj = fallback();

                if (obj != null)
                {
                    Set(cacheKey, obj, fixedExpiration);
                }
            }

            return obj;
        }

        public T Get<T>(string cacheKey, Func<T> fallback, TimeSpan timeSpanExpiration) where T : class
        {
            var obj = Get<T>(cacheKey);
            if (obj == null)
            {
                obj = fallback();

                if (obj != null)
                {
                    Set(cacheKey, obj, timeSpanExpiration);
                }
            }

            return obj;
        }
    }
}
