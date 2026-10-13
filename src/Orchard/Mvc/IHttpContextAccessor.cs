using Microsoft.AspNetCore.Http;

namespace Orchard.Mvc
{
    public interface IHttpContextAccessor
    {
        HttpContext Current();
        void Set(HttpContext httpContext);
    }
}
