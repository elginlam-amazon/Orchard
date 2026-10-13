using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Orchard.Core.Feeds.Models;
using Orchard.Logging;

namespace Orchard.Core.Feeds.Controllers
{
    public class FeedController : Controller
    {
        private readonly IEnumerable<IFeedBuilderProvider> _feedFormatProviders;
        private readonly IEnumerable<IFeedQueryProvider> _feedQueryProviders;
        private readonly IFeedItemBuilder _feedItemBuilder;

        public FeedController(
            IEnumerable<IFeedQueryProvider> feedQueryProviders,
            IEnumerable<IFeedBuilderProvider> feedFormatProviders,
            IFeedItemBuilder feedItemBuilder)
        {
            _feedQueryProviders = feedQueryProviders;
            _feedFormatProviders = feedFormatProviders;
            _feedItemBuilder = feedItemBuilder;
            Logger = NullLogger.Instance;
        }

        public ILogger Logger { get; set; }

        private IValueProvider _valueProvider;

        /// <summary>
        /// The value provider of the request (the MVC Controller.ValueProvider), can be replaced for testing purposes.
        /// </summary>
        public IValueProvider ValueProvider
        {
            get { return _valueProvider ?? (_valueProvider = CompositeValueProvider.CreateAsync(ControllerContext).GetAwaiter().GetResult()); }
            set { _valueProvider = value; }
        }

        public ActionResult Index(string format)
        {
            var context = new FeedContext(ValueProvider, format);

            var bestFormatterMatch = _feedFormatProviders
                .Select(provider => provider.Match(context))
                .Where(match => match != null && match.FeedBuilder != null)
                .OrderByDescending(match => match.Priority)
                .FirstOrDefault();

            if (bestFormatterMatch == null || bestFormatterMatch.FeedBuilder == null)
                return NotFound();

            context.Builder = bestFormatterMatch.FeedBuilder;

            var bestQueryMatch = _feedQueryProviders
                .Select(provider => provider.Match(context))
                .Where(match => match != null && match.FeedQuery != null)
                .OrderByDescending(match => match.Priority)
                .FirstOrDefault();

            if (bestQueryMatch == null || bestQueryMatch.FeedQuery == null)
                return NotFound();

            return context.Builder.Process(context, () =>
            {
                bestQueryMatch.FeedQuery.Execute(context);
                _feedItemBuilder.Populate(context);
                foreach (var contextualizer in context.Response.Contextualizers)
                {
                    // only within a request (formerly ControllerContext.RequestContext != null)
                    if (ControllerContext?.HttpContext != null)
                    {
                        contextualizer(ControllerContext);
                    }
                }
            });
        }
    }
}
