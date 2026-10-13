using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Moq;
using NUnit.Framework;
using Orchard.Mvc;
using Orchard.Mvc.Html;

namespace Orchard.Tests.Mvc.Html
{
    [TestFixture]
    public class HtmlHelperExtensionsTests
    {
        /// <summary>
        /// A view context of a request with the MVC services (the html helpers are resolved from the request services).
        /// </summary>
        private static ViewContext CreateViewContext()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(new DiagnosticListener("Orchard.Framework.Tests"));
            services.AddSingleton<DiagnosticSource>(sp => sp.GetRequiredService<DiagnosticListener>());
            services.AddSingleton(Mock.Of<IWebHostEnvironment>(e =>
                e.ApplicationName == "Orchard.Framework.Tests" &&
                e.ContentRootFileProvider == new NullFileProvider() &&
                e.WebRootFileProvider == new NullFileProvider()));
            services.AddControllersWithViews();
            var httpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };

            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
            return new ViewContext(
                new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor()),
                new Mock<IView>().Object,
                viewData,
                new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object),
                TextWriter.Null,
                new HtmlHelperOptions());
        }

        [Test]
        public void LinkReturnsIHtmlString()
        {
            //arrange
            var viewContext = CreateViewContext();
            var viewDataContainer = new Mock<IViewDataContainer>();
            var html = HtmlHelperFactory.CreateHtmlHelper(viewContext, viewDataContainer.Object);

            //act
            var result = html.Link("test", "http://example.com") as IHtmlContent;

            //assert
            Assert.IsNotNull(result);
        }

        [Test]
        public void LinkHtmlEncodesLinkText()
        {
            //arrange
            var viewContext = CreateViewContext();
            var viewDataContainer = new Mock<IViewDataContainer>();
            var html = HtmlHelperFactory.CreateHtmlHelper(viewContext, viewDataContainer.Object);

            //act
            var result = html.Link("<br />", "http://example.com");

            //assert
            Assert.AreEqual(@"<a href=""http://example.com"">&lt;br /&gt;</a>", result.ToString());
        }

        [Test]
        public void LinkHtmlAttributeEncodesHref()
        {
            //arrange
            var viewContext = CreateViewContext();
            var viewDataContainer = new Mock<IViewDataContainer>();
            var html = HtmlHelperFactory.CreateHtmlHelper(viewContext, viewDataContainer.Object);

            //act
            var result = html.Link("test", "<br />");

            //assert
            Assert.AreEqual(@"<a href=""&lt;br /&gt;"">test</a>", result.ToString());
        }

        [Test]
        public void LinkHtmlAttributeEncodesAttributes()
        {
            //arrange
            var viewContext = CreateViewContext();
            var viewDataContainer = new Mock<IViewDataContainer>();
            var html = HtmlHelperFactory.CreateHtmlHelper(viewContext, viewDataContainer.Object);

            //act
            var result = html.Link("linkText", "http://example.com", new { title = "<br />" });

            //assert
            Assert.AreEqual(@"<a href=""http://example.com"" title=""&lt;br /&gt;"">linkText</a>", result.ToString());
        }

        [Test]
        public void LinkOrDefaultReturnsIHtmlString()
        {
            //arrange
            var viewContext = CreateViewContext();
            var viewDataContainer = new Mock<IViewDataContainer>();
            var html = HtmlHelperFactory.CreateHtmlHelper(viewContext, viewDataContainer.Object);

            //act
            var result = html.LinkOrDefault("test", "http://example.com") as IHtmlContent;

            //assert
            Assert.IsNotNull(result);
        }

        [Test]
        public void LinkOrDefaultHtmlEncodesLinkText()
        {
            //arrange
            var viewContext = CreateViewContext();
            var viewDataContainer = new Mock<IViewDataContainer>();
            var html = HtmlHelperFactory.CreateHtmlHelper(viewContext, viewDataContainer.Object);

            //act
            var result = html.LinkOrDefault("<br />", "http://example.com");

            //assert
            Assert.AreEqual(@"<a href=""http://example.com"">&lt;br /&gt;</a>", result.ToString());
        }

        [Test]
        public void LinkOrDefaultWithoutHrefHtmlEncodesLinkText()
        {
            //arrange
            var viewContext = CreateViewContext();
            var viewDataContainer = new Mock<IViewDataContainer>();
            var html = HtmlHelperFactory.CreateHtmlHelper(viewContext, viewDataContainer.Object);

            //act
            var result = html.LinkOrDefault("<br />", null);

            //assert
            Assert.AreEqual(@"&lt;br /&gt;", result.ToString());
        }

        [Test]
        public void LinkOrDefaultWithHrefHtmlAttributeEncodesHref()
        {
            //arrange
            var viewContext = CreateViewContext();
            var viewDataContainer = new Mock<IViewDataContainer>();
            var html = HtmlHelperFactory.CreateHtmlHelper(viewContext, viewDataContainer.Object);

            //act
            var result = html.LinkOrDefault("test", "<br />");

            //assert
            Assert.AreEqual(@"<a href=""&lt;br /&gt;"">test</a>", result.ToString());
        }

        [Test]
        public void SelectOptionHtmlEncodesText()
        {
            //arrange
            var viewContext = CreateViewContext();
            var viewDataContainer = new Mock<IViewDataContainer>();
            var html = HtmlHelperFactory.CreateHtmlHelper(viewContext, viewDataContainer.Object);

            //act
            var result = html.SelectOption("value", false, "<br />");

            //assert
            Assert.AreEqual(@"<option value=""value"">&lt;br /&gt;</option>", result.ToString());
        }

        [Test]
        public void UnorderedListWithNullItemsReturnsEmptyHtmlString()
        {
            //arrange
            var viewContext = CreateViewContext();
            var viewDataContainer = new Mock<IViewDataContainer>();
            var html = HtmlHelperFactory.CreateHtmlHelper(viewContext, viewDataContainer.Object);

            //act
            var result = html.UnorderedList((IEnumerable<string>)null, (a, b) => new HtmlString(""), "test");

            //assert
            Assert.AreEqual(string.Empty, result.ToString());
        }

        [Test]
        public void UnorderedListWithEmptyItemsReturnsEmptyHtmlString()
        {
            //arrange
            var viewContext = CreateViewContext();
            var viewDataContainer = new Mock<IViewDataContainer>();
            var html = HtmlHelperFactory.CreateHtmlHelper(viewContext, viewDataContainer.Object);

            //act
            var result = html.UnorderedList(new string[] { }, (a, b) => new HtmlString(""), "test");

            //assert
            Assert.AreEqual(string.Empty, result.ToString());
        }

        [Test]
        public void HtmlHelperForEnablesLocalHelperMethods()
        {
            //arrange
            var viewContext = CreateViewContext();
            viewContext.ViewData.TemplateInfo.HtmlFieldPrefix = "topprefix";
            var viewDataContainer = new Mock<IViewDataContainer>();
            viewDataContainer.SetupGet(o => o.ViewData).Returns(() => new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()));
            var html = HtmlHelperFactory.CreateHtmlHelper(viewContext, viewDataContainer.Object);
            var localHelper = html.HtmlHelperFor(new { SomeString = "foo" }, "prefix");

            //act
            var result = localHelper.LabelFor(p => p.SomeString, "bar", null);

            //assert
            Assert.AreEqual(@"<label for=""prefix_SomeString"">bar</label>", result.ToHtmlString());
        }
        [Test]
        public void Ellipsize_DontCutHtmlEncodedChars()
        {
            //arrange
            var viewContext = CreateViewContext();
            var viewDataContainer = new Mock<IViewDataContainer>();
            var html = HtmlHelperFactory.CreateHtmlHelper(viewContext, viewDataContainer.Object);

            //act
            var result = html.Ellipsize("foo & bar", 5);

            //assert
            // the ASP.NET Core html encoder encodes the non ASCII characters with hexadecimal references
            Assert.AreEqual("foo &amp;&#xA0;&#x2026;", result.ToString());

        }

        [Test]
        public void Excerpt_DontCutHtmlEncodedChars()
        {
            //arrange
            var viewContext = CreateViewContext();
            var viewDataContainer = new Mock<IViewDataContainer>();
            var html = HtmlHelperFactory.CreateHtmlHelper(viewContext, viewDataContainer.Object);

            //act
            var result = html.Excerpt("<p>foo &amp; bar</p>", 7);

            //assert
            // the ASP.NET Core html encoder encodes the non ASCII characters with hexadecimal references
            Assert.AreEqual("foo &amp;&#xA0;&#x2026;", result.ToString());

        }
    }
}
