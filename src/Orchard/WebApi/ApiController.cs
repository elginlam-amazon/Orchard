using Microsoft.AspNetCore.Mvc;

namespace Orchard.WebApi
{
    /// <summary>
    /// Base class of the Orchard HTTP (Web API) controllers.
    /// </summary>
    /// <remarks>
    /// ASP.NET Core unifies MVC and Web API controllers. This base class identifies the controllers which are
    /// routed with the Web API conventions (api/{area}/{controller}/{id}, actions selected by HTTP method)
    /// and filtered by the <see cref="Filters.IApiFilterProvider"/> filters, see <see cref="ApiControllerConvention"/>.
    /// </remarks>
    public abstract class ApiController : ControllerBase
    {
    }
}
