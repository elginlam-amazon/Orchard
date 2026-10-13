using System;
using System.Collections.Concurrent;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Orchard.Web.Hosting
{
    /// <summary>
    /// Serves the static files of the folders which allowed it with their IIS Web.config file (the Content, Scripts,
    /// Styles folders of the modules and themes, the Media folder...). The application Web.config cleared all the
    /// IIS handlers, so any other file of the application was never served.
    /// </summary>
    public static class StaticFileAccessPolicy
    {
        private static readonly ConcurrentDictionary<string, bool> _allowedDirectories = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        public static IApplicationBuilder UseOrchardStaticFiles(this IApplicationBuilder app)
        {
            var environment = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
            var contentRoot = Path.GetFullPath(environment.ContentRootPath);

            var contentTypeProvider = new FileExtensionContentTypeProvider();
            contentTypeProvider.Mappings[".mp3"] = "audio/mpeg";
            contentTypeProvider.Mappings[".mp4"] = "video/mp4";
            contentTypeProvider.Mappings[".pdf"] = "application/pdf";
            contentTypeProvider.Mappings[".svg"] = "image/svg+xml";
            contentTypeProvider.Mappings[".woff"] = "application/font-woff";
            contentTypeProvider.Mappings[".woff2"] = "font/woff2";

            return app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(contentRoot),
                ContentTypeProvider = contentTypeProvider,
                OnPrepareResponse = context =>
                {
                    if (!IsAllowed(contentRoot, context.File.PhysicalPath))
                    {
                        // Not a static file folder: behave as if the file didn't exist.
                        context.Context.Response.StatusCode = 404;
                        context.Context.Response.ContentLength = 0;
                        context.Context.Response.Body = Stream.Null;
                        return;
                    }

                    // <clientCache cacheControlMode="UseMaxAge" cacheControlMaxAge="7.00:00:00" />
                    context.Context.Response.Headers.CacheControl = "public, max-age=604800";
                }
            });
        }

        /// <summary>
        /// Returns <c>true</c> if a Web.config file of the folder of the file (or of one of its parent folders in the
        /// application) enables the IIS static file handler.
        /// </summary>
        public static bool IsAllowed(string contentRoot, string physicalPath)
        {
            if (string.IsNullOrEmpty(physicalPath))
            {
                return false;
            }

            var directory = Path.GetDirectoryName(Path.GetFullPath(physicalPath));
            while (directory != null && directory.Length > contentRoot.Length && directory.StartsWith(contentRoot, StringComparison.OrdinalIgnoreCase))
            {
                if (_allowedDirectories.GetOrAdd(directory, AllowsStaticFiles))
                {
                    return true;
                }

                directory = Path.GetDirectoryName(directory);
            }

            return false;
        }

        private static bool AllowsStaticFiles(string directory)
        {
            foreach (var fileName in new[] { "Web.config", "web.config" })
            {
                var webConfig = Path.Combine(directory, fileName);
                if (File.Exists(webConfig))
                {
                    return File.ReadAllText(webConfig).IndexOf("StaticFileModule", StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }

            return false;
        }
    }
}
