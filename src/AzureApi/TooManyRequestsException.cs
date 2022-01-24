using System;

namespace AzureBillingExporter.AzureApi
{
    public class TooManyRequestsException : Exception
    {
        public TooManyRequestsException(string message) : base(message)
        {
        }
    }
}
