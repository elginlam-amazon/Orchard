// Compatibility stubs for OWIN types on .NET 10
// Owin.IAppBuilder, Microsoft.Owin.*, and Owin extension methods
// are provided by the Owin 1.0 and Microsoft.Owin 4.2.3 NuGet packages.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Owin.Builder
{
    public class AppBuilder : global::Owin.IAppBuilder
    {
        private readonly IDictionary<string, object> _properties = new Dictionary<string, object>();
        public IDictionary<string, object> Properties => _properties;

        public global::Owin.IAppBuilder Use(object middleware, params object[] args) => this;
        public object Build(Type returnType) => null;
        public Func<IDictionary<string, object>, Task> Build() => _ => Task.CompletedTask;
        public global::Owin.IAppBuilder New() => new AppBuilder();
    }
}

// GetOwinContext extension method stub for HttpContextBase
namespace System.Web
{
    public static class HttpContextBaseOwinExtensions
    {
        public static Microsoft.Owin.IOwinContext GetOwinContext(this HttpContextBase context) => null;
    }
}
