using System.IO;
using Microsoft.AspNetCore.Mvc;
using Orchard.Environment;
using Orchard.FileSystems.WebSite;
using Orchard.Localization;
using Orchard.Mvc.Extensions;
using Orchard.Security;
using Orchard.Themes;
using Orchard.Utility.Extensions;

namespace Orchard.DesignerTools.Controllers
{
    [Themed]
    public class AlternateController : Controller
    {
        private readonly IWebSiteFolder _webSiteFolder;
        private readonly IThemeManager _themeManager;

        public AlternateController(IOrchardServices orchardServices, IWebSiteFolder webSiteFolder, IThemeManager themeManager)
        {
            _webSiteFolder = webSiteFolder;
            _themeManager = themeManager;
            Services = orchardServices;
        }

        public IOrchardServices Services { get; set; }
        public Localizer T { get; set; }

        public ActionResult Create(string template, string alternate, string returnUrl)
        {
            if (!Request.IsLocal() && !Services.Authorizer.Authorize(StandardPermissions.SiteOwner, T("Not authorized to create templates")))
                return new UnauthorizedResult();

            alternate = alternate.Replace("__", "-").Replace("_", ".");

            var currentTheme = Services.WorkContext.CurrentTheme;
            var alternateFilename = HostingEnvironment.MapPath(Path.Combine(currentTheme.Location, currentTheme.Id, "Views", alternate));
            var isCodeTemplate = template.Contains("::");

            // use same extension as template, or ".cshtml" if it's a code template))
            if (!isCodeTemplate && _webSiteFolder.FileExists(template))
            {
                alternateFilename += Path.GetExtension(template);

                using (var stream = System.IO.File.Create(alternateFilename))
                {
                    _webSiteFolder.CopyFileTo(template, stream);
                }
            }
            else
            {
                alternateFilename += ".cshtml";
                using (System.IO.File.Create(alternateFilename)) { }
            }

            return this.RedirectLocal(returnUrl);
        }
    }
}