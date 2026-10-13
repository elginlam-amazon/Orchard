using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orchard.Environment.Extensions;
using Orchard.Mvc.Extensions;
using Orchard.Mvc.Filters;
using Orchard.Security;

namespace Orchard.Mvc.AntiForgery
{
    public class AntiForgeryAuthorizationFilter : FilterProvider, IAsyncAuthorizationFilter
    {
        private const string TokenFieldName = "__RequestVerificationToken";

        private readonly IAuthenticationService _authenticationService;
        private readonly IExtensionManager _extensionManager;

        public AntiForgeryAuthorizationFilter(IAuthenticationService authenticationService, IExtensionManager extensionManager)
        {
            _authenticationService = authenticationService;
            _extensionManager = extensionManager;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext filterContext)
        {
            // If the request is not a POST or is anonymous, and the request doesn't have validation forced, return.
            if ((!HttpMethods.IsPost(filterContext.HttpContext.Request.Method) ||
                 _authenticationService.GetAuthenticatedUser() == null) && !ShouldValidateGet(filterContext))
            {
                return;
            }

            if (!IsAntiForgeryProtectionEnabled(filterContext))
            {
                return;
            }

            var antiforgery = filterContext.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
            try
            {
                await antiforgery.ValidateRequestAsync(filterContext.HttpContext);
            }
            catch (AntiforgeryValidationException)
            {
                filterContext.Result = new AntiforgeryValidationFailedResult();
            }
        }

        private bool IsAntiForgeryProtectionEnabled(AuthorizationFilterContext context)
        {
            // POST is opt-out
            var attributes =
                (ValidateAntiForgeryTokenOrchardAttribute[])
                context.ActionDescriptor.GetCustomAttributes(typeof(ValidateAntiForgeryTokenOrchardAttribute), false);

            if (attributes.Length > 0 && !attributes[0].Enabled) return false;

            var currentModule = GetArea(context.RouteData);
            return !string.IsNullOrEmpty(currentModule)
                   && _extensionManager.AvailableExtensions()
                       .First(descriptor => string.Equals(descriptor.Id, currentModule, StringComparison.OrdinalIgnoreCase))
                       .AntiForgery.Equals("enabled", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetArea(RouteData routeData)
        {
            return routeData.Values.ContainsKey("area") ? routeData.Values["area"] as string : routeData.DataTokens["area"] as string ?? "";
        }

        private static bool ShouldValidateGet(AuthorizationFilterContext context)
        {
            var attributes =
                (ValidateAntiForgeryTokenOrchardAttribute[])
                context.ActionDescriptor.GetCustomAttributes(typeof(ValidateAntiForgeryTokenOrchardAttribute), false);

            if (attributes.Length > 0 && attributes[0].Enabled)
            {
                var request = context.HttpContext.Request;

                // If the token is in the querystring, pass it through the antiforgery request header so it can be validated.
                string token = request.Query[TokenFieldName];
                if (!string.IsNullOrEmpty(token))
                {
                    var options = context.HttpContext.RequestServices.GetService<IOptions<AntiforgeryOptions>>();
                    var headerName = options?.Value.HeaderName ?? "RequestVerificationToken";
                    request.Headers[headerName] = token;
                }

                return true;
            }

            return false;
        }
    }
}
