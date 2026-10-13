using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Orchard.Environment.Extensions;
using Orchard.Localization.Services;
using Orchard.Mvc.Extensions;
using Orchard.UI.Admin;
using Orchard.Utility.Extensions;

namespace Orchard.Localization.Selectors
{
    [OrchardFeature("Orchard.Localization.CultureSelector")]
    public class RouteCultureSelector : ICultureSelector
    {
        public CultureSelectorResult GetCulture(HttpContext context)
        {
            if (context == null || AdminFilter.IsApplied(context)) return null;

            // Attempt to determine culture by route.
            // This normally happens when you are using non standard pages that are not content items
            // {culture}/foo or ?culture={culture}
            var routeCulture = context.Request.HttpContext.GetRouteData().Values["culture"] ??
                context.GetActionContext().HttpContext.Request.GetParam("culture");
            if (routeCulture != null && !string.IsNullOrWhiteSpace(routeCulture.ToString()))
            {
                try
                {
                    var culture = CultureInfo.GetCultureInfo(routeCulture.ToString());
                    return new CultureSelectorResult { Priority = -3, CultureName = culture.Name };
                }
                catch { }
            }

            return null;
        }
    }
}