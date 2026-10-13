using System;
using Orchard.Glimpse.Models;

namespace Orchard.Glimpse.Tabs
{
    public class TimelineMessage : MessageBase, IDurationMessage
    {
        public TimeSpan Offset { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime StartTime { get; set; }
        public string EventName { get; set; }
        public TimelineCategoryItem EventCategory { get; set; }
        public string EventSubText { get; set; }

        public TimelineMessage AsTimedMessage(TimerResult timerResult)
        {
            Offset = timerResult.Offset;
            Duration = timerResult.Duration;
            StartTime = timerResult.StartTime;
            return this;
        }

        public override string ToString()
        {
            return $"{EventCategory}: {EventName} {EventSubText} ({Duration.TotalMilliseconds:0.00} ms at {Offset.TotalMilliseconds:0.00} ms)";
        }
    }
}
