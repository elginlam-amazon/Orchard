using System.Reflection;

namespace Orchard.Environment.Extensions.Compilers
{
    // CSharpExtensionBuildProviderShim previously extended System.Web.Compilation.BuildProvider,
    // which does not exist in .NET Core. The class is kept as an IShim registration stub only;
    // the ASP.NET Framework compilation pipeline it bridged is replaced by the Roslyn/Razor runtime.
    public class CSharpExtensionBuildProviderShim : IShim
    {
        public CSharpExtensionBuildProviderShim()
        {
            OrchardHostContainerRegistry.RegisterShim(this);
        }

        public IOrchardHostContainer HostContainer { get; set; }
    }
}
