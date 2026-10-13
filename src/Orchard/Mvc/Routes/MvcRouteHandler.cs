using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;

namespace Orchard.Mvc.Routes
{
    /// <summary>
    /// Route target which dispatches matched routes to ASP.NET Core MVC.
    /// The actual MVC route handler is provided by the hosting application when the router is configured
    /// (it is the default handler of the MVC route builder, see <c>UseMvc</c>).
    /// </summary>
    public class MvcRouteHandler : IRouter
    {
        public static IRouter Inner { get; set; }

        public Task RouteAsync(RouteContext context)
        {
            return Inner == null ? Task.CompletedTask : Inner.RouteAsync(context);
        }

        public VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            return Inner?.GetVirtualPath(context);
        }
    }
}
