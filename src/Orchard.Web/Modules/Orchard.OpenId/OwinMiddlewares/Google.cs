using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Orchard.ContentManagement;
using Orchard.Environment.Configuration;
using Orchard.Environment.Extensions;
using Orchard.OpenId.Models;
using Orchard.OpenId.Security;
using Orchard.Owin;

namespace Orchard.OpenId.OwinMiddlewares
{
    [OrchardFeature("Orchard.OpenId.Google")]
    public class Google : IOwinMiddlewareProvider
    {
        private readonly IWorkContextAccessor _workContextAccessor;
        private readonly ShellSettings _shellSettings;

        public Google(IWorkContextAccessor workContextAccessor, ShellSettings shellSettings)
        {
            _workContextAccessor = workContextAccessor;
            _shellSettings = shellSettings;
        }

        public IEnumerable<OwinMiddlewareRegistration> GetOwinMiddlewares()
        {
            var workContext = _workContextAccessor.GetContext();
            var settings = workContext.CurrentSite.As<GoogleSettingsPart>();

            if (settings == null || !settings.IsValid())
            {
                return Enumerable.Empty<OwinMiddlewareRegistration>();
            }

            var authenticationOptions = new GoogleOptions
            {
                ClientId = settings.ClientId,
                ClientSecret = settings.ClientSecret,
                // the tenant url prefix is part of the request path base
                CallbackPath = new PathString(settings.CallbackPath),
                SignInScheme = OpenIdAuthenticationSchemes.GetScheme(_shellSettings, OpenIdAuthenticationSchemes.ExternalCookie)
            };

            return new List<OwinMiddlewareRegistration> {
                new OwinMiddlewareRegistration {
                    Priority = Constants.General.OpenIdOwinMiddlewarePriority,
                    Configure = app => {
                        var dataProtection = app.ApplicationServices.GetRequiredService<IDataProtectionProvider>();
                        OpenIdAuthenticationSchemes.Register<GoogleOptions, GoogleHandler>(
                            app,
                            OpenIdAuthenticationSchemes.GetScheme(_shellSettings, GoogleDefaults.AuthenticationScheme),
                            authenticationOptions,
                            new OAuthPostConfigureOptions<GoogleOptions, GoogleHandler>(dataProtection));
                    }
                }
            };
        }
    }
}
