// Stubs for .NET Framework hosting APIs not available in net10
using System;

namespace System.Web.Hosting
{
    public interface IRegisteredObject
    {
        void Stop(bool immediate);
    }
}

namespace System.Web.Compilation
{
    public class ClientBuildManager
    {
        public ClientBuildManager(string virtualPath, string physicalPath) { }
        public void CompileApplicationDependencies() { }
        public object CreateObject(Type type, bool failIfExists)
            => throw new PlatformNotSupportedException("ClientBuildManager is not supported on .NET 10");
    }
}

namespace System.Web.Hosting
{
    public class ApplicationManager
    {
        public static ApplicationManager GetApplicationManager() => new ApplicationManager();
        public void ShutdownAll() { }
    }
}
