using System.Linq;
using Microsoft.AspNetCore.Routing;
using Orchard.Mvc.Routes;

namespace Orchard.Mvc.Extensions
{
    public static class RouteExtensions
    {
        public static string GetAreaName(this IRouter route)
        {
            if (route is IRouteWithArea routeWithArea)
            {
                return routeWithArea.Area;
            }

            if (route is Microsoft.AspNetCore.Routing.Route castRoute && castRoute.DataTokens != null)
            {
                return castRoute.DataTokens.TryGetValue("area", out var area) ? area as string : null;
            }

            return null;
        }

        public static string GetAreaName(this RouteData routeData)
        {
            if (routeData.DataTokens.TryGetValue("area", out object area))
            {
                return area as string;
            }

            if (routeData.Values.TryGetValue("area", out area))
            {
                return area as string;
            }

            return routeData.Routers.Select(r => r.GetAreaName()).FirstOrDefault(a => a != null);
        }

        public static string ToRouteString(this RouteValueDictionary route) =>
            string.Join("/", route["Area"], route["Controller"], route["Action"]);
    }
}
