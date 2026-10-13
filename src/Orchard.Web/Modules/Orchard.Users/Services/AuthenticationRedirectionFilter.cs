using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Orchard.Mvc.Filters;
using Orchard.Utility.Extensions;

namespace Orchard.Users.Services
{

    /// <summary>
    /// This class is responsible for redirecting the user to the authentication page 
    /// of the current tenant.
    /// </summary>
    /// <remarks>
    /// Formerly an MVC authentication filter (authentication challenge), now a result filter which also runs
    /// when an authorization filter short-circuits the action with an unauthorized result.
    /// </remarks>
    public class AuthenticationRedirectionFilter : FilterProvider, IAlwaysRunResultFilter
    {
        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            if (filterContext.Result is UnauthorizedResult)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary
                {
                    { "controller", "Account" },
                    { "action", "AccessDenied" },
                    { "area", "Orchard.Users"},
                    { "ReturnUrl", filterContext.HttpContext.Request.GetRawUrl() }
                });
            }
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
        }
    }
}
