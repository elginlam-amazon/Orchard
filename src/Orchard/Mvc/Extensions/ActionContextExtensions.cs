using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;

namespace Orchard.Mvc.Extensions
{
    public static class ActionContextExtensions
    {
        /// <summary>
        /// Returns an action context for the current request route data (the former HttpRequest.RequestContext).
        /// </summary>
        public static ActionContext GetActionContext(this HttpContext httpContext)
        {
            return new ActionContext(httpContext, httpContext.GetRouteData(), new ActionDescriptor());
        }
    }
}
