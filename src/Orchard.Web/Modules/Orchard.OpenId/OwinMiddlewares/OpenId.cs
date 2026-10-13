using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Orchard.Environment.Configuration;
using Orchard.Environment.Extensions;
using Orchard.OpenId.Security;
using Orchard.Owin;

namespace Orchard.OpenId.OwinMiddlewares
{
    [OrchardFeature("Orchard.OpenId")]
    public class OpenId : IOwinMiddlewareProvider
    {
        private readonly ShellSettings _shellSettings;

        public OpenId(ShellSettings shellSettings)
        {
            _shellSettings = shellSettings;
        }

        public IEnumerable<OwinMiddlewareRegistration> GetOwinMiddlewares()
        {
            var scheme = OpenIdAuthenticationSchemes.GetScheme(_shellSettings, OpenIdAuthenticationSchemes.ExternalCookie);

            return new List<OwinMiddlewareRegistration> {
                new OwinMiddlewareRegistration {
                    Priority = "9",
                    Configure = app => {
                        // the cookie in which the external identities are signed in
                        var dataProtection = app.ApplicationServices.GetRequiredService<IDataProtectionProvider>();
                        OpenIdAuthenticationSchemes.Register<CookieAuthenticationOptions, CookieAuthenticationHandler>(
                            app, scheme, new CookieAuthenticationOptions(), new PostConfigureCookieAuthenticationOptions(dataProtection));

                        // the remote authentication responses of the providers are handled in the tenant pipeline
                        OpenIdAuthenticationSchemes.UseRemoteAuthentication(app);
                    }
                }
            };
        }
    }
}
