using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;
using Orchard.Mvc.Routes;
using Route = Orchard.Mvc.Routes.Route;

namespace Orchard.MultiTenancy
{
    public class Routes : IRouteProvider
    {
        public void GetRoutes(ICollection<RouteDescriptor> routes)
        {
            var routeDescriptor = new RouteDescriptor
            {
                Route = new Route(
                                                         "Admin/MultiTenancy/Edit/{name}",
                                                         new RouteValueDictionary {
                                                                                      {"area", "Orchard.MultiTenancy"},
                                                                                      {"controller", "Admin"},
                                                                                      {"action", "Edit"}
                                                                                  },
                                                         new RouteValueDictionary {
                                                                                      {"name", ".+"}
                                                                                  },
                                                         new RouteValueDictionary {
                                                                                      {"area", "Orchard.MultiTenancy"}
                                                                                  },
                                                         new MvcRouteHandler())
            };

            routes.Add(routeDescriptor);
        }
    }
}