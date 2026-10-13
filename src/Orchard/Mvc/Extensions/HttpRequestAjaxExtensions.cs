using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Orchard.Mvc.Extensions
{
    public static class HttpRequestAjaxExtensions
    {
        /// <summary>
        /// Whether the request has been made with XMLHttpRequest (the former MVC Request.IsAjaxRequest()).
        /// </summary>
        public static bool IsAjaxRequest(this HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.Ordinal)
                || string.Equals(request.Query["X-Requested-With"], "XMLHttpRequest", StringComparison.Ordinal);
        }

        /// <summary>
        /// Whether the application runs in the Development environment (the former HttpContext.IsDebuggingEnabled(), compilation debug="true").
        /// </summary>
        public static bool IsDebuggingEnabled(this HttpContext httpContext)
        {
            var environment = httpContext?.RequestServices?.GetService(typeof(IWebHostEnvironment)) as IWebHostEnvironment;
            return environment != null && environment.EnvironmentName == "Development";
        }

        /// <summary>
        /// Prevents the browser and the proxies from caching the response
        /// (the former Response.Cache SetExpires/SetRevalidation/SetCacheability(NoCache)/SetNoStore calls).
        /// </summary>
        public static void DisableClientCaching(this HttpResponse response)
        {
            if (response.HasStarted)
            {
                return;
            }

            response.Headers["Expires"] = DateTime.UtcNow.AddDays(-1).ToString("R");
            response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate, proxy-revalidate";
            response.Headers["Pragma"] = "no-cache";
        }

        /// <summary>
        /// The referrer url of the request, if any (the former Request.GetUrlReferrer()).
        /// </summary>
        public static Uri GetUrlReferrer(this HttpRequest request)
        {
            var referrer = request.Headers["Referer"].ToString();
            return Uri.TryCreate(referrer, UriKind.Absolute, out var uri) ? uri : null;
        }
    }
}
