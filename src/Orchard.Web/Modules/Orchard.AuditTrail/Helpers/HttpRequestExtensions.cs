using System;
using Microsoft.AspNetCore.Http;
using Orchard.Mvc.Extensions;
using Orchard.Utility.Extensions;

namespace Orchard.AuditTrail.Helpers
{
    public static class HttpRequestExtensions
    {
        public static string GetReturnUrl(this HttpRequest request, string returnUrl = null, Func<string> defaultReturnUrl = null)
        {
            return request.IsLocalUrl(returnUrl)
                ? returnUrl
                : request.GetUrlReferrer() != null
                    ? request.GetUrlReferrer().ToString()
                    : defaultReturnUrl != null
                        ? defaultReturnUrl()
                        : "~/Admin";
        }
    }
}