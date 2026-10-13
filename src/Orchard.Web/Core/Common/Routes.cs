using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;
using Orchard.Mvc.Routes;
using Route = Orchard.Mvc.Routes.Route;

namespace Orchard.Core.Common
{
    public class Routes : IRouteProvider
    {
        public void GetRoutes(ICollection<RouteDescriptor> routes)
        {
            var routeDescriptor = new RouteDescriptor
            {
                Priority = -9999,
                Route = new Route(
                        "{*path}",
                        new RouteValueDictionary {
                            {"area", "Common"},
                            {"controller", "Error"},
                            {"action", "NotFound"}
                        },
                        new RouteValueDictionary
                        {
                        },
                        new RouteValueDictionary {
                            {"area", "Common"}
                        },
                        new MvcRouteHandler())
            };

            routes.Add(routeDescriptor);
        }
    }
}