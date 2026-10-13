using System;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Orchard.Mvc.Extensions
{
    /// <summary>
    /// Helpers giving access to the controller and action information of an action descriptor
    /// (formerly exposed by the MVC ActionDescriptor and ControllerDescriptor classes).
    /// </summary>
    public static class ActionDescriptorExtensions
    {
        public static string GetActionName(this ActionDescriptor actionDescriptor)
        {
            if (actionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
            {
                return controllerActionDescriptor.GetActionName();
            }

            return actionDescriptor != null && actionDescriptor.RouteValues.TryGetValue("action", out var action) ? action : null;
        }

        public static string GetControllerName(this ActionDescriptor actionDescriptor)
        {
            if (actionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
            {
                return controllerActionDescriptor.ControllerName;
            }

            return actionDescriptor != null && actionDescriptor.RouteValues.TryGetValue("controller", out var controller) ? controller : null;
        }

        public static Type GetControllerType(this ActionDescriptor actionDescriptor)
        {
            return (actionDescriptor as ControllerActionDescriptor)?.ControllerTypeInfo.AsType();
        }

        /// <summary>
        /// Returns the custom attributes of the action method.
        /// </summary>
        public static object[] GetCustomAttributes(this ActionDescriptor actionDescriptor, Type attributeType, bool inherit)
        {
            var methodInfo = (actionDescriptor as ControllerActionDescriptor)?.MethodInfo;
            return methodInfo == null ? (object[])Array.CreateInstance(attributeType, 0) : methodInfo.GetCustomAttributes(attributeType, inherit);
        }

        /// <summary>
        /// Returns the custom attributes of the controller of the action.
        /// </summary>
        public static object[] GetControllerCustomAttributes(this ActionDescriptor actionDescriptor, Type attributeType, bool inherit)
        {
            var controllerType = (actionDescriptor as ControllerActionDescriptor)?.ControllerTypeInfo;
            return controllerType == null ? (object[])Array.CreateInstance(attributeType, 0) : controllerType.GetCustomAttributes(attributeType, inherit);
        }

        public static bool IsDefined(this ActionDescriptor actionDescriptor, Type attributeType, bool inherit)
        {
            var methodInfo = (actionDescriptor as ControllerActionDescriptor)?.MethodInfo;
            return methodInfo != null && methodInfo.IsDefined(attributeType, inherit);
        }
    }
}
