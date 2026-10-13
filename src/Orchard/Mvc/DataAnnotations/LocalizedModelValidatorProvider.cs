using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Orchard.Localization;

namespace Orchard.Mvc.DataAnnotations
{
    /// <summary>
    /// Substitutes the data annotation validation attributes with their localized versions, and localizes the display names.
    /// It is registered in the MVC options model metadata details providers.
    /// </summary>
    /// <remarks>
    /// The model metadata is application wide and cached, so the localizers resolve the work context of the current request
    /// each time a message is localized.
    /// </remarks>
    public class LocalizedModelValidatorProvider : IValidationMetadataProvider, IDisplayMetadataProvider
    {
        private static readonly Microsoft.AspNetCore.Http.HttpContextAccessor _httpContextAccessor = new Microsoft.AspNetCore.Http.HttpContextAccessor();
        private static readonly Dictionary<Type, Func<ValidationAttribute, Localizer, ValidationAttribute>> _validationAttributes;

        static LocalizedModelValidatorProvider()
        {
            _validationAttributes = new Dictionary<Type, Func<ValidationAttribute, Localizer, ValidationAttribute>> {
                { typeof(RequiredAttribute),            (attribute, t) => new LocalizedRequiredAttribute((RequiredAttribute)attribute, t)},
                { typeof(RangeAttribute),               (attribute, t) => new LocalizedRangeAttribute((RangeAttribute)attribute, t)},
                { typeof(StringLengthAttribute),        (attribute, t) => new LocalizedStringLengthAttribute((StringLengthAttribute)attribute, t)},
                { typeof(RegularExpressionAttribute),   (attribute, t) => new LocalizedRegularExpressionAttribute((RegularExpressionAttribute)attribute, t)}
            };
        }

        public void CreateValidationMetadata(ValidationMetadataProviderContext context)
        {
            var validatorMetadata = context.ValidationMetadata.ValidatorMetadata;

            for (var i = 0; i < validatorMetadata.Count; i++)
            {
                var attribute = validatorMetadata[i] as ValidationAttribute;
                if (attribute == null)
                {
                    continue;
                }

                // default translations use the attribute's scope, e.g., Orchard.Mvc.DataAnnotations.LocalizedRequiredAttribute
                Func<ValidationAttribute, Localizer, ValidationAttribute> localizedAttribute;

                // substitute the attribute to its localized version if available
                if (_validationAttributes.TryGetValue(attribute.GetType(), out localizedAttribute))
                {
                    var localized = localizedAttribute(attribute, CreateLocalizer(GetLocalizedAttributeScope(attribute)));
                    validatorMetadata[i] = localized;
                }
                else
                {
                    // try to inject the localizer if it's an unkown validation attribute
                    var propertyInfo = attribute.GetType().GetProperty("T", typeof(Localizer));
                    if (propertyInfo != null)
                    {
                        propertyInfo.SetValue(attribute, CreateLocalizer(attribute.GetType().FullName), null);
                    }
                }
            }
        }

        public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
        {
            var displayName = context.DisplayMetadata.DisplayName;
            if (displayName == null)
            {
                return;
            }

            // overriden messages have their localization in the scope of the class they are applied to
            var containerType = context.Key.ContainerType ?? context.Key.ModelType;
            var t = CreateLocalizer(containerType.FullName);
            context.DisplayMetadata.DisplayName = () => t(displayName()).Text;
        }

        private static string GetLocalizedAttributeScope(ValidationAttribute attribute)
        {
            // The localized attributes are in this namespace, e.g. Orchard.Mvc.DataAnnotations.LocalizedRequiredAttribute
            return typeof(LocalizedModelValidatorProvider).Namespace + ".Localized" + attribute.GetType().Name;
        }

        private static Localizer CreateLocalizer(string scope)
        {
            return (text, args) =>
            {
                var workContext = _httpContextAccessor.HttpContext.GetWorkContext();
                var localizer = LocalizationUtilities.Resolve(workContext, scope);
                return localizer(text, args);
            };
        }
    }
}
