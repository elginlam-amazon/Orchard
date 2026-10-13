using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;
using Orchard.Mvc.Routes;
using Route = Orchard.Mvc.Routes.Route;

namespace Orchard.Setup
{
    public class Routes : IRouteProvider
    {
        public void GetRoutes(ICollection<RouteDescriptor> routes)
        {
            var routeDescriptor = new RouteDescriptor
            {
                Route = new Route(
                        "{controller}/{action}",
                        new RouteValueDictionary {
                            {"area", "Orchard.Setup"},
                            {"controller", "Setup"},
                            {"action", "Index"}
                        },
                        new RouteValueDictionary {
                            {"area", "Orchard.Setup"},
                            {"controller", "Setup"},
                        },
                        new RouteValueDictionary {
                            {"area", "Orchard.Setup"}
                        },
                        new MvcRouteHandler())
            };

            routes.Add(routeDescriptor);
        }
    }
}