using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Routing;

namespace Orchard.Mvc
{
    public class FormValueRequiredAttribute : ActionMethodSelectorAttribute
    {
        private readonly string _submitButtonName;

        public FormValueRequiredAttribute(string submitButtonName)
        {
            _submitButtonName = submitButtonName;
        }

        public override bool IsValidForRequest(RouteContext routeContext, ActionDescriptor action)
        {
            var request = routeContext.HttpContext.Request;
            if (!request.HasFormContentType)
            {
                return false;
            }

            var value = request.Form[_submitButtonName];
            return !string.IsNullOrEmpty(value);
        }
    }
}
