using System;
using System.Linq;
using System.Text.RegularExpressions;
using Orchard.Forms.Services;
using Orchard.Localization;

namespace Orchard.Projections.FilterEditors.Forms
{
    public class DateTimeFilterFormValidation : FormHandler
    {
        public Localizer T { get; set; }

        private static readonly Regex _dateRegEx = new Regex(@"(\d{1,4}(\-\d{1,2}(\-\d{1,2}\s*(\d{1,2}(:\d{1,2}(:\d{1,2})?)?)?)?)?)|(\{.*\})");

        public override void Validating(ValidatingContext context)
        {
            if (context.FormName == DateTimeFilterForm.FormName)
            {

                var op = (DateTimeOperator)Enum.Parse(typeof(DateTimeOperator), Convert.ToString(context.ValueProvider.GetValue("Operator").FirstValue));

                if (op == DateTimeOperator.IsNull || op == DateTimeOperator.IsNotNull)
                {
                    // no further validation needed
                }
                else
                {
                    var isRange = new[] { DateTimeOperator.Between, DateTimeOperator.NotBetween }.Contains(op);
                    var min = context.ValueProvider.GetValue("Min");
                    var max = context.ValueProvider.GetValue("Max");
                    var value = context.ValueProvider.GetValue("Value");
                    var valueType = context.ValueProvider.GetValue("ValueType");

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

                    // validating data type
                    if (valueType.FirstValue == "0")
                    {
                        // A date

                        if (isRange)
                        {
                            if (!_dateRegEx.IsMatch(min.FirstValue) && !IsToken(min.FirstValue))
                            {
                                context.ModelState.AddModelError("Min", T("The field {0} should contain a valid date (YYYY-MM-DD hh:mm:ss)", T("Min").Text).Text);
                            }

                            if (!_dateRegEx.IsMatch(max.FirstValue) && !IsToken(max.FirstValue))
                            {
                                context.ModelState.AddModelError("Max", T("The field {0} should contain a valid date (YYYY-MM-DD hh:mm:ss)", T("Max").Text).Text);
                            }
                        }
                        else
                        {
                            if (!_dateRegEx.IsMatch(value.FirstValue) && !IsToken(value.FirstValue))
                            {
                                context.ModelState.AddModelError("Value", T("The field {0} should contain a valid date (YYYY-MM-DD hh:mm:ss)", T("Value").Text).Text);
                            }
                        }

                    }
                    else
                    {
                        // An offset
                        int number;
                        if (isRange)
                        {
                            if (!int.TryParse(min.FirstValue, out number) && !IsToken(min.FirstValue))
                            {
                                context.ModelState.AddModelError("Min", T("The field {0} must be a valid number.", T("Min").Text).Text);
                            }

                            if (!int.TryParse(max.FirstValue, out number) && !IsToken(max.FirstValue))
                            {
                                context.ModelState.AddModelError("Max", T("The field {0} must be a valid number.", T("Max").Text).Text);
                            }
                        }
                        else
                        {
                            if (!int.TryParse(value.FirstValue, out number) && !IsToken(value.FirstValue))
                            {
                                context.ModelState.AddModelError("Value", T("The field {0} must be a valid number.", T("Value").Text).Text);
                            }

                        }
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
