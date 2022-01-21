using AzureBillingExporter.Cost;
using Prometheus;

namespace AzureBillingExporter.PrometheusMetrics
{
    public class MetricsUpdater
    {
        private static readonly Gauge DailyCosts =
            Metrics.CreateGauge(
                "azure_billing_daily",
                "Daily cost by today, yesterday and day before yesterday",
                new GaugeConfiguration
                {
                    LabelNames = new[] { "DateEnum" }
                });

        private static readonly Gauge MonthlyCosts =
            Metrics.CreateGauge(
                "azure_billing_monthly",
                "This month costs",
                new GaugeConfiguration
                {
                    LabelNames = new[] { "DateEnum" }
                });

        private readonly CustomMetricsService _customMetricsService;
        private readonly CostDataCache _costDataCache;
        private bool _metricsCreated;

        public MetricsUpdater(
            CustomMetricsService customMetricsService,
            CostDataCache costDataCache)
        {
            _customMetricsService = customMetricsService;
            _costDataCache = costDataCache;
        }

        public void Update()
        {
            if (!_metricsCreated)
            {
                _customMetricsService.CreatePrometheusMetrics();
                _metricsCreated = true;
            }

            foreach (var data in _costDataCache.GetDailyCost())
            {
                var dayEnum = DateEnumHelper.ReplaceDateValueToEnums(data.GetByColumnName("UsageDate"));

                DailyCosts
                    .WithLabels(dayEnum)
                    .Set(data.Cost);
            }

            foreach (var data in _costDataCache.GetMonthlyCost())
            {
                var monthEnum = DateEnumHelper.ReplaceDateValueToEnums(data.GetByColumnName("BillingMonth"));

                MonthlyCosts
                    .WithLabels(monthEnum)
                    .Set(data.Cost);
            }

            foreach (var metric in _customMetricsService.CustomGaugeMetrics)
            {
                var costs = _costDataCache.GetCostByKey(metric.Key.QueryFilePath);
                foreach (var cost in costs)
                {
                    _customMetricsService.SetValues(metric.Key, cost);
                }
            }
        }
    }
}
