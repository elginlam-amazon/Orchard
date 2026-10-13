using Orchard.DynamicForms.Elements;
using Orchard.DynamicForms.Services;

namespace Orchard.DynamicForms.Handlers
{
    public class ReadFormValuesHandler : FormElementEventHandlerBase
    {
        public override void GetElementValue(FormElement element, ReadElementValuesContext context)
        {

            if (string.IsNullOrWhiteSpace(element.Name))
                return;

            var key = element.Name;
            var valueProviderResult = context.ValueProvider.GetValue(key);

            if (string.IsNullOrWhiteSpace(key) || valueProviderResult == Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderResult.None)
                return;

            var items = valueProviderResult.Values.ToArray();

            var combinedValues = string.Join(",", items);
            context.Output[key] = combinedValues;
            element.RuntimeValue = combinedValues;
            element.PostedValue = combinedValues;
        }
    }
}