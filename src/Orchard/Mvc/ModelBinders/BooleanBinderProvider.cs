using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Orchard.Mvc.ModelBinders
{
    public class BooleanBinderProvider : IModelBinderProvider, IModelBinder
    {

        public IEnumerable<ModelBinderDescriptor> GetModelBinders()
        {
            return new[] {
                             new ModelBinderDescriptor {
                                                           ModelBinder = this,
                                                           Type = typeof(bool)
                                                       }
                         };
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                return Task.CompletedTask;
            }

            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult == ValueProviderResult.None)
            {
                // this is the case where we are not receiving a possible value for the boolean.
                // Not setting a result is ok here, and will let the downstream method set its own defaults.
                return Task.CompletedTask;
            }

            try
            {
                // for bool, the values may be "true,false" because in the form there may be
                // a checkbox and an hidden input.
                var attemptedValues = valueProviderResult.Values
                    .SelectMany(v => v.Split(new char[] { ',' }));
                var value = attemptedValues
                    .Select(v => Convert.ToBoolean(v))
                    // This Aggregate operation works because the "true" is normally only there when
                    // the checkbox was selected, while the false is always there thanks to the hidden
                    // input.
                    .Aggregate((a, b) => a || b);
                // The steps above don't affect binding booleans from any where other than a form,
                // because those won't give us here a list of possible values to aggregate.
                bindingContext.Result = ModelBindingResult.Success(value);
            }
            catch
            {
                bindingContext.ModelState.AddModelError(bindingContext.ModelName, new FormatException(), bindingContext.ModelMetadata);
                bindingContext.Result = ModelBindingResult.Failed();
            }

            return Task.CompletedTask;
        }
    }
}
