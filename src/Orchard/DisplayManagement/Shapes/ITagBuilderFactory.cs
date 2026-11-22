using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Web;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Orchard.DisplayManagement.Shapes
{
    public interface ITagBuilderFactory : IDependency
    {
        OrchardTagBuilder Create(dynamic shape, string tagName);
    }

    public class OrchardTagBuilder : TagBuilder
    {
        public OrchardTagBuilder(string tagName) : base(tagName) { }

        public IHtmlString StartElement
        {
            get
            {
                TagRenderMode = TagRenderMode.StartTag;
                return new HtmlString(RenderToString());
            }
        }

        public IHtmlString EndElement
        {
            get
            {
                TagRenderMode = TagRenderMode.EndTag;
                return new HtmlString(RenderToString());
            }
        }

        public IHtmlString ToHtmlString(TagRenderMode renderMode = TagRenderMode.Normal)
        {
            TagRenderMode = renderMode;
            return new HtmlString(RenderToString());
        }

        public string ToString(System.Web.Mvc.TagRenderMode renderMode)
            => ToHtmlString((TagRenderMode)(int)renderMode).ToHtmlString();

        private string RenderToString()
        {
            using var sw = new StringWriter();
            WriteTo(sw, HtmlEncoder.Default);
            return sw.ToString();
        }
    }

    public class TagBuilderFactory : ITagBuilderFactory
    {
        public OrchardTagBuilder Create(dynamic shape, string tagName)
        {
            var tagBuilder = new OrchardTagBuilder(tagName);
            tagBuilder.MergeAttributes(shape.Attributes, false);
            foreach (var cssClass in shape.Classes ?? Enumerable.Empty<string>())
                tagBuilder.AddCssClass(cssClass);
            if (!string.IsNullOrEmpty(shape.Id))
                tagBuilder.GenerateId(shape.Id, "_");
            return tagBuilder;
        }
    }
}
