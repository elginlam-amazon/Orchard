using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Orchard.Glimpse.Interceptors;
using Orchard.Glimpse.Models;
using Orchard.Glimpse.Tabs;
using Orchard.Localization;
using Orchard.Logging;
using ILogger = Orchard.Logging.ILogger;
using NullLogger = Orchard.Logging.NullLogger;

namespace Orchard.Glimpse.Services
{
    public class DefaultGlimpseService : IGlimpseService
    {
        /// <summary>
        /// The key of the list of the messages published during the request, in the request items.
        /// </summary>
        public const string MessagesKey = "Orchard.Glimpse.Messages";
        private const string RequestStartKey = "Orchard.Glimpse.RequestStart";

        private readonly IEnumerable<IGlimpseMessageInterceptor> _messageInterceptors;
        private readonly Orchard.Mvc.IHttpContextAccessor _httpContextAccessor;

        public DefaultGlimpseService(IEnumerable<IGlimpseMessageInterceptor> messageInterceptors, Orchard.Mvc.IHttpContextAccessor httpContextAccessor)
        {
            _messageInterceptors = messageInterceptors;
            _httpContextAccessor = httpContextAccessor;
            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public ILogger Logger { get; set; }
        public Localizer T { get; private set; }

        public TimerResult Time(Action action)
        {
            var startTime = DateTime.UtcNow;
            var offset = GetRequestElapsed();
            var stopwatch = Stopwatch.StartNew();

            action();

            return new TimerResult
            {
                StartTime = startTime,
                Offset = offset,
                Duration = stopwatch.Elapsed
            };
        }

        public TimedActionResult<T> Time<T>(Func<T> action)
        {
            var result = default(T);

            var duration = Time(() => { result = action(); });

            return new TimedActionResult<T>
            {
                ActionResult = result,
                TimerResult = duration
            };
        }

        public TimerResult PublishTimedAction(Action action, TimelineCategoryItem category, string eventName, string eventSubText = null)
        {
            var timedResult = Time(action);
            PublishMessage(new TimelineMessage { EventName = eventName, EventCategory = category, EventSubText = eventSubText }.AsTimedMessage(timedResult));

            return timedResult;
        }

        public TimerResult PublishTimedAction<TMessage>(Action action, Func<TMessage> messageFactory, TimelineCategoryItem category, string eventName, string eventSubText = null) where TMessage : class
        {
            var timedResult = PublishTimedAction(action, category, eventName, eventSubText);
            PublishMessage(messageFactory());

            return timedResult;
        }

        public TimerResult PublishTimedAction<TMessage>(Action action, Func<TimerResult, TMessage> messageFactory, TimelineCategoryItem category, string eventName, string eventSubText = null) where TMessage : class
        {
            var timedResult = PublishTimedAction(action, category, eventName, eventSubText);
            PublishMessage(messageFactory(timedResult));

            return timedResult;
        }

        public TimedActionResult<T> PublishTimedAction<T>(Func<T> action, TimelineCategoryItem category, string eventName, string eventSubText = null, Func<T, bool> publishCondition = null)
        {
            var timedResult = Time(action);

            if (publishCondition == null || publishCondition(timedResult.ActionResult))
            {
                PublishMessage(new TimelineMessage { EventName = eventName, EventCategory = category, EventSubText = eventSubText }.AsTimedMessage(timedResult.TimerResult));
            }

            return timedResult;
        }

        public TimedActionResult<T> PublishTimedAction<T>(Func<T> action, TimelineCategoryItem category, Func<T, string> eventNameFactory, Func<T, string> eventSubTextFactory = null, Func<T, bool> publishCondition = null)
        {
            var timedResult = Time(action);

            string eventSubText = null;
            if (eventSubTextFactory != null)
            {
                eventSubText = eventSubTextFactory(timedResult.ActionResult);
            }

            if (publishCondition == null || publishCondition(timedResult.ActionResult))
            {
                PublishMessage(new TimelineMessage { EventName = eventNameFactory(timedResult.ActionResult), EventCategory = category, EventSubText = eventSubText }.AsTimedMessage(timedResult.TimerResult));
            }

            return timedResult;
        }

        public TimedActionResult<T> PublishTimedAction<T, TMessage>(Func<T> action, Func<T, TimerResult, TMessage> messageFactory, TimelineCategoryItem category, string eventName, string eventSubText = null, Func<T, bool> publishCondition = null) where TMessage : class
        {
            var actionResult = PublishTimedAction(action, category, eventName, eventSubText);

            if (publishCondition == null || publishCondition(actionResult.ActionResult))
            {
                PublishMessage(messageFactory(actionResult.ActionResult, actionResult.TimerResult));
            }

            return actionResult;
        }

        public TimedActionResult<T> PublishTimedAction<T, TMessage>(Func<T> action, Func<T, TimerResult, TMessage> messageFactory, TimelineCategoryItem category, Func<T, string> eventNameFactory, Func<T, string> eventSubTextFactory = null, Func<T, bool> publishCondition = null) where TMessage : class
        {
            var actionResult = PublishTimedAction(action, category, eventNameFactory, eventSubTextFactory);

            if (publishCondition == null || publishCondition(actionResult.ActionResult))
            {
                PublishMessage(messageFactory(actionResult.ActionResult, actionResult.TimerResult));
            }

            return actionResult;
        }

        public void PublishMessage<T>(T message) where T : class
        {
            _messageInterceptors.Invoke(i => i.MessageReceived(message), Logger);

            // The Glimpse panel isn't available on ASP.NET Core: the messages are kept in the request items and logged.
            var httpContext = _httpContextAccessor.Current();
            if (httpContext != null)
            {
                if (!(httpContext.Items[MessagesKey] is List<object> messages))
                {
                    httpContext.Items[MessagesKey] = messages = new List<object>();
                }

                messages.Add(message);
            }

            Logger.Debug("{0}: {1}", typeof(T).Name, message);
        }

        private TimeSpan GetRequestElapsed()
        {
            var httpContext = _httpContextAccessor.Current();
            if (httpContext == null)
            {
                return TimeSpan.Zero;
            }

            if (!(httpContext.Items[RequestStartKey] is Stopwatch requestStopwatch))
            {
                httpContext.Items[RequestStartKey] = requestStopwatch = Stopwatch.StartNew();
            }

            return requestStopwatch.Elapsed;
        }
    }
}
