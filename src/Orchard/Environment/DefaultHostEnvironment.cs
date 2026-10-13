using System.IO;
using Microsoft.AspNetCore.Http;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Mvc;
using Orchard.Mvc.Extensions;
using Orchard.Services;
using IHttpContextAccessor = Orchard.Mvc.IHttpContextAccessor;

namespace Orchard.Environment
{
    public class DefaultHostEnvironment : HostEnvironment
    {
        private const string WebConfigPath = "~/web.config";
        private const string RefreshHtmlPath = "~/refresh.html";
        private const string HostRestartPath = "~/bin/HostRestart";
        private readonly IClock _clock;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DefaultHostEnvironment(IClock clock, IHttpContextAccessor httpContextAccessor)
        {
            _clock = clock;
            _httpContextAccessor = httpContextAccessor;
            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
        }

        public Localizer T { get; set; }
        public ILogger Logger { get; set; }

        public override void RestartAppDomain()
        {
            // There is no AppDomain recycling on .NET: the application is stopped and restarted by the process host.
            // The marker files are still written for the deployment tools watching them.
            bool success = TryWriteBinFolder() | TryWriteWebConfig();
            if (HostingEnvironment.IsHosted)
            {
                HostingEnvironment.InitiateShutdown();
                success = true;
            }

            if (!success)
            {
                throw new OrchardException(
                    T("Orchard needs to be restarted due to a configuration change, but was unable to do so.\r\n" +
                    "To prevent this issue in the future, a change to the web server configuration is required:\r\n" +
                    "- run the application in a full trust environment, or\r\n" +
                    "- give the application write access to the '{0}' folder, or\r\n" +
                    "- give the application write access to the '{1}' file.",
                    HostRestartPath, WebConfigPath));
            }

            // If setting up extensions/modules requires an AppDomain restart, it's very unlikely the
            // current request can be processed correctly.  So, we redirect to the same URL, so that the
            // new request will come to the newly started AppDomain.
            var httpContext = _httpContextAccessor.Current();
            if (!httpContext.IsBackgroundContext())
            {
                // Don't redirect posts...
                if (HttpMethods.IsGet(httpContext.Request.Method))
                {
                    httpContext.Response.Redirect(httpContext.Request.PathBase + httpContext.Request.Path + httpContext.Request.QueryString);
                }
                else
                {
                    httpContext.Response.ContentType = "text/html";
                    httpContext.Response.SendFileAsync(MapPath(RefreshHtmlPath)).GetAwaiter().GetResult();
                }
            }
        }

        private bool TryWriteWebConfig()
        {
            try
            {
                // In medium trust, "UnloadAppDomain" is not supported. Touch web.config
                // to force an AppDomain restart.
                File.SetLastWriteTimeUtc(MapPath(WebConfigPath), _clock.UtcNow);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TryWriteBinFolder()
        {
            try
            {
                var binMarker = MapPath(HostRestartPath);
                Directory.CreateDirectory(binMarker);

                using (var stream = File.CreateText(Path.Combine(binMarker, "marker.txt")))
                {
                    stream.WriteLine("Restart on '{0}'", _clock.UtcNow);
                    stream.Flush();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}