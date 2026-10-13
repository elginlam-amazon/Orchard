using System;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Orchard.ContentManagement;
using Orchard.Utility.Extensions;

namespace Orchard.Mvc.Html
{
    public static class ContentItemExtensions
    {

        public static IHtmlContent ItemDisplayText(this IHtmlHelper html, IContent content)
        {
            return ItemDisplayText(html, content, true);
        }

        public static IHtmlContent ItemDisplayText(this IHtmlHelper html, IContent content, bool encode)
        {
            var metadata = content.ContentItem.ContentManager.GetItemMetadata(content);
            if (metadata.DisplayText == null)
                return null;
            if (encode)
            {
                return new HtmlString(html.Encode(metadata.DisplayText));
            }
            else
            {
                return new HtmlString(metadata.DisplayText);
            }
        }

        public static IHtmlContent ItemDisplayLink(this IHtmlHelper html, IContent content)
        {
            return ItemDisplayLink(html, null, content, null);
        }

        public static IHtmlContent ItemDisplayLink(this IHtmlHelper html, IContent content, object htmlAttributes)
        {
            return ItemDisplayLink(html, null, content, htmlAttributes);
        }

        public static IHtmlContent ItemDisplayLink(this IHtmlHelper html, string linkText, IContent content)
        {
            return ItemDisplayLink(html, linkText, content, null);
        }

        public static IHtmlContent ItemDisplayLink(this IHtmlHelper html, string linkText, IContent content, object htmlAttributes = null)
        {
            var metadata = content.ContentItem.ContentManager.GetItemMetadata(content);
            if (metadata.DisplayRouteValues == null)
                return null;

            return html.ActionLink(
                NonNullOrEmpty(linkText, metadata.DisplayText, "view"),
                Convert.ToString(metadata.DisplayRouteValues["action"]),
                metadata.DisplayRouteValues,
                HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static string ItemDisplayUrl(this IUrlHelper urlHelper, IContent content)
        {
            var metadata = content.ContentItem.ContentManager.GetItemMetadata(content);
            if (metadata.DisplayRouteValues == null)
                return null;

            return urlHelper.Action(
                Convert.ToString(metadata.DisplayRouteValues["action"]),
                metadata.DisplayRouteValues);
        }

        public static IHtmlContent ItemRemoveLink(this IHtmlHelper html, IContent content)
        {
            return ItemRemoveLink(html, null, content, null);
        }

        public static IHtmlContent ItemRemoveLink(this IHtmlHelper html, string linkText, IContent content, object additionalRouteValues)
        {
            var metadata = content.ContentItem.ContentManager.GetItemMetadata(content);
            if (metadata.RemoveRouteValues == null)
                return null;

            return html.ActionLink(
                NonNullOrEmpty(linkText, metadata.DisplayText, "remove"),
                Convert.ToString(metadata.RemoveRouteValues["action"]),
                metadata.RemoveRouteValues.Merge(additionalRouteValues));
        }

        public static string ItemRemoveUrl(this IUrlHelper urlHelper, IContent content, object additionalRouteValues)
        {
            var metadata = content.ContentItem.ContentManager.GetItemMetadata(content);
            if (metadata.RemoveRouteValues == null)
                return null;

            return urlHelper.Action(
                Convert.ToString(metadata.RemoveRouteValues["action"]),
                metadata.RemoveRouteValues.Merge(additionalRouteValues));
        }

        public static IHtmlContent ItemEditLinkWithReturnUrl(this IHtmlHelper html, string linkText, IContent content)
        {
            return html.ItemEditLink(linkText, content, new { ReturnUrl = html.ViewContext.HttpContext.Request.GetRawUrl() });
        }

        public static IHtmlContent ItemEditLink(this IHtmlHelper html, string linkText, IContent content)
        {
            return html.ItemEditLink(linkText, content, null);
        }

        public static IHtmlContent ItemEditLink(this IHtmlHelper html, string linkText, IContent content, object additionalRouteValues)
        {
            var metadata = content.ContentItem.ContentManager.GetItemMetadata(content);
            if (metadata.EditorRouteValues == null)
                return null;

            return html.ActionLink(
                NonNullOrEmpty(linkText, metadata.DisplayText, content.ContentItem.TypeDefinition.DisplayName),
                Convert.ToString(metadata.EditorRouteValues["action"]),
                metadata.EditorRouteValues.Merge(additionalRouteValues));
        }

        public static IHtmlContent ItemEditLink(this IHtmlHelper html, string linkText, IContent content, object additionalRouteValues, object htmlAttributes = null)
        {
            var metadata = content.ContentItem.ContentManager.GetItemMetadata(content);
            if (metadata.EditorRouteValues == null)
                return null;

            return html.ActionLink(
                NonNullOrEmpty(linkText, metadata.DisplayText, content.ContentItem.TypeDefinition.DisplayName),
                Convert.ToString(metadata.EditorRouteValues["action"]),
                metadata.EditorRouteValues.Merge(additionalRouteValues),
                HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static IHtmlContent ItemAdminLink(this IHtmlHelper html, IContent content)
        {
            return ItemAdminLink(html, null, content);
        }

        public static IHtmlContent ItemAdminLink(this IHtmlHelper html, string linkText, IContent content)
        {
            return html.ItemAdminLink(linkText, content, null);
        }

        public static IHtmlContent ItemAdminLink(this IHtmlHelper html, string linkText, IContent content, object additionalRouteValues)
        {
            var metadata = content.ContentItem.ContentManager.GetItemMetadata(content);
            if (metadata.AdminRouteValues == null)
                return null;

            return html.ActionLink(
                NonNullOrEmpty(linkText, metadata.DisplayText, content.ContentItem.TypeDefinition.DisplayName),
                Convert.ToString(metadata.AdminRouteValues["action"]),
                metadata.AdminRouteValues.Merge(additionalRouteValues));
        }

        public static string ItemEditUrl(this IUrlHelper urlHelper, IContent content, object additionalRouteValues = null)
        {
            var metadata = content.ContentItem.ContentManager.GetItemMetadata(content);
            if (metadata.EditorRouteValues == null)
                return null;

            return urlHelper.Action(
                Convert.ToString(metadata.EditorRouteValues["action"]),
                metadata.EditorRouteValues.Merge(additionalRouteValues ?? new { }));
        }

        public static string ItemAdminUrl(this IUrlHelper urlHelper, IContent content, object additionalRouteValues = null)
        {
            var metadata = content.ContentItem.ContentManager.GetItemMetadata(content);
            return metadata.AdminRouteValues == null ? null : urlHelper.RouteUrl(metadata.AdminRouteValues.Merge(additionalRouteValues ?? new { }));
        }

        private static string NonNullOrEmpty(params string[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrEmpty(value))
                    return value;
            }
            return null;
        }

        public static IHtmlContent ItemEditLink(this IHtmlHelper html, IContent content)
        {
            return ItemEditLink(html, null, content);
        }
    }
}
