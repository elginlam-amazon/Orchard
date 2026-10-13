using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Orchard.WebApi
{
    /// <summary>
    /// Applies the Web API action selection conventions to the <see cref="ApiController"/> controllers:
    /// the routes don't provide an action name, the action is selected by HTTP method,
    /// either from an explicit HTTP method attribute or from the action name prefix (Get..., Post...).
    /// </summary>
    public class ApiControllerConvention : IControllerModelConvention
    {
        private static readonly string[] _httpMethods = { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS" };

        public void Apply(ControllerModel controller)
        {
            if (!typeof(ApiController).IsAssignableFrom(controller.ControllerType))
            {
                return;
            }

            foreach (var action in controller.Actions)
            {
                // Actions are not selected by name.
                action.RouteValues["action"] = null;

                var hasHttpMethodAttribute = action.Attributes.OfType<IActionHttpMethodProvider>().Any();
                if (hasHttpMethodAttribute)
                {
                    continue;
                }

                var httpMethod = _httpMethods.FirstOrDefault(m => action.ActionMethod.Name.StartsWith(m, StringComparison.OrdinalIgnoreCase));
                if (httpMethod == null)
                {
                    // Web API defaults to POST when no convention matches.
                    httpMethod = "POST";
                }

                foreach (var selector in action.Selectors)
                {
                    selector.ActionConstraints.Add(new HttpMethodActionConstraint(new[] { httpMethod }));
                }
            }
        }
    }
}
