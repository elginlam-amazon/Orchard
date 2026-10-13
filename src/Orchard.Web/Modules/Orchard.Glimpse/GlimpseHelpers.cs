using System;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Orchard.Utility.Extensions;

namespace Orchard.Glimpse
{
    public static class GlimpseHelpers
    {
        public static string AppendReturnUrl(string path, IUrlHelper urlHelper)
        {
            var requestUrl = urlHelper.ActionContext.HttpContext.Request.GetUrl();

            if (requestUrl == null)
            {
                return path;
            }

            var uriBuilder = new UriBuilder(requestUrl.AbsoluteUri) { Path = path };
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);

            query["returnUrl"] = requestUrl.AbsolutePath;
            uriBuilder.Query = query.ToString();
            path = uriBuilder.ToString();

            return path;
        }
    }
}