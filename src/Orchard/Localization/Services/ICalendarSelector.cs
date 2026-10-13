using Microsoft.AspNetCore.Http;

namespace Orchard.Localization.Services
{
    public class CalendarSelectorResult
    {
        public int Priority
        {
            get;
            set;
        }
        public string CalendarName
        {
            get;
            set;
        }
    }

    public interface ICalendarSelector : IDependency
    {
        CalendarSelectorResult GetCalendar(HttpContext context);
    }
}
