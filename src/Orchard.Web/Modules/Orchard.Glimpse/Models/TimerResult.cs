using System;

namespace Orchard.Glimpse.Models
{
    /// <summary>
    /// The measured time of an action (formerly Glimpse.Core.Extensibility.TimerResult).
    /// </summary>
    public class TimerResult
    {
        /// <summary>
        /// The time elapsed between the beginning of the request and the beginning of the action.
        /// </summary>
        public TimeSpan Offset { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime StartTime { get; set; }
    }

    /// <summary>
    /// A category of the timeline (formerly Glimpse.Core.Message.TimelineCategoryItem).
    /// </summary>
    public class TimelineCategoryItem
    {
        public TimelineCategoryItem(string name, string color, string colorHighlight)
        {
            Name = name;
            Color = color;
            ColorHighlight = colorHighlight;
        }

        public string Name { get; set; }
        public string Color { get; set; }
        public string ColorHighlight { get; set; }

        public override string ToString() => Name;
    }

    /// <summary>
    /// The base class of the diagnostics messages (formerly Glimpse.Core.Message.MessageBase).
    /// </summary>
    public abstract class MessageBase
    {
        protected MessageBase()
        {
            Id = Guid.NewGuid();
        }

        public Guid Id { get; private set; }
    }
}
