using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Orchard.Localization;
using Orchard.Mvc.ViewEngines;
using Orchard.UI.PageClass;
using Orchard.UI.PageTitle;

namespace Orchard.Mvc.Html
{
    public static class LayoutExtensions
    {
        public static HelperResult RenderOrchardBody(this IHtmlHelper html)
        {
            LayoutViewContext layoutViewContext = LayoutViewContext.From(html.ViewContext);
            return new HelperResult(writer => writer.WriteAsync(layoutViewContext.BodyContent));
        }

        public static IHtmlContent Body(this IHtmlHelper html)
        {
            LayoutViewContext layoutViewContext = LayoutViewContext.From(html.ViewContext);

            return new HtmlString(layoutViewContext.BodyContent);
        }

        public static void AddTitleParts(this IHtmlHelper html, params string[] titleParts)
        {
            html.GetWorkContext().Resolve<IPageTitleBuilder>().AddTitleParts(titleParts);
        }

        public static void AppendTitleParts(this IHtmlHelper html, params string[] titleParts)
        {
            html.GetWorkContext().Resolve<IPageTitleBuilder>().AppendTitleParts(titleParts);
        }

        public static IHtmlContent Title(this IHtmlHelper html, params string[] titleParts)
        {
            var pageTitleBuilder = html.GetWorkContext().Resolve<IPageTitleBuilder>();

            html.AddTitleParts(titleParts);

            return new HtmlString(html.Encode(pageTitleBuilder.GenerateTitle()));
        }

        public static IHtmlContent TitleForPage(this IHtmlHelper html, params string[] titleParts)
        {
            if (titleParts == null || titleParts.Length < 1)
                return null;

            html.AppendTitleParts(titleParts);

            return new HtmlString(html.Encode(titleParts[0]));
        }

        public static IHtmlContent TitleForPage(this IHtmlHelper html, params LocalizedString[] titleParts)
        {
            if (titleParts == null || titleParts.Length < 1)
                return null;

            html.AppendTitleParts(titleParts.Select(part => part.ToString()).ToArray());

            return new HtmlString(html.Encode(titleParts[0]));
        }

        public static void AddPageClassNames(this IHtmlHelper html, params object[] classNames)
        {
            html.GetWorkContext().Resolve<IPageClassBuilder>().AddClassNames(classNames);
        }

        public static IHtmlContent ClassForPage(this IHtmlHelper html, params object[] classNames)
        {
            IPageClassBuilder pageClassBuilder = html.GetWorkContext().Resolve<IPageClassBuilder>();

            html.AddPageClassNames(classNames);
            //todo: (heskew) need ContentItem.ContentType
            html.AddPageClassNames(html.ViewContext.RouteData.Values["area"]);

            return new HtmlString(html.Encode(pageClassBuilder.ToString()));
        }
    }
}