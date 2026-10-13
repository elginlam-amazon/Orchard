using Microsoft.AspNetCore.Mvc.Rendering;
using Orchard.Environment.Extensions.Models;
using Orchard.Validation;

namespace Orchard.Mvc.Html
{
    public static class ThemeExtensions
    {
        /// <summary>
        /// Include a view from the current view.
        /// </summary>
        public static void Include(this IHtmlHelper helper, string viewName)
        {
            Argument.ThrowIfNull(helper, "helper");
            helper.RenderPartial(viewName);
        }

        public static string ThemePath(this IHtmlHelper helper, ExtensionDescriptor theme, string path)
        {
            return theme.Location + "/" + theme.Id + path;
        }
    }
}
