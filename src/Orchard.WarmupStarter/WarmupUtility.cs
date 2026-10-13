using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Orchard.WarmupStarter
{
    public static class WarmupUtility
    {
        public static readonly string WarmupFilesPath = "~/App_Data/Warmup/";

        /// <summary>
        /// return true if the request can be processed while the application is warming up (the warmup copy
        /// of the page has been served, or the request is for a static file) - return false to put the request on hold
        /// </summary>
        public static async Task<bool> DoBeginRequestAsync(HttpContext context)
        {
            var contentRoot = context.RequestServices.GetRequiredService<IWebHostEnvironment>().ContentRootPath;

            // use the url as it was requested by the client
            // the real url might be different if it has been translated (proxy, load balancing, ...)
            var url = ToUrlString(context.Request);
            var virtualFileCopy = EncodeUrl(url.Trim('/'));
            var localCopy = Path.Combine(contentRoot, WarmupFilesPath.TrimStart('~', '/'), virtualFileCopy);

            if (File.Exists(localCopy))
            {
                // result should not be cached, even on proxies
                var response = context.Response;
                response.Headers.Expires = DateTime.UtcNow.AddDays(-1).ToString("R");
                response.Headers.CacheControl = "no-cache, no-store, must-revalidate, proxy-revalidate";
                response.Headers.Pragma = "no-cache";

                await response.SendFileAsync(localCopy);
                return true;
            }

            // there is no local copy and the file exists
            // serve the static file
            var path = context.Request.PathBase.Add(context.Request.Path).Value ?? string.Empty;
            var physicalPath = Path.Combine(contentRoot, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (path.Length > 1 && File.Exists(physicalPath))
            {
                return true;
            }

            return false;
        }

        public static string ToUrlString(HttpRequest request)
        {
            return string.Format("{0}://{1}{2}", request.Scheme, request.Headers["Host"], request.GetEncodedPathAndQuery());
        }

        public static string EncodeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException("url can't be empty");
            }

            var sb = new StringBuilder();
            foreach (var c in url.ToLowerInvariant())
            {
                // only accept alphanumeric chars
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                {
                    sb.Append(c);
                }
                // otherwise encode them in UTF8
                else
                {
                    sb.Append("_");
                    foreach (var b in Encoding.UTF8.GetBytes(new[] { c }))
                    {
                        sb.Append(b.ToString("X"));
                    }
                }
            }

            return sb.ToString();
        }
    }
}
