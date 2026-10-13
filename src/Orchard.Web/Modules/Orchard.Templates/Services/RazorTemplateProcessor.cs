using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Orchard.DisplayManagement.Implementation;
using Orchard.Environment.Extensions;
using Orchard.Logging;
using Orchard.Mvc.Extensions;
using Orchard.Templates.Compilation.Razor;

namespace Orchard.Templates.Services
{
    [OrchardFeature("Orchard.Templates.Razor")]
    public class RazorTemplateProcessor : TemplateProcessorImpl
    {
        private readonly IRazorCompiler _compiler;
        private readonly HttpContext _httpContextBase;
        private readonly IWorkContextAccessor _wca;

        public override string Type => "Razor";

        public RazorTemplateProcessor(
            IRazorCompiler compiler,
            HttpContext httpContextBase,
            IWorkContextAccessor wca)
        {

            _compiler = compiler;
            _httpContextBase = httpContextBase;
            _wca = wca;
            Logger = NullLogger.Instance;
        }

        ILogger Logger { get; set; }

        public override void Verify(string template)
        {
            var viewPath = _compiler.CompileRazor(template, null, new Dictionary<string, object>());

            // compiling the view reports the template syntax errors
            GetView(_httpContextBase, viewPath);
        }

        public override string Process(string template, string name, DisplayContext context = null, dynamic model = null)
        {
            if (string.IsNullOrEmpty(template))
                return string.Empty;

            var viewPath = _compiler.CompileRazor(template, name, new Dictionary<string, object>());
            var result = ActivateAndRenderTemplate(viewPath, context, model);
            return result;
        }

        private static IView GetView(HttpContext httpContext, string viewPath)
        {
            var razorViewEngine = httpContext.RequestServices.GetRequiredService<IRazorViewEngine>();
            var viewResult = razorViewEngine.GetView(null, viewPath, false);
            if (!viewResult.Success)
            {
                throw new InvalidOperationException(string.Format("The Razor template '{0}' could not be found.", viewPath));
            }

            return viewResult.View;
        }

        private string ActivateAndRenderTemplate(string viewPath, DisplayContext displayContext, object model)
        {
            using (var writer = new StringWriter())
            {
                ViewContext viewContext;

                if (displayContext != null && displayContext.ViewContext != null && displayContext.ViewContext.View != null)
                {
                    var view = GetView(displayContext.ViewContext.HttpContext, viewPath);
                    var viewData = new ViewDataDictionary(displayContext.ViewDataContainer?.ViewData ?? displayContext.ViewContext.ViewData) { Model = model };
                    viewContext = new ViewContext(displayContext.ViewContext, view, viewData, writer);
                }
                else
                {
                    // Setup a fake view context in order to support razor syntax inside of HTML attributes,
                    // for instance: <a href="@WorkContext.CurrentSite.BaseUrl">Homepage</a>.
                    var view = GetView(_httpContextBase, viewPath);
                    var actionContext = _httpContextBase.GetActionContext();
                    actionContext.RouteData.DataTokens["IWorkContextAccessor"] = _wca;
                    var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = model };
                    viewContext = new ViewContext(
                        actionContext,
                        view,
                        viewData,
                        new TempDataDictionary(_httpContextBase, new NullTempDataProvider()),
                        writer,
                        new HtmlHelperOptions());
                }

                // Templates are rendered synchronously.
                viewContext.View.RenderAsync(viewContext).GetAwaiter().GetResult();
                return writer.ToString();
            }
        }

        private class NullTempDataProvider : ITempDataProvider
        {
            public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

            public void SaveTempData(HttpContext context, IDictionary<string, object> values)
            {
            }
        }
    }
}
