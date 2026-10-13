using System.Collections.Generic;
using Orchard.Localization;
using Orchard.Mvc.Extensions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Orchard.Users.Extensions
{
    public static class ModelStateDictionaryExtensions
    {
        public static void AddModelErrors(this ModelStateDictionary modelStateDictionary, IDictionary<string, LocalizedString> validationErrors)
        {
            foreach (var error in validationErrors)
            {
                modelStateDictionary.AddModelError(error.Key, error.Value);
            }
        }
    }
}
