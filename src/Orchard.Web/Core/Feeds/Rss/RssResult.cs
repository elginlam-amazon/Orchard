using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;

namespace Orchard.Core.Feeds.Rss
{
    public class RssResult : ActionResult
    {
        public XDocument Document { get; private set; }

        public RssResult(XDocument document)
        {
            Document = document;
        }

        public override async Task ExecuteResultAsync(ActionContext context)
        {

            // not returning application/rss+xml because of
            // https://bugzilla.mozilla.org/show_bug.cgi?id=256379
            context.HttpContext.Response.ContentType = "text/xml";

            using (var writer = XmlWriter.Create(context.HttpContext.Response.Body, new XmlWriterSettings { Async = true }))
            {
                await Document.WriteToAsync(writer, context.HttpContext.RequestAborted);
                await writer.FlushAsync();
            }
        }
    }
}