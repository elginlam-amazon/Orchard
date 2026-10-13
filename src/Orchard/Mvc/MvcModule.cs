using System;
using System.IO;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Orchard.Mvc.Extensions;
using Orchard.Mvc.Routes;
using Orchard.Settings;

namespace Orchard.Mvc
{
    public class MvcModule : Module
    {
        public const string IsBackgroundHttpContextKey = "IsBackgroundHttpContext";

        /// <summary>
        /// Name of the registration of the ASP.NET Core application <see cref="IServiceProvider"/> in the host container.
        /// </summary>
        public const string ApplicationServicesKey = "AspNetCore.ApplicationServices";

        protected override void Load(ContainerBuilder moduleBuilder)
        {
            moduleBuilder.RegisterType<ShellRoute>().InstancePerDependency();

            moduleBuilder.Register(HttpContextBaseFactory).As<HttpContext>().InstancePerDependency();
            moduleBuilder.Register(RequestContextFactory).As<ActionContext>().InstancePerDependency();
            moduleBuilder.Register(UrlHelperFactory).As<IUrlHelper>().InstancePerDependency();
        }

        static HttpContext HttpContextBaseFactory(IComponentContext context)
        {

            var httpContext = context.Resolve<IHttpContextAccessor>().Current();
            if (httpContext != null)
                return httpContext;

            var siteService = context.Resolve<ISiteService>();

            // Wrapping the code accessing the SiteSettings in a function that will be executed later (in HttpContextPlaceholder),
            // so that the ActionContext will have been established when the time comes to actually load the site settings,
            // which requires activating the Site content item, which in turn requires a UrlHelper, which in turn requires an ActionContext,
            // thus preventing a StackOverflowException.

            var baseUrl = new Func<string>(() =>
            {
                var s = siteService.GetSiteSettings().BaseUrl;

                // When Setup is running from the command line, no BaseUrl exists yet.
                return string.IsNullOrEmpty(s) ? "http://localhost" : s;
            });

            IServiceProvider applicationServices;
            context.TryResolveNamed(ApplicationServicesKey, typeof(IServiceProvider), out var services);
            applicationServices = services as IServiceProvider;

            return HttpContextPlaceholder.Create(baseUrl, applicationServices);
        }

        static ActionContext RequestContextFactory(IComponentContext context)
        {

            var httpContext = HttpContextBaseFactory(context);

            RouteData routeData;
            if (!httpContext.IsBackgroundContext())
            {
                // The route data of the current request (set by the routing middleware).
                routeData = new RouteData(httpContext.GetRouteData());
            }
            else
            {
                routeData = new RouteData();
            }

            // Url generation requires a root router.
            if (routeData.Routers.Count == 0 && context.TryResolve(out OrchardRouteCollection routes))
            {
                routeData.Routers.Add(routes);
            }

            return new ActionContext(httpContext, routeData, new ActionDescriptor());
        }

        static IUrlHelper UrlHelperFactory(IComponentContext context)
        {
            return new UrlHelper(context.Resolve<ActionContext>());
        }

        /// <summary>
        /// Standin context for background tasks.
        /// </summary>
        public static class HttpContextPlaceholder
        {
            public static HttpContext Create(Func<string> baseUrl, IServiceProvider applicationServices = null)
            {
                var httpContext = new DefaultHttpContext(CreateFeatures(baseUrl));
                httpContext.Items[IsBackgroundHttpContextKey] = true;
                if (applicationServices != null)
                {
                    httpContext.RequestServices = applicationServices;
                }
                return httpContext;
            }

            private static IFeatureCollection CreateFeatures(Func<string> baseUrl)
            {
                var features = new FeatureCollection();
                features.Set<IHttpRequestFeature>(new HttpRequestPlaceholder(new Lazy<string>(baseUrl)));
                features.Set<IHttpResponseFeature>(new HttpResponseFeature());
                features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(Stream.Null));
                return features;
            }
        }

        /// <summary>
        /// Standin request for background tasks, built from the site base url.
        /// The base url is resolved lazily (see the HttpContext factory comments).
        /// </summary>
        public class HttpRequestPlaceholder : IHttpRequestFeature
        {
            private readonly Lazy<string> _baseUrl;
            private Uri _uri;
            private IHeaderDictionary _headers;
            private string _pathBase;

            public HttpRequestPlaceholder(Lazy<string> baseUrl)
            {
                _baseUrl = baseUrl;
            }

            private Uri Url
            {
                get
                {
                    if (_uri == null)
                    {
                        _uri = new Uri(_baseUrl.Value);
                    }
                    return _uri;
                }
            }

            public string Protocol { get; set; } = "HTTP/1.1";

            private string _scheme;
            public string Scheme
            {
                get => _scheme ?? Url.Scheme;
                set => _scheme = value;
            }

            public string Method { get; set; } = "";

            public string PathBase
            {
                get => _pathBase ?? Url.AbsolutePath.TrimEnd('/');
                set => _pathBase = value;
            }

            public string Path { get; set; } = "/";

            public string QueryString { get; set; } = "";

            public string RawTarget { get; set; } = "/";

            public IHeaderDictionary Headers
            {
                get
                {
                    if (_headers == null)
                    {
                        _headers = new HeaderDictionary
                        {
                            { "Host", Url.Authority },
                            { "User-Agent", "Placeholder" }
                        };
                    }
                    return _headers;
                }
                set => _headers = value;
            }

            public Stream Body { get; set; } = Stream.Null;
        }
    }
}
