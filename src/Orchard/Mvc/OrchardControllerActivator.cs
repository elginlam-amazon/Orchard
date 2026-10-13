using System;
using Autofac;
using Autofac.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace Orchard.Mvc
{
    /// <summary>
    /// Resolves controllers from the work context (tenant) lifetime scope, replacing the former OrchardControllerFactory.
    /// </summary>
    public class OrchardControllerActivator : IControllerActivator
    {
        private const string CreatedByActivatorKey = "Orchard.ControllerCreatedByActivator";

        /// <summary>
        /// Tries to resolve an instance for the controller associated with a given service key for the work context scope.
        /// </summary>
        /// <typeparam name="T">The type of the controller.</typeparam>
        /// <param name="workContext">The work context.</param>
        /// <param name="serviceKey">The service key for the controller.</param>
        /// <param name="instance">The controller instance.</param>
        /// <returns>True if the controller was resolved; false otherwise.</returns>
        protected bool TryResolve<T>(WorkContext workContext, object serviceKey, out T instance)
        {
            if (workContext != null && serviceKey != null)
            {
                var key = new Autofac.Core.KeyedService(serviceKey, typeof(T));
                object value;
                if (workContext.Resolve<ILifetimeScope>().TryResolveService(key, out value))
                {
                    instance = (T)value;
                    return true;
                }
            }

            instance = default(T);
            return false;
        }

        public object Create(ControllerContext context)
        {
            var controllerType = context.ActionDescriptor.ControllerTypeInfo.AsType();

            ControllerBase controller;
            var workContext = context.GetWorkContext();
            if (TryResolve(workContext, controllerType, out controller))
            {
                return controller;
            }

            // fail as appropriate for MVC's expectations
            context.HttpContext.Items[CreatedByActivatorKey] = true;
            return ActivatorUtilities.CreateInstance(context.HttpContext.RequestServices, controllerType);
        }

        public void Release(ControllerContext context, object controller)
        {
            // Controllers resolved from the work context are disposed with the work context lifetime scope.
            if (context.HttpContext.Items.ContainsKey(CreatedByActivatorKey) && controller is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
