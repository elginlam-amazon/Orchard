using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Orchard.DisplayManagement.Descriptors.ShapeTemplateStrategy;
using Orchard.Logging;

namespace Orchard.Mvc.ViewEngines.Razor
{
    public class RazorViewEngineProvider : IViewEngineProvider, IShapeTemplateViewEngine
    {
        public RazorViewEngineProvider()
        {
            Logger = NullLogger.Instance;
        }

        public ILogger Logger { get; set; }

        public IViewEngine CreateThemeViewEngine(CreateThemeViewEngineParams parameters)
        {
            // Area: if "area" in RouteData. Url hit for module...
            // Area-Layout Paths - no-op because LayoutViewEngine uses multi-pass instead of layout paths
            // Area-View Paths - no-op because LayoutViewEngine relies entirely on Partial view resolution
            // Area-Partial Paths - enable theming views associated with a module based on the route

            // Layout Paths - no-op because LayoutViewEngine uses multi-pass instead of layout paths
            // View Paths - no-op because LayoutViewEngine relies entirely on Partial view resolution
            // Partial Paths - 
            //   {area}/{controller}/

            // for "routed" request views...
            // enable /Views/{area}/{controller}/{viewName}

            // enable /Views/{partialName}
            // enable /Views/"DisplayTemplates/"+{templateName}
            // enable /Views/"EditorTemplates/+{templateName}
            var partialViewLocationFormats = new[] {
                parameters.VirtualPath + "/Views/{0}.cshtml",
                parameters.VirtualPath + "/Views/{1}/{0}.cshtml",
            };

            var areaPartialViewLocationFormats = new[] {
                parameters.VirtualPath + "/Views/{2}/{1}/{0}.cshtml",
            };

            return new LocationFormatsViewEngine
            {
                PartialViewLocationFormats = partialViewLocationFormats,
                AreaPartialViewLocationFormats = areaPartialViewLocationFormats,
            };
        }

        public IViewEngine CreateModulesViewEngine(CreateModulesViewEngineParams parameters)
        {
            //TBD: It would probably be better to determined the area deterministically from the module of the controller, not by trial and error.
            var areaFormats = parameters.ExtensionLocations.Select(location => location + "/{2}/Views/{1}/{0}.cshtml").ToArray();

            var universalFormats = parameters.VirtualPaths
                .SelectMany(x => new[] {
                                           x + "/Views/{0}.cshtml",
                                       })
                .Concat(new[] { "~/Views/{1}/{0}.cshtml", "~/Views/{0}.cshtml" })
                .ToArray();

            return new LocationFormatsViewEngine
            {
                ViewLocationFormats = universalFormats,
                PartialViewLocationFormats = universalFormats,
                AreaViewLocationFormats = areaFormats,
                AreaPartialViewLocationFormats = areaFormats,
            };
        }

        public IViewEngine CreateBareViewEngine()
        {
            // Only resolves fully qualified view paths.
            return new LocationFormatsViewEngine();
        }

        public IEnumerable<string> DetectTemplateFileNames(IEnumerable<string> fileNames)
        {
            return fileNames.Where(fileName => fileName.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase));
        }
    }
}
