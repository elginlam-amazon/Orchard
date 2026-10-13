using System;
using System.Linq;
using Autofac;
using Autofac.Core;
using CoreWCF.Description;
using Orchard.Environment;
using Orchard.Environment.Configuration;
using Orchard.Environment.ShellBuilders;

namespace Orchard.Wcf
{
    /// <summary>
    /// Creates the service behavior resolving a WCF service implementation from the container of the tenant
    /// matching the service base address.
    /// </summary>
    /// <remarks>
    /// CoreWCF has no .svc activation (ServiceHostFactory): services are declared in the ASP.NET Core host
    /// (AddServiceModelServices/UseServiceModel), and the behavior created here is added to the service host with
    /// <c>ConfigureServiceHostBase&lt;TService&gt;(host =&gt; host.Description.Behaviors.Add(factory.CreateServiceBehavior(...)))</c>.
    /// </remarks>
    public class OrchardServiceHostFactory : IShim
    {
        public OrchardServiceHostFactory()
        {
            OrchardHostContainerRegistry.RegisterShim(this);
        }

        public IOrchardHostContainer HostContainer { get; set; }

        public IServiceBehavior CreateServiceBehavior(string constructorString, Uri baseAddress)
        {
            IComponentRegistration registration;
            if (constructorString == null)
            {
                throw new ArgumentNullException("constructorString");
            }

            if (constructorString == string.Empty)
            {
                throw new ArgumentOutOfRangeException("constructorString");
            }

            if (HostContainer == null)
            {
                throw new InvalidOperationException();
            }

            // Create work context
            IRunningShellTable runningShellTable = HostContainer.Resolve<IRunningShellTable>();
            ShellSettings shellSettings = runningShellTable.Match(baseAddress.Host, baseAddress.LocalPath);

            IOrchardHost orchardHost = HostContainer.Resolve<IOrchardHost>();
            ShellContext shellContext = orchardHost.GetShellContext(shellSettings);
            IWorkContextAccessor workContextAccessor = shellContext.LifetimeScope.Resolve<IWorkContextAccessor>();
            WorkContext workContext = workContextAccessor.GetContext();
            if (workContext == null)
            {
                using (IWorkContextScope workContextScope = workContextAccessor.CreateWorkContextScope())
                {
                    ILifetimeScope lifetimeScope = workContextScope.Resolve<ILifetimeScope>();
                    registration = GetRegistration(lifetimeScope, constructorString);
                }
            }
            else
            {
                ILifetimeScope lifetimeScope = workContext.Resolve<ILifetimeScope>();
                registration = GetRegistration(lifetimeScope, constructorString);
            }

            if (registration == null)
            {
                throw new InvalidOperationException();
            }

            if (!registration.Activator.LimitType.IsClass)
            {
                throw new InvalidOperationException();
            }

            return new OrchardDependencyInjectionServiceBehavior(workContextAccessor, registration.Activator.LimitType, registration);
        }

        private IComponentRegistration GetRegistration(ILifetimeScope lifetimeScope, string constructorString)
        {
            IComponentRegistration registration;
            if (!lifetimeScope.ComponentRegistry.TryGetRegistration(new KeyedService(constructorString, typeof(object)), out registration))
            {
                Type serviceType = Type.GetType(constructorString, false);
                if (serviceType != null)
                {
                    lifetimeScope.ComponentRegistry.TryGetRegistration(new TypedService(serviceType), out registration);
                }
            }

            return registration;
        }
    }
}
