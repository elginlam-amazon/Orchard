using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;
using Orchard.Mvc.Routes;
using Route = Orchard.Mvc.Routes.Route;

namespace Orchard.Tags
{
    public class Routes : IRouteProvider
    {
        public void GetRoutes(ICollection<RouteDescriptor> routes)
        {
            var routeDescriptor = new RouteDescriptor
            {
                Priority = 5,
                Route = new Route(
                        "Tags/{tagName}",
                        new RouteValueDictionary {
                            {"area", "Orchard.Tags"},
                            {"controller", "Home"},
                            {"action", "Search"}
                        },
                        new RouteValueDictionary(),
                        new RouteValueDictionary {
                            {"area", "Orchard.Tags"}
                        },
                        new MvcRouteHandler())
            };

            routes.Add(routeDescriptor);
        }
    }
}