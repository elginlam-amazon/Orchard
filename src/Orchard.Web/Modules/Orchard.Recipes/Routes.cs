using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;
using Orchard.Mvc.Routes;
using Route = Orchard.Mvc.Routes.Route;

namespace Orchard.Recipes
{
    public class Routes : IRouteProvider
    {
        public void GetRoutes(ICollection<RouteDescriptor> routes)
        {
            var routeDescriptor = new RouteDescriptor
            {
                Priority = 5,
                Route = new Route(
                                                         "Recipes/Status/{executionId}",
                                                         new RouteValueDictionary {
                                                                                      {"area", "Orchard.Recipes"},
                                                                                      {"controller", "Recipes"},
                                                                                      {"action", "RecipeExecutionStatus"}
                                                         },
                                                         new RouteValueDictionary(),
                                                         new RouteValueDictionary {
                                                                                      {"area", "Orchard.Recipes"}
                                                         },
                                                         new MvcRouteHandler())
            };

            routes.Add(routeDescriptor);
        }
    }
}