using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Orchard.Environment.Extensions.Compilers;
using Orchard.Exceptions;
using Orchard.FileSystems.VirtualPath;
using Orchard.Logging;

namespace Orchard.Environment
{
    public interface IBuildManager : IDependency
    {
        IEnumerable<Assembly> GetReferencedAssemblies();
        bool HasReferencedAssembly(string name);
        Assembly GetReferencedAssembly(string name);
        Assembly GetCompiledAssembly(string virtualPath);
    }

    public class DefaultBuildManager : IBuildManager
    {
        private readonly IVirtualPathProvider _virtualPathProvider;
        private readonly IAssemblyLoader _assemblyLoader;
        private readonly Lazy<IExtensionCompiler> _extensionCompiler;
        private readonly ConcurrentDictionary<string, Assembly> _compiledAssemblies = new ConcurrentDictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

        public ILogger Logger { get; set; }

        public DefaultBuildManager(
            IVirtualPathProvider virtualPathProvider,
            IAssemblyLoader assemblyLoader,
            Lazy<IExtensionCompiler> extensionCompiler)
        {

            _virtualPathProvider = virtualPathProvider;
            _assemblyLoader = assemblyLoader;
            _extensionCompiler = extensionCompiler;

            Logger = NullLogger.Instance;
        }

        public IEnumerable<Assembly> GetReferencedAssemblies()
        {
            // The assemblies of the application (formerly the ASP.NET "bin" referenced assemblies).
            return AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !assembly.IsDynamic);
        }

        public bool HasReferencedAssembly(string name)
        {
            var assemblyPath = _virtualPathProvider.Combine("~/bin", name + ".dll");
            return _virtualPathProvider.FileExists(assemblyPath)
                || File.Exists(Path.Combine(AppContext.BaseDirectory, name + ".dll"));
        }

        public Assembly GetReferencedAssembly(string name)
        {
            if (!HasReferencedAssembly(name))
                return null;

            return _assemblyLoader.Load(name);
        }


        public Assembly GetCompiledAssembly(string virtualPath)
        {
            try
            {
                return _compiledAssemblies.GetOrAdd(virtualPath, Compile);
            }
            catch (Exception ex)
            {
                if (ex.IsFatal())
                {
                    throw;
                }
                Logger.Warning(ex, "Error when compiling assembly under {0}.", virtualPath);
                return null;
            }
        }

        private Assembly Compile(string virtualPath)
        {
            if (!".csproj".Equals(Path.GetExtension(virtualPath), StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException(string.Format("Only extension project files can be compiled, '{0}' is not supported.", virtualPath));
            }

            var orchardVersion = new AssemblyName(typeof(IDependency).Assembly.FullName).Version;
            var assemblyBuilder = new RoslynAssemblyBuilder(new[] { "TRACE", string.Format("ORCHARD_{0}_{1}", orchardVersion.Major, orchardVersion.Minor) });

            _extensionCompiler.Value.Compile(new CompileExtensionContext
            {
                VirtualPath = virtualPath,
                AssemblyBuilder = assemblyBuilder
            });

            return assemblyBuilder.Build(Path.GetFileNameWithoutExtension(virtualPath));
        }
    }
}
