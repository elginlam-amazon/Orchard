using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;

namespace Orchard.Core.Feeds
{
    public interface IFeedManager : IDependency
    {
        void Register(string title, string format, RouteValueDictionary values);
        void Register(string title, string format, string url);
        IHtmlContent GetRegisteredLinks(IHtmlHelper html);

        // Currently implemented in FeedController action... tbd
        //ActionResult Execute(string format, IValueProvider values);
    }
}
