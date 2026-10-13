using Microsoft.AspNetCore.Http;

namespace Orchard.Localization.Services
{
    public class SiteCalendarSelector : ICalendarSelector
    {
        private readonly IWorkContextAccessor _workContextAccessor;

        public SiteCalendarSelector(IWorkContextAccessor workContextAccessor)
        {
            _workContextAccessor = workContextAccessor;
        }

        public CalendarSelectorResult GetCalendar(HttpContext context)
        {
            string currentCalendarName = _workContextAccessor.GetContext().CurrentSite.SiteCalendar;

            if (string.IsNullOrEmpty(currentCalendarName))
            {
                return null;
            }

            return new CalendarSelectorResult
            {
                Priority = -5,
                CalendarName = currentCalendarName
            };
        }
    }
}
