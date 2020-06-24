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
        
        [YamlMember(Alias = "query_file", ApplyNamingConventions = false)]
        public string QueryFilePath { get; set; }
        
    }
    
    public class CustomCollectorConfiguration
    {
        public readonly Dictionary<MetricConfig, Gauge> CustomGaugeMetrics = new Dictionary<MetricConfig, Gauge>();    // <MetricConfig, Gauge>
        
        public void ReadCustomCollectorConfig()
        {
            const string customCollectorsConfigFile = "custom_collectors.yml";
            if (!File.Exists(customCollectorsConfigFile))
            {
                return;
            }
            
            var configText = File.ReadAllText(customCollectorsConfigFile);
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

            foreach (var keyLabel in key.KeyLabels)
            {
                var labels = key.StaticLabel.Select(x => x.Value).ToList();
                var dataColumnByKeyLabel = customData.GetByColumnName(keyLabel);
                labels.Add(dataColumnByKeyLabel);
                
                if (!string.IsNullOrEmpty(dataColumnByKeyLabel))
                {
                    gauge
                        .WithLabels(labels.ToArray())
                        .Set(customData.GetValueByColumnName(key.Value));
                }
            }
        }
    }
}