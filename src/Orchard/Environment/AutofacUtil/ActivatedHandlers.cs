using System;
using Autofac;
using Autofac.Core;
using Autofac.Core.Resolving;
using Autofac.Core.Resolving.Pipeline;

namespace Orchard.Environment.AutofacUtil
{
    public static class ActivatedHandlers
    {
        /// <summary>
        /// Runs a handler on each new instance of the component once the whole resolve operation has completed,
        /// like the former Autofac <c>IComponentRegistration.Activated</c> event. The handler can resolve services
        /// which depend on the instance being activated (e.g. property injection of the loggers).
        /// </summary>
        public static void OnActivatedDeferred(this IComponentRegistration registration, Action<IComponentContext, object> handler)
        {
            registration.PipelineBuilding += (sender, pipeline) =>
            {
                pipeline.Use(PipelinePhase.Activation, MiddlewareInsertionMode.EndOfPhase, (context, next) =>
                {
                    next(context);

                    if (!context.NewInstanceActivated)
                        return;

                    var instance = context.Instance;
                    var scope = context.ActivationScope;
                    var operation = context.Operation;

                    void OnOperationEnding(object s, ResolveOperationEndingEventArgs args)
                    {
                        operation.CurrentOperationEnding -= OnOperationEnding;

                        if (args.Exception == null)
                        {
                            handler(scope, instance);
                        }
                    }

                    operation.CurrentOperationEnding += OnOperationEnding;
                });
            };
        }
    }
}
