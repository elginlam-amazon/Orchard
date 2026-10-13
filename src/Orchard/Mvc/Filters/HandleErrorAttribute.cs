using System;

namespace Orchard.Mvc.Filters
{
    /// <summary>
    /// Marks an action (or a controller) handling its own errors: the Orchard unhandled exception filter
    /// doesn't render the error page for the exceptions it throws.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class HandleErrorAttribute : Attribute
    {
    }
}
