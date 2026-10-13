using Microsoft.AspNetCore.Routing;

namespace Orchard.Mvc.Routes
{
    /// <summary>
    /// Session state requirement declared by a route or an extension manifest.
    /// ASP.NET Core has no per-route session state behavior, the value is kept for compatibility
    /// with the extension manifests (SessionState: ...) and route descriptors.
    /// </summary>
    public enum SessionStateBehavior
    {
        Default = 0,
        Required = 1,
        ReadOnly = 2,
        Disabled = 3
    }

    public class RouteDescriptor
    {
        public string Name { get; set; }
        public int Priority { get; set; }
        public IRouter Route { get; set; }
        public SessionStateBehavior SessionState { get; set; }
    }

    public class HttpRouteDescriptor : RouteDescriptor
    {
        public string RouteTemplate { get; set; }
        public object Defaults { get; set; }
        public object Constraints { get; set; }
    }
}
