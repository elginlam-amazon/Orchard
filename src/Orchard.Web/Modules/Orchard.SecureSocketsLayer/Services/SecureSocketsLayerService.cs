using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Orchard.Caching;
using Orchard.ContentManagement;
using Orchard.Mvc.Extensions;
using Orchard.Mvc.Html;
using Orchard.SecureSocketsLayer.Models;
using Orchard.UI.Admin;
using Orchard.Utility.Extensions;

namespace Orchard.SecureSocketsLayer.Services
{
    public class SecureSocketsLayerService : ISecureSocketsLayerService
    {
        private readonly IWorkContextAccessor _workContextAccessor;
        private readonly ICacheManager _cacheManager;
        private readonly ISignals _signals;

        public SecureSocketsLayerService(
            IWorkContextAccessor workContextAccessor,
            ICacheManager cacheManager,
            ISignals signals)
        {
            _workContextAccessor = workContextAccessor;
            _cacheManager = cacheManager;
            _signals = signals;
        }

        public bool ShouldBeSecure(string actionName, string controllerName, RouteValueDictionary routeValues)
        {
            var requestContext = GetRequestContext(actionName, controllerName, routeValues);
            return ShouldBeSecure(requestContext, null);
        }

        public bool ShouldBeSecure(ActionContext requestContext)
        {
            return ShouldBeSecure(requestContext, null);
        }

        public bool ShouldBeSecure(ActionExecutingContext actionContext)
        {
            var requestContext = GetRequestContext(
                actionContext.ActionDescriptor.GetActionName(),
                actionContext.ActionDescriptor.GetControllerName(),
                actionContext.RouteData.Values);
            return ShouldBeSecure(requestContext, actionContext);
        }

        private bool ShouldBeSecure(ActionContext requestContext, ActionExecutingContext actionContext)
        {
            var controllerName = (string)requestContext.RouteData.Values["controller"];
            if (controllerName == null) return false;
            var actionName = (string)requestContext.RouteData.Values["action"];
            if (actionName == null) return false;

            var settings = GetSettings();
            if (settings == null || !settings.Enabled)
            {
                return false;
            }

            if (actionName.EndsWith("Ssl") || controllerName.EndsWith("Ssl"))
            {
                return true;
            }

            var actionDescriptor = actionContext != null
                ? actionContext.ActionDescriptor as ControllerActionDescriptor
                : FindActionDescriptor(requestContext, controllerName, actionName);
            if (actionDescriptor != null)
            {
                if (actionDescriptor.ControllerTypeInfo.GetCustomAttributes(typeof(RequireHttpsAttribute), false).Any())
                {
                    return true;
                }
                if (actionDescriptor.MethodInfo.GetCustomAttributes(typeof(RequireHttpsAttribute), false).Any())
                {
                    return true;
                }
            }

            if (settings.SecureEverything) return true;

            if (controllerName == "Account" &&
                (actionName == "LogOn"
                 || actionName == "ChangePassword"
                 || actionName == "AccessDenied"
                 || actionName == "Register"
                 || actionName.StartsWith("ChallengeEmail", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (controllerName == "Admin" || AdminFilter.IsApplied(requestContext))
            {
                return true;
            }

            if (!settings.CustomEnabled) return false;

            var urlHelper = HtmlHelperFactory.CreateUrlHelper(requestContext);
            var url = urlHelper.Action(actionName, controllerName, requestContext.RouteData.Values);

            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            return IsRequestProtected(
                url, requestContext.HttpContext.Request.GetApplicationPath(), settings);
        }

        public string SecureActionUrl(string actionName, string controllerName)
        {
            return SecureActionUrl(actionName, controllerName, new object());
        }

        public string SecureActionUrl(string actionName, string controllerName, object routeValues)
        {
            return SecureActionUrl(actionName, controllerName, new RouteValueDictionary(routeValues));
        }

        public string SecureActionUrl(string actionName, string controllerName, RouteValueDictionary routeValues)
        {
            var requestContext = GetRequestContext(actionName, controllerName, routeValues);
            var url = HtmlHelperFactory.CreateUrlHelper(requestContext);
            var actionUrl = url.Action(actionName, controllerName, routeValues);
            if (actionUrl == null) return null;
            var currentUri = _workContextAccessor.GetContext().HttpContext.Request.GetUrl();
            return currentUri != null && currentUri.Scheme.Equals(Uri.UriSchemeHttps) ?
                actionUrl /* action url is relative so will keep current protocol */ :
                MakeSecure(actionUrl);
        }

        public string InsecureActionUrl(string actionName, string controllerName)
        {
            return InsecureActionUrl(actionName, controllerName, new object());
        }

        public string InsecureActionUrl(string actionName, string controllerName, object routeValues)
        {
            return InsecureActionUrl(actionName, controllerName, new RouteValueDictionary(routeValues));
        }

        public string InsecureActionUrl(string actionName, string controllerName, RouteValueDictionary routeValues)
        {
            var requestContext = GetRequestContext(actionName, controllerName, routeValues);
            var url = HtmlHelperFactory.CreateUrlHelper(requestContext);
            var actionUrl = url.Action(actionName, controllerName, routeValues);
            if (actionUrl == null) return null;
            var currentUri = _workContextAccessor.GetContext().HttpContext.Request.GetUrl();
            return currentUri != null && currentUri.Scheme.Equals(Uri.UriSchemeHttp) ?
                actionUrl /* action url is relative so will keep current protocol */ :
                MakeInsecure(actionUrl);
        }

        /// <summary>
        /// Finds the MVC action matching the area, controller and action names.
        /// </summary>
        private static ControllerActionDescriptor FindActionDescriptor(ActionContext requestContext, string controllerName, string actionName)
        {
            var provider = requestContext.HttpContext.RequestServices?.GetService(typeof(IActionDescriptorCollectionProvider)) as IActionDescriptorCollectionProvider;
            if (provider == null)
            {
                return null;
            }

            var areaName = requestContext.RouteData.Values.TryGetValue("area", out var area) ? Convert.ToString(area, CultureInfo.InvariantCulture) : null;

            return provider.ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .FirstOrDefault(descriptor =>
                    string.Equals(descriptor.ControllerName, controllerName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(descriptor.ActionName, actionName, StringComparison.OrdinalIgnoreCase)
                    && (string.IsNullOrEmpty(areaName)
                        || (descriptor.RouteValues.TryGetValue("area", out var descriptorArea) && string.Equals(descriptorArea, areaName, StringComparison.OrdinalIgnoreCase))));
        }

        private ActionContext GetRequestContext(
            string actionName,
            string controllerName,
            RouteValueDictionary routeValues)
        {

            var httpContext = _workContextAccessor.GetContext().HttpContext;
            var routeData = new RouteData();
            foreach (var routeValue in routeValues)
            {
                routeData.Values[routeValue.Key] = routeValue.Value;
            }
            routeData.Values["controller"] = controllerName;
            routeData.Values["action"] = actionName;
            var requestContext = new ActionContext(httpContext, routeData, new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
            return requestContext;
        }

        private static bool IsRequestProtected(string path, string appPath, SslSettings settings)
        {
            var match = false;
            var sr = new StringReader(settings.Urls ?? "");
            string pattern;

            while (!match && null != (pattern = sr.ReadLine()))
            {
                pattern = pattern.Trim();
                match = IsMatched(pattern, path, appPath);
            }

            return match;
        }

        private static bool IsMatched(string pattern, string path, string appPath)
        {
            if (pattern.StartsWith("~/"))
            {
                pattern = pattern.Substring(2);
                if (appPath == "/")
                    appPath = "";
                pattern = string.Format("{0}/{1}", appPath, pattern);
            }

            if (!pattern.Contains("?"))
                pattern = pattern.TrimEnd('/');

            var requestPath = path;
            if (!requestPath.Contains("?"))
                requestPath = requestPath.TrimEnd('/');

            return pattern.EndsWith("*")
                ? requestPath.StartsWith(pattern.TrimEnd('*'), StringComparison.OrdinalIgnoreCase)
                : string.Equals(requestPath, pattern, StringComparison.OrdinalIgnoreCase);
        }

        public SslSettings GetSettings()
        {
            return _cacheManager.Get("SslSettings",
                ctx =>
                {
                    ctx.Monitor(_signals.When(SslSettingsPart.CacheKey));
                    var settingsPart = _workContextAccessor.GetContext().CurrentSite.As<SslSettingsPart>();
                    return new SslSettings
                    {
                        Urls = settingsPart.Urls,
                        CustomEnabled = settingsPart.CustomEnabled,
                        SecureEverything = settingsPart.SecureEverything,
                        SecureHostName = settingsPart.SecureHostName,
                        InsecureHostName = settingsPart.InsecureHostName,
                        Enabled = settingsPart.Enabled,
                        UsePermanentRedirect = settingsPart.UsePermanentRedirect
                    };
                });
        }

        private string MakeInsecure(string path)
        {
            var settings = GetSettings();
            if (settings == null) return path;
            var insecureHostName = settings.InsecureHostName;
            var builder = new UriBuilder(insecureHostName.Split(':').First().Trim('/') + path)
            {
                Scheme = Uri.UriSchemeHttp,
                Port = 80
            };
            SetPort(insecureHostName, builder);
            return builder.Uri.ToString();
        }

        private string MakeSecure(string path)
        {
            var settings = GetSettings();
            if (settings == null) return path;
            var secureHostName = settings.SecureHostName;
            var builder = new UriBuilder(secureHostName.Split(':').First().Trim('/') + path)
            {
                Scheme = Uri.UriSchemeHttps,
                Port = 443
            };
            SetPort(secureHostName, builder);
            return builder.Uri.ToString();
        }

        private static void SetPort(string hostName, UriBuilder builder)
        {
            if (string.IsNullOrWhiteSpace(hostName)) return;
            var splitSecuredHostName = hostName.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (splitSecuredHostName.Length == 2)
            {
                int port;
                if (int.TryParse(splitSecuredHostName[1], NumberStyles.Integer, CultureInfo.InvariantCulture,
                    out port))
                {
                    builder.Port = port;
                }
            }
        }
    }
}
