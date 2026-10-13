using System.Linq;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Orchard.Mvc.Html;

namespace Orchard.DisplayManagement.Shapes
{
    public interface ITagBuilderFactory : IDependency
    {
        OrchardTagBuilder Create(dynamic shape, string tagName);
    }

    public class OrchardTagBuilder : TagBuilder
    {
        public OrchardTagBuilder(string tagName) : base(tagName) { }

        public IHtmlContent StartElement => new HtmlString(this.ToString(TagRenderMode.StartTag));
        public IHtmlContent EndElement => new HtmlString(this.ToString(TagRenderMode.EndTag));
        public IHtmlContent ToHtmlString(TagRenderMode renderMode = TagRenderMode.Normal)
        {
            return new HtmlString(this.ToString(renderMode));
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
                tagBuilder.GenerateId((string)shape.Id, "_");
            return tagBuilder;
        }
    }
}
