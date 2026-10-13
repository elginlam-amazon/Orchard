using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Orchard.DynamicForms.Services.Models
{
    public class FormValidatingEventContext : FormEventContext
    {
        public ModelStateDictionary ModelState { get; set; }
    }
}