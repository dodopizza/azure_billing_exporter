using System.Collections.Generic;
using System.Linq;
using AzureBillingExporter.Configuration;
using AzureBillingExporter.Configuration.Metrics;
using AzureBillingExporter.Cost;
using Prometheus;

namespace AzureBillingExporter.PrometheusMetrics
{
    public class CustomMetricsService
    {
        public readonly Dictionary<MetricConfig, Gauge> CustomGaugeMetrics = new Dictionary<MetricConfig, Gauge>();

        private readonly CustomCollectorConfiguration _customCollectorConfiguration;
        public CustomMetricsService(CustomCollectorConfiguration customCollectorConfiguration)
        {
            _customCollectorConfiguration = customCollectorConfiguration;
        }

        public void CreatePrometheusMetrics()
        {
            var customCollector = _customCollectorConfiguration.GetCustomCollectorConfig();
            foreach (var metricConfig in customCollector.Metrics)
            {
                // Labels
                var labelNames = new List<string>();
                foreach (var staticLabel in metricConfig.StaticLabel)
                {
                    labelNames.Add(staticLabel.Key);
                }
                foreach (var keyLabel in metricConfig.KeyLabels)
                {
                    labelNames.Add(keyLabel);
                }

                if (metricConfig.Type?.ToLower() == "gauge")
                {
                    var gauge = Metrics.CreateGauge(metricConfig.MetricName, metricConfig.Help,
                        new GaugeConfiguration()
                        {
                            LabelNames = labelNames.ToArray()
                        });

                    CustomGaugeMetrics.Add(metricConfig, gauge);
                }
            }
        }

        public void SetValues(MetricConfig key, CostResultRows customData)
        {
            var gauge = CustomGaugeMetrics[key];

            var labelValues = new List<string>();
            labelValues.AddRange(key.StaticLabel.Select(x => x.Value));
            foreach (var keyLabel in key.KeyLabels)
            {
                var dataColumnByKeyLabel = customData.GetByColumnName(keyLabel);

                if (key.ReplaceDateLabelsToEnum)
                {
                    dataColumnByKeyLabel = DateEnumHelper.ReplaceDateValueToEnums(dataColumnByKeyLabel);
                }
                labelValues.Add(dataColumnByKeyLabel);
            }

            gauge
                .WithLabels(labelValues.ToArray())
                .Set(customData.GetValueByColumnName(key.Value));
        }
    }
}
