using System;
using System.Collections.Generic;
using System.Threading;
using System.Web;
using Microsoft.AspNetCore.Routing;

// Compatibility stubs for System.Web.Routing types that don't exist in .NET 10
namespace System.Web.Routing
{
    public class RequestContext
    {
        public RequestContext() { }

        public RequestContext(HttpContextBase httpContext, RouteData routeData)
        {
            HttpContext = httpContext;
            RouteData = routeData;
        }

        public HttpContextBase HttpContext { get; set; }
        public RouteData RouteData { get; set; }
    }

    public class RouteData
    {
        public RouteData() { }
        public RouteData(RouteBase route, IRouteHandler routeHandler)
        {
            Route = route;
            RouteHandler = routeHandler;
        }

        public RouteValueDictionary Values { get; } = new RouteValueDictionary();
        public RouteValueDictionary DataTokens { get; } = new RouteValueDictionary();
        public IRouteHandler RouteHandler { get; set; }
        public RouteBase Route { get; set; }

        public string GetAreaName()
        {
            if (DataTokens.TryGetValue("area", out var area) && area is string areaStr)
                return areaStr;
            if (Route is IRouteWithArea routeWithArea)
                return routeWithArea.Area;
            return null;
        }

        public string GetRequiredString(string valueName)
        {
            if (Values.TryGetValue(valueName, out var value) && value != null)
                return value.ToString();
            throw new InvalidOperationException($"Required route value '{valueName}' not found.");
        }
    }

    public interface IRouteHandler
    {
        System.Web.IHttpHandler GetHttpHandler(RequestContext requestContext);
    }

    public abstract class RouteBase
    {
        public abstract RouteData GetRouteData(HttpContextBase httpContext);
        public virtual VirtualPathData GetVirtualPath(RequestContext requestContext, RouteValueDictionary values) => null;
        public virtual string GetAreaName() => null;
    }

    public class VirtualPathData
    {
        public VirtualPathData(RouteBase route, string virtualPath)
        {
            Route = route;
            VirtualPath = virtualPath;
        }
        public RouteBase Route { get; }
        public string VirtualPath { get; set; }
        public RouteValueDictionary DataTokens { get; } = new RouteValueDictionary();
    }

    public interface IRouteWithArea
    {
        string Area { get; }
    }

    public class StopRoutingHandler : IRouteHandler
    {
        public System.Web.IHttpHandler GetHttpHandler(RequestContext requestContext) => null;
    }

    public class MvcRouteHandler : IRouteHandler
    {
        public System.Web.IHttpHandler GetHttpHandler(RequestContext requestContext) => null;
    }

    public interface IRouteConstraint
    {
        bool Match(HttpContextBase httpContext, Route route, string parameterName, RouteValueDictionary values, RouteDirection routeDirection);
    }

    public enum RouteDirection { IncomingRequest, UrlGeneration }

    public interface INamedRoute
    {
        string Name { get; }
    }

    public class Route : RouteBase, INamedRoute
    {
        public Route() { }

        public Route(string url, IRouteHandler routeHandler)
        {
            Url = url;
            Defaults = new RouteValueDictionary();
            Constraints = new RouteValueDictionary();
            DataTokens = new RouteValueDictionary();
            RouteHandler = routeHandler;
        }

        public Route(string url, RouteValueDictionary defaults, IRouteHandler routeHandler)
        {
            Url = url;
            Defaults = defaults ?? new RouteValueDictionary();
            Constraints = new RouteValueDictionary();
            DataTokens = new RouteValueDictionary();
            RouteHandler = routeHandler;
        }

        public Route(string url, RouteValueDictionary defaults = null, RouteValueDictionary constraints = null, IRouteHandler routeHandler = null)
        {
            Url = url;
            Defaults = defaults ?? new RouteValueDictionary();
            Constraints = constraints ?? new RouteValueDictionary();
            DataTokens = new RouteValueDictionary();
            RouteHandler = routeHandler;
        }

        public Route(string url, RouteValueDictionary defaults, RouteValueDictionary constraints, RouteValueDictionary dataTokens, IRouteHandler routeHandler = null)
        {
            Url = url;
            Defaults = defaults ?? new RouteValueDictionary();
            Constraints = constraints ?? new RouteValueDictionary();
            DataTokens = dataTokens ?? new RouteValueDictionary();
            RouteHandler = routeHandler;
        }

        public string Name { get; set; }
        public string Url { get; set; }
        public RouteValueDictionary Defaults { get; set; } = new RouteValueDictionary();
        public RouteValueDictionary DataTokens { get; set; } = new RouteValueDictionary();
        public RouteValueDictionary Constraints { get; set; } = new RouteValueDictionary();
        public IRouteHandler RouteHandler { get; set; }

        public override RouteData GetRouteData(HttpContextBase httpContext) => null;
        public override VirtualPathData GetVirtualPath(RequestContext requestContext, RouteValueDictionary values) => null;
    }

    public class RouteCollection : List<RouteBase>
    {
        private readonly Dictionary<string, RouteBase> _namedMap = new Dictionary<string, RouteBase>(StringComparer.OrdinalIgnoreCase);
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public RouteBase this[string name]
        {
            get
            {
                _namedMap.TryGetValue(name, out var route);
                return route;
            }
        }

        public new void Add(RouteBase route)
        {
            base.Add(route);
            if (route is INamedRoute named && named.Name != null)
                _namedMap[named.Name] = route;
        }

        public void Add(string name, RouteBase route)
        {
            base.Add(route);
            if (name != null) _namedMap[name] = route;
        }

        public new void Insert(int index, RouteBase route)
        {
            base.Insert(index, route);
            if (route is INamedRoute named && named.Name != null)
                _namedMap[named.Name] = route;
        }

        public Route MapRoute(string name, string url, object defaults = null, object constraints = null)
        {
            var route = new Route(url,
                defaults != null ? new RouteValueDictionary(defaults) : new RouteValueDictionary(),
                constraints != null ? new RouteValueDictionary(constraints) : new RouteValueDictionary(),
                new MvcRouteHandler());
            route.Name = name;
            Add(name, route);
            return route;
        }

        public RouteData GetRouteData(HttpContextBase httpContext)
        {
            foreach (var route in this)
            {
                var routeData = route.GetRouteData(httpContext);
                if (routeData != null) return routeData;
            }
            return null;
        }

        public VirtualPathData GetVirtualPath(RequestContext requestContext, RouteValueDictionary values)
        {
            foreach (var route in this)
            {
                var vp = route.GetVirtualPath(requestContext, values);
                if (vp != null) return vp;
            }
            return null;
        }

        public IDisposable GetWriteLock() => new WriteLockScope(_lock);
        public IDisposable GetReadLock() => new ReadLockScope(_lock);

        private class WriteLockScope : IDisposable
        {
            private readonly ReaderWriterLockSlim _lk;
            public WriteLockScope(ReaderWriterLockSlim l) { _lk = l; _lk.EnterWriteLock(); }
            public void Dispose() { _lk.ExitWriteLock(); }
        }

        private class ReadLockScope : IDisposable
        {
            private readonly ReaderWriterLockSlim _lk;
            public ReadLockScope(ReaderWriterLockSlim l) { _lk = l; _lk.EnterReadLock(); }
            public void Dispose() { _lk.ExitReadLock(); }
        }
    }

    public static class RouteTable
    {
        public static RouteCollection Routes { get; } = new RouteCollection();
    }
}
