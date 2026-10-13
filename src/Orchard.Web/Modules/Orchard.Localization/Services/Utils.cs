using System.Web;
using Microsoft.AspNetCore.Http;
using Orchard.Mvc.Extensions;
using Orchard.Utility.Extensions;

namespace Orchard.CulturePicker.Services
{
    public static class Utils
    {
        public static string GetReturnUrl(HttpRequest request)
        {
            if (request.GetUrlReferrer() == null)
            {
                return "";
            }

            string localUrl = GetAppRelativePath(request.GetUrlReferrer().AbsolutePath, request);
            return HttpUtility.UrlDecode(localUrl);
        }

        public static string GetAppRelativePath(string logicalPath, HttpRequest request)
        {
            if (request.GetApplicationPath() == null)
            {
                return "";
            }

            logicalPath = logicalPath.ToLower();
            string appPath = request.GetApplicationPath().ToLower();
            if (appPath != "/")
            {
                appPath += "/";
            }
            else
            {
                return logicalPath.Substring(1);
            }

            return logicalPath.Replace(appPath, "");
        }
    }
}
