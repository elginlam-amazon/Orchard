using System.Collections.Generic;

namespace Orchard.Owin
{
    /// <summary>
    /// Represents a provider that makes ASP.NET Core middlewares available for the Orchard tenant pipeline.
    /// </summary>
    /// <remarks>
    /// If you want to write a middleware and inject it into the Orchard tenant pipeline, implement this interface.
    /// </remarks>
    public interface IOwinMiddlewareProvider : IDependency
    {
        /// <summary>
        /// Gets <see cref="OwinMiddlewareRegistration"/> objects that will be used to alter the Orchard tenant pipeline.
        /// </summary>
        /// <returns><see cref="OwinMiddlewareRegistration"/> objects that will be used to alter the Orchard tenant pipeline.</returns>
        IEnumerable<OwinMiddlewareRegistration> GetOwinMiddlewares();
    }
}