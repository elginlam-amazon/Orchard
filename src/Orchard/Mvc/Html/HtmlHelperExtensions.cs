using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Orchard.Localization;
using Orchard.Utility;
using Orchard.Utility.Extensions;
using Orchard.Mvc.Extensions;

namespace Orchard.Mvc.Html
{
    public static class HtmlHelperExtensions
    {

        public static string NameOf<T>(this IHtmlHelper<T> html, Expression<Action<T>> expression)
        {
            return Reflect.NameOf(html.ViewData.Model, expression);
        }

        public static string NameOf<T, TResult>(this IHtmlHelper<T> html, Expression<Func<T, TResult>> expression)
        {
            return Reflect.NameOf(html.ViewData.Model, expression);
        }

        public static string FieldNameFor<T, TResult>(this IHtmlHelper<T> html, Expression<Func<T, TResult>> expression)
        {
            return html.ViewData.TemplateInfo.GetFullHtmlFieldName(GetExpressionText(html, expression));
        }

        public static string FieldIdFor<T, TResult>(this IHtmlHelper<T> html, Expression<Func<T, TResult>> expression)
        {
            var id = html.GenerateIdFromName(html.ViewData.TemplateInfo.GetFullHtmlFieldName(GetExpressionText(html, expression)));
            // because "[" and "]" aren't replaced with "_" in GetFullHtmlFieldId
            return id.Replace('[', '_').Replace(']', '_');
        }

        public static IHtmlContent LabelFor<TModel, TValue>(this IHtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, LocalizedString labelText)
        {
            return LabelFor(html, expression, labelText.ToString());
        }

        public static IHtmlContent LabelFor<TModel, TValue>(this IHtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, string labelText)
        {
            if (string.IsNullOrEmpty(labelText))
            {
                return HtmlString.Empty;
            }
            var htmlFieldName = GetExpressionText(html, expression);
            var tag = new TagBuilder("label");
            tag.Attributes.Add("for", html.GenerateIdFromName(html.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(htmlFieldName)));
            tag.InnerHtml.Append(labelText);
            return new HtmlString(tag.ToString(TagRenderMode.Normal));
        }

        public static IHtmlContent SelectOption<T>(this IHtmlHelper html, T currentValue, T optionValue, string text)
        {
            return SelectOption(html, optionValue, object.Equals(optionValue, currentValue), text);
        }

        public static IHtmlContent SelectOption<T>(this IHtmlHelper html, T currentValue, T optionValue, string text, object htmlAttributes)
        {
            return SelectOption(html, optionValue, object.Equals(optionValue, currentValue), text, new RouteValueDictionary(HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes)));
        }

        public static IHtmlContent SelectOption<T>(this IHtmlHelper html, T currentValue, T optionValue, string text, RouteValueDictionary htmlAttributes)
        {
            return SelectOption(html, optionValue, object.Equals(optionValue, currentValue), text, htmlAttributes);
        }

        public static IHtmlContent SelectOption(this IHtmlHelper html, object optionValue, bool selected, string text)
        {
            return SelectOption(html, optionValue, selected, text, null);
        }

        public static IHtmlContent SelectOption(this IHtmlHelper html, object optionValue, bool selected, string text, object htmlAttributes)
        {
            return SelectOption(html, optionValue, selected, text, new RouteValueDictionary(HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes)));
        }

        public static IHtmlContent SelectOption(this IHtmlHelper html, object optionValue, bool selected, string text, RouteValueDictionary htmlAttributes)
        {
            var builder = new TagBuilder("option");

            if (optionValue != null)
                builder.MergeAttribute("value", optionValue.ToString());

            if (selected)
                builder.MergeAttribute("selected", "selected");

            builder.InnerHtml.Append(text ?? string.Empty);

            if (htmlAttributes != null)
            {
                builder.MergeAttributes(htmlAttributes);
            }

            return new HtmlString(builder.ToString(TagRenderMode.Normal));
        }

        public static WorkContext GetWorkContext(this IHtmlHelper html)
        {
            var workContext = html.ViewContext.GetWorkContext();

            if (workContext == null)
                throw new ApplicationException("The WorkContext cannot be found for the request");

            return workContext;
        }

        #region UnorderedList

        public static IHtmlContent UnorderedList<T>(this IHtmlHelper htmlHelper, IEnumerable<T> items, Func<T, int, IHtmlContent> generateContent, string cssClass)
        {
            return htmlHelper.UnorderedList(items, generateContent, cssClass, null, null);
        }

        public static IHtmlContent UnorderedList<T>(this IHtmlHelper htmlHelper, IEnumerable<T> items, Func<T, int, IHtmlContent> generateContent, string cssClass, string itemCssClass, string alternatingItemCssClass)
        {
            return UnorderedList(items, generateContent, cssClass, t => itemCssClass, t => alternatingItemCssClass);
        }

        private static IHtmlContent UnorderedList<T>(IEnumerable<T> items, Func<T, int, IHtmlContent> generateContent, string cssClass, Func<T, string> generateItemCssClass, Func<T, string> generateAlternatingItemCssClass)
        {
            if (items == null)
            {
                return new HtmlString(string.Empty);
            }

            // prevent multiple evaluations of the enumeration
            items = items.ToArray();

            if (!items.Any())
            {
                return new HtmlString(string.Empty);
            }

            var sb = new StringBuilder(250);
            int counter = 0, count = items.Count() - 1;

            if (string.IsNullOrEmpty(cssClass))
            {
                sb.Append("<ul>");
            }
            else
            {
                sb.Append("<ul class=\"")
                  .Append(cssClass)
                  .Append("\">");
            }

            foreach (var item in items)
            {
                var sbClass = new StringBuilder(50);

                if (counter == 0)
                {
                    sbClass.Append("first ");
                }

                if (counter == count)
                {
                    sbClass.Append("last ");
                }

                if (generateItemCssClass != null)
                {
                    sbClass.Append(generateItemCssClass(item)).Append(" ");
                }

                if (counter % 2 != 0 && generateAlternatingItemCssClass != null)
                {
                    sbClass.Append(generateAlternatingItemCssClass(item)).Append(" ");
                }

                var clss = sbClass.ToString().TrimEnd();

                if (string.IsNullOrWhiteSpace(clss))
                {
                    sb.Append("<li>")
                      .Append(generateContent(item, counter))
                      .Append("</li>");
                }
                else
                {
                    sb.Append("<li class=\"")
                      .Append(clss)
                      .Append("\">")
                      .Append(generateContent(item, counter))
                      .Append("</li>");
                }

                counter++;
            }

            sb.Append("</ul>");

            return new HtmlString(sb.ToString());
        }

        #endregion

        #region Ellipsize

        public static IHtmlContent Ellipsize(this IHtmlHelper htmlHelper, string text, int characterCount)
        {
            return new HtmlString(htmlHelper.Encode(text.Ellipsize(characterCount)));
        }

        public static IHtmlContent Ellipsize(this IHtmlHelper htmlHelper, string text, int characterCount, string ellipsis)
        {
            return new HtmlString(htmlHelper.Encode(text.Ellipsize(characterCount, ellipsis)));
        }

        #endregion

        #region Excerpt

        public static IHtmlContent Excerpt(this IHtmlHelper html, string markup, int length)
        {
            return new HtmlString(html.Encode(HttpUtility.HtmlDecode(markup.RemoveTags()).Ellipsize(length)));
        }

        #endregion

        #region Image

        public static IHtmlContent Image(this IHtmlHelper htmlHelper, string src, string alt, object htmlAttributes)
        {
            return htmlHelper.Image(src, alt, new RouteValueDictionary(HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes)));
        }

        public static IHtmlContent Image(this IHtmlHelper htmlHelper, string src, string alt, IDictionary<string, object> htmlAttributes)
        {
            IUrlHelper url = htmlHelper.ViewContext.HttpContext.RequestServices.GetRequiredService<IUrlHelperFactory>().GetUrlHelper(htmlHelper.ViewContext);
            string imageUrl = url.Content(src);
            TagBuilder imageTag = new TagBuilder("img");

            if (!string.IsNullOrEmpty(imageUrl))
                imageTag.MergeAttribute("src", imageUrl);

            if (!string.IsNullOrEmpty(alt))
                imageTag.MergeAttribute("alt", alt);

            imageTag.MergeAttributes(htmlAttributes, true);

            if (imageTag.Attributes.ContainsKey("alt") && !imageTag.Attributes.ContainsKey("title"))
                imageTag.MergeAttribute("title", imageTag.Attributes["alt"] ?? "");

            return new HtmlString(imageTag.ToString(TagRenderMode.SelfClosing));
        }

        #endregion

        #region Link

        public static IHtmlContent Link(this IHtmlHelper htmlHelper, string linkContents, string href)
        {
            return htmlHelper.Link(linkContents, href, null);
        }

        public static IHtmlContent Link(this IHtmlHelper htmlHelper, IHtmlContent linkContents, string href)
        {
            return htmlHelper.Link(linkContents, href, null);
        }

        public static IHtmlContent Link(this IHtmlHelper htmlHelper, string linkContents, string href, object htmlAttributes)
        {
            return htmlHelper.Link(linkContents, href, new RouteValueDictionary(HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes)));
        }

        public static IHtmlContent Link(this IHtmlHelper htmlHelper, string linkContents, string href, IDictionary<string, object> htmlAttributes)
        {
            var tagBuilder = new TagBuilder("a");
            tagBuilder.InnerHtml.Append(linkContents ?? string.Empty);
            tagBuilder.MergeAttributes(htmlAttributes);
            tagBuilder.MergeAttribute("href", href);
            return new HtmlString(tagBuilder.ToString(TagRenderMode.Normal));
        }

        public static IHtmlContent Link(this IHtmlHelper htmlHelper, IHtmlContent linkContents, string href, IDictionary<string, object> htmlAttributes)
        {
            var tagBuilder = new TagBuilder("a");
            tagBuilder.InnerHtml.AppendHtml(linkContents);
            tagBuilder.MergeAttributes(htmlAttributes);
            tagBuilder.MergeAttribute("href", href);
            return new HtmlString(tagBuilder.ToString(TagRenderMode.Normal));
        }

        #endregion

        #region LinkOrDefault

        public static IHtmlContent LinkOrDefault(this IHtmlHelper htmlHelper, string linkContents, string href)
        {
            return htmlHelper.LinkOrDefault(linkContents, href, null);
        }

        public static IHtmlContent LinkOrDefault(this IHtmlHelper htmlHelper, string linkContents, string href, object htmlAttributes)
        {
            return htmlHelper.LinkOrDefault(linkContents, href, new RouteValueDictionary(HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes)));
        }

        public static IHtmlContent LinkOrDefault(this IHtmlHelper htmlHelper, string linkContents, string href, IDictionary<string, object> htmlAttributes)
        {
            string linkText = htmlHelper.Encode(linkContents);

            if (string.IsNullOrEmpty(href))
            {
                return new HtmlString(linkText);
            }

            TagBuilder tagBuilder = new TagBuilder("a");
            tagBuilder.InnerHtml.AppendHtml(linkText);
            tagBuilder.MergeAttributes(htmlAttributes);
            tagBuilder.MergeAttribute("href", href);
            return new HtmlString(tagBuilder.ToString(TagRenderMode.Normal));
        }

        #endregion

        #region Hint
        public static IHtmlContent Hint(this IHtmlHelper htmlHelper, LocalizedString text)
        {
            return Hint(htmlHelper, text, default(object));
        }

        public static IHtmlContent Hint(this IHtmlHelper htmlHelper, LocalizedString text, object htmlAttributes)
        {
            return Hint(htmlHelper, text, htmlAttributes != null ? new RouteValueDictionary(HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes)) : null);
        }

        public static IHtmlContent Hint(this IHtmlHelper htmlHelper, LocalizedString text, IDictionary<string, object> htmlAttributes)
        {
            var tagBuilder = new TagBuilder("span");
            tagBuilder.InnerHtml.AppendHtml(text.Text);

            if (htmlAttributes != null)
            {
                tagBuilder.MergeAttributes(htmlAttributes);
            }

            tagBuilder.AddCssClass("hint");
            return new HtmlString(tagBuilder.ToString(TagRenderMode.Normal));
        }
        #endregion


        #region BeginFormAntiForgeryPost

        public static MvcForm BeginFormAntiForgeryPost(this IHtmlHelper htmlHelper)
        {
            return htmlHelper.BeginFormAntiForgeryPost(htmlHelper.ViewContext.HttpContext.Request.GetRawUrl(), FormMethod.Post, new RouteValueDictionary());
        }

        public static MvcForm BeginFormAntiForgeryPost(this IHtmlHelper htmlHelper, string formAction)
        {
            return htmlHelper.BeginFormAntiForgeryPost(formAction, FormMethod.Post, new RouteValueDictionary());
        }

        public static MvcForm BeginFormAntiForgeryPost(this IHtmlHelper htmlHelper, string formAction, FormMethod formMethod)
        {
            return htmlHelper.BeginFormAntiForgeryPost(formAction, formMethod, new RouteValueDictionary());
        }

        public static MvcForm BeginFormAntiForgeryPost(this IHtmlHelper htmlHelper, string formAction, FormMethod formMethod, object htmlAttributes)
        {
            return htmlHelper.BeginFormAntiForgeryPost(formAction, formMethod, new RouteValueDictionary(HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes)));
        }

        public static MvcForm BeginFormAntiForgeryPost(this IHtmlHelper htmlHelper, string formAction, FormMethod formMethod, IDictionary<string, object> htmlAttributes)
        {
            // Force the browser not to cache protected forms, and to reload them if needed.
            var response = htmlHelper.ViewContext.HttpContext.Response;
            response.DisableClientCaching();

            var tagBuilder = new TagBuilder("form");

            tagBuilder.MergeAttributes(htmlAttributes);
            tagBuilder.MergeAttribute("action", formAction);
            tagBuilder.MergeAttribute("method", formMethod.ToString().ToLowerInvariant(), true);

            htmlHelper.ViewContext.Writer.Write(tagBuilder.ToString(TagRenderMode.StartTag));

            return new MvcFormAntiForgeryPost(htmlHelper);
        }
        #endregion

        #region AntiForgeryTokenOrchard

        public static IHtmlContent AntiForgeryTokenOrchard(this IHtmlHelper htmlHelper)
        {
            // An invalid antiforgery cookie (e.g. sent after a configuration change) is replaced by a new one.
            return htmlHelper.AntiForgeryToken();
        }

        #endregion

        #region AntiForgeryTokenValueOrchardLink

        public static IHtmlContent AntiForgeryTokenValueOrchardLink(this IHtmlHelper htmlHelper, string linkContents, string href)
        {
            return htmlHelper.AntiForgeryTokenValueOrchardLink(linkContents, href, (object)null);
        }

        public static IHtmlContent AntiForgeryTokenValueOrchardLink(this IHtmlHelper htmlHelper, string linkContents, string href, object htmlAttributes)
        {
            return htmlHelper.AntiForgeryTokenValueOrchardLink(linkContents, href, new RouteValueDictionary(HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes)));
        }

        public static IHtmlContent AntiForgeryTokenValueOrchardLink(this IHtmlHelper htmlHelper, string linkContents, string href, IDictionary<string, object> htmlAttributes)
        {
            return htmlHelper.Link(linkContents, htmlHelper.AntiForgeryTokenGetUrl(href).ToString(), htmlAttributes);
        }

        #endregion

        #region AntiForgeryTokenGetUrl

        public static IHtmlContent AntiForgeryTokenGetUrl(this IHtmlHelper htmlHelper, string baseUrl)
        {
            return new HtmlString(string.Format("{0}{1}__RequestVerificationToken={2}", baseUrl, baseUrl.IndexOf('?') > -1 ? "&" : "?", HttpUtility.UrlEncode(htmlHelper.AntiForgeryTokenValueOrchard().ToHtmlString())));
        }

        #endregion

        #region AntiForgeryTokenValueOrchard

        public static IHtmlContent AntiForgeryTokenValueOrchard(this IHtmlHelper htmlHelper)
        {
            var antiforgery = htmlHelper.ViewContext.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
            var tokens = antiforgery.GetAndStoreTokens(htmlHelper.ViewContext.HttpContext);

            return new HtmlString(tokens.RequestToken);
        }

        #endregion

        #region HtmlHelperFor
        // Credit: Max Toro http://maxtoroq.github.io/2012/07/patterns-for-aspnet-mvc-plugins-viewmodels.html
        public static IHtmlHelper<TModel> HtmlHelperFor<TModel>(this IHtmlHelper htmlHelper)
        {
            return HtmlHelperFor(htmlHelper, default(TModel));
        }

        public static IHtmlHelper<TModel> HtmlHelperFor<TModel>(this IHtmlHelper htmlHelper, TModel model)
        {
            return HtmlHelperFor(htmlHelper, model, null);
        }

        public static IHtmlHelper<TModel> HtmlHelperFor<TModel>(this IHtmlHelper htmlHelper, TModel model, string htmlFieldPrefix)
        {
            var newViewData = new ViewDataDictionary<TModel>(htmlHelper.ViewData, model);

            var templateInfo = newViewData.TemplateInfo;

            if (!string.IsNullOrEmpty(htmlFieldPrefix))
                templateInfo.HtmlFieldPrefix = templateInfo.GetFullHtmlFieldName(htmlFieldPrefix);

            var viewContext = htmlHelper.ViewContext;
            var newViewContext = new ViewContext(
                viewContext,
                viewContext.View,
                newViewData,
                viewContext.Writer);

            var newHtmlHelper = viewContext.HttpContext.RequestServices.GetRequiredService<IHtmlHelper<TModel>>();
            ((IViewContextAware)newHtmlHelper).Contextualize(newViewContext);
            return newHtmlHelper;
        }

        private static string GetExpressionText<TModel, TResult>(IHtmlHelper<TModel> html, Expression<Func<TModel, TResult>> expression)
        {
            var expressionProvider = html.ViewContext.HttpContext.RequestServices.GetRequiredService<ModelExpressionProvider>();
            return expressionProvider.GetExpressionText(expression);
        }
        #endregion
    }
}
