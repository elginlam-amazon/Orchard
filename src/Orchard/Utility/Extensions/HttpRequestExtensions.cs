using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Orchard.Utility.Extensions
{
    public static class HttpRequestExtensions
    {
        /// <summary>
        /// Returns the root part of a request.
        /// </summary>
        /// <example>http://localhost:3030</example>
        /// <remarks>Prevents port number issues by using the client requested host</remarks>
        public static string ToRootUrlString(this HttpRequest request)
        {
            return string.Format("{0}://{1}", request.Scheme, request.Headers["Host"]);
        }

        /// <summary>
        /// Returns the application root part of a request.
        /// </summary>
        /// <example>http://localhost:3030/OrchardLocal</example>
        /// <remarks>Prevents port number issues by using the client requested host</remarks>
        public static string ToApplicationRootUrlString(this HttpRequest request)
        {
            string url = string.Format("{0}://{1}{2}", request.Scheme, request.Headers["Host"], request.PathBase.Value == "/" ? string.Empty : request.PathBase.Value);
            return url;
        }

        /// <summary>
        /// Returns the client requested url.
        /// </summary>
        /// <example>http://localhost:3030/OrchardLocal/Admin/Blogs</example>
        /// <remarks>Prevents port number issues by using the client requested host</remarks>
        public static string ToUrlString(this HttpRequest request)
        {
            return string.Format("{0}://{1}{2}", request.Scheme, request.Headers["Host"], request.GetRawUrl());
        }

        /// <summary>
        /// Returns the requested url relative to the host (path base, path and query string), the former HttpRequest.GetRawUrl().
        /// </summary>
        public static string GetRawUrl(this HttpRequest request)
        {
            return request.PathBase.Add(request.Path).Value + request.QueryString.Value;
        }

        /// <summary>
        /// Returns the absolute url of the request (the former HttpRequest.GetUrl()).
        /// </summary>
        public static Uri GetUrl(this HttpRequest request)
        {
            return new Uri(request.GetDisplayUrl());
        }

        /// <summary>
        /// Returns the virtual path of the application, without the tenant url prefix (the former HttpRequest.GetApplicationPath()).
        /// </summary>
        public static string GetApplicationPath(this HttpRequest request)
        {
            if (request.HttpContext.Items.TryGetValue(VirtualPathUtility.ApplicationPathBaseKey, out var pathBase) && pathBase is string applicationPath && applicationPath.Length > 0)
            {
                return applicationPath;
            }

            return request.PathBase.HasValue ? request.PathBase.Value : "/";
        }

        /// <summary>
        /// Returns the app relative path of the request (~/...), relative to the tenant (the former HttpRequest.GetAppRelativeCurrentExecutionFilePath()).
        /// </summary>
        public static string GetAppRelativeCurrentExecutionFilePath(this HttpRequest request)
        {
            return "~" + (request.Path.HasValue ? request.Path.Value : "/");
        }

        /// <summary>
        /// Returns the languages of the Accept-Language header, ordered by preference (the former HttpRequest.GetUserLanguages()).
        /// </summary>
        public static string[] GetUserLanguages(this HttpRequest request)
        {
            var languages = request.GetTypedHeaders().AcceptLanguage;
            if (languages == null || languages.Count == 0)
            {
                return null;
            }

            return languages
                .OrderByDescending(language => language.Quality ?? 1)
                .Select(language => language.Value.ToString())
                .ToArray();
        }

        /// <summary>
        /// Returns a value of the query string, form, cookies or headers (the former HttpRequest.Params).
        /// </summary>
        public static string GetParam(this HttpRequest request, string key)
        {
            if (request.Query.TryGetValue(key, out var queryValue))
            {
                return queryValue.ToString();
            }

            if (request.HasFormContentType && request.Form.TryGetValue(key, out var formValue))
            {
                return formValue.ToString();
            }

            if (request.Cookies.TryGetValue(key, out var cookieValue))
            {
                return cookieValue;
            }

            return request.Headers.TryGetValue(key, out var headerValue) ? headerValue.ToString() : null;
        }

        /// <summary>
        /// Whether the request comes from the local computer (the former HttpRequest.IsLocal).
        /// </summary>
        public static bool IsLocal(this HttpRequest request)
        {
            var connection = request.HttpContext.Connection;
            if (connection.RemoteIpAddress == null)
            {
                return true;
            }

            return connection.LocalIpAddress != null
                ? connection.RemoteIpAddress.Equals(connection.LocalIpAddress) || System.Net.IPAddress.IsLoopback(connection.RemoteIpAddress)
                : System.Net.IPAddress.IsLoopback(connection.RemoteIpAddress);
        }

        /// <summary>
        /// Returns wether the specified url is local to the host or not
        /// </summary>
        /// <param name="request"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool IsLocalUrl(this HttpRequest request, string url)
        {

            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            url = url.Trim();

            if (url.StartsWith("~/"))
            {
                return true;
            }

            if (url.StartsWith("//") || url.StartsWith("/\\"))
            {
                return false;
            }

            // at this point is the url starts with "/" it is local
            if (url.StartsWith("/"))
            {
                return true;
            }

            // at this point, check for an fully qualified url
            try
            {
                var uri = new Uri(url);

                if (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                    && !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (uri.Authority.Equals(request.Headers["Host"], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // finally, check the base url from the settings
                var workContext = request.HttpContext.GetWorkContext();
                if (workContext != null)
                {
                    var baseUrl = workContext.CurrentSite.BaseUrl;
                    if (!string.IsNullOrWhiteSpace(baseUrl))
                    {
                        if (uri.Authority.Equals(new Uri(baseUrl).Authority, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch
            {
                // mall-formed url e.g, "abcdef"
                return false;
            }

        }
    }
}
