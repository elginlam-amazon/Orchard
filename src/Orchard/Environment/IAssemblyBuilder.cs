using System.CodeDom;
using System.Reflection;

namespace Orchard.Environment
{
    public interface IAssemblyBuilder
    {
        void AddCodeCompileUnit(CodeCompileUnit compileUnit);
        void AddAssemblyReference(Assembly assembly);
    }

    // AspNetAssemblyBuilder was removed: System.Web.Compilation.AssemblyBuilder and BuildProvider
    // do not exist in .NET Core. The Roslyn/Razor compilation pipeline is used instead.
    public class AspNetAssemblyBuilder : IAssemblyBuilder
    {
        public void AddCodeCompileUnit(CodeCompileUnit compileUnit) { }
        public void AddAssemblyReference(Assembly assembly) { }
    }
}
