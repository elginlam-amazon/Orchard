// Stubs for WCF types not provided by CoreWCF 1.9.2
using System;
using CoreWCF;
using CoreWCF.Description;

namespace CoreWCF
{
    public class ServiceHost : ServiceHostBase
    {
        public ServiceHost(Type serviceType, params Uri[] baseAddresses) { }

        public new ServiceDescription Description => base.Description;

        protected override ServiceDescription CreateDescription(out IDictionary<string, ContractDescription> implementedContracts)
        {
            implementedContracts = new Dictionary<string, ContractDescription>();
            return new ServiceDescription();
        }

        protected override void OnAbort() { }

        protected override System.Threading.Tasks.Task OnCloseAsync(System.Threading.CancellationToken token) => System.Threading.Tasks.Task.CompletedTask;

        protected override System.Threading.Tasks.Task OnOpenAsync(System.Threading.CancellationToken token) => System.Threading.Tasks.Task.CompletedTask;
    }
}

namespace CoreWCF.Configuration
{
    public abstract class ServiceHostFactory
    {
        public abstract ServiceHostBase CreateServiceHost(string constructorString, Uri[] baseAddresses);

        protected virtual CoreWCF.ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses) => null;
    }
}
