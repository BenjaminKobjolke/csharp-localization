using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace CSharpLocalization
{
    /// <summary>
    /// Thread-safe cache for loaded translations.
    /// </summary>
    public class TranslationCache
    {
        private readonly ConcurrentDictionary<string, Dictionary<string, object>> _cache;

        /// <summary>
        /// Creates a new translation cache.
        /// </summary>
        public TranslationCache()
        {
            _cache = new ConcurrentDictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets a cached translation dictionary, or loads and caches it if not present.
        /// </summary>
        /// <param name="cacheKey">The cache key (typically the language code or path).</param>
        /// <param name="loader">A function to load the translations if not cached.</param>
        /// <returns>The translation dictionary.</returns>
        public Dictionary<string, object> GetOrLoad(string cacheKey, Func<Dictionary<string, object>> loader)
        {
            return _cache.GetOrAdd(cacheKey, _ => loader());
        }

        /// <summary>
        /// Invalidates cached translations.
        /// </summary>
        /// <param name="cacheKey">The specific key to invalidate, or null to clear all.</param>
        public void Invalidate(string cacheKey = null)
        {
            if (cacheKey == null)
            {
                _cache.Clear();
            }
            else
            {
                _cache.TryRemove(cacheKey, out _);
            }
        }

        /// <summary>
        /// Checks if a key is cached.
        /// </summary>
        /// <param name="cacheKey">The cache key to check.</param>
        /// <returns>True if the key is cached.</returns>
        public bool IsCached(string cacheKey)
        {
            return _cache.ContainsKey(cacheKey);
        }
    }
}
