using System;
using System.Linq;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Orchard.Caching;
using Orchard.Environment;
using Orchard.Environment.Configuration;
using Orchard.Environment.Extensions;
using Orchard.Mvc;
using Orchard.Mvc.Routes;
using Orchard.Tests.Stubs;
using Orchard.Tests.Utility;
using IHttpContextAccessor = Orchard.Mvc.IHttpContextAccessor;
using Route = Orchard.Mvc.Routes.Route;

namespace Orchard.Tests.Mvc.Routes
{
    [TestFixture]
    public class ShellRouteTests
    {
        private OrchardRouteCollection _routes;
        private ILifetimeScope _containerA;
        private ILifetimeScope _containerB;
        private ShellSettings _settingsA;
        private ShellSettings _settingsB;
        private IContainer _rootContainer;
        private IRouter _mvcRouteHandler;

        // the services of the requests (the routes resolve their constraints and loggers from the request services)
        private static readonly IServiceProvider _requestServices = new ServiceCollection().AddLogging().AddRouting().BuildServiceProvider();

        private static HttpContext Request(string appRelativePath, string host = "localhost")
        {
            return new StubHttpContext(appRelativePath, host) { RequestServices = _requestServices };
        }

        /// <summary>
        /// A request for a tenant with a url prefix: the prefix is moved into the path base by the host.
        /// </summary>
        private static HttpContext TenantRequest(string prefix, string appRelativePath)
        {
            var httpContext = Request(appRelativePath);
            if (httpContext.Request.Path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase, out var matched, out var remaining))
            {
                httpContext.Request.PathBase = matched;
                httpContext.Request.Path = remaining;
            }
            return httpContext;
        }

        /// <summary>
        /// Returns the route data of the request if a route matches it (formerly RouteBase.GetRouteData).
        /// </summary>
        private static RouteData GetRouteData(IRouter router, HttpContext httpContext)
        {
            var context = new RouteContext(httpContext);
            context.RouteData.Routers.Add(router);
            router.RouteAsync(context).GetAwaiter().GetResult();
            return context.Handler == null ? null : context.RouteData;
        }

        private static string GetRequiredString(RouteData routeData, string key)
        {
            return (string)routeData.Values[key];
        }

        [TearDown]
        public void Cleanup()
        {
            MvcRouteHandler.Inner = _mvcRouteHandler;
        }

        [SetUp]
        public void Init()
        {
            // the matched routes are handled by MVC
            _mvcRouteHandler = MvcRouteHandler.Inner;
            MvcRouteHandler.Inner = new RouteHandler(httpContext => System.Threading.Tasks.Task.CompletedTask);

            _settingsA = new ShellSettings { Name = "Alpha" };
            _settingsB = new ShellSettings { Name = "Beta", };
            _routes = new OrchardRouteCollection();

            var rootBuilder = new ContainerBuilder();
            rootBuilder.Register(ctx => _routes);
            rootBuilder.RegisterType<ShellRoute>().InstancePerDependency();
            rootBuilder.RegisterType<RunningShellTable>().As<IRunningShellTable>().SingleInstance();
            rootBuilder.RegisterModule(new WorkContextModule());
            rootBuilder.RegisterType<WorkContextAccessor>().As<IWorkContextAccessor>().InstancePerMatchingLifetimeScope("shell");
            rootBuilder.RegisterType<Orchard.Mvc.HttpContextAccessor>().As<IHttpContextAccessor>();
            rootBuilder.RegisterType<ExtensionManager>().As<IExtensionManager>();
            rootBuilder.RegisterType<StubCacheManager>().As<ICacheManager>();
            rootBuilder.RegisterType<StubAsyncTokenProvider>().As<IAsyncTokenProvider>();
            rootBuilder.RegisterType<StubParallelCacheContext>().As<IParallelCacheContext>();

            rootBuilder.Register<Func<IRouter, ShellRoute>>(c =>
            {
                var context = c.Resolve<IComponentContext>();
                return new Func<IRouter, ShellRoute>(routeBase =>
                    new ShellRoute(
                        routeBase,
                        _settingsA,
                        context.Resolve<IWorkContextAccessor>(),
                        context.Resolve<IRunningShellTable>(), null));
            });

            _rootContainer = rootBuilder.Build();

            _containerA = _rootContainer.BeginLifetimeScope(
                "shell",
                builder =>
                {
                    builder.Register(ctx => _settingsA);
                    builder.RegisterType<RoutePublisher>().As<IRoutePublisher>().InstancePerMatchingLifetimeScope("shell");
                });

            _containerB = _rootContainer.BeginLifetimeScope(
                "shell",
                builder =>
                {
                    builder.Register(ctx => _settingsB);
                    builder.RegisterType<RoutePublisher>().As<IRoutePublisher>().InstancePerMatchingLifetimeScope("shell");
                });
        }

        [Test]
        public void FactoryMethodWillCreateShellRoutes()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ShellRoute>().InstancePerDependency();
            builder.RegisterAutoMocking();

            var container = builder.Build();
            var buildShellRoute = new Func<IRouter, ShellRoute>(routeBase =>
                    new ShellRoute(
                        routeBase,
                        _settingsA,
                        container.Resolve<IWorkContextAccessor>(),
                        container.Resolve<IRunningShellTable>(),
                        null));

            var routeA = new Route("foo", new MvcRouteHandler());
            var route1 = buildShellRoute(routeA);

            var routeB = new Route("bar", null, null, new RouteValueDictionary { { "area", _settingsB.Name } }, new MvcRouteHandler());
            var route2 = buildShellRoute(routeB);

            Assert.That(route1, Is.Not.SameAs(route2));

            Assert.That(route1.ShellSettingsName, Is.EqualTo(_settingsA.Name));
            Assert.That(route1.Area, Is.Null);

            Assert.That(route2.ShellSettingsName, Is.EqualTo(_settingsA.Name));
            Assert.That(route2.Area, Is.EqualTo(_settingsB.Name));
        }


        [Test]
        public void RoutePublisherReplacesOnlyNamedShellsRoutes()
        {

            var routeA = new Route("foo", new MvcRouteHandler());
            var routeB = new Route("bar", new MvcRouteHandler());
            var routeC = new Route("quux", new MvcRouteHandler());

            _containerA.Resolve<IRoutePublisher>().Publish(
                new[] { new RouteDescriptor { Priority = 0, Route = routeA } });

            _containerB.Resolve<IRoutePublisher>().Publish(
                new[] { new RouteDescriptor { Priority = 0, Route = routeB } });

            // routes are grouped by name/priority/area
            Assert.That(_routes.Count(), Is.EqualTo(1));

            _containerA.Resolve<IRoutePublisher>().Publish(
                new[] { new RouteDescriptor { Priority = 0, Route = routeC } });

            Assert.That(_routes.Count(), Is.EqualTo(1));

            _containerB.Resolve<IRoutePublisher>().Publish(
                new[] {
                          new RouteDescriptor {Priority = 0, Route = routeA},
                          new RouteDescriptor {Priority = 0, Route = routeB},
                      });

            Assert.That(_routes.Count(), Is.EqualTo(1));
        }

        [Test]
        public void RoutePublisherGroupsShellRoutesByName()
        {

            var routeA = new Route("foo", new MvcRouteHandler());
            var routeB = new Route("bar", new MvcRouteHandler());
            var routeC = new Route("quux", new MvcRouteHandler());

            _containerA.Resolve<IRoutePublisher>().Publish(
                new[] { new RouteDescriptor { Name = "1", Priority = 0, Route = routeA } });

            _containerA.Resolve<IRoutePublisher>().Publish(
                new[] { new RouteDescriptor { Name = "2", Priority = 0, Route = routeB } });

            Assert.That(_routes.Count(), Is.EqualTo(2));

            _containerB.Resolve<IRoutePublisher>().Publish(
                new[] { new RouteDescriptor { Name = "2", Priority = 0, Route = routeC } });

            Assert.That(_routes.Count(), Is.EqualTo(2));
        }

        [Test]
        public void MatchingRouteToActiveShellTableWillLimitTheAbilityToMatchRoutes()
        {

            var routeFoo = new Route("foo", new MvcRouteHandler());

            _settingsA.RequestUrlHost = "a.example.com";
            _containerA.Resolve<IRoutePublisher>().Publish(
                new[] { new RouteDescriptor { Priority = 0, Route = routeFoo } });
            _rootContainer.Resolve<IRunningShellTable>().Add(_settingsA);

            _settingsB.RequestUrlHost = "b.example.com";
            _containerB.Resolve<IRoutePublisher>().Publish(
                new[] { new RouteDescriptor { Priority = 0, Route = routeFoo } });
            _rootContainer.Resolve<IRunningShellTable>().Add(_settingsB);

            var httpContext = Request("~/foo");
            var routeData = GetRouteData(_routes, httpContext);
            Assert.That(routeData, Is.Null);

            var httpContextA = Request("~/foo", "a.example.com");
            var routeDataA = GetRouteData(_routes, httpContextA);
            Assert.That(routeDataA, Is.Not.Null);
            Assert.That(routeDataA.DataTokens.ContainsKey("IWorkContextAccessor"), Is.True);
            var workContextAccessorA = (IWorkContextAccessor)routeDataA.DataTokens["IWorkContextAccessor"];
            var workContextScopeA = workContextAccessorA.CreateWorkContextScope(httpContextA);

            Assert.That(workContextScopeA.Resolve<IRoutePublisher>(), Is.SameAs(_containerA.Resolve<IRoutePublisher>()));
            Assert.That(workContextScopeA.Resolve<IRoutePublisher>(), Is.Not.SameAs(_containerB.Resolve<IRoutePublisher>()));

            var httpContextB = Request("~/foo", "b.example.com");
            var routeDataB = GetRouteData(_routes, httpContextB);
            Assert.That(routeDataB, Is.Not.Null);
            Assert.That(routeDataB.DataTokens.ContainsKey("IWorkContextAccessor"), Is.True);
            var workContextAccessorB = (IWorkContextAccessor)routeDataB.DataTokens["IWorkContextAccessor"];
            var workContextScopeB = workContextAccessorB.CreateWorkContextScope(httpContextB);

            Assert.That(workContextScopeB.Resolve<IRoutePublisher>(), Is.SameAs(_containerB.Resolve<IRoutePublisher>()));
            Assert.That(workContextScopeB.Resolve<IRoutePublisher>(), Is.Not.SameAs(_containerA.Resolve<IRoutePublisher>()));

        }

        [Test]
        public void RequestUrlPrefixAdjustsMatchingAndPathGeneration()
        {
            var settings = new ShellSettings { RequestUrlPrefix = "~/foo" };

            var builder = new ContainerBuilder();
            builder.RegisterType<ShellRoute>().InstancePerDependency();
            builder.RegisterAutoMocking();
            builder.Register(ctx => settings);

            var container = builder.Build();
            container.Mock<IRunningShellTable>()
                .Setup(x => x.Match(It.IsAny<HttpContext>()))
                .Returns(settings);


            var shellRouteFactory = new Func<IRouter, ShellRoute>(routeBase =>
                    new ShellRoute(
                        routeBase,
                        settings,
                        container.Resolve<IWorkContextAccessor>(),
                        container.Resolve<IRunningShellTable>(),
                        null));

            var helloRoute = shellRouteFactory(new Route(
                "hello",
                new RouteValueDictionary { { "controller", "foo" }, { "action", "bar" } },
                new MvcRouteHandler()));

            var tagsRoute = shellRouteFactory(new Route(
                "tags/{tagName}",
                new RouteValueDictionary { { "controller", "tags" }, { "action", "show" } },
                new MvcRouteHandler()));

            var defaultRoute = shellRouteFactory(new Route(
                "{controller}/{action}",
                new RouteValueDictionary { { "controller", "home" }, { "action", "index" } },
                new MvcRouteHandler()));

            var routes = new OrchardRouteCollection { helloRoute, tagsRoute, defaultRoute };

            var helloRouteData = GetRouteData(routes, TenantRequest("/Foo", "~/Foo/Hello"));
            Assert.That(helloRouteData, Is.Not.Null);
            Assert.That(helloRouteData.Values.Count(), Is.EqualTo(2));
            Assert.That(GetRequiredString(helloRouteData, "controller"), Is.EqualTo("foo"));
            Assert.That(GetRequiredString(helloRouteData, "action"), Is.EqualTo("bar"));

            var tagsRouteData = GetRouteData(routes, TenantRequest("/Foo", "~/Foo/Tags/my-tag-name"));
            Assert.That(tagsRouteData, Is.Not.Null);
            Assert.That(tagsRouteData.Values.Count(), Is.EqualTo(3));
            Assert.That(GetRequiredString(tagsRouteData, "controller"), Is.EqualTo("tags"));
            Assert.That(GetRequiredString(tagsRouteData, "action"), Is.EqualTo("show"));
            Assert.That(GetRequiredString(tagsRouteData, "tagName"), Is.EqualTo("my-tag-name"));

            var defaultRouteData = GetRouteData(routes, TenantRequest("/Foo", "~/Foo/Alpha/Beta"));
            Assert.That(defaultRouteData, Is.Not.Null);
            Assert.That(defaultRouteData.Values.Count(), Is.EqualTo(2));
            Assert.That(GetRequiredString(defaultRouteData, "controller"), Is.EqualTo("Alpha"));
            Assert.That(GetRequiredString(defaultRouteData, "action"), Is.EqualTo("Beta"));

            var defaultRouteData2 = GetRouteData(routes, TenantRequest("/Foo", "~/Foo/Alpha"));
            Assert.That(defaultRouteData2, Is.Not.Null);
            Assert.That(defaultRouteData2.Values.Count(), Is.EqualTo(2));
            Assert.That(GetRequiredString(defaultRouteData2, "controller"), Is.EqualTo("Alpha"));
            Assert.That(GetRequiredString(defaultRouteData2, "action"), Is.EqualTo("index"));

            var defaultRouteData3 = GetRouteData(routes, TenantRequest("/Foo", "~/Foo/"));
            Assert.That(defaultRouteData3, Is.Not.Null);
            Assert.That(defaultRouteData3.Values.Count(), Is.EqualTo(2));
            Assert.That(GetRequiredString(defaultRouteData3, "controller"), Is.EqualTo("home"));
            Assert.That(GetRequiredString(defaultRouteData3, "action"), Is.EqualTo("index"));

            var defaultRouteData4 = GetRouteData(routes, TenantRequest("/Foo", "~/Foo"));
            Assert.That(defaultRouteData4, Is.Not.Null);
            Assert.That(defaultRouteData4.Values.Count(), Is.EqualTo(2));
            Assert.That(GetRequiredString(defaultRouteData4, "controller"), Is.EqualTo("home"));
            Assert.That(GetRequiredString(defaultRouteData4, "action"), Is.EqualTo("index"));

            // the generated paths are relative to the path base (the tenant prefix), which is added by the url helper
            var requestContext = TenantRequest("/Foo", "~/Foo/Alpha/Beta");
            var helloVirtualPath = routes.GetVirtualPath(new VirtualPathContext(requestContext, defaultRouteData.Values, helloRouteData.Values));
            Assert.That(helloVirtualPath, Is.Not.Null);
            Assert.That(helloVirtualPath.VirtualPath, Is.EqualTo("/hello"));

            var defaultVirtualPath4 = routes.GetVirtualPath(new VirtualPathContext(requestContext, defaultRouteData.Values, defaultRouteData4.Values));
            Assert.That(defaultVirtualPath4, Is.Not.Null);
            Assert.That(defaultVirtualPath4.VirtualPath, Is.Empty);
        }
    }
}
