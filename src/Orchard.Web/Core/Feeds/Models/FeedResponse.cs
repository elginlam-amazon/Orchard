using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;

namespace Orchard.Core.Feeds.Models
{
    public class FeedResponse
    {
        public FeedResponse()
        {
            Items = new List<FeedItem>();
            Contextualizers = new List<Action<ActionContext>>();
        }

        public IList<FeedItem> Items { get; set; }
        public XElement Element { get; set; }
        public IList<Action<ActionContext>> Contextualizers { get; set; }

        public void Contextualize(Action<ActionContext> contextualizer)
        {
            Contextualizers.Add(contextualizer);
        }
    }
}
