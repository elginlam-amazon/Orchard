using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;

namespace Orchard
{
    public static class WorkContextExtensions
    {
        public static WorkContext GetContext(this IWorkContextAccessor workContextAccessor, ActionContext controllerContext)
        {
            return workContextAccessor.GetContext(controllerContext.HttpContext);
        }

        public static WorkContext GetLogicalContext(this IWorkContextAccessor workContextAccessor)
        {
            var wca = workContextAccessor as ILogicalWorkContextAccessor;
            return wca != null ? wca.GetLogicalContext() : null;
        }

        public static WorkContext GetWorkContext(this ActionContext requestContext)
        {
            if (requestContext == null)
                return null;

            var routeData = requestContext.RouteData;
            if (routeData == null || routeData.DataTokens == null)
                return null;

            object workContextValue;
            if (!routeData.DataTokens.TryGetValue("IWorkContextAccessor", out workContextValue))
            {
                workContextValue = FindWorkContextInParent(routeData);
            }

            if (!(workContextValue is IWorkContextAccessor))
                return null;

            var workContextAccessor = (IWorkContextAccessor)workContextValue;
            return workContextAccessor.GetContext(requestContext.HttpContext);
        }

        /// <summary>
        /// Returns the work context of a routed request.
        /// </summary>
        public static WorkContext GetWorkContext(this HttpContext httpContext)
        {
            if (httpContext == null)
                return null;

            var routeData = httpContext.GetRouteData();
            if (routeData == null || routeData.DataTokens == null)
                return null;

            object workContextValue;
            if (!routeData.DataTokens.TryGetValue("IWorkContextAccessor", out workContextValue))
            {
                workContextValue = FindWorkContextInParent(routeData);
            }

            var workContextAccessor = workContextValue as IWorkContextAccessor;
            return workContextAccessor?.GetContext(httpContext);
        }

        private static object FindWorkContextInParent(RouteData routeData)
        {
            object parentViewContextValue;
            if (!routeData.DataTokens.TryGetValue("ParentActionViewContext", out parentViewContextValue)
                || !(parentViewContextValue is ViewContext))
            {
                return null;
            }

            var parentRouteData = ((ViewContext)parentViewContextValue).RouteData;
            if (parentRouteData == null || parentRouteData.DataTokens == null)
                return null;

            object workContextValue;
            if (!parentRouteData.DataTokens.TryGetValue("IWorkContextAccessor", out workContextValue))
            {
                workContextValue = FindWorkContextInParent(parentRouteData);
            }

            return workContextValue;
        }

        public static IWorkContextScope CreateWorkContextScope(this ILifetimeScope lifetimeScope, HttpContext httpContext)
        {
            return lifetimeScope.Resolve<IWorkContextAccessor>().CreateWorkContextScope(httpContext);
        }

        public static IWorkContextScope CreateWorkContextScope(this ILifetimeScope lifetimeScope)
        {
            return lifetimeScope.Resolve<IWorkContextAccessor>().CreateWorkContextScope();
        }
    }
}
