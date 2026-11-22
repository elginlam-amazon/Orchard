using System;
using System.Collections;
using Autofac;
using Autofac.Core;
using Autofac.Core.Registration;
using Autofac.Core.Resolving.Pipeline;

namespace Orchard.Environment
{
    internal class CollectionOrderModule : IModule
    {
        public void Configure(IComponentRegistryBuilder componentRegistry)
        {
            componentRegistry.Registered += (sender, registered) =>
            {
                // only bother watching enumerable resolves
                var limitType = registered.ComponentRegistration.Activator.LimitType;
                if (typeof(IEnumerable).IsAssignableFrom(limitType))
                {
                    registered.ComponentRegistration.PipelineBuilding += (sender2, pipeline) =>
                    {
                        pipeline.Use(PipelinePhase.Activation, MiddlewareInsertionMode.EndOfPhase, (ctxt, next) =>
                        {
                            next(ctxt);
                            if (ctxt.Instance is Array arr)
                            {
                                Array.Reverse(arr);
                            }
                        });
                    };
                }
            };
        }
    }
}
