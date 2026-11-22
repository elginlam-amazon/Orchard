using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public ILogger Logger { get; set; }

        public DefaultBuildManager(
            IVirtualPathProvider virtualPathProvider,
            IAssemblyLoader assemblyLoader)
        {

            _virtualPathProvider = virtualPathProvider;
            _assemblyLoader = assemblyLoader;

            Logger = NullLogger.Instance;
        }

        public IEnumerable<Assembly> GetReferencedAssemblies()
        {
            // System.Web.Compilation.BuildManager is not available in .NET Core.
            // Return the assemblies currently loaded in the runtime instead.
            return AppDomain.CurrentDomain.GetAssemblies();
        }

        public bool HasReferencedAssembly(string name)
        {
            var assemblyPath = _virtualPathProvider.Combine("~/bin", name + ".dll");
            return _virtualPathProvider.FileExists(assemblyPath);
        }

        public Assembly GetReferencedAssembly(string name)
        {
            if (!HasReferencedAssembly(name))
                return null;

            return _assemblyLoader.Load(name);
        }

        public Assembly GetCompiledAssembly(string virtualPath)
        {
            // System.Web.Compilation.BuildManager.GetCompiledAssembly is not available in .NET Core.
            // Razor views are compiled by the Razor/MVC runtime; there is no equivalent on-demand API.
            Logger.Debug("GetCompiledAssembly: '{0}' – no-op on .NET Core.", virtualPath);
            return null;
        }
    }
}
