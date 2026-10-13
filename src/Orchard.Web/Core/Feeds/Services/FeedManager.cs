using System.Collections.Generic;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using Orchard.Mvc.Html;

namespace Orchard.Core.Feeds.Services
{
    public class FeedManager : IFeedManager
    {
        private readonly IList<Link> _links = new List<Link>();

        class Link
        {
            public string Title { get; set; }
            public RouteValueDictionary RouteValues { get; set; }
            public string Url { get; set; }
        }

        public void Register(string title, string format, RouteValueDictionary values)
        {
            var link = new RouteValueDictionary(values) { { "format", format } };
            if (!link.ContainsKey("area"))
            {
                link["area"] = "Feeds";
            }
            if (!link.ContainsKey("controller"))
            {
                link["controller"] = "Feed";
            }
            if (!link.ContainsKey("action"))
            {
                link["action"] = "Index";
            }
            _links.Add(new Link { Title = title, RouteValues = link });
        }

        public void Register(string title, string format, string url)
        {
            _links.Add(new Link { Title = title, Url = url });
        }

        public IHtmlContent GetRegisteredLinks(IHtmlHelper html)
        {
            var urlHelper = HtmlHelperFactory.CreateUrlHelper(html.ViewContext);

            var sb = new StringBuilder();
            foreach (var link in _links)
            {
                var linkUrl = string.IsNullOrWhiteSpace(link.Url) ? urlHelper.RouteUrl(link.RouteValues) : link.Url;
                sb.Append("\r\n");
                sb.Append(@"<link rel=""alternate"" type=""application/rss+xml""");
                if (!string.IsNullOrEmpty(link.Title))
                {
                    sb
                        .Append(@" title=""")
                        .Append(HttpUtility.HtmlAttributeEncode(link.Title))
                        .Append(@"""");
                }
                sb.Append(@" href=""")
                    .Append(HttpUtility.HtmlAttributeEncode(linkUrl))
                    .AppendLine(@""" />");
            }

            return new HtmlString(sb.ToString());
        }

        //public ActionResult Execute(string format, IValueProvider values) {

        //}
    }
}
