using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orchard.Environment.Configuration;

namespace Orchard.OpenId.Security
{
    /// <summary>
    /// Registers the authentication schemes of the external providers of a tenant at runtime
    /// (they were OWIN authentication middlewares of the tenant pipeline).
    /// </summary>
    /// <remarks>
    /// The schemes are prefixed by the tenant name, so that the tenants can have different settings.
    /// The remote authentication responses (callbacks) are handled by <see cref="UseRemoteAuthentication"/>
    /// in the tenant pipeline.
    /// </remarks>
    public static class OpenIdAuthenticationSchemes
    {
        /// <summary>
        /// The cookie in which the external identities are signed in (formerly the OWIN "Cookies" authentication type).
        /// </summary>
        public const string ExternalCookie = CookieAuthenticationDefaults.AuthenticationScheme;

        private const string SchemesKey = "Orchard.OpenId.Schemes";

        public static string GetScheme(ShellSettings shellSettings, string authenticationType)
        {
            return shellSettings.Name + ":" + authenticationType;
        }

        /// <summary>
        /// Adds or replaces an authentication scheme with the specified options.
        /// </summary>
        public static void Register<TOptions, THandler>(IApplicationBuilder app, string scheme, TOptions options, params IPostConfigureOptions<TOptions>[] postConfigureOptions)
            where TOptions : AuthenticationSchemeOptions, new()
            where THandler : class, IAuthenticationHandler
        {
            var services = app.ApplicationServices;
            var schemeProvider = services.GetRequiredService<IAuthenticationSchemeProvider>();
            var optionsCache = services.GetRequiredService<IOptionsMonitorCache<TOptions>>();

            foreach (var postConfigure in postConfigureOptions)
            {
                postConfigure.PostConfigure(scheme, options);
            }
            options.Validate(scheme);

            optionsCache.TryRemove(scheme);
            optionsCache.TryAdd(scheme, options);

            schemeProvider.RemoveScheme(scheme);
            schemeProvider.TryAddScheme(new AuthenticationScheme(scheme, scheme, typeof(THandler)));

            GetTenantSchemes(app).Add(scheme);
        }

        /// <summary>
        /// Handles the remote authentication responses of the tenant schemes.
        /// </summary>
        public static void UseRemoteAuthentication(IApplicationBuilder app)
        {
            var schemes = GetTenantSchemes(app);

            app.Use(async (context, next) =>
            {
                var handlers = context.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();
                foreach (var scheme in schemes.ToArray())
                {
                    if (await handlers.GetHandlerAsync(context, scheme) is IAuthenticationRequestHandler handler && await handler.HandleRequestAsync())
                    {
                        return;
                    }
                }

                await next();
            });
        }

        private static ISet<string> GetTenantSchemes(IApplicationBuilder app)
        {
            if (!app.Properties.TryGetValue(SchemesKey, out var schemes))
            {
                app.Properties[SchemesKey] = schemes = new HashSet<string>(StringComparer.Ordinal);
            }

            return (ISet<string>)schemes;
        }

        /// <summary>
        /// Returns the external identity of the request, if any.
        /// </summary>
        public static System.Security.Claims.ClaimsPrincipal GetExternalUser(HttpContext httpContext, ShellSettings shellSettings)
        {
            var result = httpContext.AuthenticateAsync(GetScheme(shellSettings, ExternalCookie)).GetAwaiter().GetResult();
            return result.Succeeded ? result.Principal : null;
        }
    }
}
