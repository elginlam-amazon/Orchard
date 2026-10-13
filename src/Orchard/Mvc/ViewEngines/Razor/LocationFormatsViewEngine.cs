using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.DependencyInjection;
using Orchard.Mvc.Extensions;

namespace Orchard.Mvc.ViewEngines.Razor
{
    /// <summary>
    /// A view engine locating Razor views from location formats ({0}: view name, {1}: controller, {2}: area),
    /// the way the former MVC RazorViewEngine was configured by Orchard, and delegating the view
    /// creation to the ASP.NET Core Razor view engine.
    /// </summary>
    /// <remarks>
    /// Orchard views are always rendered as partial views (no _ViewStart, the layout is a shape).
    /// </remarks>
    public class LocationFormatsViewEngine : IViewEngine
    {
        private static readonly string[] _empty = new string[0];

        public string[] ViewLocationFormats { get; set; } = _empty;
        public string[] PartialViewLocationFormats { get; set; } = _empty;
        public string[] AreaViewLocationFormats { get; set; } = _empty;
        public string[] AreaPartialViewLocationFormats { get; set; } = _empty;

        public ViewEngineResult FindView(ActionContext context, string viewName, bool isMainPage)
        {
            var razorViewEngine = context.HttpContext.RequestServices.GetRequiredService<IRazorViewEngine>();

            if (IsPath(viewName))
            {
                return razorViewEngine.GetView(null, viewName, false);
            }

            var controllerName = context.RouteData.Values.TryGetValue("controller", out var controller) ? Convert.ToString(controller, CultureInfo.InvariantCulture) : null;
            var areaName = context.RouteData.GetAreaName();

            var formats = isMainPage ? ViewLocationFormats : PartialViewLocationFormats;
            var areaFormats = isMainPage ? AreaViewLocationFormats : AreaPartialViewLocationFormats;

            var candidates = new List<string>();
            if (!string.IsNullOrEmpty(areaName))
            {
                candidates.AddRange(areaFormats.Select(format => string.Format(CultureInfo.InvariantCulture, format, viewName, controllerName, areaName)));
            }
            candidates.AddRange(formats.Select(format => string.Format(CultureInfo.InvariantCulture, format, viewName, controllerName, areaName)));

            var searchedLocations = new List<string>();
            foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var result = razorViewEngine.GetView(null, candidate, false);
                if (result.Success)
                {
                    return ViewEngineResult.Found(viewName, result.View);
                }
                searchedLocations.Add(candidate);
            }

            return ViewEngineResult.NotFound(viewName, searchedLocations);
        }

        public ViewEngineResult GetView(string executingFilePath, string viewPath, bool isMainPage)
        {
            // The views are resolved from the action context, see FindView.
            return ViewEngineResult.NotFound(viewPath, Enumerable.Empty<string>());
        }

        internal static bool IsPath(string viewName)
        {
            return viewName.StartsWith("~/", StringComparison.Ordinal) || viewName.StartsWith("/", StringComparison.Ordinal);
        }
    }
}
