using System.Collections.Specialized;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Orchard.ContentManagement;
using Orchard.DynamicForms.Elements;

namespace Orchard.DynamicForms.Services.Models
{
    public class FormSubmissionTokenContext
    {
        public Form Form { get; set; }
        public ModelStateDictionary ModelState { get; set; }
        public NameValueCollection PostedValues { get; set; }
        public ContentItem CreatedContent { get; set; }
    }
}
