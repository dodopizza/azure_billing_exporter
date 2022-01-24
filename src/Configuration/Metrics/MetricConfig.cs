using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace AzureBillingExporter.Configuration.Metrics
{
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
}
