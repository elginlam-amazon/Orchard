using System;
using System.IO;

namespace Orchard.Environment
{
    /// <summary>
    /// Process wide information about the hosting environment (replaces System.Web.Hosting.HostingEnvironment).
    /// It is initialized by the web host at startup with the content root of the application.
    /// </summary>
    public static class HostingEnvironment
    {
        private static string _applicationPhysicalPath;

        /// <summary>
        /// Whether the application runs inside a web host (as opposed to the command line or unit tests).
        /// </summary>
        public static bool IsHosted { get; private set; }

        /// <summary>
        /// The virtual path of the application root, always "/": the path base of the requests is handled by ASP.NET Core.
        /// </summary>
        public static string ApplicationVirtualPath => "/";

        /// <summary>
        /// The physical path of the application root (the content root).
        /// </summary>
        public static string ApplicationPhysicalPath
        {
            get { return _applicationPhysicalPath ?? AppContext.BaseDirectory; }
        }

        private static Action _shutdown;

        /// <summary>
        /// Initializes the hosting environment.
        /// </summary>
        /// <param name="contentRootPath">The content root of the web application.</param>
        /// <param name="shutdown">The callback stopping the application (the process host is responsible for restarting it).</param>
        public static void Initialize(string contentRootPath, Action shutdown)
        {
            _applicationPhysicalPath = contentRootPath;
            _shutdown = shutdown;
            IsHosted = true;
        }

        /// <summary>
        /// Maps an application relative (~/...) or rooted (/...) virtual path to a physical path under the content root.
        /// </summary>
        public static string MapPath(string virtualPath)
        {
            if (virtualPath == null)
            {
                return null;
            }

            var relativePath = virtualPath;
            if (relativePath.StartsWith("~", StringComparison.Ordinal))
            {
                relativePath = relativePath.Substring(1);
            }

            relativePath = relativePath.TrimStart('/', '\\').Replace('\\', '/');
            var physicalPath = Path.Combine(ApplicationPhysicalPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

            return Path.GetFullPath(physicalPath);
        }

        /// <summary>
        /// Requests the application to stop, the former equivalent of an AppDomain recycle.
        /// </summary>
        public static void InitiateShutdown()
        {
            _shutdown?.Invoke();
        }
    }
}
