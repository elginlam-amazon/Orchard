using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Orchard.ContentManagement;
using Orchard.Environment.Configuration;
using Orchard.Environment.Extensions;
using Orchard.Logging;
using Orchard.OpenId.Models;
using Orchard.OpenId.Security;
using Orchard.OpenId.Services.AzureActiveDirectory;
using Orchard.Owin;
using LogLevel = Orchard.Logging.LogLevel;

namespace Orchard.OpenId.OwinMiddlewares
{
    [OrchardFeature("Orchard.OpenId.AzureActiveDirectory")]
    public class AzureActiveDirectory : IOwinMiddlewareProvider
    {
        public ILogger Logger { get; set; }

        private readonly IWorkContextAccessor _workContextAccessor;
        private readonly IAzureActiveDirectoryService _azureActiveDirectoryService;
        private readonly ShellSettings _shellSettings;
        private string _azureGraphApiUri;
        private string _azureGraphApiKey;
        private string _azureClientId;
        private string _azureTenant;
        private string _azureAdInstance;

        public AzureActiveDirectory(
            IWorkContextAccessor workContextAccessor,
            IAzureActiveDirectoryService azureActiveDirectoryService,
            ShellSettings shellSettings)
        {
            _workContextAccessor = workContextAccessor;
            _azureActiveDirectoryService = azureActiveDirectoryService;
            _shellSettings = shellSettings;

            Logger = NullLogger.Instance;
        }

        public IEnumerable<OwinMiddlewareRegistration> GetOwinMiddlewares()
        {
            var settings = _workContextAccessor.GetContext().CurrentSite.As<AzureActiveDirectorySettingsPart>();
            var logoutRedirectUri = string.Empty;
            var azureAppKey = string.Empty;
            var azureUseAzureGraphApi = false;

            if (settings == null || !settings.IsValid())
            {
                return Enumerable.Empty<OwinMiddlewareRegistration>();
            }

            _azureClientId = settings.ClientId;
            _azureTenant = settings.Tenant;
            _azureAdInstance = settings.ADInstance;
            _azureGraphApiUri = settings.GraphApiUrl;
            _azureGraphApiKey = settings.GraphApiKey;
            logoutRedirectUri = settings.LogoutRedirectUri;
            azureAppKey = settings.AppKey;
            azureUseAzureGraphApi = settings.UseAzureGraphApi;

            var authority = string.Format(CultureInfo.InvariantCulture, _azureAdInstance, _azureTenant);
            var middlewares = new List<OwinMiddlewareRegistration>();

            var openIdOptions = new OpenIdConnectOptions
            {
                ClientId = _azureClientId,
                Authority = authority,
                SignedOutRedirectUri = logoutRedirectUri,
                ResponseType = OpenIdConnectResponseType.CodeIdToken,
                SignInScheme = OpenIdAuthenticationSchemes.GetScheme(_shellSettings, OpenIdAuthenticationSchemes.ExternalCookie),
                Events = new OpenIdConnectEvents
                {
                    OnAuthorizationCodeReceived = async context =>
                    {
                        // The authorization code is redeemed with MSAL (formerly ADAL) for the Graph API resource.
                        var application = ConfidentialClientApplicationBuilder.Create(_azureClientId)
                            .WithClientSecret(azureAppKey)
                            .WithAuthority(authority)
                            .WithRedirectUri(context.TokenEndpointRequest?.RedirectUri ?? context.Request.GetEncodedUrl())
                            .Build();

                        var result = await application
                            .AcquireTokenByAuthorizationCode(new[] { GetGraphApiScope() }, context.ProtocolMessage.Code)
                            .ExecuteAsync();

                        context.HandleCodeRedemption(result.AccessToken, result.IdToken);
                    },
                    OnRemoteFailure = context =>
                    {
                        context.HandleResponse();
                        context.Response.Redirect(context.Request.PathBase + Constants.General.AuthenticationErrorUrl);
                        Logger.Debug(context.Failure, "AAD authentication failed.");
                        return Task.CompletedTask;
                    }
                }
            };

            // Allowing login from all AAD tenants (so with any Microsoft ID). We'd need to list all possible AAD tenants 
            // here otherwise.
            openIdOptions.TokenValidationParameters.ValidateIssuer = false;

            middlewares.Add(new OwinMiddlewareRegistration
            {
                Priority = Constants.General.OpenIdOwinMiddlewarePriority,
                Configure = app =>
                {
                    var dataProtection = app.ApplicationServices.GetRequiredService<IDataProtectionProvider>();
                    OpenIdAuthenticationSchemes.Register<OpenIdConnectOptions, OpenIdConnectHandler>(
                        app,
                        OpenIdAuthenticationSchemes.GetScheme(_shellSettings, OpenIdConnectDefaults.AuthenticationScheme),
                        openIdOptions,
                        new OpenIdConnectPostConfigureOptions(dataProtection));
                }
            });

            if (azureUseAzureGraphApi)
            {
                middlewares.Add(new OwinMiddlewareRegistration
                {
                    Priority = "11",
                    Configure = app => app.Use(async (context, next) =>
                    {
                        try
                        {
                            if (string.IsNullOrEmpty(_azureActiveDirectoryService.Token))
                            {
                                await RegenerateAzureGraphApiTokenAsync();
                            }
                            else
                            {
                                if (DateTimeOffset.Compare(DateTimeOffset.UtcNow, _azureActiveDirectoryService.TokenExpiresOn) > 0)
                                {
                                    await RegenerateAzureGraphApiTokenAsync();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(LogLevel.Error, ex, "An error occurred generating azure api credential {0}", ex.Message);
                        }

                        await next();
                    })
                });
            }

            return middlewares;
        }

        private string GetGraphApiScope()
        {
            return (_azureGraphApiUri ?? string.Empty).TrimEnd('/') + "/.default";
        }

        private async Task RegenerateAzureGraphApiTokenAsync()
        {
            var authority = string.Format(CultureInfo.InvariantCulture, _azureAdInstance, _azureTenant);
            var application = ConfidentialClientApplicationBuilder.Create(_azureClientId)
                .WithClientSecret(_azureGraphApiKey)
                .WithAuthority(authority)
                .Build();

            var result = await application.AcquireTokenForClient(new[] { GetGraphApiScope() }).ExecuteAsync();

            _azureActiveDirectoryService.TokenExpiresOn = result.ExpiresOn;
            _azureActiveDirectoryService.Token = result.AccessToken;
            _azureActiveDirectoryService.AzureTenant = _azureTenant;
        }
    }
}
