using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AzureBillingExporter.Configuration;

namespace AzureBillingExporter.Cost
{
    public class CostDataCache
    {
        private readonly ConcurrentDictionary<string, CacheItem> _costDataCache =
            new ConcurrentDictionary<string, CacheItem>();

        private readonly EnvironmentConfiguration _environmentConfiguration;

        public CostDataCache(EnvironmentConfiguration environmentConfiguration)
        {
            _environmentConfiguration = environmentConfiguration;
        }

        public List<CostResultRows> GetDailyCost()
        {
            return GetCostByKey("Daily");
        }

        public void SetDailyCost(List<CostResultRows> data)
        {
            SetCostByKey("Daily", data);
        }

        public List<CostResultRows> GetMonthlyCost()
        {
            return GetCostByKey("Monthly");
        }

        public void SetMonthlyCost(List<CostResultRows> data)
        {
            SetCostByKey("Monthly", data);
        }

        public List<CostResultRows> GetCostByKey(string key)
        {
            var cacheItem = GetCacheItem(key);
            if (cacheItem == null)
            {
                return new List<CostResultRows>();
            }

            return cacheItem.Data;
        }

        public void SetCostByKey(string key, List<CostResultRows> data)
        {
            SetCacheItem(key, data);
        }

        private CacheItem GetCacheItem(string key)
        {
            var expireDate = DateTime.UtcNow.Add(-1*TimeSpan.FromMinutes(_environmentConfiguration.CachePeriodInMinutes));
            var cacheItem = _costDataCache.ContainsKey(key) ? _costDataCache[key] : null;
            if (cacheItem == null || cacheItem.Created <= expireDate)
            {
                return null;
            }

            return _costDataCache[key];
        }

        private void SetCacheItem(string key, List<CostResultRows> data)
        {
             _costDataCache[key] = new CacheItem
             {
                 Created = DateTime.UtcNow,
                 Data = data
             };
        }

        class CacheItem
        {
            public DateTime Created { get; set; }
            public List<CostResultRows> Data { get; set; }
        }
    }
}
