using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Orchard.ContentManagement;
using Orchard.DisplayManagement;
using Orchard.Localization;
using Orchard.Mvc.Html;
using Orchard.Utility.Extensions;

namespace Orchard.Comments
{
    public class Shapes : IDependency
    {
        public Shapes()
        {
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        [Shape]
        public void CommentSummaryLinks(dynamic Display, TextWriter Output, IHtmlHelper Html, ContentItem item, int count, int pendingCount)
        {
            var commentText = "";

            if (item.Id != 0)
            {
                var totalCommentCount = count + pendingCount;
                var totalCommentText = T.Plural("1 comment", "{0} comments", totalCommentCount);
                if (totalCommentCount == 0)
                {
                    commentText += totalCommentText.ToString();
                }
                else
                {
                    commentText +=
                        Html.ActionLink(
                            totalCommentText.ToString(),
                            "Details",
                            new
                            {
                                Area = "Orchard.Comments",
                                Controller = "Admin",
                                id = item.Id,
                                returnUrl = Html.ViewContext.HttpContext.Request.GetRawUrl()
                            });
                }

                if (pendingCount > 0)
                {
                    commentText += " " + Html.ActionLink(T("({0} pending)", pendingCount).ToString(),
                                                   "Details",
                                                   new
                                                   {
                                                       Area = "Orchard.Comments",
                                                       Controller = "Admin",
                                                       id = item.Id,
                                                       returnUrl = Html.ViewContext.HttpContext.Request.GetUrl()
                                                   });
                }
            }

            Output.Write(commentText);
        }
    }
}
