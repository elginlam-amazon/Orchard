using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Orchard.ContentManagement;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Orchard.OpenId.Models;
using Orchard.UI.Admin.Notification;
using Orchard.UI.Notify;

namespace Orchard.Azure.Authentication.Services.Twitter
{
    [OrchardFeature("Orchard.OpenId.Twitter")]
    public class MissingSettingsBanner : INotificationProvider
    {
        private readonly IOrchardServices _orchardServices;
        private readonly IUrlHelper _urlHelper;

        public MissingSettingsBanner(IOrchardServices orchardServices, IUrlHelper urlHelper)
        {
            _orchardServices = orchardServices;
            _urlHelper = urlHelper;

            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        public IEnumerable<NotifyEntry> GetNotifications()
        {
            var workContext = _orchardServices.WorkContext;
            var settings = workContext.CurrentSite.As<TwitterSettingsPart>();

            if (settings == null || !settings.IsValid())
            {
                var url = _urlHelper.Action("OpenId", "Admin", new { Area = "Settings" });
                yield return new NotifyEntry { Message = T("The <a href=\"{0}\">Twitter settings</a> need to be configured.", url), Type = NotifyType.Warning };
            }
        }
    }
}
