using System;
using System.Linq;
using Autofac;
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

        protected override void AttachToComponentRegistration(Autofac.Core.Registration.IComponentRegistryBuilder componentRegistry, Autofac.Core.IComponentRegistration registration)
        {
            var needsCacheManager = registration.Activator.LimitType
                .GetConstructors()
                .Any(x => x.GetParameters()
                    .Any(xx => xx.ParameterType == typeof(ICacheManager)));

            if (needsCacheManager)
            {
                registration.PipelineBuilding += (sender, pipeline) =>
                {
                    pipeline.Use(PipelinePhase.ParameterSelection, (context, next) =>
                    {
                        var parameter = new TypedParameter(
                            typeof(ICacheManager),
                            context.Resolve<ICacheManager>(new TypedParameter(typeof(Type), registration.Activator.LimitType)));
                        context.ChangeParameters(context.Parameters.Concat(new[] { parameter }));
                        next(context);
                    });
                };
            }
        }
    }
}
