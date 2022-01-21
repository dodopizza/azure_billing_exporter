using System.Collections.Concurrent;
using System.Collections.Generic;

namespace AzureBillingExporter
{
    public class CostDataCache
    {
        private readonly ConcurrentDictionary<string, List<CostResultRows>> _costDataCache =
            new ConcurrentDictionary<string, List<CostResultRows>>();

        public List<CostResultRows> GetDailyCost()
        {
            return _costDataCache.ContainsKey("Daily") ? _costDataCache["Daily"] : new List<CostResultRows>();
        }

        public void SetDailyCost(List<CostResultRows> data)
        {
            _costDataCache["Daily"] = data;
        }

        public List<CostResultRows> GetMonthlyCost()
        {
            return _costDataCache.ContainsKey("Monthly") ? _costDataCache["Monthly"] : new List<CostResultRows>();
        }

        public void SetMonthlyCost(List<CostResultRows> data)
        {
            _costDataCache["Monthly"] = data;
        }
    }
}
