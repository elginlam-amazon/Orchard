using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Orchard.OutputCache.Services
{
    /// <summary>
    /// Represent the logic deciding if the result of an action can be cached
    /// </summary>
    public class DefaultCacheControlStrategy : ICacheControlStrategy
    {
        private static readonly string[] AuthorizedContentTypes = { "text/html", "text/xml", "text/json", "text/plain" };

        public bool IsCacheable(ActionResult result, HttpResponse response)
        {
            // only cache view results
            if ((result is ViewResult || result is PartialViewResult))
            {
                return true;
            }

            if (result is FileResult)
            {
                return false;
            }

            if (AuthorizedContentTypes.Contains(response.ContentType))
            {
                return true;
            }

            return false;
        }
    }
}