using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Orchard.Environment.Configuration;
using Orchard.Utility.Extensions;
using Orchard.Utility;

namespace Orchard.Mvc.Extensions
{
    public static class ControllerExtensions
    {
        public static ActionResult RedirectLocal(this Controller controller, string redirectUrl, Func<ActionResult> invalidUrlBehavior)
        {
            if (!string.IsNullOrWhiteSpace(redirectUrl) && controller.Request.IsLocalUrl(redirectUrl))
            {
                return RedirectWithTenantPrefix(redirectUrl, controller);
            }

            return invalidUrlBehavior != null ? invalidUrlBehavior() : null;
        }

        public static ActionResult RedirectLocal(this Controller controller, string redirectUrl)
        {
            return RedirectLocal(controller, redirectUrl, (string)null);
        }

        public static ActionResult RedirectLocal(this Controller controller, string redirectUrl, string defaultUrl)
        {
            if (controller.Request.IsLocalUrl(redirectUrl))
            {
                return RedirectWithTenantPrefix(redirectUrl, controller);
            }

            return RedirectWithTenantPrefix(defaultUrl ?? "~/", controller);
        }

        private const string ValueProviderKey = "Orchard.Mvc.ValueProvider";

        /// <summary>
        /// Returns the value provider of the request (route values, query string and form, the former Controller.ValueProvider).
        /// </summary>
        public static IValueProvider GetValueProvider(this ControllerBase controller)
        {
            var items = controller.HttpContext.Items;
            if (items.TryGetValue(ValueProviderKey, out var valueProvider) && valueProvider is IValueProvider cached)
            {
                return cached;
            }

            var result = CompositeValueProvider.CreateAsync(controller.ControllerContext).GetAwaiter().GetResult();
            items[ValueProviderKey] = result;
            return result;
        }

        /// <summary>
        /// Replaces the value provider used to update the models (the former settable Controller.ValueProvider).
        /// </summary>
        public static void SetValueProvider(this ControllerBase controller, IValueProvider valueProvider)
        {
            if (valueProvider == null)
            {
                controller.HttpContext.Items.Remove(ValueProviderKey);
            }
            else
            {
                controller.HttpContext.Items[ValueProviderKey] = valueProvider;
            }
        }

        /// <summary>
        /// Updates the specified model instance using values from the request (the former synchronous Controller.TryUpdateModel).
        /// </summary>
        public static bool TryUpdateModel<TModel>(this ControllerBase controller, TModel model, string prefix = null, string[] includeProperties = null, string[] excludeProperties = null) where TModel : class
        {
            var valueProvider = controller.GetValueProvider();

            return controller.TryUpdateModelAsync(model, prefix ?? string.Empty, valueProvider, metadata =>
                {
                    var propertyName = metadata.PropertyName;
                    if (propertyName == null)
                        return true;

                    if (includeProperties != null && includeProperties.Length > 0 && !includeProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase))
                        return false;

                    return excludeProperties == null || !excludeProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase);
                })
                .GetAwaiter().GetResult();
        }

        public static bool TryUpdateModel<TModel>(this ControllerBase controller, TModel model, string[] includeProperties) where TModel : class
        {
            return TryUpdateModel(controller, model, null, includeProperties, null);
        }

        public static bool TryUpdateModel<TModel>(this ControllerBase controller, TModel model, IValueProvider valueProvider) where TModel : class
        {
            return controller.TryUpdateModelAsync(model, string.Empty, valueProvider).GetAwaiter().GetResult();
        }

        public static bool TryUpdateModel<TModel>(this ControllerBase controller, TModel model, string prefix, IValueProvider valueProvider) where TModel : class
        {
            return controller.TryUpdateModelAsync(model, prefix ?? string.Empty, valueProvider).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Updates the specified model instance using values from the request, and throws if the model is invalid (the former Controller.UpdateModel).
        /// </summary>
        public static void UpdateModel<TModel>(this ControllerBase controller, TModel model, string prefix = null, string[] includeProperties = null, string[] excludeProperties = null) where TModel : class
        {
            if (!TryUpdateModel(controller, model, prefix, includeProperties, excludeProperties))
            {
                throw new InvalidOperationException(string.Format("The model of type '{0}' could not be updated.", typeof(TModel).FullName));
            }
        }

        private static ActionResult RedirectWithTenantPrefix(string redirectUrl, Controller controller)
        {
            if (redirectUrl.StartsWith("~/"))
            {
                ShellSettings settings;
                var context = controller.ControllerContext.GetWorkContext();

                if (context != null &&
                    context.TryResolve<ShellSettings>(out settings) &&
                    !string.IsNullOrWhiteSpace(settings.RequestUrlPrefix))
                {
                    redirectUrl = VirtualPathUtility.ToAbsolute(redirectUrl, controller.Request.GetApplicationPath().TrimEnd('/') + "/" + settings.RequestUrlPrefix);
                }
            }

            return new RedirectResult(redirectUrl);
        }
    }
}
