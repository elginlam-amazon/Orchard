using Microsoft.AspNetCore.Http;
using Orchard.Utility;

namespace Orchard.Tests.Stubs
{
    /// <summary>
    /// A request for an application relative path and a host (the application is at the root of the site).
    /// </summary>
    public class StubHttpContext : HttpContext
    {
        // DefaultHttpContext is sealed: the stub delegates to an instance.
        private readonly DefaultHttpContext _inner = new DefaultHttpContext();

        public StubHttpContext()
            : this("~/yadda")
        {
        }

        public StubHttpContext(string appRelativeCurrentExecutionFilePath)
            : this(appRelativeCurrentExecutionFilePath, "localhost")
        {
        }

        public StubHttpContext(string appRelativeCurrentExecutionFilePath, string hostHeader)
        {
            Request.Scheme = "http";
            Request.Host = new HostString(hostHeader);
            Request.PathBase = PathString.Empty;
            Request.Path = new PathString("/" + appRelativeCurrentExecutionFilePath.TrimStart('~').TrimStart('/'));
            Items[VirtualPathUtility.ApplicationPathBaseKey] = string.Empty;
        }

        public override Microsoft.AspNetCore.Http.Features.IFeatureCollection Features => _inner.Features;
        public override HttpRequest Request => _inner.Request;
        public override HttpResponse Response => _inner.Response;
        public override ConnectionInfo Connection => _inner.Connection;
        public override WebSocketManager WebSockets => _inner.WebSockets;
        public override System.Security.Claims.ClaimsPrincipal User { get => _inner.User; set => _inner.User = value; }
        public override System.Collections.Generic.IDictionary<object, object> Items { get => _inner.Items; set => _inner.Items = value; }
        public override System.IServiceProvider RequestServices { get => _inner.RequestServices; set => _inner.RequestServices = value; }
        public override System.Threading.CancellationToken RequestAborted { get => _inner.RequestAborted; set => _inner.RequestAborted = value; }
        public override string TraceIdentifier { get => _inner.TraceIdentifier; set => _inner.TraceIdentifier = value; }
        public override ISession Session { get => _inner.Session; set => _inner.Session = value; }
        public override void Abort() => _inner.Abort();
    }
}
