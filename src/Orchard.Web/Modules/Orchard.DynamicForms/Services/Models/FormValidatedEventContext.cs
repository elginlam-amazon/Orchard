using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Orchard.DynamicForms.Services.Models
{
    public class FormValidatedEventContext : FormEventContext
    {
        public ModelStateDictionary ModelState { get; set; }
    }
}