using System;
using System.Collections;
using System.Globalization;
using System.Security.Principal;
using System.Web;

namespace Orchard.Mvc.Wrappers
{
    public abstract class HttpContextBaseWrapper : HttpContextBase
    {
        protected readonly HttpContextBase _httpContextBase;

        protected HttpContextBaseWrapper(HttpContextBase httpContextBase)
        {
            _httpContextBase = httpContextBase;
        }

        public override void AddError(Exception errorInfo)
        {
            _httpContextBase.AddError(errorInfo);
        }

        public override void ClearError()
        {
            _httpContextBase.ClearError();
        }

        public override object GetService(Type serviceType)
        {
            return ((IServiceProvider)_httpContextBase).GetService(serviceType);
        }

        public override void RewritePath(string path)
        {
            _httpContextBase.RewritePath(path);
        }

        public override void RewritePath(string path, bool rebaseClientPath)
        {
            _httpContextBase.RewritePath(path, rebaseClientPath);
        }

        public override void RewritePath(string filePath, string pathInfo, string queryString)
        {
            _httpContextBase.RewritePath(filePath, pathInfo, queryString);
        }

        public override void RewritePath(string filePath, string pathInfo, string queryString, bool setClientFilePath)
        {
            _httpContextBase.RewritePath(filePath, pathInfo, queryString, setClientFilePath);
        }

        public override Exception[] AllErrors => _httpContextBase.AllErrors;

        public override HttpApplicationStateBase Application => _httpContextBase.Application;

        public override HttpApplication ApplicationInstance
        {
            get => _httpContextBase.ApplicationInstance;
        }

        public override IHttpHandler CurrentHandler => _httpContextBase.CurrentHandler;

        public override Exception Error => _httpContextBase.Error;

        public override IHttpHandler Handler
        {
            get => _httpContextBase.Handler;
            set => _httpContextBase.Handler = value;
        }

        public override bool IsDebuggingEnabled => _httpContextBase.IsDebuggingEnabled;

        public override IDictionary Items => _httpContextBase.Items;

        public override IHttpHandler PreviousHandler => _httpContextBase.PreviousHandler;

        public override HttpRequestBase Request => _httpContextBase.Request;

        public override HttpResponseBase Response => _httpContextBase.Response;

        public override HttpServerUtilityBase Server => _httpContextBase.Server;

        public override HttpSessionStateBase Session => _httpContextBase.Session;

        public override DateTime Timestamp => _httpContextBase.Timestamp;

        public override IPrincipal User
        {
            get => _httpContextBase.User;
            set => _httpContextBase.User = value;
        }
    }
}
