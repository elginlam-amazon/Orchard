using System;
using System.Collections.Generic;
using System.Threading;
using System.Web;
using Autofac;
using Orchard.Logging;
using Orchard.Mvc;
using Orchard.Mvc.Extensions;

namespace Orchard.Environment
{
    public class WorkContextAccessor : ILogicalWorkContextAccessor
    {
        // AsyncLocal replaces CallContext.LogicalSetData/GetData for .NET Core async-flow storage.
        // A single static dictionary keyed by slot name so that each WorkContextAccessor instance
        // (one per tenant, differentiated by _workContextSlot GUID) stores its own entry.
        private static readonly AsyncLocal<Dictionary<string, WorkContext>> _asyncLocalContexts =
            new AsyncLocal<Dictionary<string, WorkContext>>();

        readonly ILifetimeScope _lifetimeScope;

        readonly IHttpContextAccessor _httpContextAccessor;
        // a different symbolic key is used for each tenant.
        // this guarantees the correct accessor is being resolved.
        readonly object _workContextKey = new object();
        private readonly string _workContextSlot;

        public WorkContextAccessor(
            IHttpContextAccessor httpContextAccessor,
            ILifetimeScope lifetimeScope)
        {
            _httpContextAccessor = httpContextAccessor;
            _lifetimeScope = lifetimeScope;
            _workContextSlot = "WorkContext." + Guid.NewGuid().ToString("n");
        }

        public WorkContext GetContext(HttpContextBase httpContext)
        {
            if (!httpContext.IsBackgroundContext())
                return httpContext.Items[_workContextKey] as WorkContext;

            return GetLogicalContext();
        }

        public WorkContext GetContext()
        {
            var httpContext = _httpContextAccessor.Current();
            return GetContext(httpContext);
        }

        public WorkContext GetLogicalContext()
        {
            if (_asyncLocalContexts.Value == null) return null;
            _asyncLocalContexts.Value.TryGetValue(_workContextSlot, out var context);
            return context;
        }

        public IWorkContextScope CreateWorkContextScope(HttpContextBase httpContext)
        {
            var workLifetime = _lifetimeScope.BeginLifetimeScope("work");

            var events = workLifetime.Resolve<IEnumerable<IWorkContextEvents>>();
            events.Invoke(e => e.Started(), NullLogger.Instance);

            if (!httpContext.IsBackgroundContext())
                return new HttpContextScopeImplementation(events, workLifetime, httpContext, _workContextKey);

            return new CallContextScopeImplementation(events, workLifetime, _workContextSlot);
        }

        public IWorkContextScope CreateWorkContextScope()
        {
            var httpContext = _httpContextAccessor.Current();
            return CreateWorkContextScope(httpContext);
        }

        class HttpContextScopeImplementation : IWorkContextScope
        {
            readonly Action _disposer;

            public HttpContextScopeImplementation(IEnumerable<IWorkContextEvents> events, ILifetimeScope lifetimeScope, HttpContextBase httpContext, object workContextKey)
            {
                WorkContext = lifetimeScope.Resolve<WorkContext>();
                httpContext.Items[workContextKey] = WorkContext;

                _disposer = () =>
                {
                    events.Invoke(e => e.Finished(), NullLogger.Instance);
                    httpContext.Items.Remove(workContextKey);
                    lifetimeScope.Dispose();
                };
            }

            void IDisposable.Dispose()
            {
                _disposer();
            }

            public WorkContext WorkContext { get; }

            public TService Resolve<TService>()
            {
                return WorkContext.Resolve<TService>();
            }

            public bool TryResolve<TService>(out TService service)
            {
                return WorkContext.TryResolve(out service);
            }
        }

        class CallContextScopeImplementation : IWorkContextScope
        {
            readonly Action _disposer;

            public CallContextScopeImplementation(IEnumerable<IWorkContextEvents> events, ILifetimeScope lifetimeScope, string workContextSlot)
            {
                // Clear any existing context for this slot
                SetSlotValue(workContextSlot, null);

                WorkContext = lifetimeScope.Resolve<WorkContext>();
                var httpContext = lifetimeScope.Resolve<HttpContextBase>();
                WorkContext.HttpContext = httpContext;

                SetSlotValue(workContextSlot, WorkContext);

                _disposer = () =>
                {
                    events.Invoke(e => e.Finished(), NullLogger.Instance);
                    RemoveSlotValue(workContextSlot);
                    lifetimeScope.Dispose();
                };
            }

            private static void SetSlotValue(string slot, WorkContext value)
            {
                if (_asyncLocalContexts.Value == null)
                    _asyncLocalContexts.Value = new Dictionary<string, WorkContext>();
                _asyncLocalContexts.Value[slot] = value;
            }

            private static void RemoveSlotValue(string slot)
            {
                _asyncLocalContexts.Value?.Remove(slot);
            }

            void IDisposable.Dispose()
            {
                _disposer();
            }

            public WorkContext WorkContext { get; }

            public TService Resolve<TService>()
            {
                return WorkContext.Resolve<TService>();
            }

            public bool TryResolve<TService>(out TService service)
            {
                return WorkContext.TryResolve(out service);
            }
        }
    }
}
