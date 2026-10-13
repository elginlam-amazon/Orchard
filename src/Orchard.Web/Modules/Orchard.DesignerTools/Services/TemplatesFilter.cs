using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Orchard.DisplayManagement;
using Orchard.Mvc.Filters;
using Orchard.Security;
using Orchard.UI.Admin;
using Orchard.Utility.Extensions;

namespace Orchard.DesignerTools.Services
{
    public class TemplatesFilter : FilterProvider, IResultFilter
    {
        private readonly WorkContext _workContext;
        private readonly IAuthorizer _authorizer;
        private readonly dynamic _shapeFactory;

        public TemplatesFilter(
            WorkContext workContext,
            IAuthorizer authorizer,
            IShapeFactory shapeFactory)
        {
            _workContext = workContext;
            _authorizer = authorizer;
            _shapeFactory = shapeFactory;
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            // should only run on a full view rendering result
            if (!(filterContext.Result is ViewResult))
                return;

            if (!IsActivable())
            {
                return;
            }

            var tail = _workContext.Layout.Tail;
            tail.Add(_shapeFactory.ShapeTracingTemplates());
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
        }

        private bool IsActivable()
        {
            // activate on front-end only
            if (AdminFilter.IsApplied(new ActionContext(_workContext.HttpContext, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor())))
                return false;

            // if not logged as a site owner, still activate if it's a local request (development machine)
            if (!_authorizer.Authorize(StandardPermissions.SiteOwner))
                return _workContext.HttpContext.Request.IsLocal();

            return true;
        }
    }
}