using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Orchard.WebApi;
using Orchard.WebApi.Filters;

namespace Orchard.Mvc.Filters
{
    /// <summary>
    /// Adds the filters of the current tenant (the <see cref="IFilterProvider"/> implementations for MVC controllers, and the
    /// <see cref="IApiFilterProvider"/> implementations for the HTTP API controllers) to the ASP.NET Core filter pipeline.
    /// </summary>
    public class OrchardFilterProvider : Microsoft.AspNetCore.Mvc.Filters.IFilterProvider
    {
        // Runs after the default filter provider.
        public int Order => 100;

        public void OnProvidersExecuting(FilterProviderContext context)
        {
            var workContext = context.ActionContext.GetWorkContext();
            if (workContext == null)
                return;

            var isApiController = context.ActionContext.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor
                && typeof(ApiController).IsAssignableFrom(controllerActionDescriptor.ControllerTypeInfo);

            IEnumerable<object> filters = isApiController
                ? workContext.Resolve<IEnumerable<IApiFilterProvider>>()
                : workContext.Resolve<IEnumerable<IFilterProvider>>();

            // Map IFilterProvider implementations to MVC Filter objects
            // Need to provide order values since Filter objects of identical
            // scope and order would run in undefined order.
            // We create negative order values to avoid conflicts with other
            // potential user-provided MVC Filter objects, which hopefully use
            // positive order values. We do this by reversing the list and
            // negating the index.
            var index = 0;
            foreach (var filter in filters.OfType<IFilterMetadata>().Reverse())
            {
                var descriptor = new FilterDescriptor(filter, FilterScope.Action) { Order = -(++index) };
                context.Results.Add(new FilterItem(descriptor, filter) { IsReusable = false });
            }
        }

        public void OnProvidersExecuted(FilterProviderContext context)
        {
        }
    }
}
