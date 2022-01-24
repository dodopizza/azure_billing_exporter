namespace AzureBillingExporter.Configuration
{
    public class EnvironmentConfiguration
    {
        public bool LogsAtJsonFormat { get; set; }

        public int CollectPeriodInMinutes { get; set; } = 5;
        public int CachePeriodInMinutes { get; set; } = 10;
    }
}
