using System;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Orchard.Core.Feeds;
using Orchard.Core.Feeds.Models;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Orchard.Mvc.Extensions;
using Orchard.Mvc.Html;
using Orchard.Tags.Services;

namespace Orchard.Tags.Feeds
{
    [OrchardFeature("Orchard.Tags.Feeds")]
    public class TagFeedQuery : IFeedQueryProvider, IFeedQuery
    {
        private readonly ITagService _tagService;

        public TagFeedQuery(ITagService tagService)
        {
            _tagService = tagService;

            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        public FeedQueryMatch Match(FeedContext context)
        {
            var tagIdValue = context.ValueProvider.GetValue("tag");
            if (tagIdValue == Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderResult.None)
                return null;

            var tagName = (string)System.Convert.ChangeType(tagIdValue.FirstValue, typeof(string), System.Globalization.CultureInfo.InvariantCulture);
            var tag = _tagService.GetTagByName(tagName);

            if (tag == null)
            {
                return null;
            }

            return new FeedQueryMatch { FeedQuery = this, Priority = -5 };
        }

        public void Execute(FeedContext context)
        {
            var tagIdValue = context.ValueProvider.GetValue("tag");
            if (tagIdValue == Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderResult.None)
                return;

            var limitValue = context.ValueProvider.GetValue("limit");
            var limit = 20;
            if (limitValue != Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderResult.None)
            {
                int.TryParse(Convert.ToString(limitValue), out limit);
            }

            limit = Math.Min(limit, 100);

            var tagName = (string)System.Convert.ChangeType(tagIdValue.FirstValue, typeof(string), System.Globalization.CultureInfo.InvariantCulture);
            var tag = _tagService.GetTagByName(tagName);

            if (tag == null)
            {
                return;
            }

            var displayRouteValues = new RouteValueDictionary {
                {"area", "Orchard.Tags"},
                {"controller", "Home"},
                {"action", "Search"},
                {"tagName", tag.TagName}
            };

            if (context.Format == "rss")
            {
                var link = new XElement("link");
                context.Response.Element.SetElementValue("title", tag.TagName);
                context.Response.Element.Add(link);
                context.Response.Element.SetElementValue("description", T("Content tagged with {0}", tag.TagName).ToString());

                context.Response.Contextualize(requestContext =>
                {
                    var urlHelper = HtmlHelperFactory.CreateUrlHelper(requestContext);
                    var uriBuilder = new UriBuilder(urlHelper.MakeAbsolute("/")) { Path = urlHelper.RouteUrl(displayRouteValues) };
                    link.Add(uriBuilder.Uri.OriginalString);
                });
            }
            else
            {
                context.Builder.AddProperty(context, null, "title", tag.TagName);
                context.Builder.AddProperty(context, null, "description", T("Content tagged with {0}", tag.TagName).ToString());
                context.Response.Contextualize(requestContext =>
                {
                    var urlHelper = HtmlHelperFactory.CreateUrlHelper(requestContext);
                    context.Builder.AddProperty(context, null, "link", urlHelper.MakeAbsolute(urlHelper.RouteUrl(displayRouteValues)));
                });
            }

            var items = _tagService.GetTaggedContentItems(tag.Id, 0, limit);

            foreach (var item in items)
            {
                context.Builder.AddItem(context, item.ContentItem);
            }
        }
    }
}