using Microsoft.AspNetCore.Http;

namespace Orchard.Mvc.Extensions
{
    public static class HttpContextBaseExtensions
    {
        public static bool IsBackgroundContext(this HttpContext httpContextBase)
        {
            return httpContextBase == null || httpContextBase.Items.ContainsKey(MvcModule.IsBackgroundHttpContextKey);
        }
    }

    public static class HttpContextExtensions
    {
        public static bool IsBackgroundHttpContext(this HttpContext httpContext)
        {
            return HttpContextAccessor.IsBackgroundHttpContext(httpContext);
        }
    }
}
