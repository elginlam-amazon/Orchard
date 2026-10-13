using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using NUnit.Framework;
using Orchard.Tests.Stubs;

namespace Orchard.Tests.UI.Notify
{
    [TestFixture]
    public class NotifyFilterTests
    {
        private static ActionExecutedContext BuildContext()
        {
            var httpContext = new StubHttpContext();
            var routeData = new RouteData();

            var actionContext = new ActionContext(httpContext, routeData, new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
            return new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), new Mock<ControllerBase>().Object);
        }
    }
}