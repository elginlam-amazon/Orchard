using Microsoft.AspNetCore.Routing;

namespace Orchard.Blogs.Routing
{
    public interface IRsdConstraint : IRouteConstraint, ISingletonDependency
    {
        string FindPath(string path);
    }
}