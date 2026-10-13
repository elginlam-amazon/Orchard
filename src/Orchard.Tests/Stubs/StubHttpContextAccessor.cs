using Microsoft.AspNetCore.Http;
using Orchard.Mvc;
using IHttpContextAccessor = Orchard.Mvc.IHttpContextAccessor;

namespace Orchard.Tests.Stubs
{
    public class StubHttpContextAccessor : IHttpContextAccessor
    {
        private HttpContext _httpContext;

        public StubHttpContextAccessor()
        {
        }

        public StubHttpContextAccessor(HttpContext httpContext)
        {
            _httpContext = httpContext;
        }

        public HttpContext Current()
        {
            return _httpContext;
        }

        public void Set(HttpContext httpContext)
        {
            _httpContext = httpContext;
        }
    }
}