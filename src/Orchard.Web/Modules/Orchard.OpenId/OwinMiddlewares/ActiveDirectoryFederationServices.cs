using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Orchard.ContentManagement;
using Orchard.Environment.Configuration;
using Orchard.Environment.Extensions;
using Orchard.OpenId.Models;
using Orchard.OpenId.Security;
using Orchard.Owin;

namespace Orchard.OpenId.OwinMiddlewares
{
    [OrchardFeature("Orchard.OpenId.ActiveDirectoryFederationServices")]
    public class ActiveDirectoryFederationServices : IOwinMiddlewareProvider
    {
        private readonly IWorkContextAccessor _workContextAccessor;
        private readonly ShellSettings _shellSettings;

        public ActiveDirectoryFederationServices(IWorkContextAccessor workContextAccessor, ShellSettings shellSettings)
        {
            _workContextAccessor = workContextAccessor;
            _shellSettings = shellSettings;
        }

        public IEnumerable<OwinMiddlewareRegistration> GetOwinMiddlewares()
        {
            var settings = _workContextAccessor.GetContext().CurrentSite.As<ActiveDirectoryFederationServicesSettingsPart>();

            if (settings == null || !settings.IsValid())
            {
                return Enumerable.Empty<OwinMiddlewareRegistration>();
            }

            var openIdOptions = new OpenIdConnectOptions
            {
                ClientId = settings.ClientId,
                MetadataAddress = settings.MetadataAddress,
                SignedOutRedirectUri = settings.PostLogoutRedirectUri,
                SignInScheme = OpenIdAuthenticationSchemes.GetScheme(_shellSettings, OpenIdAuthenticationSchemes.ExternalCookie),
                Events = new OpenIdConnectEvents
                {
                    OnRemoteFailure = context =>
                    {
                        context.HandleResponse();
                        context.Response.Redirect(context.Request.PathBase + Constants.General.AuthenticationErrorUrl);

                        return Task.CompletedTask;
                    }
                }
            };

            return new List<OwinMiddlewareRegistration> {
                new OwinMiddlewareRegistration {
                    Priority = Constants.General.OpenIdOwinMiddlewarePriority,
                    Configure = app => {
                        var dataProtection = app.ApplicationServices.GetRequiredService<IDataProtectionProvider>();
                        OpenIdAuthenticationSchemes.Register<OpenIdConnectOptions, OpenIdConnectHandler>(
                            app,
                            OpenIdAuthenticationSchemes.GetScheme(_shellSettings, OpenIdConnectDefaults.AuthenticationScheme),
                            openIdOptions,
                            new OpenIdConnectPostConfigureOptions(dataProtection));
                    }
                }
            };
        }
    }
}
