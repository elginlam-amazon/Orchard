using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Orchard.DisplayManagement.Implementation;
using Orchard.DisplayManagement.Shapes;
using Orchard.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Orchard.Mvc.Html;

namespace Orchard.DisplayManagement
{
    public class ShapeDisplay : IShapeDisplay
    {
        private readonly IDisplayHelperFactory _displayHelperFactory;
        private readonly IWorkContextAccessor _workContextAccessor;
        private readonly HttpContext _httpContextBase;
        private readonly ActionContext _requestContext;

        public ShapeDisplay(
            IDisplayHelperFactory displayHelperFactory,
            IWorkContextAccessor workContextAccessor,
            HttpContext httpContextBase,
            ActionContext requestContext)
        {
            _displayHelperFactory = displayHelperFactory;
            _workContextAccessor = workContextAccessor;
            _httpContextBase = httpContextBase;
            _requestContext = requestContext;
        }

        public string Display(Shape shape)
        {
            return Display((object)shape);
        }

        public string Display(object shape)
        {
            var viewContext = new ViewContext
            {
                HttpContext = _httpContextBase,
                RouteData = _requestContext.RouteData,
                ActionDescriptor = _requestContext.ActionDescriptor
            };
            viewContext.RouteData.DataTokens["IWorkContextAccessor"] = _workContextAccessor;
            var display = _displayHelperFactory.CreateHelper(viewContext, new ViewDataContainer());

            return ((DisplayHelper)display).ShapeExecute(shape).ToHtmlString();
        }

        public IEnumerable<string> Display(IEnumerable<object> shapes)
        {
            return shapes.Select(Display).ToArray();
        }

        private class ViewDataContainer : IViewDataContainer
        {
            public ViewDataDictionary ViewData { get; set; }

            public ViewDataContainer()
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
            }
        }
    }
}