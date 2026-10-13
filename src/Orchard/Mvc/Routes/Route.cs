using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.DependencyInjection;

namespace Orchard.Mvc.Routes
{
    /// <summary>
    /// A template based route definition as published by <see cref="IRouteProvider"/> implementations.
    /// The definition is turned into an ASP.NET Core <see cref="Microsoft.AspNetCore.Routing.Route"/> the first time it is used,
    /// because the inline constraint resolver is only available from the request services.
    /// </summary>
    public class Route : IRouter, IRouteWithArea
    {
        private readonly object _syncLock = new object();
        private Microsoft.AspNetCore.Routing.Route _route;

        public Route(string url, IRouter routeHandler)
            : this(url, null, null, null, routeHandler)
        {
        }

        public Route(string url, RouteValueDictionary defaults, IRouter routeHandler)
            : this(url, defaults, null, null, routeHandler)
        {
        }

        public Route(string url, RouteValueDictionary defaults, RouteValueDictionary constraints, IRouter routeHandler)
            : this(url, defaults, constraints, null, routeHandler)
        {
        }

        public Route(string url, RouteValueDictionary defaults, RouteValueDictionary constraints, RouteValueDictionary dataTokens, IRouter routeHandler)
        {
            Url = url;
            Defaults = defaults ?? new RouteValueDictionary();
            Constraints = constraints ?? new RouteValueDictionary();
            DataTokens = dataTokens ?? new RouteValueDictionary();
            RouteHandler = routeHandler ?? new MvcRouteHandler();
        }

        public string Name { get; set; }
        public string Url { get; }
        public RouteValueDictionary Defaults { get; }
        public RouteValueDictionary Constraints { get; }
        public RouteValueDictionary DataTokens { get; }
        public IRouter RouteHandler { get; }

        public string Area => DataTokens.TryGetValue("area", out var area) ? area as string : null;

        /// <summary>
        /// Matches an application relative path (without the leading slash) against the route template, defaults and constraints,
        /// without dispatching the request (the former RouteBase.GetRouteData used for lookups).
        /// </summary>
        /// <returns>The route values if the path matches, <c>null</c> otherwise.</returns>
        public RouteValueDictionary MatchPath(HttpContext httpContext, string path)
        {
            var matcher = GetTemplateMatcher();
            var values = new RouteValueDictionary();
            if (!matcher.TryMatch(new PathString("/" + (path ?? string.Empty).TrimStart('/')), values))
            {
                return null;
            }

            foreach (var constraint in Constraints)
            {
                var routeConstraint = constraint.Value as IRouteConstraint;
                if (routeConstraint == null && constraint.Value is string pattern)
                {
                    routeConstraint = new RegexRouteConstraint("^(" + pattern + ")$");
                }

                if (routeConstraint != null && !routeConstraint.Match(httpContext, this, constraint.Key, values, RouteDirection.IncomingRequest))
                {
                    return null;
                }
            }

            return values;
        }

        private TemplateMatcher _templateMatcher;

        private TemplateMatcher GetTemplateMatcher()
        {
            return _templateMatcher ?? (_templateMatcher = new TemplateMatcher(TemplateParser.Parse(Url ?? string.Empty), Defaults));
        }

        public Task RouteAsync(RouteContext context)
        {
            return GetRoute(context.HttpContext.RequestServices).RouteAsync(context);
        }

        public VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            return GetRoute(context.HttpContext.RequestServices).GetVirtualPath(context);
        }

        private Microsoft.AspNetCore.Routing.Route GetRoute(IServiceProvider services)
        {
            if (_route == null)
            {
                lock (_syncLock)
                {
                    if (_route == null)
                    {
                        var constraintResolver = services.GetRequiredService<IInlineConstraintResolver>();
                        _route = new Microsoft.AspNetCore.Routing.Route(
                            RouteHandler,
                            Name,
                            Url,
                            Defaults,
                            new Dictionary<string, object>(Constraints, StringComparer.OrdinalIgnoreCase),
                            DataTokens,
                            constraintResolver);
                    }
                }
            }

            return _route;
        }
    }
}
