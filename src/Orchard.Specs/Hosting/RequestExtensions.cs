using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Orchard.Specs.Util;

namespace Orchard.Specs.Hosting
{
    public static class RequestExtensions
    {
        public static RequestDetails SendRequest(this WebHost webHost, string urlPath, IDictionary<string, IEnumerable<string>> postData, string requestMethod = null)
        {

            var physicalPath = Bleroy.FluentPath.Path.Get(webHost.PhysicalDirectory);

            bool isHomepage = urlPath == "/";

            if (!isHomepage)
                urlPath = StripVDir(urlPath, webHost.VirtualDirectory);

            var details = new RequestDetails
            {
                HostName = webHost.HostName,
                UrlPath = urlPath.Replace('\\', '/'),
            };

            int queryIndex = urlPath.IndexOf('?');
            if (queryIndex >= 0)
            {
                details.UrlPath = urlPath.Substring(0, queryIndex).Replace('\\', '/');
                details.Query = urlPath.Substring(queryIndex + 1);
            }

            var physicalFilePath = physicalPath.Combine(details.UrlPath.TrimStart('/', '\\'));
            details.Page = isHomepage ? "" : physicalFilePath.GetRelativePath(physicalPath).ToString();

            if (!File.Exists(physicalFilePath))
                details.Page = details.Page.Replace('\\', '/');

            if (!string.IsNullOrEmpty(webHost.Cookies))
            {
                details.RequestHeaders.Add("Cookie", webHost.Cookies);
            }

            details.RequestHeaders.Add("Accept-Charset", "utf-8");

            if (postData != null)
            {
                var requestBodyText = postData
                    .SelectMany(kv => kv.Value.Select(v => new { k = kv.Key, v }))
                    .Select((kv, n) => new { p = HttpUtility.UrlEncode(kv.k) + "=" + HttpUtility.UrlEncode(kv.v), n })
                    .Aggregate("", (a, x) => a + (x.n == 0 ? "" : "&") + x.p);

                if (requestMethod == "POST")
                    details.PostData = Encoding.UTF8.GetBytes(requestBodyText);
                else
                    details.Query = requestBodyText;
            }

            var setCookies = ProcessRequest(webHost, details);

            foreach (var setCookie in setCookies)
            {
                // Trace.WriteLine(string.Format("Set-Cookie: {0}", setCookie));
                var cookieName = setCookie.Split(';')[0].Split('=')[0];
                DateTime expires;
                if (!string.IsNullOrEmpty(webHost.Cookies)
                    && setCookie.Contains("expires=")
                    && DateTime.TryParse(setCookie.Split(new[] { "expires=" }, 2, StringSplitOptions.None)[1].Split(';')[0], out expires)
                    && expires < DateTime.Now)
                {
                    // remove
                    // Trace.WriteLine(string.Format("Removing cookie: {0}", cookieName));
                    webHost.Cookies = Regex.Replace(webHost.Cookies, string.Format("{0}=[^;]*;?", cookieName), "");
                }
                else if (!string.IsNullOrEmpty(webHost.Cookies)
                    && Regex.IsMatch(webHost.Cookies, string.Format("\b{0}=", cookieName)))
                {
                    // replace
                    // Trace.WriteLine(string.Format("Replacing cookie: {0}", cookieName));
                    webHost.Cookies = Regex.Replace(webHost.Cookies, string.Format("{0}=[^;]*(;?)", cookieName), string.Format("{0}$1", setCookie.Split(';')[0]));
                }
                else
                {
                    // add
                    // Trace.WriteLine(string.Format("Adding cookie: {0}", cookieName));
                    webHost.Cookies = (webHost.Cookies + ';' + setCookie.Split(';').FirstOrDefault()).Trim(';');
                }
                // Trace.WriteLine(string.Format("Cookie jar: {0}", webHost.Cookies));
            }

            return details;
        }

        private static string StripVDir(string urlPath, string virtualDirectory)
        {
            if (urlPath == "/")
                return urlPath;

            return urlPath.StartsWith(virtualDirectory, StringComparison.OrdinalIgnoreCase)
                ? urlPath.Substring(virtualDirectory.Length)
                : urlPath;
        }

        public static RequestDetails SendRequest(this WebHost webHost, string urlPath)
        {
            return webHost.SendRequest(urlPath, null);
        }

        /// <summary>
        /// Sends the request to the site test server (formerly an ASP.NET SimpleWorkerRequest), and returns the cookies set by the response.
        /// </summary>
        private static IList<string> ProcessRequest(WebHost webHost, RequestDetails details)
        {
            var virtualDirectory = webHost.VirtualDirectory ?? "/";

            var context = webHost.Server.SendAsync(httpContext =>
            {
                var request = httpContext.Request;
                request.Method = details.PostData == null ? "GET" : "POST";
                request.Host = new HostString(details.HostName);
                request.PathBase = virtualDirectory == "/" ? PathString.Empty : new PathString("/" + virtualDirectory.Trim('/'));
                request.Path = new PathString("/" + details.UrlPath.TrimStart('/'));
                if (!string.IsNullOrEmpty(details.Query))
                {
                    request.QueryString = new QueryString("?" + details.Query);
                }

                foreach (var header in details.RequestHeaders)
                {
                    request.Headers[header.Key] = header.Value;
                }

                if (details.PostData != null)
                {
                    request.ContentType = "application/x-www-form-urlencoded; charset=utf-8";
                    request.ContentLength = details.PostData.Length;
                    request.Body = new MemoryStream(details.PostData);
                }
            }).GetAwaiter().GetResult();

            var response = context.Response;
            details.StatusCode = response.StatusCode;
            details.StatusDescription = ReasonPhrases.GetReasonPhrase(response.StatusCode);

            foreach (var header in response.Headers)
            {
                details.ResponseHeaders[header.Key] = header.Value.ToString();
            }

            using (var reader = new StreamReader(response.Body, Encoding.UTF8))
            {
                details.ResponseText = reader.ReadToEnd();
            }

            return response.Headers.SetCookie.ToArray();
        }
    }
}

