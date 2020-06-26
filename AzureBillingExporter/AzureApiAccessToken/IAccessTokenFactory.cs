using System.Threading;
using System.Threading.Tasks;

namespace AzureBillingExporter.AzureApiAccessToken
{
    public interface IAccessTokenFactory
    {
        public Task<string> CreateAsync(CancellationToken cancellationToken = default);
    }
}