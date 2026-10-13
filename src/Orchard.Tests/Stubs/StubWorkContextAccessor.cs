using System;
using System.Collections.Generic;
using Autofac;
using Microsoft.AspNetCore.Http;
using Orchard.ContentManagement;
using Orchard.Security;
using Orchard.Settings;

namespace Orchard.Tests.Stubs
{
    public class StubWorkContextAccessor : IWorkContextAccessor
    {
        private readonly ILifetimeScope _lifetimeScope;
        private readonly WorkContext _workContext;

        public StubWorkContextAccessor(ILifetimeScope lifetimeScope)
        {
            _lifetimeScope = lifetimeScope;
            _workContext = new WorkContextImpl(lifetimeScope);
        }

        public class WorkContextImpl : WorkContext
        {
            private readonly ILifetimeScope _lifetimeScope;
            private readonly Dictionary<string, object> _contextDictonary;

            public delegate void MyInitMethod(WorkContextImpl workContextImpl);

            public static MyInitMethod _initMethod;

            public WorkContextImpl(ILifetimeScope lifetimeScope)
            {
                _contextDictonary = new Dictionary<string, object>();
                CurrentUser = new StubUser();
                var ci = new ContentItem();
                ci.Weld(new StubSite());
                CurrentSite = ci.As<ISite>();
                _lifetimeScope = lifetimeScope;

                if (_initMethod != null)
                {
                    _initMethod(this);
                }
            }

            public class StubSite : ContentPart, ISite
            {
                public static string DefaultSuperUser;

                public string PageTitleSeparator => throw new NotImplementedException();

                public string SiteName => "TestSite";

                public string SiteSalt => throw new NotImplementedException();

                public string SuperUser => DefaultSuperUser;

                public string HomePage
                {
                    get { throw new NotImplementedException(); }
                    set { throw new NotImplementedException(); }
                }

                public string SiteCulture
                {
                    get { throw new NotImplementedException(); }
                    set { throw new NotImplementedException(); }
                }

                public string SiteCalendar
                {
                    get { throw new NotImplementedException(); }
                    set { throw new NotImplementedException(); }
                }

                public ResourceDebugMode ResourceDebugMode
                {
                    get { throw new NotImplementedException(); }
                    set { throw new NotImplementedException(); }
                }

                public bool UseCdn
                {
                    get { throw new NotImplementedException(); }
                    set { throw new NotImplementedException(); }
                }

                public bool UseFileHash
                {
                    get { throw new NotImplementedException(); }
                    set { throw new NotImplementedException(); }
                }

                public int PageSize
                {
                    get { throw new NotImplementedException(); }
                    set { throw new NotImplementedException(); }
                }

                public int MaxPageSize
                {
                    get { throw new NotImplementedException(); }
                    set { throw new NotImplementedException(); }
                }

                public int MaxPagedCount
                {
                    get { throw new NotImplementedException(); }
                    set { throw new NotImplementedException(); }
                }

                public string BaseUrl { get; set; }

                public string SiteTimeZone { get; set; }
            }

            public class StubUser : IUser
            {
                public ContentItem ContentItem => throw new NotImplementedException();

                public int Id => 5;

                public string UserName => "Fake";

                public string Email => "Fake@fake.com";
            }

            public override T Resolve<T>()
            {
                return _lifetimeScope.Resolve<T>();
            }

            public override object Resolve(Type serviceType)
            {
                return _lifetimeScope.Resolve(serviceType);
            }

            public override bool TryResolve<T>(out T service)
            {
                // Autofac only supports reference types in the generic TryResolve
                var resolved = _lifetimeScope.TryResolve(typeof(T), out var instance);
                service = resolved ? (T)instance : default(T);
                return resolved;
            }

            public override bool TryResolve(Type serviceType, out object service)
            {
                return _lifetimeScope.TryResolve(serviceType, out service);
            }

            public override T GetState<T>(string name)
            {
                return (T)_contextDictonary[name];
            }

            public override void SetState<T>(string name, T value)
            {
                _contextDictonary[name] = value;
            }
        }

        public WorkContext GetContext(HttpContext httpContext)
        {
            return _workContext;
        }

        public IWorkContextScope CreateWorkContextScope(HttpContext httpContext)
        {
            throw new NotSupportedException();
        }

        public WorkContext GetContext()
        {
            return _workContext;
        }

        public IWorkContextScope CreateWorkContextScope()
        {
            var workLifetime = _lifetimeScope.BeginLifetimeScope("work");
            var workContext = new WorkContextImpl(workLifetime);
            return new StubWorkContextScope(workContext, workLifetime);
        }
    }
}
