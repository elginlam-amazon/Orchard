using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Autofac;
using Orchard.Environment.AutofacUtil;
using Autofac.Core.Resolving.Pipeline;
using Autofac.Core;
using Autofac.Core.Registration;
using Module = Autofac.Module;

namespace Orchard.Localization
{
    public class LocalizationModule : Module
    {
        private readonly ConcurrentDictionary<string, Localizer> _localizerCache;

        public LocalizationModule()
        {
            _localizerCache = new ConcurrentDictionary<string, Localizer>();
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<Text>().As<IText>().InstancePerDependency();
        }

        protected override void AttachToComponentRegistration(IComponentRegistryBuilder componentRegistry, IComponentRegistration registration)
        {

            var userProperty = FindUserProperty(registration.Activator.LimitType);
            if (userProperty != null)
            {
                List<string> scopes = new List<string>();
                var type = registration.Activator.LimitType;
                // we don't need this behavior on CLR types, so that's an optimization
                while (!type.Namespace.Equals("System"))
                {
                    scopes.Add(type.FullName);
                    type = type.BaseType;
                }

                foreach (var scope in scopes)
                {
                    registration.OnActivatedDeferred((context, instance) =>
                    {
                        if (instance.GetType().FullName != scope)
                        {
                            return;
                        }
                        var localizer = _localizerCache.GetOrAdd(scope, key => LocalizationUtilities.Resolve(context, scopes));
                        userProperty.SetValue(instance, localizer, null);
                    });
                }
            }
        }

        private static PropertyInfo FindUserProperty(Type type)
        {
            return type.GetProperty("T", typeof(Localizer));
        }
    }
}
