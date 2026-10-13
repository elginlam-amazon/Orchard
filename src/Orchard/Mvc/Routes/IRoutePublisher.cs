using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Orchard.Mvc.Routes
{
    public interface IRoutePublisher : IDependency
    {
        void Publish(IEnumerable<RouteDescriptor> routes, RequestDelegate pipeline = null);
    }
}
