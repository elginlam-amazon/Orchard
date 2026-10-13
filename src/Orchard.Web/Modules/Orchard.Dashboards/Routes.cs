using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;
using Orchard.Mvc.Routes;
using Route = Orchard.Mvc.Routes.Route;

namespace Orchard.Dashboards
{
    public class Routes : IRouteProvider
    {
        public void GetRoutes(ICollection<RouteDescriptor> routes)
        {
            var routeDescriptors = new[] {
                new RouteDescriptor {
                    Priority = -4,
                    Route = new Route(
                        "Admin",
                        new RouteValueDictionary {
                            {"area", "Orchard.Dashboards"},
                            {"controller", "Dashboard"},
                            {"action", "Display"}
                        },
                        new RouteValueDictionary(),
                        new RouteValueDictionary {
                            {"area", "Orchard.Dashboards"}
                        },
                        new MvcRouteHandler())
                },

                new RouteDescriptor {
                    Route = new Route(
                    "Admin/Dashboards/Settings",
                    new RouteValueDictionary {
                        {"area", "Orchard.Dashboards"},
                        {"controller", "Settings"},
                        {"action", "Index"}
                    },
                    new RouteValueDictionary(),
                    new RouteValueDictionary {
                        {"area", "Orchard.Dashboards"}
                    },
                    new MvcRouteHandler())
                },

                new RouteDescriptor {
                    Route = new Route(
                    "Admin/Dashboards/List",
                    new RouteValueDictionary {
                        {"area", "Orchard.Dashboards"},
                        {"controller", "Dashboard"},
                        {"action", "List"}
                    },
                    new RouteValueDictionary(),
                    new RouteValueDictionary {
                        {"area", "Orchard.Dashboards"}
                    },
                    new MvcRouteHandler())
                }
            };

            foreach (var routeDescriptor in routeDescriptors)
                routes.Add(routeDescriptor);
        }
    }
}