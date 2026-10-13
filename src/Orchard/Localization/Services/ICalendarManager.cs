using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace Orchard.Localization.Services
{
    public interface ICalendarManager : IDependency
    {
        IEnumerable<string> ListCalendars();
        string GetCurrentCalendar(HttpContext requestContext);
        Calendar GetCalendarByName(string calendarName);
    }
}
