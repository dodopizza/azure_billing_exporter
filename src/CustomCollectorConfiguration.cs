using System.Collections.Generic;
using System.IO;
using System.Linq;
using Prometheus;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AzureBillingExporter
{
    public class MetricConfigCollections
    {
        [YamlMember(Alias = "metrics", ApplyNamingConventions = false)]
        public List<MetricConfig> Metrics { get; set; }
    }

    public class MetricConfig
    {
        [YamlMember(Alias = "metric_name", ApplyNamingConventions = false)]
        public string MetricName { get; set; }

        public string Type { get; set; }
        public string Help { get; set; }

        [YamlMember(Alias = "key_labels", ApplyNamingConventions = false)]
        public List<string> KeyLabels { get; set; }

        [YamlMember(Alias = "static_labels", ApplyNamingConventions = false)]
        public Dictionary<string, string> StaticLabel { get; set; }

        public string Value { get; set; }

        public int? Limit { get; set; }

        [YamlMember(Alias = "replace_date_labels_to_enum", ApplyNamingConventions = false)]
        public bool ReplaceDateLabelsToEnum { get; set; }

        [YamlMember(Alias = "query_file", ApplyNamingConventions = false)]
        public string QueryFilePath { get; set; }

    }

    public class CustomCollectorConfiguration
    {
        public readonly Dictionary<MetricConfig, Gauge> CustomGaugeMetrics = new Dictionary<MetricConfig, Gauge>();    // <MetricConfig, Gauge>

        private string CustomCollectorsConfigFile { get; }
        public CustomCollectorConfiguration(string customCollectorsFilePath)
        {
            if (!string.IsNullOrEmpty(customCollectorsFilePath))
            {
                CustomCollectorsConfigFile = customCollectorsFilePath;
            }
            else
            {
                CustomCollectorsConfigFile = "custom_collectors.yml";
            }
        }
        public void ReadCustomCollectorConfig()
        {
            if (!File.Exists(CustomCollectorsConfigFile))
            {
                return;
            }

            var configText = File.ReadAllText(CustomCollectorsConfigFile);
            var input = new StringReader(configText);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var metricCollections = deserializer.Deserialize<MetricConfigCollections>(input);

            CreatePrometheusMetrics(metricCollections);
        }

        private void CreatePrometheusMetrics(MetricConfigCollections metricCollection)
        {
            foreach (var metricConfig in metricCollection.Metrics)
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
