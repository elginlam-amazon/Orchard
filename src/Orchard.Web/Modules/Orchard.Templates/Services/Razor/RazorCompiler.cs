using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Orchard.Caching;
using Orchard.Environment.Extensions;
using Orchard.Logging;
using Orchard.Mvc.ViewEngines.Razor;
using Orchard.Utility.Extensions;

namespace Orchard.Templates.Compilation.Razor
{
    /// <summary>
    /// Turns the Razor templates into views served by the <see cref="DynamicViewFileProvider"/>, which are compiled
    /// by the ASP.NET Core Razor runtime compilation (formerly System.Web.Razor code generation and CodeDom compilation).
    /// </summary>
    [OrchardFeature("Orchard.Templates.Razor")]
    public class RazorCompiler : IRazorCompiler
    {
        private readonly ICacheManager _cache;
        private readonly ISignals _signals;
        private const string DynamicallyGeneratedClassName = "RazorTemplate";
        private const string TemplatesPath = "/_OrchardTemplates/";
        private const string ForceRecompile = "Razor.ForceRecompile";
        private static readonly string[] DefaultNamespaces = {
                    "System",
                    "System.IO",
                    "System.Linq",
                    "System.Collections",
                    "System.Collections.Generic",
                    "System.Dynamic",
                    "System.Text",
                    "System.Web",
                    "Microsoft.AspNetCore.Html",
                    "Microsoft.AspNetCore.Mvc",
                    "Microsoft.AspNetCore.Mvc.Rendering",
                    "Microsoft.AspNetCore.Routing",
                    "Orchard.Templates.Compilation.Razor",
                    "Orchard.ContentManagement",
                    "Orchard.DisplayManagement",
                    "Orchard.DisplayManagement.Shapes",
                    "Orchard.Security.Permissions",
                    "Orchard.UI.Resources",
                    "Orchard.Security",
                    "Orchard.Mvc.Spooling",
                    "Orchard.Mvc.Html"
                };

        public RazorCompiler(
            ICacheManager cache,
            ISignals signals)
        {
            _cache = cache;
            _signals = signals;
            Logger = NullLogger.Instance;
        }

        public ILogger Logger { get; set; }

        public string CompileRazor(string code, string name, IDictionary<string, object> parameters)
        {
            var cacheKey = (name ?? DynamicallyGeneratedClassName) + GetHash(code);
            var generatedClassName = name != null ? name.Strip(c => !c.IsLetter() && !char.IsDigit(c)) : DynamicallyGeneratedClassName;

            return _cache.Get(cacheKey, ctx =>
            {
                ctx.Monitor(_signals.When(ForceRecompile));

                var modelTypeName = "dynamic";
                var reader = new StringReader(code);
                var builder = new StringBuilder();

                // The @model directive is used to compile a strongly-typed template.
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var trimmedLine = line.TrimStart(' ', '\t', '\n', '\r');
                    if (trimmedLine.StartsWith("@model "))
                    {
                        modelTypeName = trimmedLine.Substring("@model ".Length).Trim();
                        continue;
                    }

                    builder.AppendLine(line);
                }

                var header = new StringBuilder();
                foreach (var n in DefaultNamespaces)
                {
                    header.Append("@using ").AppendLine(n);
                }
                header.Append("@inherits Orchard.Templates.Compilation.Razor.RazorTemplateBase<").Append(modelTypeName).AppendLine(">");

                var path = TemplatesPath + generatedClassName + "_" + GetHash(code) + ".cshtml";
                return DynamicViewFileProvider.Instance.AddOrUpdate(path, header.ToString() + builder);
            });
        }

        public static string GetHash(string value)
        {
            var data = Encoding.UTF8.GetBytes(value);
            var hashData = SHA256.HashData(data);

            var strBuilder = new StringBuilder();
            hashData.Take(16).Aggregate(strBuilder, (current, b) => strBuilder.Append(b.ToString("x2")));

            return strBuilder.ToString();
        }
    }
}
