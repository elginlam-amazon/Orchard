using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Orchard.Mvc.Filters;
using Orchard.UI.Admin;
using Orchard.Mvc.Extensions;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Orchard.Themes
{
    public class ThemeFilter : FilterProvider, IActionFilter, IResultFilter
    {
        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var attribute = GetThemedAttribute(filterContext.ActionDescriptor);
            if (AdminFilter.IsApplied(filterContext))
            {
                // admin are themed by default
                if (attribute == null || attribute.Enabled)
                {
                    Apply(filterContext);
                }
            }
            else
            {
                // non-admin are explicitly themed
                // Don't layout the result if it's not an Admin controller and it's disabled
                if (attribute != null && attribute.Enabled)
                {
                    Apply(filterContext);
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext filterContext) { }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
        }

        public void OnResultExecuted(ResultExecutedContext filterContext) { }

        public static void Apply(ActionContext context)
        {
            // the value isn't important
            context.HttpContext.Items[typeof(ThemeFilter)] = null;
        }

        public static void Disable(ActionContext context)
        {
            context.HttpContext.Items.Remove(typeof(ThemeFilter));
        }

        public static bool IsApplied(ActionContext context)
        {
            return context.HttpContext.Items.ContainsKey(typeof(ThemeFilter));
        }

        private static ThemedAttribute GetThemedAttribute(ActionDescriptor descriptor)
        {
            return descriptor.GetCustomAttributes(typeof(ThemedAttribute), true)
                .Concat(descriptor.GetControllerCustomAttributes(typeof(ThemedAttribute), true))
                .OfType<ThemedAttribute>()
                .FirstOrDefault();
        }
    }
}