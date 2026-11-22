using Microsoft.AspNetCore.Routing;

namespace Orchard.Mvc.Extensions
{
    public static class RouteExtensions
    {
        public static string GetAreaName(this RouteData routeData)
        {
            if (routeData.DataTokens.TryGetValue("area", out object area))
                return area as string;

            if (routeData.Values.TryGetValue("area", out object areaValue))
                return areaValue as string;

            return null;
        }

        public static string ToRouteString(this RouteValueDictionary route) =>
            string.Join("/", route["Area"], route["Controller"], route["Action"]);
    }
}
