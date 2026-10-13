using System;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Orchard.ContentManagement;
using Orchard.Core.Common.Models;
using Orchard.Core.Feeds.Models;
using Orchard.Core.Feeds.StandardBuilders;
using Orchard.Mvc.Extensions;
using Orchard.Mvc.Html;
using Orchard.Services;

namespace Orchard.Core.Feeds.StandardQueries
{
    public class ContainerFeedQuery : IFeedQueryProvider, IFeedQuery
    {
        private readonly IContentManager _contentManager;
        private readonly IHtmlFilterProcessor _htmlFilterProcessor;

        public ContainerFeedQuery(IContentManager contentManager, IHtmlFilterProcessor htmlFilterProcessor)
        {
            _contentManager = contentManager;
            _htmlFilterProcessor = htmlFilterProcessor;
        }

        public FeedQueryMatch Match(FeedContext context)
        {
            var containerIdValue = context.ValueProvider.GetValue("containerid");
            if (containerIdValue == ValueProviderResult.None)
                return null;

            var containerId = Convert.ToInt32(containerIdValue.FirstValue, System.Globalization.CultureInfo.InvariantCulture);
            var container = _contentManager.Get(containerId);

            if (container == null)
            {
                return null;
            }

            return new FeedQueryMatch { FeedQuery = this, Priority = -5 };
        }

        public void Execute(FeedContext context)
        {
            var containerIdValue = context.ValueProvider.GetValue("containerid");
            if (containerIdValue == ValueProviderResult.None)
                return;

            var limitValue = context.ValueProvider.GetValue("limit");
            var limit = 20;
            if (limitValue != ValueProviderResult.None)
            {
                int.TryParse(Convert.ToString(limitValue), out limit);
            }

            limit = Math.Min(limit, 100);

            var containerId = Convert.ToInt32(containerIdValue.FirstValue, System.Globalization.CultureInfo.InvariantCulture);
            var container = _contentManager.Get(containerId);

            if (container == null)
            {
                return;
            }

            var inspector = new ItemInspector(container, _contentManager.GetItemMetadata(container), _htmlFilterProcessor);
            if (context.Format == "rss")
            {
                var link = new XElement("link");
                context.Response.Element.SetElementValue("title", inspector.Title);
                context.Response.Element.Add(link);
                context.Response.Element.SetElementValue("description", inspector.Description);

                context.Response.Contextualize(requestContext =>
                {
                    var urlHelper = HtmlHelperFactory.CreateUrlHelper(requestContext);
                    var uriBuilder = new UriBuilder(urlHelper.MakeAbsolute("/")) { Path = urlHelper.RouteUrl(inspector.Link) };
                    link.Add(uriBuilder.Uri.OriginalString);
                });
            }
            else
            {
                context.Builder.AddProperty(context, null, "title", inspector.Title);
                context.Builder.AddProperty(context, null, "description", inspector.Description);
                context.Response.Contextualize(requestContext =>
                {
                    var urlHelper = HtmlHelperFactory.CreateUrlHelper(requestContext);
                    context.Builder.AddProperty(context, null, "link", urlHelper.MakeAbsolute(urlHelper.RouteUrl(inspector.Link)));
                });
            }

            var items = _contentManager.Query()
                .Where<CommonPartRecord>(x => x.Container == container.Record)
                .OrderByDescending(x => x.CreatedUtc)
                .Slice(0, limit);

            foreach (var item in items)
            {
                context.Builder.AddItem(context, item);
            }
        }
    }
}