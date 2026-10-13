using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Orchard.Environment;

namespace Orchard.Mvc.ViewEngines.ThemeAwareness
{
    /// <summary>
    /// The application wide view engine (registered in the MVC view options) forwarding to the
    /// theme aware view engine of the current tenant work context.
    /// </summary>
    public class ThemeAwareViewEngineShim : IViewEngine, IShim
    {
        public ThemeAwareViewEngineShim()
        {
            OrchardHostContainerRegistry.RegisterShim(this);
        }

        public IOrchardHostContainer HostContainer { get; set; }

        public ViewEngineResult FindView(ActionContext context, string viewName, bool isMainPage)
        {
            return Forward(
                context,
                dve => isMainPage
                    ? dve.FindView(context, viewName, null, true, false /*useDeepPaths*/)
                    : dve.FindPartialView(context, viewName, true, false /*useDeepPaths*/),
                () => EmptyViewEngineResult(viewName));
        }

        public ViewEngineResult GetView(string executingFilePath, string viewPath, bool isMainPage)
        {
            // The views are resolved from the action context, see FindView.
            return EmptyViewEngineResult(viewPath);
        }

        static TResult Forward<TResult>(ActionContext controllerContext, Func<IThemeAwareViewEngine, TResult> forwardAction, Func<TResult> defaultAction)
        {
            var workContext = controllerContext.GetWorkContext();
            if (workContext != null)
            {
                var displayViewEngine = workContext.Resolve<IThemeAwareViewEngine>();
                if (displayViewEngine != null)
                {
                    return forwardAction(displayViewEngine);
                }
            }
            return defaultAction();
        }

        static ViewEngineResult EmptyViewEngineResult(string viewName)
        {
            return ViewEngineResult.NotFound(viewName, Enumerable.Empty<string>());
        }
    }
}
