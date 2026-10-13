using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Orchard.AuditTrail.Helpers
{
    public static class HtmlExtensions
    {
        public static IHtmlContent ContentPartEditLink(this IHtmlHelper html, string contentPartName)
        {
            return html.ActionLink(contentPartName, "EditPart", "Admin", new { id = contentPartName, area = "Orchard.ContentTypes" }, null);
        }

        public static IHtmlContent ContentTypeEditLink(this IHtmlHelper html, string contentTypeName)
        {
            return html.ActionLink(contentTypeName, "Edit", "Admin", new { id = contentTypeName, area = "Orchard.ContentTypes" }, null);
        }

        public static IHtmlContent ItemEditLink(this IHtmlHelper html, string linkText, int contentItemId)
        {
            return html.ActionLink(linkText, "Edit", "Admin", new { id = contentItemId, area = "Contents" }, null);
        }
    }
}