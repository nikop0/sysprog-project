using System;
using System.Collections.Generic;
using System.Threading;

namespace zadatak2
{
    internal class SearchCache
    {
        private readonly Dictionary<string, (string Result, DateTime Expiry)> _cache = new();
        
        private readonly HashSet<string> _processing = new();
        
        private readonly object _lock = new();
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

        public string GetOrAdd(string query, Func<string> searchAction)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(query, out var entry))
                {
                    if (DateTime.Now < entry.Expiry)
                    {
                        return entry.Result;
                    }
                    _cache.Remove(query);
                }

                while (_processing.Contains(query))
                {
                    Monitor.Wait(_lock);
                    
                    if (_cache.TryGetValue(query, out var newEntry))
                    {
                        return newEntry.Result;
                    }
                }

                _processing.Add(query);
            }

            try
            {
                string searchResult = searchAction();

                lock (_lock)
                {
                    _cache[query] = (searchResult, DateTime.Now.Add(_cacheDuration));
                    return searchResult;
                }
            }
            finally
            {
                lock (_lock)
                {
                    _processing.Remove(query);
                    Monitor.PulseAll(_lock);
                }
            }
        }
    }
}