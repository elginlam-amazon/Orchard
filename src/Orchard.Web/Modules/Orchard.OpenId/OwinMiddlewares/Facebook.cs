using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.OAuth;
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
    [OrchardFeature("Orchard.OpenId.Facebook")]
    public class Facebook : IOwinMiddlewareProvider
    {
        private readonly IWorkContextAccessor _workContextAccessor;
        private readonly ShellSettings _shellSettings;

        public Facebook(IWorkContextAccessor workContextAccessor, ShellSettings shellSettings)
        {
            _workContextAccessor = workContextAccessor;
            _shellSettings = shellSettings;
        }

        public IEnumerable<OwinMiddlewareRegistration> GetOwinMiddlewares()
        {
            var settings = _workContextAccessor.GetContext().CurrentSite.As<FacebookSettingsPart>();

            if (settings == null || !settings.IsValid())
            {
                return Enumerable.Empty<OwinMiddlewareRegistration>();
            }

            var options = new FacebookOptions
            {
                AppId = settings.AppId,
                AppSecret = settings.AppSecret,
                SignInScheme = OpenIdAuthenticationSchemes.GetScheme(_shellSettings, OpenIdAuthenticationSchemes.ExternalCookie)
            };

            return new List<OwinMiddlewareRegistration> {
                new OwinMiddlewareRegistration {
                    Priority = Constants.General.OpenIdOwinMiddlewarePriority,
                    Configure = app => {
                        var dataProtection = app.ApplicationServices.GetRequiredService<IDataProtectionProvider>();
                        OpenIdAuthenticationSchemes.Register<FacebookOptions, FacebookHandler>(
                            app,
                            OpenIdAuthenticationSchemes.GetScheme(_shellSettings, FacebookDefaults.AuthenticationScheme),
                            options,
                            new OAuthPostConfigureOptions<FacebookOptions, FacebookHandler>(dataProtection));
                    }
                }
            };
        }
    }
}
