using System.Collections.Generic;

namespace Orchard.Templates.Compilation.Razor
{
    public interface IRazorCompiler : IDependency
    {
        /// <summary>
        /// Registers a Razor template and returns the path of the corresponding view,
        /// which is compiled by the Razor view engine when it is first rendered.
        /// </summary>
        string CompileRazor(string code, string name, IDictionary<string, object> parameters);
    }
}
