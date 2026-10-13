using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Orchard.Mvc
{
    public class ShapeResult : ViewResult
    {
        public ShapeResult(object controller, dynamic shape)
        {
            var mvcController = controller as Controller;
            ViewData = mvcController != null ? mvcController.ViewData : new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
            TempData = mvcController?.TempData;
            ViewData.Model = shape;
            ViewName = "ShapeResult/Display";
        }
    }

    public class ShapePartialResult : PartialViewResult
    {
        public ShapePartialResult(object controller, dynamic shape)
        {
            var mvcController = controller as Controller;
            ViewData = mvcController != null ? mvcController.ViewData : new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
            TempData = mvcController?.TempData;
            ViewData.Model = shape;
            ViewName = "ShapeResult/DisplayPartial";
        }
    }
}
