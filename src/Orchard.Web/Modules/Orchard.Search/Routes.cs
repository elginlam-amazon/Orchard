using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;
using Orchard.Mvc.Routes;
using Route = Orchard.Mvc.Routes.Route;

namespace Orchard.Search
{
    public class Routes : IRouteProvider
    {

        public void GetRoutes(ICollection<RouteDescriptor> routes)
        {
            var routeDescriptors = new[] {
                new RouteDescriptor {
                    Priority = 5,
                    Route = new Route("Search/ContentPicker",
                        new RouteValueDictionary {
                            {"area", "Orchard.Search"},
                            {"controller", "ContentPicker"},
                            {"action", "Index"}
                        },
                        null,
                        new RouteValueDictionary {
                            {"area", "Orchard.Search"}
                        },
                        new MvcRouteHandler())
                },
                new RouteDescriptor {
                    Priority = 5,
                    Route = new Route("Search/{searchIndex}",
                        new RouteValueDictionary {
                            {"area", "Orchard.Search"},
                            {"controller", "Search"},
                            {"action", "Index"},
                            {"searchIndex", ""}
                        },
                        null,
                        new RouteValueDictionary {
                            {"area", "Orchard.Search"}
                        },
                        new MvcRouteHandler())
                },
                new RouteDescriptor {
                    Priority = 5,
                    Route = new Route("Admin/Search/BlogSearch/{blogId}",
                        new RouteValueDictionary {
                            {"area", "Orchard.Search"},
                            {"controller", "BlogSearch"},
                            {"action", "Index"},
                            {"blogId", ""}
                        },
                        null,
                        new RouteValueDictionary {
                            {"area", "Orchard.Search"}
                        },
                        new MvcRouteHandler())
                }
            };

            foreach (var routeDescriptor in routeDescriptors)
                routes.Add(routeDescriptor);
        }
    }
}
