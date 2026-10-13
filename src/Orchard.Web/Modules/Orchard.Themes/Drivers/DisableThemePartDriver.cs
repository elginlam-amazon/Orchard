using Microsoft.AspNetCore.Http;
using Orchard.ContentManagement.Drivers;
using Orchard.Mvc.Extensions;
using Orchard.Themes.Models;
using Orchard.UI.Admin;

namespace Orchard.Themes.Drivers
{
    public class DisableThemePartDriver : ContentPartDriver<DisableThemePart>
    {
        private readonly HttpContext _httpContext;

        public DisableThemePartDriver(HttpContext httpContext)
        {
            _httpContext = httpContext;
        }

        protected override DriverResult Display(DisableThemePart part, string displayType, dynamic shapeHelper)
        {
            if (AdminFilter.IsApplied(_httpContext.GetActionContext()))
            {
                return null;
            }
            return ContentShape("Parts_DisableTheme", () =>
            {
                ThemeFilter.Disable(_httpContext.GetActionContext());
                return null;
            });
        }
    }

}
