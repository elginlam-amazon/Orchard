using Autofac;
using Microsoft.AspNetCore.Http;

namespace Orchard.Mvc
{
    public class HttpContextAccessor : IHttpContextAccessor
    {
        // The ASP.NET Core accessor stores the current request in an AsyncLocal shared by all its instances.
        private static readonly Microsoft.AspNetCore.Http.HttpContextAccessor _aspNetCoreHttpContextAccessor = new Microsoft.AspNetCore.Http.HttpContextAccessor();

        readonly ILifetimeScope _lifetimeScope;
        private HttpContext _httpContext;
        private IWorkContextAccessor _wca;

        public HttpContextAccessor(ILifetimeScope lifetimeScope)
        {
            _lifetimeScope = lifetimeScope;
        }

        public HttpContext Current()
        {
            var httpContext = _aspNetCoreHttpContextAccessor.HttpContext;

            if (!IsBackgroundHttpContext(httpContext))
                return httpContext;

            if (_httpContext != null)
                return _httpContext;

            if (_wca == null && _lifetimeScope.IsRegistered<IWorkContextAccessor>())
                _wca = _lifetimeScope.Resolve<IWorkContextAccessor>();

            var workContext = _wca != null ? _wca.GetLogicalContext() : null;
            return workContext != null ? workContext.HttpContext : null;
        }

        public void Set(HttpContext httpContext)
        {
            _httpContext = httpContext;
        }

        internal static bool IsBackgroundHttpContext(HttpContext httpContext)
        {
            return httpContext == null || httpContext.Items.ContainsKey(MvcModule.IsBackgroundHttpContextKey);
        }
    }
}
