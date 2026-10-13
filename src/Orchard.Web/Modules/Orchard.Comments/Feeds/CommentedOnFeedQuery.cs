using System;
using Orchard.Comments.Models;
using Orchard.ContentManagement;
using Orchard.Core.Feeds;
using Orchard.Core.Feeds.Models;

namespace Orchard.Comments.Feeds
{
    public class CommentedOnFeedQuery : IFeedQueryProvider, IFeedQuery
    {
        private readonly IContentManager _contentManager;

        public CommentedOnFeedQuery(IContentManager contentManager)
        {
            _contentManager = contentManager;
        }

        public FeedQueryMatch Match(FeedContext context)
        {
            if (context.ValueProvider.GetValue("commentedon") != Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderResult.None)
            {
                return new FeedQueryMatch { Priority = -1, FeedQuery = this };
            }
            return null;
        }

        public void Execute(FeedContext context)
        {
            var commentedOn = (int)System.Convert.ChangeType(context.ValueProvider.GetValue("commentedon").FirstValue, typeof(int), System.Globalization.CultureInfo.InvariantCulture);

            var limit = 20;
            var limitValue = context.ValueProvider.GetValue("limit");
            if (limitValue != Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderResult.None)
            {
                int.TryParse(Convert.ToString(limitValue), out limit);
            }

            var comments = _contentManager
                .Query<CommentPart, CommentPartRecord>()
                .Where(x => x.CommentsPartRecord.Id == commentedOn && x.Status == CommentStatus.Approved)
                .OrderByDescending(x => x.CommentDateUtc)
                .Slice(0, limit);

            foreach (var comment in comments)
            {
                context.Builder.AddItem(context, comment);
            }
        }
    }
}