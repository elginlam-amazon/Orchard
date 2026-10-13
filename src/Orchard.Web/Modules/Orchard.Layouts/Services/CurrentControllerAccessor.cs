using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Orchard.Layouts.Filters;

namespace Orchard.Layouts.Services
{
    public class CurrentControllerAccessor : ICurrentControllerAccessor
    {
        private readonly HttpContext _httpContext;

        public CurrentControllerAccessor(HttpContext httpContext)
        {
            _httpContext = httpContext;
        }

        public Controller CurrentController => (Controller)_httpContext.Items[ControllerAccessorFilter.CurrentControllerKey];
    }
}