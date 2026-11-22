using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Autofac;
using Orchard.Mvc.Extensions;
using Orchard.Mvc.Routes;
using Orchard.Settings;

namespace Orchard.Mvc
{
    public class MvcModule : Module
    {
        public const string IsBackgroundHttpContextKey = "IsBackgroundHttpContext";

        protected override void Load(ContainerBuilder moduleBuilder)
        {
            moduleBuilder.RegisterType<ShellRoute>().InstancePerDependency();

            moduleBuilder.Register(HttpContextBaseFactory).As<HttpContextBase>().InstancePerDependency();
            moduleBuilder.Register(RequestContextFactory).As<RequestContext>().InstancePerDependency();
            moduleBuilder.Register(UrlHelperFactory).As<UrlHelper>().InstancePerDependency();
        }

        static HttpContextBase HttpContextBaseFactory(IComponentContext context)
        {
            var httpContext = context.Resolve<IHttpContextAccessor>().Current();
            if (httpContext != null)
                return httpContext;

            var siteService = context.Resolve<ISiteService>();

            var baseUrl = new Func<string>(() =>
            {
                var s = siteService.GetSiteSettings().BaseUrl;
                return string.IsNullOrEmpty(s) ? "http://localhost" : s;
            });

            return new HttpContextPlaceholder(baseUrl);
        }

        static RequestContext RequestContextFactory(IComponentContext context)
        {
            var httpContext = HttpContextBaseFactory(context);

            if (!httpContext.IsBackgroundContext())
            {
                var mvcHandler = httpContext.Handler as MvcHandler;
                if (mvcHandler != null)
                {
                    return mvcHandler.RequestContext;
                }

                var hasRequestContext = httpContext.Handler as IHasRequestContext;
                if (hasRequestContext != null)
                {
                    if (hasRequestContext.RequestContext != null)
                        return hasRequestContext.RequestContext;
                }
            }
            else if (httpContext is HttpContextPlaceholder)
            {
                return ((HttpContextPlaceholder)httpContext).RequestContext;
            }

            return new RequestContext(httpContext, new RouteData());
        }

        static UrlHelper UrlHelperFactory(IComponentContext context)
        {
            return new UrlHelper(context.Resolve<RequestContext>(), context.Resolve<RouteCollection>());
        }

        /// <summary>
        /// Standin context for background tasks.
        /// </summary>
        public class HttpContextPlaceholder : HttpContextBase, IDisposable
        {
            private HttpRequestPlaceholder _request;
            private readonly Lazy<string> _baseUrl;
            private readonly IDictionary _items = new Dictionary<object, object>();

            public HttpContextPlaceholder(Func<string> baseUrl)
            {
                _baseUrl = new Lazy<string>(baseUrl);
            }

            public void Dispose()
            {
            }

            public override HttpRequestBase Request
            {
                get
                {
                    if (_request == null)
                    {
                        _request = new HttpRequestPlaceholder(this, _baseUrl);
                    }
                    return _request;
                }
            }

            internal RequestContext RequestContext
            {
                get
                {
                    if (_request == null)
                    {
                        _request = new HttpRequestPlaceholder(this, _baseUrl);
                    }
                    return _request.RequestContext;
                }
            }

            public override HttpSessionStateBase Session => new HttpSessionStatePlaceholder();

            public override IHttpHandler Handler { get; set; }

            public override HttpResponseBase Response => new HttpResponsePlaceholder();

            public override IDictionary Items => _items;

            public override HttpServerUtilityBase Server => new HttpServerUtilityPlaceholder();

            public override object GetService(Type serviceType) => null;
        }

        public class HttpSessionStatePlaceholder : HttpSessionStateBase
        {
            public override object this[string name] => null;
        }

        public class HttpResponsePlaceholder : HttpResponseBase
        {
            public new string ApplyAppPathModifier(string virtualPath) => virtualPath;
            public override HttpCookieCollection Cookies => new HttpCookieCollection();
        }

        /// <summary>
        /// standin context for background tasks.
        /// </summary>
        public class HttpRequestPlaceholder : HttpRequestBase
        {
            private HttpContextBase _httpContext;
            private RequestContext _requestContext;
            private readonly Lazy<string> _baseUrl;
            private readonly NameValueCollection _queryString = new NameValueCollection();
            private Uri _uri;

            public HttpRequestPlaceholder(HttpContextBase httpContext, Lazy<string> baseUrl)
            {
                _httpContext = httpContext;
                _baseUrl = baseUrl;
            }

            public new System.Web.Routing.RequestContext RequestContext
            {
                get
                {
                    if (_requestContext == null)
                    {
                        _requestContext = new System.Web.Routing.RequestContext(_httpContext, new System.Web.Routing.RouteData());
                    }
                    return _requestContext;
                }
            }

            public override NameValueCollection QueryString => _queryString;

            public override string RawUrl => Url.OriginalString;

            public override bool IsAuthenticated => false;

            public new string AnonymousID => Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture);

            public override NameValueCollection Form => new NameValueCollection();

            public override Uri Url
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

            public override NameValueCollection Headers => new NameValueCollection { { "Host", Url.Authority } };

            public override string HttpMethod => "";

            public override NameValueCollection Params => new NameValueCollection();

            public override string AppRelativeCurrentExecutionFilePath => "~/";

            public override string ApplicationPath => Url.LocalPath;

            public override NameValueCollection ServerVariables
            {
                get
                {
                    return new NameValueCollection {
                        { "SERVER_PORT", Url.Port.ToString(CultureInfo.InvariantCulture) },
                        { "HTTP_HOST", Url.Authority.ToString(CultureInfo.InvariantCulture) },
                    };
                }
            }

            public override HttpCookieCollection Cookies => new HttpCookieCollection();

            public override bool IsLocal => true;

            public override string Path => "/";

            public override string UserAgent => "Placeholder";

            public override string UserHostAddress => "127.0.0.1";

            public override string[] UserLanguages => new string[0];

            public override HttpBrowserCapabilitiesBase Browser => new HttpBrowserCapabilitiesPlaceholder();
        }

        public class HttpBrowserCapabilitiesPlaceholder : HttpBrowserCapabilitiesBase
        {
            public override bool IsMobileDevice => false;
        }

        public class HttpServerUtilityPlaceholder : HttpServerUtilityBase
        {
            public override string MapPath(string path) => System.Web.Hosting.HostingEnvironment.MapPath(path);
        }
    }
}
