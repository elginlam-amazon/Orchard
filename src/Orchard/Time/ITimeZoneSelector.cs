using Microsoft.AspNetCore.Http;

namespace Orchard.Time
{
    public interface ITimeZoneSelector : IDependency
    {
        TimeZoneSelectorResult GetTimeZone(HttpContext context);
    }
}
