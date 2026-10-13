using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Orchard.Environment;
using Orchard.Environment.Configuration;
using Orchard.Environment.Extensions;
using Orchard.Environment.Extensions.Models;

namespace Orchard.Mvc.Routes
{
    public class RoutePublisher : IRoutePublisher
    {
        private readonly OrchardRouteCollection _routeCollection;
        private readonly ShellSettings _shellSettings;
        private readonly IWorkContextAccessor _workContextAccessor;
        private readonly IRunningShellTable _runningShellTable;
        private readonly IExtensionManager _extensionManager;

        public RoutePublisher(
            OrchardRouteCollection routeCollection,
            ShellSettings shellSettings,
            IWorkContextAccessor workContextAccessor,
            IRunningShellTable runningShellTable,
            IExtensionManager extensionManager)
        {
            _routeCollection = routeCollection;
            _shellSettings = shellSettings;
            _workContextAccessor = workContextAccessor;
            _runningShellTable = runningShellTable;
            _extensionManager = extensionManager;
        }

        public void Publish(IEnumerable<RouteDescriptor> routes, RequestDelegate pipeline = null)
        {
            var routesArray = routes
                .OrderByDescending(r => r.Priority)
                .ToArray();

            // this is not called often, but is intended to surface problems before
            // the actual collection is modified
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var routeDescriptor in routesArray)
            {
                // the names of the routes must be unique (formerly enforced by the preloading RouteCollection)
                if (!string.IsNullOrEmpty(routeDescriptor.Name) && !names.Add(routeDescriptor.Name))
                {
                    throw new ArgumentException(string.Format("A route named '{0}' is already in the route collection. Route names must be unique.", routeDescriptor.Name));
                }

                // extract the WebApi route implementation
                var httpRouteDescriptor = routeDescriptor as HttpRouteDescriptor;
                if (httpRouteDescriptor != null)
                {
                    routeDescriptor.Route = new Route(
                        httpRouteDescriptor.RouteTemplate,
                        new RouteValueDictionary(httpRouteDescriptor.Defaults),
                        new RouteValueDictionary(httpRouteDescriptor.Constraints),
                        new MvcRouteHandler()) { Name = httpRouteDescriptor.Name };
                }

                if (routeDescriptor.Route == null)
                {
                    throw new ArgumentException(string.Format("The route descriptor '{0}' doesn't define a route.", routeDescriptor.Name));
                }
            }

            using (_routeCollection.GetWriteLock())
            {
                // existing routes are removed while the collection is briefly inaccessable
                foreach (var hubRoute in _routeCollection.OfType<HubRoute>())
                {
                    hubRoute.ReleaseShell(_shellSettings);
                }

                // new routes are added
                foreach (var routeDescriptor in routesArray)
                {
                    // Loading session state information. 
                    var defaultSessionState = SessionStateBehavior.Default;

                    ExtensionDescriptor extensionDescriptor = null;
                    if (routeDescriptor.Route is Route)
                    {
                        object extensionId;
                        var route = routeDescriptor.Route as Route;
                        if ((route.DataTokens != null && route.DataTokens.TryGetValue("area", out extensionId)) ||
                           (route.Defaults != null && route.Defaults.TryGetValue("area", out extensionId)))
                        {
                            extensionDescriptor = _extensionManager.GetExtension(extensionId.ToString());
                        }
                    }
                    else if (routeDescriptor.Route is IRouteWithArea)
                    {
                        var route = routeDescriptor.Route as IRouteWithArea;
                        extensionDescriptor = _extensionManager.GetExtension(route.Area);
                    }

                    if (extensionDescriptor != null)
                    {
                        // if session state is not define explicitly, use the one define for the extension
                        if (routeDescriptor.SessionState == SessionStateBehavior.Default)
                        {
                            Enum.TryParse(extensionDescriptor.SessionState, true /*ignoreCase*/, out defaultSessionState);
                        }
                    }

                    // Route-level setting overrides module-level setting (from manifest).
                    var sessionStateBehavior = routeDescriptor.SessionState == SessionStateBehavior.Default ? defaultSessionState : routeDescriptor.SessionState;

                    var shellRoute = new ShellRoute(routeDescriptor.Route, _shellSettings, _workContextAccessor, _runningShellTable, pipeline)
                    {
                        IsHttpRoute = routeDescriptor is HttpRouteDescriptor,
                        SessionState = sessionStateBehavior
                    };

                    var area = extensionDescriptor == null ? "" : extensionDescriptor.Id;

                    var matchedHubRoute = _routeCollection.FirstOrDefault(x =>
                    {
                        var hubRoute = x as HubRoute;
                        if (hubRoute == null)
                        {
                            return false;
                        }

                        return routeDescriptor.Priority == hubRoute.Priority && hubRoute.Area.Equals(area, StringComparison.OrdinalIgnoreCase) && hubRoute.Name == routeDescriptor.Name;
                    }) as HubRoute;

                    if (matchedHubRoute == null)
                    {
                        matchedHubRoute = new HubRoute(routeDescriptor.Name, area, routeDescriptor.Priority, _runningShellTable);

                        int index;
                        for (index = 0; index < _routeCollection.Count; index++)
                        {
                            var hubRoute = _routeCollection[index] as HubRoute;
                            if (hubRoute == null)
                            {
                                continue;
                            }
                            if (hubRoute.Priority < matchedHubRoute.Priority)
                            {
                                break;
                            }
                        }

                        _routeCollection.Insert(index, matchedHubRoute);
                    }

                    matchedHubRoute.Add(shellRoute, _shellSettings);
                }
            }
        }
    }
}
