using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Orchard.Environment.Configuration;
using Orchard.Utility.Extensions;

namespace Orchard.MultiTenancy.Extensions
{
    public static class UrlHelperExtensions
    {
        public static string Tenant(this IUrlHelper urlHelper, ShellSettings tenantShellSettings)
        {

            var requestUrlHost = !string.IsNullOrEmpty(tenantShellSettings.RequestUrlHost) ? tenantShellSettings.RequestUrlHost.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).First() : null;

            //info: (heskew) might not keep the port/vdir insertion around beyond...
            var port = string.Empty;
            var host = urlHelper.ActionContext.HttpContext.Request.Headers["Host"].ToString();

            if (host.Contains(":"))
                port = host.Substring(host.IndexOf(":"));

            var result = string.Format("{0}://{1}",
                urlHelper.ActionContext.HttpContext.Request.GetUrl().Scheme,
                !string.IsNullOrEmpty(requestUrlHost) ? requestUrlHost + port : host);

            var applicationPath = urlHelper.ActionContext.HttpContext.Request.GetApplicationPath();
            if (!string.IsNullOrEmpty(applicationPath) && applicationPath != "/")
                result += applicationPath;

            if (!string.IsNullOrEmpty(tenantShellSettings.RequestUrlPrefix))
                result += "/" + tenantShellSettings.RequestUrlPrefix;

            return result;
        }
    }
}