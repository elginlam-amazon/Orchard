using System.Net;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Orchard.Mvc.Html
{
    public static class AntiForgeryExtensions
    {
        /// <summary>
        /// Returns the hidden input holding the antiforgery token of the request (the former HtmlHelper.AntiForgeryToken() used outside of a view).
        /// </summary>
        public static HtmlString AntiForgeryTokenHtml(this HttpContext httpContext)
        {
            var antiforgery = httpContext.RequestServices.GetRequiredService<IAntiforgery>();
            var tokens = antiforgery.GetAndStoreTokens(httpContext);
            return new HtmlString(string.Format("<input name=\"{0}\" type=\"hidden\" value=\"{1}\" />",
                WebUtility.HtmlEncode(tokens.FormFieldName), WebUtility.HtmlEncode(tokens.RequestToken)));
        }
    }
}
