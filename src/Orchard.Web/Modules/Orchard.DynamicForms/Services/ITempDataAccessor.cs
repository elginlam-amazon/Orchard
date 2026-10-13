using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Orchard.DynamicForms.Services
{
    public interface IController
    {
        ITempDataDictionary TempData { get; }
        ModelStateDictionary ModelState { get; }
    }
}