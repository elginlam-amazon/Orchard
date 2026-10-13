using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Core;

namespace Orchard.Environment.AutofacUtil
{
    public static class ComponentContextExtensions
    {
        /// <summary>
        /// Resolves an instance of a specific component registration (the former IComponentContext.ResolveComponent(registration, parameters)).
        /// </summary>
        public static object ResolveComponent(this IComponentContext context, IComponentRegistration registration, IEnumerable<Parameter> parameters)
        {
            var service = registration.Services.FirstOrDefault() ?? new TypedService(registration.Activator.LimitType);

            // the service registration from the registry includes the service middleware (sharing, circular dependency detection...)
            var serviceRegistration = context.ComponentRegistry
                .ServiceRegistrationsFor(service)
                .FirstOrDefault(x => x.Registration.Id == registration.Id);

            if (serviceRegistration.Registration == null)
            {
                serviceRegistration = new ServiceRegistration(registration.ResolvePipeline, registration);
            }

            return context.ResolveComponent(new ResolveRequest(service, serviceRegistration, parameters));
        }
    }
}
