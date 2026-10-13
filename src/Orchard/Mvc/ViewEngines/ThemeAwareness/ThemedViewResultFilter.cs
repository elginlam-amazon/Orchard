using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Orchard.Logging;
using Orchard.Mvc.Filters;
using Orchard.Themes;

namespace Orchard.Mvc.ViewEngines.ThemeAwareness
{

    public class ThemedViewResultFilter : FilterProvider, IResultFilter
    {
        private readonly IThemeManager _themeManager;
        private readonly WorkContext _workContext;
        private readonly ILayoutAwareViewEngine _layoutAwareViewEngine;

        public ThemedViewResultFilter(IThemeManager themeManager, WorkContext workContext, ILayoutAwareViewEngine layoutAwareViewEngine)
        {
            _themeManager = themeManager;
            _workContext = workContext;
            _layoutAwareViewEngine = layoutAwareViewEngine;
            Logger = NullLogger.Instance;
        }

        public ILogger Logger { get; set; }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            var viewResult = filterContext.Result as ViewResult;
            var partialViewResult = filterContext.Result as PartialViewResult;
            if (viewResult == null && partialViewResult == null)
            {
                return;
            }

            if (_workContext.CurrentTheme == null)
            {
                _workContext.CurrentTheme = _themeManager.GetRequestTheme(filterContext);
            }

            if (viewResult != null)
            {
                viewResult.ViewEngine = _layoutAwareViewEngine;
            }
            else
            {
                partialViewResult.ViewEngine = _layoutAwareViewEngine;
            }
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {

        }
    }
}