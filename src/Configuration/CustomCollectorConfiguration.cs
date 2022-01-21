using System.IO;
using AzureBillingExporter.Configuration.Metrics;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AzureBillingExporter.Configuration
{
    public class CustomCollectorConfiguration
    {
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
        public CustomCollectorConfig GetCustomCollectorConfig()
        {
            if (!File.Exists(CustomCollectorsConfigFile))
            {
                return null;
            }

            var configText = File.ReadAllText(CustomCollectorsConfigFile);
            var input = new StringReader(configText);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var config = deserializer.Deserialize<CustomCollectorConfig>(input);
            return config;
        }
    }
}
