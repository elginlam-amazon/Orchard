using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Orchard.Environment;
using Orchard.Environment.Configuration;
using Orchard.Mvc.Extensions;

namespace Orchard.Mvc.Routes
{

    /// <summary>
    /// Wraps a route of a tenant: it only matches requests for this tenant, and the matched handler
    /// is executed inside a work context scope and through the tenant middleware pipeline.
    /// </summary>
    /// <remarks>
    /// The tenant url prefix (if any) is moved into the request path base by the Orchard host middleware,
    /// so that both route matching and url generation are relative to the tenant.
    /// </remarks>
    public class ShellRoute : IRouter, IRouteWithArea
    {
        public const string OrchardHandlerKey = "orchard.Handler";

        private readonly IRouter _route;
        private readonly ShellSettings _shellSettings;
        private readonly IWorkContextAccessor _workContextAccessor;
        private readonly IRunningShellTable _runningShellTable;
        private readonly RequestDelegate _pipeline;

        public ShellRoute(IRouter route, ShellSettings shellSettings, IWorkContextAccessor workContextAccessor, IRunningShellTable runningShellTable, RequestDelegate pipeline)
        {
            _route = route;
            _shellSettings = shellSettings;
            _runningShellTable = runningShellTable;
            _pipeline = pipeline;
            _workContextAccessor = workContextAccessor;

            Area = route.GetAreaName();
        }

        public SessionStateBehavior SessionState { get; set; }

        public string ShellSettingsName => _shellSettings.Name;
        public string Area { get; private set; }
        public bool IsHttpRoute { get; set; }

        public async Task RouteAsync(RouteContext context)
        {
            // locate appropriate shell settings for request
            var settings = _runningShellTable.Match(context.HttpContext);

            // only proceed if there was a match, and it was for this client
            if (settings == null || settings.Name != _shellSettings.Name)
                return;

            // the matched route is part of the route data routers (e.g. used by the output cache)
            var snapshot = context.RouteData.PushState(_route, null, null);
            await _route.RouteAsync(context);

            var handler = context.Handler;
            if (handler == null)
            {
                snapshot.Restore();
                return;
            }

            context.RouteData.DataTokens["IWorkContextAccessor"] = _workContextAccessor;

            // otherwise wrap handler so that it runs in the tenant work context and middleware pipeline
            var workContextAccessor = _workContextAccessor;
            var pipeline = _pipeline;
            context.Handler = async httpContext =>
            {
                using (workContextAccessor.CreateWorkContextScope(httpContext))
                {
                    if (pipeline == null)
                    {
                        await handler(httpContext);
                        return;
                    }

                    httpContext.Items[OrchardHandlerKey] = handler;
                    await pipeline(httpContext);
                }
            };
        }

        public VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            // only if MVC or WebApi match for this route
            object httpRouteValue;
            if (context.Values.TryGetValue("httproute", out httpRouteValue))
            {
                if (httpRouteValue is bool && (bool)httpRouteValue != IsHttpRoute)
                {
                    return null;
                }
            }

            // locate appropriate shell settings for request
            var settings = _runningShellTable.Match(context.HttpContext);

            // only proceed if there was a match, and it was for this client
            if (settings == null || settings.Name != _shellSettings.Name)
                return null;

            return _route.GetVirtualPath(context);
        }
    }
}
