using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace AzureBillingExporter.Configuration.Metrics
{
    public class CustomCollectorConfig
    {
        [YamlMember(Alias = "metrics", ApplyNamingConventions = false)]
        public List<MetricConfig> Metrics { get; set; }
    }
}
