using Orchard.Blogs.Models;
using Orchard.ContentManagement;
using Orchard.Core.Feeds;
using Orchard.Core.Feeds.Models;

namespace Orchard.Blogs.Feeds
{
    public class BlogPartFeedItemBuilder : IFeedItemBuilder
    {
        private IContentManager _contentManager;
        public BlogPartFeedItemBuilder(IContentManager contentManager)
        {
            _contentManager = contentManager;
        }

        public void Populate(FeedContext context)
        {
            var containerIdValue = context.ValueProvider.GetValue("containerid");
            if (containerIdValue == Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderResult.None)
                return;

            var containerId = (int)System.Convert.ChangeType(containerIdValue.FirstValue, typeof(int), System.Globalization.CultureInfo.InvariantCulture);
            var container = _contentManager.Get(containerId);

            if (container == null)
            {
                return;
            }

            if (container.ContentType != "Blog")
            {
                return;
            }

            var blog = container.As<BlogPart>();

            if (context.Format == "rss")
            {
                context.Response.Element.SetElementValue("description", blog.Description);
            }
            else
            {
                context.Builder.AddProperty(context, null, "description", blog.Description);
            }
        }
    }
}