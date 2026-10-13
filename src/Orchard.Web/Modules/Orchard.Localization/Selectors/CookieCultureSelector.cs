using Microsoft.AspNetCore.Http;
using Orchard.Environment.Configuration;
using Orchard.Environment.Extensions;
using Orchard.Localization.Providers;
using Orchard.Localization.Services;
using Orchard.Mvc;
using Orchard.Services;
using Orchard.UI.Admin;
using Orchard.Utility.Extensions;
using IHttpContextAccessor = Orchard.Mvc.IHttpContextAccessor;

namespace Orchard.Localization.Selectors
{
    [OrchardFeature("Orchard.Localization.CultureSelector")]
    public class CookieCultureSelector : ICultureSelector, ICultureStorageProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IClock _clock;
        private readonly ShellSettings _shellSettings;

        private const string FrontEndCookieName = "OrchardCurrentCulture-FrontEnd";
        private const string AdminCookieName = "OrchardCurrentCulture-Admin";
        private const int DefaultExpireTimeYear = 1;

        public CookieCultureSelector(
            IHttpContextAccessor httpContextAccessor,
            IClock clock,
            ShellSettings shellSettings)
        {
            _httpContextAccessor = httpContextAccessor;
            _clock = clock;
            _shellSettings = shellSettings;
        }

        public void SetCulture(string culture)
        {
            var httpContext = _httpContextAccessor.Current();

            if (httpContext == null) return;

            var cookieName = AdminFilter.IsApplied(httpContext) ? AdminCookieName : FrontEndCookieName;

            var cookieOptions = new CookieOptions
            {
                Expires = _clock.UtcNow.AddYears(DefaultExpireTimeYear),
                Domain = httpContext.Request.IsLocal() ? null : httpContext.Request.GetUrl().Host
            };

            if (!string.IsNullOrEmpty(_shellSettings.RequestUrlPrefix))
            {
                cookieOptions.Path = GetCookiePath(httpContext);
            }

            // the request cookies are read-only, the new culture is used for the rest of the request
            httpContext.Items[cookieName] = culture;
            httpContext.Response.Cookies.Append(cookieName, culture, cookieOptions);
        }

        public CultureSelectorResult GetCulture(HttpContext context)
        {
            if (context == null) return null;

            var cookieName = AdminFilter.IsApplied(context) ? AdminCookieName : FrontEndCookieName;

            var culture = context.Items.TryGetValue(cookieName, out var currentCulture)
                ? currentCulture as string
                : context.Request.Cookies[cookieName];

            if (culture != null)
                return new CultureSelectorResult { Priority = -1, CultureName = culture };

            return null;
        }

        private string GetCookiePath(HttpContext httpContext)
        {
            var cookiePath = httpContext.Request.GetApplicationPath();
            if (cookiePath != null && cookiePath.Length > 1)
            {
                cookiePath += '/';
            }

            cookiePath += _shellSettings.RequestUrlPrefix;

            return cookiePath;
        }
    }
}
