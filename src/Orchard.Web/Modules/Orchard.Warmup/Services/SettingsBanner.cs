using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Orchard.Localization;
using Orchard.Mvc.Extensions;
using Orchard.Mvc.Html;
using Orchard.UI.Admin.Notification;
using Orchard.UI.Notify;

namespace Orchard.Warmup.Services
{
    public class SettingsBanner : INotificationProvider
    {
        private readonly IOrchardServices _orchardServices;
        private readonly WorkContext _workContext;

        public SettingsBanner(IOrchardServices orchardServices, IWorkContextAccessor workContextAccessor)
        {
            _orchardServices = orchardServices;
            _workContext = workContextAccessor.GetContext();
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        public IEnumerable<NotifyEntry> GetNotifications()
        {
            if (string.IsNullOrWhiteSpace(_orchardServices.WorkContext.CurrentSite.BaseUrl))
            {
                var urlHelper = HtmlHelperFactory.CreateUrlHelper(_workContext.HttpContext.GetActionContext());
                var url = urlHelper.Action("Index", "Admin", new { Area = "Settings" });
                yield return new NotifyEntry { Message = T("The Warmup feature needs the <a href=\"{0}\">Base Url site setting</a> to be set.", url), Type = NotifyType.Warning };
            }
        }
    }
}
