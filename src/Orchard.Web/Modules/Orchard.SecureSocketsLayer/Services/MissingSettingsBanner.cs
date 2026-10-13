using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Orchard.Localization;
using Orchard.Mvc.Extensions;
using Orchard.Mvc.Html;
using Orchard.UI.Admin.Notification;
using Orchard.UI.Notify;

namespace Orchard.SecureSocketsLayer.Services
{
    public class MissingSettingsBanner : INotificationProvider
    {
        private readonly IOrchardServices _orchardServices;
        private readonly ISecureSocketsLayerService _sslService;

        public MissingSettingsBanner(IOrchardServices orchardServices, ISecureSocketsLayerService sslService)
        {
            _orchardServices = orchardServices;
            _sslService = sslService;
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        public IEnumerable<NotifyEntry> GetNotifications()
        {
            var workContext = _orchardServices.WorkContext;
            var settings = _sslService.GetSettings();

            if (!settings.Enabled)
            {
                var urlHelper = HtmlHelperFactory.CreateUrlHelper(workContext.HttpContext.GetActionContext());
                var url = urlHelper.Action("Ssl", "Admin", new { Area = "Settings" });
                yield return new NotifyEntry { Message = T("The <a href=\"{0}\">SSL settings</a> need to be configured.", url), Type = NotifyType.Warning };
            }
        }
    }
}
