using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;
using Orchard.Mvc.Routes;
using Route = Orchard.Mvc.Routes.Route;

namespace Orchard.Core.Reports
{
    public class Routes : IRouteProvider
    {
        public void GetRoutes(ICollection<RouteDescriptor> routes)
        {
            var routeDescriptor = new RouteDescriptor
            {
                Priority = -5,
                Route = new Route(
                    "Admin/Reports",
                    new RouteValueDictionary {
                        {"area", "Reports"},
                        {"controller", "Admin"},
                        {"action", "Index"}
                    },
                    new RouteValueDictionary(),
                    new RouteValueDictionary {
                        {"area", "Reports"}
                    },
                    new MvcRouteHandler())
            };

            routes.Add(routeDescriptor);
        }
    }
}