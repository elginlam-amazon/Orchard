using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;
using Orchard.Alias.Implementation.Holder;
using Orchard.Alias.Implementation.Map;
using Orchard.Mvc.Routes;

namespace Orchard.Alias.Implementation
{
    public class AliasRoute : IRouter, IRouteWithArea
    {
        private readonly AliasMap _aliasMap;
        private readonly IRouter _routeHandler;

        public AliasRoute(IAliasHolder aliasHolder, string areaName, IRouter routeHandler)
        {
            Area = areaName;
            _aliasMap = aliasHolder.GetMap(areaName);
            _routeHandler = routeHandler;
        }

        public async Task RouteAsync(RouteContext context)
        {
            // don't compute unnecessary virtual path if the map is empty
            if (!_aliasMap.Any())
            {
                return;
            }

            // Get the full inbound request path, relative to the tenant (the tenant prefix is part of the path base)
            var virtualPath = (context.HttpContext.Request.Path.Value ?? "/").TrimStart('/');

            // Attempt to lookup RouteValues in the alias map
            AliasInfo aliasInfo;
            // TODO: Might as well have the lookup in AliasHolder...
            if (_aliasMap.TryGetAlias(virtualPath, out aliasInfo))
            {
                // Construct RouteData from the route values
                var data = context.RouteData;
                foreach (var routeValue in aliasInfo.RouteValues)
                {
                    var key = routeValue.Key;
                    if (key.EndsWith("-"))
                        data.Values[key.Substring(0, key.Length - 1)] = routeValue.Value;
                    else
                        data.Values[key] = routeValue.Value;
                }

                data.Values["area"] = Area;
                data.DataTokens["area"] = Area;
                data.Routers.Add(_routeHandler);

                await _routeHandler.RouteAsync(context);
            }
        }

        public VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            var routeValues = context.Values;

            // Lookup best match for route values in the expanded tree
            var match = _aliasMap.Locate(routeValues);
            if (match != null)
            {
                // Build any "spare" route values onto the Alias (so we correctly support any additional query parameters)
                var sb = new StringBuilder("/").Append(match.Item2);
                var extra = 0;
                foreach (var routeValue in routeValues)
                {
                    // Ignore any we already have
                    if (match.Item1.ContainsKey(routeValue.Key))
                    {
                        continue;
                    }

                    // Add a query string fragment
                    sb.Append((extra++ == 0) ? '?' : '&');
                    sb.Append(Uri.EscapeDataString(routeValue.Key));
                    sb.Append('=');
                    sb.Append(Uri.EscapeDataString(Convert.ToString(routeValue.Value, CultureInfo.InvariantCulture)));
                }
                // Construct data
                var data = new VirtualPathData(this, sb.ToString());
                // Set the Area for this route
                data.DataTokens["area"] = Area;
                return data;
            }

            return null;
        }

        public string Area { get; private set; }
    }
}
