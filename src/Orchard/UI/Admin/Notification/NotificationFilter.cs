using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;
using Orchard.DisplayManagement;
using Orchard.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Orchard.UI.Admin.Notification
{
    public class AdminNotificationFilter : FilterProvider, IResultFilter
    {
        private readonly INotificationManager _notificationManager;
        private readonly IWorkContextAccessor _workContextAccessor;
        private readonly dynamic _shapeFactory;

        public AdminNotificationFilter(
            INotificationManager notificationManager,
            IWorkContextAccessor workContextAccessor,
            IShapeFactory shapeFactory)
        {
            _notificationManager = notificationManager;
            _workContextAccessor = workContextAccessor;
            _shapeFactory = shapeFactory;
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            if (!AdminFilter.IsApplied(filterContext))
                return;

            // if it's not a view result, a redirect for example
            if (!(filterContext.Result is ViewResult))
                return;



            var messageEntries = _notificationManager.GetNotifications().ToList();
            if (!messageEntries.Any())
                return;

            var messagesZone = _workContextAccessor.GetContext(filterContext).Layout.Zones["Messages"];
            foreach (var messageEntry in messageEntries)
                messagesZone = messagesZone.Add(_shapeFactory.Message(messageEntry));
        }

        public void OnResultExecuted(ResultExecutedContext filterContext) { }
    }
}
