using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;
using Orchard.Mvc.Routes;
using Route = Orchard.Mvc.Routes.Route;

namespace Orchard.Layouts
{
    public class Routes : IRouteProvider
    {
        public void GetRoutes(ICollection<RouteDescriptor> routes)
        {
            var routeDescriptor = new RouteDescriptor
            {
                Route = new Route(
                    "Admin/Layouts/{controller}/{action}/{id}",
                    new RouteValueDictionary {
                        {"area", "Orchard.Layouts"},
                        {"id", ""}
                    },
                    new RouteValueDictionary(),
                    new RouteValueDictionary() {
                        {"area", "Orchard.Layouts"}
                    },
                    new MvcRouteHandler())
            };

            routes.Add(routeDescriptor);
        }
    }
}