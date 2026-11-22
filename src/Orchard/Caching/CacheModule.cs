using System;
using System.Linq;
using Autofac;
using Autofac.Core.Registration;
using Autofac.Core.Resolving.Pipeline;

namespace Orchard.Caching
{
    public class CacheModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DefaultCacheManager>()
                .As<ICacheManager>()
                .InstancePerDependency();
        }

        protected override void AttachToComponentRegistration(IComponentRegistryBuilder componentRegistry, Autofac.Core.IComponentRegistration registration)
        {
            var needsCacheManager = registration.Activator.LimitType
                .GetConstructors()
                .Any(x => x.GetParameters()
                    .Any(xx => xx.ParameterType == typeof(ICacheManager)));

            if (needsCacheManager)
            {
                registration.PipelineBuilding += (sender, pipeline) =>
                {
                    pipeline.Use(PipelinePhase.ParameterSelection, MiddlewareInsertionMode.EndOfPhase, (ctxt, next) =>
                    {
                        var parameter = new TypedParameter(
                            typeof(ICacheManager),
                            ctxt.ActivationScope.Resolve<ICacheManager>(new TypedParameter(typeof(Type), registration.Activator.LimitType)));
                        ctxt.ChangeParameters(ctxt.Parameters.Concat(new[] { parameter }));
                        next(ctxt);
                    });
                };
            }
        }
    }
}
