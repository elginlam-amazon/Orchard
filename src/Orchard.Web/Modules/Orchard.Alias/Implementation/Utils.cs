using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Orchard.Logging;
using Orchard.Mvc.Extensions;
using Orchard.Mvc.Routes;
using Route = Orchard.Mvc.Routes.Route;

namespace Orchard.Alias.Implementation
{
    public static class Utils
    {
        public static IDictionary<string, string> LookupRouteValues
            (HttpContext httpContext, IEnumerable<RouteDescriptor>
            routeDescriptors,
            string routePath)
        {
            var queryStringIndex = routePath.IndexOf('?');
            var routePathNoQueryString = queryStringIndex == -1 ? routePath : routePath.Substring(0, queryStringIndex);
            var queryString = queryStringIndex == -1 ? null : routePath.Substring(queryStringIndex + 1);

            var matches = routeDescriptors
                .Select(routeDescriptor => routeDescriptor.Route as Route)
                .Where(route => route != null)
                .Invoke(route => new { Route = route, Values = route.MatchPath(httpContext, routePathNoQueryString) }, NullLogger.Instance)
                .Where(match => match.Values != null)
                .Select(match => ToRouteValues(match.Route, match.Values, queryString));

            return matches.FirstOrDefault();
        }

        public static IEnumerable<VirtualPathData> LookupVirtualPaths(
            HttpContext httpContext,
            IEnumerable<RouteDescriptor> routeDescriptors,
            RouteValueDictionary routeValues)
        {

            var areaName = "";
            object value;
            if (routeValues.TryGetValue("area", out value))
                areaName = Convert.ToString(value, CultureInfo.InvariantCulture);

            var virtualPathDatas = routeDescriptors.Where(r2 => r2.Route.GetAreaName() == areaName)
                .Invoke(r2 => r2.Route.GetVirtualPath(new VirtualPathContext(httpContext, null, routeValues)), NullLogger.Instance)
                .Where(vp => vp != null)
                .ToArray();

            return virtualPathDatas;
        }

        public static IEnumerable<VirtualPathData> LookupVirtualPaths(
            HttpContext httpContext,
            IEnumerable<RouteDescriptor> routeDescriptors,
            string areaName,
            IDictionary<string, string> routeValues)
        {

            var routeValueDictionary = new RouteValueDictionary(routeValues.ToDictionary(kv => RemoveDash(kv.Key), kv => (object)kv.Value));
            var virtualPathDatas = routeDescriptors.Where(r2 => r2.Route.GetAreaName() == areaName)
                .Invoke(r2 => r2.Route.GetVirtualPath(new VirtualPathContext(httpContext, null, routeValueDictionary)), NullLogger.Instance)
                .Where(vp => vp != null)
                .ToArray();

            return virtualPathDatas;
        }

        private static string RemoveDash(string key)
        {
            return key.EndsWith("-", StringComparison.InvariantCulture) ? key.Substring(0, key.Length - 1) : key;
        }

        private static Dictionary<string, string> ToRouteValues(Route route, RouteValueDictionary values, string queryString)
        {
            var routeValues = values
                .Select(kv =>
                {
                    var value = Convert.ToString(kv.Value, CultureInfo.InvariantCulture);
                    var defaultValue = FindDefault(route, kv.Key);
                    if (defaultValue != null && string.Equals(defaultValue, value, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return new { Key = kv.Key + "-", Value = value };
                    }
                    return new { kv.Key, Value = value };
                })
                .ToDictionary(kv => kv.Key, kv => kv.Value);
            if (queryString != null)
            {
                foreach (var term in queryString
                    .Split(new[] { "&" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(ParseTerm))
                {
                    if (!routeValues.ContainsKey(term[0]))
                        routeValues[term[0]] = term[1];
                }
            }
            return routeValues;
        }

        private static string[] ParseTerm(string term)
        {
            var equalsIndex = term.IndexOf('=');
            if (equalsIndex == -1)
            {
                return new[] { Uri.UnescapeDataString(term), null };
            }
            return new[] { Uri.UnescapeDataString(term.Substring(0, equalsIndex)), Uri.UnescapeDataString(term.Substring(equalsIndex + 1)) };
        }

        private static string FindDefault(Route route, string key)
        {
            object defaultValue;
            if (!route.Defaults.TryGetValue(key, out defaultValue))
            {
                return null;
            }
            return Convert.ToString(defaultValue, CultureInfo.InvariantCulture);
        }
    }
}