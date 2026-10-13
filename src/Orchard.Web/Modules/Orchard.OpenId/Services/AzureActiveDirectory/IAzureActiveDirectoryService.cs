using System;
using System.Threading.Tasks;

namespace Orchard.OpenId.Services.AzureActiveDirectory
{
    /// <summary>
    /// Holds the application token of the Azure AD Graph API.
    /// </summary>
    /// <remarks>
    /// The Azure AD Graph client (Microsoft.Azure.ActiveDirectory.GraphClient) is retired and not available on .NET:
    /// the consumers use the token to call the Graph API directly.
    /// </remarks>
    public interface IAzureActiveDirectoryService : ISingletonDependency
    {
        string Token { get; set; }
        DateTimeOffset TokenExpiresOn { get; set; }
        string AzureTenant { get; set; }
        Task<string> AcquireTokenAsync();
    }
}
