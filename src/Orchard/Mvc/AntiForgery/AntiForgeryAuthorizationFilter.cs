using System;
using System.Linq;
using System.Web;
using System.Web.Routing;
using Orchard.Environment.Extensions;
using Orchard.Mvc.Filters;
using Orchard.Security;

namespace Orchard.Mvc.AntiForgery
{
    public class AntiForgeryAuthorizationFilter : FilterProvider, System.Web.Mvc.IAuthorizationFilter
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IExtensionManager _extensionManager;

        public AntiForgeryAuthorizationFilter(IAuthenticationService authenticationService, IExtensionManager extensionManager)
        {
            _authenticationService = authenticationService;
            _extensionManager = extensionManager;
        }

        public void OnAuthorization(System.Web.Mvc.AuthorizationContext filterContext)
        {
            // If the request is not a POST or is anonymous, and the request doesn't have validation forced, return.
            if ((filterContext.HttpContext?.Request?.HttpMethod != "POST" ||
                 _authenticationService.GetAuthenticatedUser() == null) && !ShouldValidateGet(filterContext))
            {
                return;
            }

            if (!IsAntiForgeryProtectionEnabled(filterContext))
            {
                return;
            }

            // Stub: in .NET 10, anti-forgery token validation should be done via ASP.NET Core mechanisms
        }

        private bool IsAntiForgeryProtectionEnabled(System.Web.Mvc.AuthorizationContext context)
        {
            // POST is opt-out
            var attributes = GetValidateAttributes(context);

            if (attributes.Length > 0 && !attributes[0].Enabled) return false;

            var currentModule = GetArea(context.RouteData);
            return !string.IsNullOrEmpty(currentModule)
                   && _extensionManager.AvailableExtensions()
                       .First(descriptor => string.Equals(descriptor.Id, currentModule, StringComparison.OrdinalIgnoreCase))
                       .AntiForgery.Equals("enabled", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetArea(RouteData routeData)
        {
            if (routeData == null) return "";
            return routeData.Values.ContainsKey("area") ? routeData.Values["area"] as string : routeData.DataTokens["area"] as string ?? "";
        }

        private static bool ShouldValidateGet(System.Web.Mvc.AuthorizationContext context)
        {
            var attributes = GetValidateAttributes(context);
            return attributes.Length > 0 && attributes[0].Enabled;
        }

        private static ValidateAntiForgeryTokenOrchardAttribute[] GetValidateAttributes(System.Web.Mvc.AuthorizationContext context)
        {
            if (context?.ActionDescriptor == null)
                return Array.Empty<ValidateAntiForgeryTokenOrchardAttribute>();

            return (ValidateAntiForgeryTokenOrchardAttribute[])
                context.ActionDescriptor.GetCustomAttributes(typeof(ValidateAntiForgeryTokenOrchardAttribute), false);
        }
    }
}
