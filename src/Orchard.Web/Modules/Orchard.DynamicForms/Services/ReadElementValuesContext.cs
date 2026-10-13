using System.Collections.Specialized;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Orchard.DynamicForms.Services
{
    public class ReadElementValuesContext
    {
        public ReadElementValuesContext()
        {
            Output = new NameValueCollection();
        }
        public IValueProvider ValueProvider { get; set; }
        public NameValueCollection Output { get; set; }
    }
}