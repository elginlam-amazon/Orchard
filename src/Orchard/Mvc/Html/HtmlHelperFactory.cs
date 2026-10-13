using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace Orchard.Mvc.Html
{
    /// <summary>
    /// Creates HTML and URL helpers outside of a Razor view (formerly "new HtmlHelper(viewContext, viewDataContainer)").
    /// </summary>
    public static class HtmlHelperFactory
    {
        public static IHtmlHelper CreateHtmlHelper(ViewContext viewContext, IViewDataContainer viewDataContainer = null)
        {
            var htmlHelper = viewContext.HttpContext.RequestServices.GetRequiredService<IHtmlHelper>();
            Contextualize(htmlHelper, viewContext, viewDataContainer?.ViewData);
            return htmlHelper;
        }

        public static IHtmlHelper CreateHtmlHelper(ViewContext viewContext, ViewDataDictionary viewData)
        {
            var htmlHelper = viewContext.HttpContext.RequestServices.GetRequiredService<IHtmlHelper>();
            Contextualize(htmlHelper, viewContext, viewData);
            return htmlHelper;
        }

        public static IHtmlHelper<TModel> CreateHtmlHelper<TModel>(ViewContext viewContext, ViewDataDictionary viewData)
        {
            var htmlHelper = viewContext.HttpContext.RequestServices.GetRequiredService<IHtmlHelper<TModel>>();
            Contextualize(htmlHelper, viewContext, viewData);
            return htmlHelper;
        }

        public static IUrlHelper CreateUrlHelper(ActionContext actionContext)
        {
            var urlHelperFactory = actionContext.HttpContext?.RequestServices?.GetService<IUrlHelperFactory>();
            return urlHelperFactory != null ? urlHelperFactory.GetUrlHelper(actionContext) : new UrlHelper(actionContext);
        }

        private static void Contextualize(object htmlHelper, ViewContext viewContext, ViewDataDictionary viewData)
        {
            if (viewData != null && viewData != viewContext.ViewData)
            {
                viewContext = new ViewContext(viewContext, viewContext.View, viewData, viewContext.Writer);
            }

            ((IViewContextAware)htmlHelper).Contextualize(viewContext);
        }
    }
}
