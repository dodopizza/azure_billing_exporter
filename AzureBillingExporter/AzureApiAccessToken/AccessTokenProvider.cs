namespace AzureBillingExporter.AzureApiAccessToken
{
    public class AccessTokenProvider : IAccessTokenProvider
    {
        private string _accessToken = string.Empty;

        public string GetAccessToken()
        {
            return _accessToken;
        }

        public void SetAccessToken(string newAccessToken)
        {
            _accessToken = newAccessToken;
        }        
    }
}