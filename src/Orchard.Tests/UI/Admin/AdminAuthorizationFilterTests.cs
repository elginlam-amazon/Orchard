using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using NUnit.Framework;
using Orchard.Localization;
using Orchard.Security;
using Orchard.Tests.Stubs;
using Orchard.UI.Admin;

namespace Orchard.Tests.UI.Admin
{
    [TestFixture]
    public class AdminAuthorizationFilterTests
    {

        private static AuthorizationFilterContext GetAuthorizationContext<TController>() where TController : ControllerBase, new()
        {
            var controllerType = typeof(TController);
            var actionDescriptor = new ControllerActionDescriptor
            {
                ControllerTypeInfo = controllerType.GetTypeInfo(),
                ControllerName = controllerType.Name.Substring(0, controllerType.Name.Length - "Controller".Length),
                MethodInfo = controllerType.GetMethod("Index"),
                ActionName = "Index",
            };
            var actionContext = new ActionContext(new StubHttpContext(), new RouteData(), actionDescriptor);
            return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        }

        private static IAuthorizer GetAuthorizer(bool result)
        {
            var authorizer = new Mock<IAuthorizer>();
            authorizer
                .Setup(x => x.Authorize(StandardPermissions.AccessAdminPanel, It.IsAny<LocalizedString>())).
                Returns(result);
            return authorizer.Object;
        }

        [Test]
        public void NormalRequestShouldNotBeAffected()
        {
            var authorizationContext = GetAuthorizationContext<NormalController>();

            var filter = new AdminFilter(GetAuthorizer(false));
            filter.OnAuthorization(authorizationContext);

            Assert.That(authorizationContext.Result, Is.Null);
        }

        private static void TestActionThatShouldRequirePermission<TController>() where TController : ControllerBase, new()
        {
            var authorizationContext = GetAuthorizationContext<TController>();
            var filter = new AdminFilter(GetAuthorizer(false));
            filter.OnAuthorization(authorizationContext);
            Assert.That(authorizationContext.Result, Is.InstanceOf<UnauthorizedResult>());
            Assert.That(AdminFilter.IsApplied(authorizationContext), Is.True);

            var authorizationContext2 = GetAuthorizationContext<TController>();
            var filter2 = new AdminFilter(GetAuthorizer(true));
            filter2.OnAuthorization(authorizationContext2);
            Assert.That(authorizationContext2.Result, Is.Null);
            Assert.That(AdminFilter.IsApplied(authorizationContext2), Is.True);
        }


        [Test]
        public void AdminRequestShouldRequirePermission()
        {
            TestActionThatShouldRequirePermission<AdminController>();
        }

        [Test]
        public void NormalWithAttribRequestShouldRequirePermission()
        {
            TestActionThatShouldRequirePermission<NormalWithAttribController>();
        }

        [Test]
        public void NormalWithActionAttribRequestShouldRequirePermission()
        {
            TestActionThatShouldRequirePermission<NormalWithActionAttribController>();
        }

        [Test]
        public void InheritedAttribRequestShouldRequirePermission()
        {
            TestActionThatShouldRequirePermission<InheritedAttribController>();
        }
    }

    public class NormalController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
    }

    public class AdminController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
    }

    [Admin]
    public class NormalWithAttribController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
    }

    public class NormalWithActionAttribController : Controller
    {
        [Admin]
        public ActionResult Index()
        {
            return View();
        }
    }

    [Admin]
    public class BaseWithAttribController : Controller
    {
        public ActionResult Something()
        {
            return View();
        }
    }

    public class InheritedAttribController : BaseWithAttribController
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}
