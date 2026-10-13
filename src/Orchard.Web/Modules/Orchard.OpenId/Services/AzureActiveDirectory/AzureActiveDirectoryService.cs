using System;
using System.Threading.Tasks;
using Orchard.Environment.Extensions;

namespace Orchard.OpenId.Services.AzureActiveDirectory
{
    [OrchardFeature("Orchard.OpenId.AzureActiveDirectory")]
    public class AzureActiveDirectoryService : IAzureActiveDirectoryService
    {
        public string Token { get; set; }
        public DateTimeOffset TokenExpiresOn { get; set; }
        public string AzureTenant { get; set; }

        public async Task<string> AcquireTokenAsync()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("Authorization Required.");
            }
            return await Task.FromResult(Token);
        }
    }
}
