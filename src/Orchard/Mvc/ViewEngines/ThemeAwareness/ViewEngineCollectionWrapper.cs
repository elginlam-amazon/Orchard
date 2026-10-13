using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace Orchard.Mvc.ViewEngines.ThemeAwareness
{
    public class ViewEngineCollectionWrapper : IViewEngine
    {
        private readonly IEnumerable<IViewEngine> _engines;

        public ViewEngineCollectionWrapper(IEnumerable<IViewEngine> engines)
        {
            _engines = engines.ToArray();
        }

        public ViewEngineResult FindView(ActionContext context, string viewName, bool isMainPage)
        {
            var searchedLocations = Enumerable.Empty<string>();
            foreach (var engine in _engines)
            {
                var result = engine.FindView(context, viewName, isMainPage);
                if (result.Success)
                    return result;
                searchedLocations = searchedLocations.Concat(result.SearchedLocations);
            }
            return ViewEngineResult.NotFound(viewName, searchedLocations.Distinct());
        }

        public ViewEngineResult GetView(string executingFilePath, string viewPath, bool isMainPage)
        {
            var searchedLocations = Enumerable.Empty<string>();
            foreach (var engine in _engines)
            {
                var result = engine.GetView(executingFilePath, viewPath, isMainPage);
                if (result.Success)
                    return result;
                searchedLocations = searchedLocations.Concat(result.SearchedLocations);
            }
            return ViewEngineResult.NotFound(viewPath, searchedLocations.Distinct());
        }
    }
}
