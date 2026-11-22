using System;
using Microsoft.AspNetCore.Mvc;
using Orchard.Utility.Extensions;

namespace Orchard.Mvc.Extensions
{
    public static class UrlHelperExtensions
    {
        public static string AbsoluteAction(this IUrlHelper urlHelper, Func<string> urlAction)
        {
            return urlHelper.MakeAbsolute(urlAction());
        }

        public static string AbsoluteAction(this IUrlHelper urlHelper, string actionName)
        {
            return urlHelper.MakeAbsolute(urlHelper.Action(actionName));
        }

        public static string AbsoluteAction(this IUrlHelper urlHelper, string actionName, object routeValues)
        {
            return urlHelper.MakeAbsolute(urlHelper.Action(actionName, routeValues));
        }

        public static string AbsoluteAction(this IUrlHelper urlHelper, string actionName, string controller)
        {
            return urlHelper.MakeAbsolute(urlHelper.Action(actionName, controller));
        }

        public static string AbsoluteAction(this IUrlHelper urlHelper, string actionName, string controller, object routeValues)
        {
            return urlHelper.MakeAbsolute(urlHelper.Action(actionName, controller, routeValues));
        }

        public static string MakeAbsolute(this IUrlHelper urlHelper, string url, string baseUrl = null)
        {
            if (url != null && url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            if (string.IsNullOrEmpty(baseUrl))
            {
                var aspReq = urlHelper.ActionContext.HttpContext.Request;
                var appPath = aspReq.PathBase.Value ?? "";
                baseUrl = $"{aspReq.Scheme}://{aspReq.Host.Value}{(appPath == "/" ? "" : appPath)}";
            }

            if (string.IsNullOrEmpty(url))
            {
                return baseUrl;
            }

            // remove any application path from the base url
            var applicationPath = urlHelper.ActionContext.HttpContext.Request.PathBase.Value ?? string.Empty;

            // ~/foo/bar => /foo/bar
            if (url.StartsWith("~/", StringComparison.OrdinalIgnoreCase))
            {
                url = url.Substring(1);
            }

            // orchardlocal/foo/bar => /orchardlocal/foo/bar
            if (!url.StartsWith("/"))
            {
                url = "/" + url;
            }

            // /orchardlocal/foo/bar => foo/bar
            if (url.StartsWith(applicationPath, StringComparison.OrdinalIgnoreCase))
            {
                url = url.Substring(applicationPath.Length);
            }

            baseUrl = baseUrl.TrimEnd('/');
            url = url.TrimStart('/');

            return baseUrl + "/" + url;
        }
    }
}
