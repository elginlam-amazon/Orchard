using System;
using System.Collections.Generic;
using System.Threading;
using Autofac;
using Microsoft.AspNetCore.Http;
using Orchard.Logging;
using Orchard.Mvc;
using Orchard.Mvc.Extensions;
using IHttpContextAccessor = Orchard.Mvc.IHttpContextAccessor;

namespace Orchard.Environment
{
    public class WorkContextAccessor : ILogicalWorkContextAccessor
    {
        readonly ILifetimeScope _lifetimeScope;

        readonly IHttpContextAccessor _httpContextAccessor;
        // a different symbolic key is used for each tenant.
        // this guarantees the correct accessor is being resolved.
        readonly object _workContextKey = new object();
        // logical (async flow) context replacing the former CallContext slot.
        private readonly AsyncLocal<WorkContext> _workContextSlot = new AsyncLocal<WorkContext>();

        public WorkContextAccessor(
            IHttpContextAccessor httpContextAccessor,
            ILifetimeScope lifetimeScope)
        {
            _httpContextAccessor = httpContextAccessor;
            _lifetimeScope = lifetimeScope;
        }

        public WorkContext GetContext(HttpContext httpContext)
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
            return _workContextSlot.Value;
        }

        public IWorkContextScope CreateWorkContextScope(HttpContext httpContext)
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

            public HttpContextScopeImplementation(IEnumerable<IWorkContextEvents> events, ILifetimeScope lifetimeScope, HttpContext httpContext, object workContextKey)
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

            public CallContextScopeImplementation(IEnumerable<IWorkContextEvents> events, ILifetimeScope lifetimeScope, AsyncLocal<WorkContext> workContextSlot)
            {

                workContextSlot.Value = null;

                WorkContext = lifetimeScope.Resolve<WorkContext>();
                var httpContext = lifetimeScope.Resolve<HttpContext>();
                WorkContext.HttpContext = httpContext;

                workContextSlot.Value = WorkContext;

                _disposer = () =>
                {
                    events.Invoke(e => e.Finished(), NullLogger.Instance);
                    workContextSlot.Value = null;
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
    }
}
