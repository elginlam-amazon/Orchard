using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Orchard.DynamicForms.Elements;

namespace Orchard.DynamicForms.Helpers
{
    internal static class FormControllerExtensions
    {
        // The temp data only supports simple types: the model errors and the values are stored as JSON.

        internal static void TransferFormSubmission(this Controller controller, Form form, NameValueCollection values)
        {
            var errors = controller.ModelState
                .Where(x => x.Value.Errors.Any())
                .ToDictionary(x => x.Key, x => x.Value.Errors.Select(e => e.ErrorMessage).ToArray());
            var postedValues = values.AllKeys
                .Where(key => key != null)
                .ToDictionary(key => key, key => values.GetValues(key));

            controller.TempData[string.Format("Form_ModelState_{0}", form.Name)] = JsonSerializer.Serialize(errors);
            controller.TempData[string.Format("Form_Values_{0}", form.Name)] = JsonSerializer.Serialize(postedValues);
        }

        internal static ModelStateDictionary FetchModelState(this Controller controller, Form form)
        {
            var json = controller.TempData[string.Format("Form_ModelState_{0}", form.Name)] as string;
            if (json == null)
            {
                return null;
            }

            var modelState = new ModelStateDictionary();
            foreach (var entry in JsonSerializer.Deserialize<Dictionary<string, string[]>>(json))
            {
                foreach (var error in entry.Value)
                {
                    modelState.AddModelError(entry.Key, error);
                }
            }

            return modelState;
        }

        internal static NameValueCollection FetchPostedValues(this Controller controller, Form form)
        {
            var values = new NameValueCollection();
            var json = controller.TempData[string.Format("Form_Values_{0}", form.Name)] as string;
            if (json != null)
            {
                foreach (var entry in JsonSerializer.Deserialize<Dictionary<string, string[]>>(json))
                {
                    foreach (var value in entry.Value ?? new string[0])
                    {
                        values.Add(entry.Key, value);
                    }
                }
            }

            return values;
        }

        internal static void ApplyAnyModelErrors(this Controller controller, Form form, ModelStateDictionary modelState)
        {
            var hasErrors = modelState != null && !modelState.IsValid;

            if (hasErrors)
            {
                foreach (var state in modelState)
                {
                    if (state.Value.Errors.Any())
                    {
                        foreach (var error in state.Value.Errors)
                        {
                            controller.ModelState.AddModelError(state.Key, error.ErrorMessage);
                        }
                    }
                }
            }
        }
    }
}