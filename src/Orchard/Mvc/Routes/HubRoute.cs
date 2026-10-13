using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;
using Orchard.Environment;
using Orchard.Environment.Configuration;

namespace Orchard.Mvc.Routes
{

    public class HubRoute : IRouter, INamedRouter, IRouteWithArea, IComparable<HubRoute>
    {
        private readonly IRunningShellTable _runningShellTable;

        private readonly ConcurrentDictionary<string, IList<IRouter>> _routesByShell = new ConcurrentDictionary<string, IList<IRouter>>();

        public HubRoute(string name, string area, int priority, IRunningShellTable runningShellTable)
        {
            Priority = priority;
            Area = area;
            Name = name;
            _runningShellTable = runningShellTable;
        }

        public string Name { get; private set; }
        public string Area { get; private set; }
        public int Priority { get; private set; }

        /// <summary>
        /// Removes the routes associated with a shell
        /// </summary>
        public void ReleaseShell(ShellSettings shellSettings)
        {
            IList<IRouter> routes;
            _routesByShell.TryRemove(shellSettings.Name, out routes);
        }

        public void Add(IRouter route, ShellSettings shellSettings)
        {
            var routes = _routesByShell.GetOrAdd(shellSettings.Name, key => new List<IRouter>());
            routes.Add(route);
        }

        public async Task RouteAsync(RouteContext context)
        {
            var settings = _runningShellTable.Match(context.HttpContext);

            if (settings == null)
                return;

            IList<IRouter> routes;
            if (!_routesByShell.TryGetValue(settings.Name, out routes))
            {
                return;
            }

            foreach (var route in routes)
            {
                var snapshot = context.RouteData.PushState(route, null, null);
                await route.RouteAsync(context);
                if (context.Handler != null)
                {
                    return;
                }
                snapshot.Restore();
            }
        }

        public VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            var settings = _runningShellTable.Match(context.HttpContext);

            if (settings == null)
                return null;

            IList<IRouter> routes;
            if (!_routesByShell.TryGetValue(settings.Name, out routes))
            {
                return null;
            }

            // The name of the hub route has already been matched, inner routes are not named.
            var innerContext = string.IsNullOrEmpty(context.RouteName)
                ? context
                : new VirtualPathContext(context.HttpContext, context.AmbientValues, context.Values);

            foreach (var route in routes)
            {
                var virtualPathData = route.GetVirtualPath(innerContext);
                if (virtualPathData != null)
                {
                    return virtualPathData;
                }
            }

            return null;
        }

        public int CompareTo(HubRoute other)
        {
            if (other == null)
            {
                return -1;
            }

            if (other == this)
            {
                return 0;
            }

            if ((string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(other.Name)) || Name == other.Name)
            {
                return 0;
            }

            if (!string.Equals(other.Area, Area, StringComparison.OrdinalIgnoreCase))
            {
                return StringComparer.OrdinalIgnoreCase.Compare(other.Area, Area);
            }

            if (other.Priority == Priority)
            {
                return StringComparer.OrdinalIgnoreCase.Compare(other.Area, Area);
            }

            return Priority.CompareTo(other.Priority);
        }
    }
}
