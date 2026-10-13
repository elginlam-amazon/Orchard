using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Orchard.DisplayManagement;
using Orchard.Logging;
using Orchard.Mvc.Spooling;
using Orchard.Themes;

namespace Orchard.Mvc.ViewEngines.ThemeAwareness
{
    public interface ILayoutAwareViewEngine : IDependency, IViewEngine
    {
    }

    public class LayoutAwareViewEngine : ILayoutAwareViewEngine
    {
        private readonly WorkContext _workContext;
        private readonly IThemeAwareViewEngine _themeAwareViewEngine;
        private readonly IDisplayHelperFactory _displayHelperFactory;

        public LayoutAwareViewEngine(
            WorkContext workContext,
            IThemeAwareViewEngine themeAwareViewEngine,
            IDisplayHelperFactory displayHelperFactory)
        {
            _workContext = workContext;
            _themeAwareViewEngine = themeAwareViewEngine;
            _displayHelperFactory = displayHelperFactory;
            Logger = NullLogger.Instance;
        }

        public ILogger Logger { get; set; }

        public ViewEngineResult FindView(ActionContext context, string viewName, bool isMainPage)
        {
            var viewResult = _themeAwareViewEngine.FindPartialView(context, viewName, true, true);

            // Partial views are not rendered in the layout.
            if (!isMainPage || !viewResult.Success)
            {
                return viewResult;
            }

            if (!ThemeFilter.IsApplied(context))
            {
                return viewResult;
            }

            var layoutView = new LayoutView(viewResult.View, async (viewContext, viewDataContainer) =>
            {
                Logger.Information("Rendering layout view");

                var childContentWriter = new HtmlStringWriter();

                var childContentViewContext = new ViewContext(
                    viewContext,
                    viewResult.View,
                    viewContext.ViewData,
                    childContentWriter);

                await viewResult.View.RenderAsync(childContentViewContext);
                _workContext.Layout.Metadata.ChildContent = childContentWriter;

                var display = _displayHelperFactory.CreateHelper(viewContext, viewDataContainer);
                IHtmlContent result = display(_workContext.Layout);
                result.WriteTo(viewContext.Writer, HtmlEncoder.Default);

                Logger.Information("Done rendering layout view");
            });

            return ViewEngineResult.Found(viewName, layoutView);
        }

        public ViewEngineResult GetView(string executingFilePath, string viewPath, bool isMainPage)
        {
            // The views are resolved from the action context, see FindView.
            return ViewEngineResult.NotFound(viewPath, new string[0]);
        }

        class LayoutView : IView, IViewDataContainer
        {
            private readonly IView _view;
            private readonly System.Func<ViewContext, IViewDataContainer, Task> _render;

            public LayoutView(IView view, System.Func<ViewContext, IViewDataContainer, Task> render)
            {
                _view = view;
                _render = render;
            }

            public string Path => _view.Path;

            public ViewDataDictionary ViewData { get; set; }

            public Task RenderAsync(ViewContext viewContext)
            {
                ViewData = viewContext.ViewData;
                return _render(viewContext, this);
            }
        }
    }
}
