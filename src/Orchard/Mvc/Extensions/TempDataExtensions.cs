using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace Orchard.Mvc.Extensions
{
    public static class TempDataExtensions
    {
        /// <summary>
        /// Returns the temp data of the request (the former filterContext.GetTempData()).
        /// </summary>
        public static ITempDataDictionary GetTempData(this ActionContext context)
        {
            return GetTempData(context.HttpContext);
        }

        public static ITempDataDictionary GetTempData(this HttpContext httpContext)
        {
            return httpContext.RequestServices.GetRequiredService<ITempDataDictionaryFactory>().GetTempData(httpContext);
        }
    }
}
