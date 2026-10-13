using System.Linq;
using Orchard.Forms.Services;
using Orchard.Localization;

namespace Orchard.Projections.FilterEditors.Forms
{
    public class NumericFieldTypeEditorValidation : FormHandler
    {
        public Localizer T { get; set; }

        public override void Validating(ValidatingContext context)
        {
            if (context.FormName == NumericFilterForm.FormName)
            {

                var isRange = new[] { "Between", "NotBetween" }.Contains(context.ValueProvider.GetValue("Operator").FirstValue);
                var min = context.ValueProvider.GetValue("Min");
                var max = context.ValueProvider.GetValue("Max");
                var value = context.ValueProvider.GetValue("Value");

                // validating mandatory values
                if (isRange)
                {
                    if (min == Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderResult.None || string.IsNullOrWhiteSpace(min.FirstValue))
                    {
                        context.ModelState.AddModelError("Min", T("The field {0} is required.", T("Min").Text).Text);
                    }

                    if (max == Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderResult.None || string.IsNullOrWhiteSpace(max.FirstValue))
                    {
                        context.ModelState.AddModelError("Max", T("The field {0} is required.", T("Max").Text).Text);
                    }
                }
                else
                {
                    if (min == Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderResult.None || string.IsNullOrWhiteSpace(value.FirstValue))
                    {
                        context.ModelState.AddModelError("Value", T("The field {0} is required.", T("Value").Text).Text);
                    }
                }

                if (!context.ModelState.IsValid)
                {
                    return;
                }

                decimal output;

                if (isRange)
                {
                    if (!decimal.TryParse(min.FirstValue, out output) && !IsToken(min.FirstValue))
                    {
                        context.ModelState.AddModelError("Min", T("The field {0} should contain a valid number", T("Min").Text).Text);
                    }

                    if (!decimal.TryParse(max.FirstValue, out output) && !IsToken(max.FirstValue))
                    {
                        context.ModelState.AddModelError("Max", T("The field {0} should contain a valid number", T("Max").Text).Text);
                    }
                }
                else
                {
                    if (!decimal.TryParse(value.FirstValue, out output) && !IsToken(value.FirstValue))
                    {
                        context.ModelState.AddModelError("Value", T("The field {0} should contain a valid number", T("Value").Text).Text);
                    }
                }
            }
        }

        private bool IsToken(string value)
        {
            return value.StartsWith("{") && value.EndsWith("}");
        }
    }
}