using System;
using System.Reflection;
using Autofac;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Orchard.Environment.Extensions.Models;
using Orchard.Mvc;
using Orchard.Tests.DisplayManagement;
using Orchard.Tests.Stubs;

namespace Orchard.Tests.Mvc
{
    /// <summary>
    /// The controllers are created by <see cref="OrchardControllerActivator"/> (formerly OrchardControllerFactory).
    /// </summary>
    [TestFixture]
    public class OrchardControllerActivatorTests
    {
        private OrchardControllerActivator _controllerActivator;
        private IWorkContextAccessor _workContextAccessor;
        private ILifetimeScope _requestLifetime;

        [SetUp]
        public void Init()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<FooController>()
                .Keyed<ControllerBase>(typeof(FooController))
                .WithMetadata("ControllerType", typeof(FooController))
                .InstancePerDependency();

            // a feature replacing the controller of another one
            builder.RegisterType<ReplacementFooController>()
                .Keyed<ControllerBase>(typeof(FooController))
                .WithMetadata("ControllerType", typeof(ReplacementFooController))
                .InstancePerDependency();

            var container = builder.Build();
            _requestLifetime = container.BeginLifetimeScope();

            var workContext = new DefaultDisplayManagerTests.TestWorkContext
            {
                CurrentTheme = new ExtensionDescriptor { Id = "Hello" },
                LifetimeScope = _requestLifetime
            };
            _workContextAccessor = new DefaultDisplayManagerTests.TestWorkContextAccessor(workContext);

            _controllerActivator = new OrchardControllerActivator();
        }

        [Test]
        public void ControllerShouldBeResolvedFromTheWorkContextWhenPresent()
        {
            var controllerContext = GetControllerContext(_workContextAccessor, typeof(FooController));
            var controller = _controllerActivator.Create(controllerContext);

            Assert.That(controller, Is.TypeOf<ReplacementFooController>());
        }

        [Test]
        public void WhenNullOrMissingContainerNormalControllerActivationShouldBeUsedAsFallback()
        {
            var controllerContext = GetControllerContext(null, typeof(FooController));
            var controller = _controllerActivator.Create(controllerContext);

            Assert.That(controller, Is.TypeOf<FooController>());
        }

        [Test]
        public void WhenContainerIsPresentButControllerIsNotResolvedNormalControllerActivationShouldBeUsedAsFallback()
        {
            var controllerContext = GetControllerContext(_workContextAccessor, typeof(BarController));
            var controller = _controllerActivator.Create(controllerContext);

            Assert.That(controller, Is.TypeOf<BarController>());
        }

        [Test]
        public void DisposingControllerThatCameFromContainerShouldNotCauseProblemWhenContainerIsDisposed()
        {
            var controllerContext = GetControllerContext(_workContextAccessor, typeof(FooController));
            var controller = _controllerActivator.Create(controllerContext);

            Assert.That(controller, Is.TypeOf<ReplacementFooController>());

            _controllerActivator.Release(controllerContext, controller);
            _requestLifetime.Dispose();

            // explicitly dispose a few more, just to make sure it's getting hit from all different directions
            ((IDisposable)controller).Dispose();
            ((ReplacementFooController)controller).Dispose();

            Assert.That(((ReplacementFooController)controller).Disposals, Is.EqualTo(3));
        }

        [Test]
        public void NullServiceKeyReturnsDefault()
        {
            var orchardControllerActivator = new OrchardControllerActivatorAccessor();
            ReplacementFooController fooController;

            Assert.That(orchardControllerActivator.TryResolveAccessor(_workContextAccessor.GetContext(), null, out fooController), Is.False);
            Assert.That(fooController, Is.Null);
        }

        private static ControllerContext GetControllerContext(IWorkContextAccessor workContextAccessor, Type controllerType)
        {
            var httpContext = new StubHttpContext
            {
                RequestServices = new ServiceCollection().BuildServiceProvider()
            };

            var routeData = new RouteData();
            routeData.DataTokens["IWorkContextAccessor"] = workContextAccessor;

            var actionDescriptor = new ControllerActionDescriptor { ControllerTypeInfo = controllerType.GetTypeInfo() };
            return new ControllerContext(new ActionContext(httpContext, routeData, actionDescriptor));
        }

        public class FooController : Controller { }

        public class BarController : Controller { }

        public class ReplacementFooController : Controller
        {
            protected override void Dispose(bool disposing)
            {
                ++Disposals;

                base.Dispose(disposing);
            }

            public int Disposals { get; set; }
        }

        internal class OrchardControllerActivatorAccessor : OrchardControllerActivator
        {
            public bool TryResolveAccessor<T>(WorkContext workContext, object serviceKey, out T instance)
            {
                return TryResolve(workContext, serviceKey, out instance);
            }
        }
    }
}
