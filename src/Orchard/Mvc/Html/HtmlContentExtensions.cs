using System.IO;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Orchard.Mvc.Html
{
    public static class HtmlContentExtensions
    {
        /// <summary>
        /// Returns the HTML markup of an <see cref="IHtmlContent"/> (the former IHtmlString.ToHtmlString()).
        /// </summary>
        public static string ToHtmlString(this IHtmlContent content)
        {
            if (content == null)
            {
                return null;
            }

            if (content is HtmlString htmlString)
            {
                return htmlString.Value;
            }

            using (var writer = new StringWriter())
            {
                content.WriteTo(writer, HtmlEncoder.Default);
                return writer.ToString();
            }
        }

        /// <summary>
        /// Renders a tag builder with the specified render mode (the former TagBuilder.ToString(TagRenderMode)).
        /// </summary>
        public static string ToString(this TagBuilder tagBuilder, TagRenderMode renderMode)
        {
            var previousRenderMode = tagBuilder.TagRenderMode;
            tagBuilder.TagRenderMode = renderMode;
            try
            {
                return ToHtmlString(tagBuilder);
            }
            finally
            {
                tagBuilder.TagRenderMode = previousRenderMode;
            }
        }
    }
}
