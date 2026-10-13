using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Orchard.Environment
{
    public interface IAssemblyBuilder
    {
        void AddCodeCompileUnit(CodeCompileUnit compileUnit);
        void AddAssemblyReference(Assembly assembly);
    }

    /// <summary>
    /// Compiles the code units of an extension into an assembly with Roslyn
    /// (replaces the ASP.NET BuildManager / CodeDom based compilation).
    /// </summary>
    public class RoslynAssemblyBuilder : IAssemblyBuilder
    {
        private readonly List<SyntaxTree> _syntaxTrees = new List<SyntaxTree>();
        private readonly HashSet<Assembly> _references = new HashSet<Assembly>();
        private readonly CSharpParseOptions _parseOptions;

        public RoslynAssemblyBuilder(IEnumerable<string> preprocessorSymbols = null)
        {
            _parseOptions = new CSharpParseOptions(LanguageVersion.Latest, preprocessorSymbols: preprocessorSymbols ?? Enumerable.Empty<string>());
        }

        public void AddCodeCompileUnit(CodeCompileUnit compileUnit)
        {
            var snippet = compileUnit as CodeSnippetCompileUnit;
            if (snippet == null)
            {
                throw new NotSupportedException("Only code snippet compile units are supported.");
            }

            var path = snippet.LinePragma != null ? snippet.LinePragma.FileName : string.Empty;
            _syntaxTrees.Add(CSharpSyntaxTree.ParseText(SourceText.From(snippet.Value, System.Text.Encoding.UTF8), _parseOptions, path));
        }

        public void AddAssemblyReference(Assembly assembly)
        {
            _references.Add(assembly);
        }

        public Assembly Build(string assemblyName)
        {
            var references = GetMetadataReferences();

            var compilation = CSharpCompilation.Create(
                assemblyName,
                _syntaxTrees,
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release));

            using (var assemblyStream = new MemoryStream())
            using (var symbolsStream = new MemoryStream())
            {
                var result = compilation.Emit(assemblyStream, symbolsStream);
                if (!result.Success)
                {
                    var errors = result.Diagnostics
                        .Where(d => d.Severity == DiagnosticSeverity.Error)
                        .Select(d => d.ToString());
                    throw new OrchardCoreException(new Localization.LocalizedString(
                        string.Format("Compilation of '{0}' failed:\r\n{1}", assemblyName, string.Join("\r\n", errors))));
                }

                assemblyStream.Seek(0, SeekOrigin.Begin);
                symbolsStream.Seek(0, SeekOrigin.Begin);
                return AssemblyLoadContext.Default.LoadFromStream(assemblyStream, symbolsStream);
            }
        }

        private IEnumerable<MetadataReference> GetMetadataReferences()
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // The platform (framework) assemblies.
            var trustedPlatformAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
            if (!string.IsNullOrEmpty(trustedPlatformAssemblies))
            {
                foreach (var path in trustedPlatformAssemblies.Split(Path.PathSeparator))
                {
                    paths.Add(path);
                }
            }

            // The assemblies loaded by the application, and the explicit references.
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Concat(_references))
            {
                if (!assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                {
                    paths.Add(assembly.Location);
                }
            }

            // Keep a single reference per assembly name.
            return paths
                .GroupBy(p => Path.GetFileNameWithoutExtension(p), StringComparer.OrdinalIgnoreCase)
                .Select(g => MetadataReference.CreateFromFile(g.Last()))
                .ToArray();
        }
    }
}
