using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.Twitter;
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
    [OrchardFeature("Orchard.OpenId.Twitter")]
    public class Twitter : IOwinMiddlewareProvider
    {
        private readonly IWorkContextAccessor _workContextAccessor;
        private readonly ShellSettings _shellSettings;

        public Twitter(IWorkContextAccessor workContextAccessor, ShellSettings shellSettings)
        {
            _workContextAccessor = workContextAccessor;
            _shellSettings = shellSettings;
        }

        public IEnumerable<OwinMiddlewareRegistration> GetOwinMiddlewares()
        {
            var settings = _workContextAccessor.GetContext().CurrentSite.As<TwitterSettingsPart>();

            if (settings == null || !settings.IsValid())
            {
                return Enumerable.Empty<OwinMiddlewareRegistration>();
            }

            // The certificates of the Twitter API chain are pinned by their subject key identifiers.
            var subjectKeyIdentifiers = new[]
            {
                settings.VeriSignClass3SecureServerCA_G2,
                settings.VeriSignClass3SecureServerCA_G3,
                settings.VeriSignClass3PublicPrimaryCA_G5,
                settings.SymantecClass3SecureServerCA_G4,
                settings.DigiCertSHA2HighAssuranceServerCA,
                settings.DigiCertHighAssuranceEVRootCA
            }.Where(ski => !string.IsNullOrWhiteSpace(ski)).ToArray();

            var twitterOptions = new TwitterOptions
            {
                ConsumerKey = settings.ConsumerKey,
                ConsumerSecret = settings.ConsumerSecret,
                SignInScheme = OpenIdAuthenticationSchemes.GetScheme(_shellSettings, OpenIdAuthenticationSchemes.ExternalCookie),
                BackchannelHttpHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, certificate, chain, errors) =>
                        errors == System.Net.Security.SslPolicyErrors.None &&
                        (subjectKeyIdentifiers.Length == 0 || chain.ChainElements.Cast<X509ChainElement>().Any(element =>
                            element.Certificate.Extensions.OfType<X509SubjectKeyIdentifierExtension>()
                                .Any(extension => subjectKeyIdentifiers.Contains(extension.SubjectKeyIdentifier, System.StringComparer.OrdinalIgnoreCase))))
                }
            };

            return new List<OwinMiddlewareRegistration> {
                new OwinMiddlewareRegistration {
                    Priority = Constants.General.OpenIdOwinMiddlewarePriority,
                    Configure = app => {
                        var dataProtection = app.ApplicationServices.GetRequiredService<IDataProtectionProvider>();
                        OpenIdAuthenticationSchemes.Register<TwitterOptions, TwitterHandler>(
                            app,
                            OpenIdAuthenticationSchemes.GetScheme(_shellSettings, TwitterDefaults.AuthenticationScheme),
                            twitterOptions,
                            new TwitterPostConfigureOptions(dataProtection));
                    }
                }
            };
        }
    }
}
