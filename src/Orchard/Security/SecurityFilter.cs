using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Orchard.Logging;
using Orchard.Mvc.Filters;
using Orchard.UI.Admin;
using Orchard.Mvc.Extensions;

namespace Orchard.Security
{
    public class SecurityFilter : FilterProvider, IExceptionFilter, IAuthorizationFilter
    {
        private readonly IAuthorizer _authorizer;

        public SecurityFilter(IAuthorizer authorizer)
        {
            _authorizer = authorizer;
            Logger = NullLogger.Instance;
        }

        public ILogger Logger { get; set; }

        public void OnAuthorization(AuthorizationFilterContext filterContext)
        {

            var accessFrontEnd = filterContext.ActionDescriptor.GetCustomAttributes(typeof(AlwaysAccessibleAttribute), true).Any();

            if (!accessFrontEnd && filterContext.ActionDescriptor.GetControllerType().GetCustomAttributes(typeof(AlwaysAccessibleAttribute), true).Any())
            {
                accessFrontEnd = true;
            }

            if (!AdminFilter.IsApplied(filterContext) && !accessFrontEnd && !_authorizer.Authorize(StandardPermissions.AccessFrontEnd))
            {
                filterContext.Result = new UnauthorizedResult();
            }
        }

        public void OnException(ExceptionContext filterContext)
        {
            if (!(filterContext.Exception is OrchardSecurityException))
                return;

            try
            {
                Logger.Information(filterContext.Exception, "Security exception converted to access denied result");
            }
            catch
            {
                //a logger exception can't be allowed to interrupt this process
            }

            filterContext.Result = new UnauthorizedResult();
            filterContext.ExceptionHandled = true;
        }
    }
}
