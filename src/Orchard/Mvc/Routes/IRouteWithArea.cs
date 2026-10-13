namespace Orchard.Mvc.Routes
{
    /// <summary>
    /// A route that is bound to an area (an Orchard extension).
    /// </summary>
    public interface IRouteWithArea
    {
        string Area { get; }
    }
}
