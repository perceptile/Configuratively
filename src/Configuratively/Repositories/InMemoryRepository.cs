using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Configuratively.Repositories
{
    public static class InMemoryRepository
    {
        private static ActiveCacheEnum _activeCache = ActiveCacheEnum.Green;
        private static IDictionary<ActiveCacheEnum, System.Runtime.Caching.MemoryCache> _caches;

        static InMemoryRepository()
        {
            _caches = new Dictionary<ActiveCacheEnum, System.Runtime.Caching.MemoryCache>();
            _caches.Add(ActiveCacheEnum.Green, new System.Runtime.Caching.MemoryCache("Green"));
            _caches.Add(ActiveCacheEnum.Blue, new System.Runtime.Caching.MemoryCache("Blue"));
        }
        
        public static System.Runtime.Caching.MemoryCache CurrentCache
        {
            get { return _caches.First(c => c.Key == _activeCache).Value; }
        }
        public static System.Runtime.Caching.MemoryCache StandbyCache
        {
            get { return _caches.First(c => c.Key != _activeCache).Value; }
        }

        private static System.Runtime.Caching.MemoryCache _standbyCache
        {
            get { return _caches.First(c => c.Key == _activeCache).Value; }
        }

        private enum ActiveCacheEnum
        {
            Green = 0,
            Blue = 1
        }

        public static void SwapActiveCache()
        {
            switch(_activeCache)
            {
                case ActiveCacheEnum.Green:
                {
                    _activeCache = ActiveCacheEnum.Blue;
                    break;
                }
                case ActiveCacheEnum.Blue:
                {
                    _activeCache = ActiveCacheEnum.Green;
                    break;
                }
            }
        }

        public static void ClearStandByCache()
        {
            //var envs = (GetEnvironmentsList(_standbyCache) as dynamic);
            //if (envs != null && envs.environments != null)
            //{
            //    var envArray = envs.environments as dynamic[] ?? envs.environments.ToArray();
            //    if (envArray.Length > 0)
            //    {
            //        foreach (var env in envArray)
            //        {
            //            var environment = InMemoryRepository.GetEnvironment(env.name, _standbyCache);
            //            if (((IDictionary<string, object>)environment).ContainsKey("servers"))
            //            {
            //                foreach (var s in environment.servers)
            //                {
            //                    TryRemoveCacheItem(string.Format(ServerTokensCacheFormatString, env.name, s.name), _standbyCache);
            //                }
            //                TryRemoveCacheItem(string.Format(EnvironmentTokensCacheFormatString, env.name), _standbyCache);
            //                TryRemoveCacheItem(env.name, _standbyCache);
            //            }
            //        }

            //        TryRemoveCacheItem(DefaultTokensCache, _standbyCache);
            //        TryRemoveCacheItem(ProvisioniongDefaultsCache, _standbyCache);

            //        TryRemoveCacheItem(EnvironmentsListCache, _standbyCache);
            //    }
            //}
        }

        //
        // All Read Methods operate against the live c ache by defaultand have a private
        // overload for reading from the stand-by cache.
        //
        public static object Get(string cacheKey)
        {
            return Get(cacheKey, CurrentCache);
        }
        private static object Get(string cacheKey, System.Runtime.Caching.MemoryCache cache)
        {
            return cache[cacheKey];
        }



        public static void Persist(string cacheKey, object value)
        {
            Persist(cacheKey, value, CurrentCache);
        }
        public static void Persist(string cacheKey, object value, System.Runtime.Caching.MemoryCache cache)
        {
            cache[cacheKey] = value;
        }


        private static void TryRemoveCacheItem(string key, System.Runtime.Caching.MemoryCache cache)
        {
            if (cache.Contains(key))
            {
                cache.Remove(key);
            }
        }
    }
}