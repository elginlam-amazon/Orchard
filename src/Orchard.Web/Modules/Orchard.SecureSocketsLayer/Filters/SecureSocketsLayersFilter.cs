using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Orchard.Localization;
using Orchard.Mvc.Extensions;
using Orchard.Mvc.Filters;
using Orchard.SecureSocketsLayer.Services;

namespace Orchard.SecureSocketsLayer.Filters
{
    public class SecureSocketsLayersFilter : FilterProvider, IActionFilter
    {
        private readonly ISecureSocketsLayerService _sslService;

        public SecureSocketsLayersFilter(ISecureSocketsLayerService sslService)
        {
            _sslService = sslService;
        }
        public Localizer T { get; set; }

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
        }

        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var settings = _sslService.GetSettings();

            if (!settings.Enabled)
            {
                return;
            }

            var user = filterContext.HttpContext.User;
            var secure =
                (user != null && user.Identity.IsAuthenticated) ||
                _sslService.ShouldBeSecure(filterContext);
            var usePermanentRedirect = settings.UsePermanentRedirect;
            var request = filterContext.HttpContext.Request;

            // redirect to a secured connection ?
            if (secure && !request.IsHttps)
            {
                var secureActionUrl = AppendQueryString(
                    request.QueryString,
                    _sslService.SecureActionUrl(
                        filterContext.ActionDescriptor.GetActionName(),
                        filterContext.ActionDescriptor.GetControllerName(),
                        filterContext.RouteData.Values));

                filterContext.Result = new RedirectResult(secureActionUrl, usePermanentRedirect);
                return;
            }

            // non auth page on a secure canal
            // nb: needed as the ReturnUrl for LogOn doesn't force the scheme to http, and reuses the current one
            // Also don't force http on ajax requests.
            if (!secure && request.IsHttps && !request.IsAjaxRequest())
            {
                var insecureActionUrl = AppendQueryString(
                    request.QueryString,
                    _sslService.InsecureActionUrl(
                        filterContext.ActionDescriptor.GetActionName(),
                        filterContext.ActionDescriptor.GetControllerName(),
                        filterContext.RouteData.Values));

                filterContext.Result = new RedirectResult(insecureActionUrl, usePermanentRedirect);
            }
        }

        private static string AppendQueryString(QueryString queryString, string url)
        {
            if (queryString.HasValue && queryString.Value.Length > 1)
            {
                url += queryString.Value;
            }
            return url;
        }
    }
}
