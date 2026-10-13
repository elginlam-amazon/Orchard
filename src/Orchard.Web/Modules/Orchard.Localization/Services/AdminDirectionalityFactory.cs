using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Orchard.ContentManagement;
using Orchard.DisplayManagement.Implementation;
using Orchard.Environment.Extensions;
using Orchard.UI.Admin;

namespace Orchard.Localization.Services
{
    [OrchardFeature("Orchard.Localization.CultureSelector")]
    public class AdminDirectionalityFactory : ShapeDisplayEvents
    {
        private readonly WorkContext _workContext;

        public AdminDirectionalityFactory(
            IWorkContextAccessor workContextAccessor)
        {
            _workContext = workContextAccessor.GetContext();
        }


        private bool IsActivable()
        {
            // activate on admin screen only
            if (AdminFilter.IsApplied(new ActionContext(_workContext.HttpContext, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor())))
                return true;

            return false;
        }

        public override void Displaying(ShapeDisplayingContext context)
        {
            context.ShapeMetadata.OnDisplaying(displayedContext =>
            {
                if (!IsActivable())
                {
                    return;
                }

                if (context.ShapeMetadata.Type != "EditorTemplate" &&
                    context.ShapeMetadata.Type != "Zone")
                    return;

                ContentItem contentItem = context.Shape.ContentItem;

                // if not, check for ContentPart 
                if (contentItem == null)
                {
                    ContentPart contentPart = context.Shape.ContentPart;
                    if (contentPart != null)
                    {
                        contentItem = contentPart.ContentItem;
                    }
                }

                var className = "content-" + _workContext.GetTextDirection(contentItem);

                if (!_workContext.Layout.Content.Classes.Contains(className))
                    _workContext.Layout.Content.Classes.Add(className);
            });
        }


    }
}