namespace AzureBillingExporter.AzureApiAccessToken
{
    public interface IAccessTokenProvider
    {
        public string GetAccessToken();

        public void SetAccessToken(string newAccessToken);        
    }
}