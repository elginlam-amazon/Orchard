using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Orchard.Localization;
using Orchard.Mvc.Filters;
using Orchard.Security;
using Orchard.Mvc.Extensions;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Orchard.UI.Admin
{
    public class AdminFilter : FilterProvider, IAuthorizationFilter
    {
        private readonly IAuthorizer _authorizer;

        public AdminFilter(IAuthorizer authorizer)
        {
            _authorizer = authorizer;
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        public void OnAuthorization(AuthorizationFilterContext filterContext)
        {
            if (IsAdmin(filterContext))
            {
                Apply(filterContext);

                if (!_authorizer.Authorize(StandardPermissions.AccessAdminPanel, T("Can't access the admin")))
                {
                    filterContext.Result = new UnauthorizedResult();
                }
            }
        }

        public static void Apply(ActionContext context)
        {
            // the value isn't important
            context.HttpContext.Items[typeof(AdminFilter)] = null;
        }

        public static bool IsApplied(ActionContext context)
        {
            return IsApplied(context.HttpContext);
        }

        public static bool IsApplied(HttpContext context)
        {
            return context.Items.ContainsKey(typeof(AdminFilter));
        }

        private static bool IsAdmin(AuthorizationFilterContext filterContext)
        {
            if (IsNameAdmin(filterContext) || IsNameAdminProxy(filterContext))
            {
                return true;
            }

            var adminAttributes = GetAdminAttributes(filterContext.ActionDescriptor);
            if (adminAttributes != null && adminAttributes.Any())
            {
                return true;
            }
            return false;
        }

        private static bool IsNameAdmin(AuthorizationFilterContext filterContext)
        {
            return string.Equals(filterContext.ActionDescriptor.GetControllerName(), "Admin",
                                 StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsNameAdminProxy(AuthorizationFilterContext filterContext)
        {
            return filterContext.ActionDescriptor.GetControllerName().StartsWith(
                "AdminControllerProxy", StringComparison.InvariantCultureIgnoreCase) &&
                filterContext.ActionDescriptor.GetControllerName().Length == "AdminControllerProxy".Length + 32;
        }

        private static IEnumerable<AdminAttribute> GetAdminAttributes(ActionDescriptor descriptor)
        {
            return descriptor.GetCustomAttributes(typeof(AdminAttribute), true)
                .Concat(descriptor.GetControllerCustomAttributes(typeof(AdminAttribute), true))
                .OfType<AdminAttribute>();
        }

    }
}