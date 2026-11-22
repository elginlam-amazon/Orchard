using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

// Compatibility stubs for System.Web.Http (Web API 2) types that don't exist in .NET 10.
// These stubs allow existing WebApi infrastructure code to compile; they are not functionally
// equivalent to the original Web API 2 types.

namespace System.Web.Http
{
    public interface IHttpActionResult
    {
        Task<System.Net.Http.HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken);
    }

    public class OkResult : IHttpActionResult
    {
        public Task<System.Net.Http.HttpResponseMessage> ExecuteAsync(CancellationToken ct)
            => Task.FromResult(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }

    public class OkNegotiatedContentResult<T> : IHttpActionResult
    {
        public T Content { get; }
        public OkNegotiatedContentResult(T content) { Content = content; }
        public Task<System.Net.Http.HttpResponseMessage> ExecuteAsync(CancellationToken ct)
            => Task.FromResult(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }

    public class NotFoundResult : IHttpActionResult
    {
        public Task<System.Net.Http.HttpResponseMessage> ExecuteAsync(CancellationToken ct)
            => Task.FromResult(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
    }

    public class StatusCodeResult : IHttpActionResult
    {
        public System.Net.HttpStatusCode StatusCode { get; }
        public StatusCodeResult(System.Net.HttpStatusCode statusCode) { StatusCode = statusCode; }
        public Task<System.Net.Http.HttpResponseMessage> ExecuteAsync(CancellationToken ct)
            => Task.FromResult(new System.Net.Http.HttpResponseMessage(StatusCode));
    }

    public class BadRequestResult : IHttpActionResult
    {
        public Task<System.Net.Http.HttpResponseMessage> ExecuteAsync(CancellationToken ct)
            => Task.FromResult(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.BadRequest));
    }

    public class NegotiatedContentResult<T> : IHttpActionResult
    {
        public System.Net.HttpStatusCode StatusCode { get; }
        public T Content { get; }
        public NegotiatedContentResult(System.Net.HttpStatusCode statusCode, T content) { StatusCode = statusCode; Content = content; }
        public Task<System.Net.Http.HttpResponseMessage> ExecuteAsync(CancellationToken ct)
            => Task.FromResult(new System.Net.Http.HttpResponseMessage(StatusCode));
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class HttpGetAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class HttpPostAttribute : Attribute { }

    public abstract class ApiController : System.Web.Http.Controllers.IHttpController, IDisposable
    {
        public System.Net.Http.HttpRequestMessage Request { get; set; }

        protected IHttpActionResult Ok() => new OkResult();
        protected IHttpActionResult Ok<T>(T content) => new OkNegotiatedContentResult<T>(content);
        protected IHttpActionResult NotFound() => new NotFoundResult();
        protected IHttpActionResult StatusCode(System.Net.HttpStatusCode statusCode) => new StatusCodeResult(statusCode);
        protected IHttpActionResult BadRequest() => new BadRequestResult();
        protected IHttpActionResult Content<T>(System.Net.HttpStatusCode statusCode, T value) => new NegotiatedContentResult<T>(statusCode, value);
        protected IHttpActionResult Json<T>(T content) => new JsonActionResult<T>(content);

        public virtual void Dispose() { }
    }

    public class JsonActionResult<T> : IHttpActionResult
    {
        public T Content { get; }
        public JsonActionResult(T content) { Content = content; }
        public Task<System.Net.Http.HttpResponseMessage> ExecuteAsync(CancellationToken ct)
        {
            var resp = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK);
            resp.Content = new System.Net.Http.StringContent(System.Text.Json.JsonSerializer.Serialize(Content));
            return Task.FromResult(resp);
        }
    }

    public class HttpConfiguration { }

    public static class RouteParameter
    {
        public static readonly object Optional = new object();
    }

    // Extension methods for HttpRequestMessage to support GetRouteData()
    public static class HttpRequestMessageWebApiExtensions
    {
        public static System.Web.Http.Routing.IHttpRouteData GetRouteData(this HttpRequestMessage request)
        {
            return null;
        }
    }
}

namespace System.Web.Http.Dependencies
{
    public interface IDependencyScope : IDisposable
    {
        object GetService(Type serviceType);
        IEnumerable<object> GetServices(Type serviceType);
    }

    public interface IDependencyResolver : IDependencyScope
    {
        IDependencyScope BeginScope();
    }
}

namespace System.Web.Http.Controllers
{
    public interface IHttpController { }

    public class HttpControllerContext
    {
        public HttpControllerContext() { }

        public HttpControllerContext(
            System.Web.Http.HttpConfiguration configuration,
            System.Web.Http.Routing.IHttpRouteData routeData,
            HttpRequestMessage request)
        {
            Configuration = configuration;
            RouteData = routeData;
            Request = request;
        }

        public System.Web.Http.HttpConfiguration Configuration { get; set; }
        public System.Web.Http.Routing.IHttpRouteData RouteData { get; set; }
        public HttpRequestMessage Request { get; set; }
        public IHttpController Controller { get; set; }
        public HttpControllerDescriptor ControllerDescriptor { get; set; }
    }

    public class HttpControllerDescriptor
    {
        public HttpControllerDescriptor() { }

        public HttpControllerDescriptor(
            System.Web.Http.HttpConfiguration configuration,
            string controllerName,
            Type controllerType)
        {
            Configuration = configuration;
            ControllerName = controllerName;
            ControllerType = controllerType;
        }

        public string ControllerName { get; set; }
        public Type ControllerType { get; set; }
        public System.Web.Http.HttpConfiguration Configuration { get; set; }
    }

    public class HttpActionDescriptor { }

    public class HttpActionContext
    {
        public HttpControllerContext ControllerContext { get; set; }
        public HttpActionDescriptor ActionDescriptor { get; set; }
    }
}

namespace System.Web.Http.Dispatcher
{
    public interface IHttpControllerSelector
    {
        System.Web.Http.Controllers.HttpControllerDescriptor SelectController(HttpRequestMessage request);
        IDictionary<string, System.Web.Http.Controllers.HttpControllerDescriptor> GetControllerMapping();
    }

    public interface IHttpControllerActivator
    {
        System.Web.Http.Controllers.IHttpController Create(
            HttpRequestMessage request,
            System.Web.Http.Controllers.HttpControllerDescriptor controllerDescriptor,
            Type controllerType);
    }

    public class DefaultHttpControllerSelector : IHttpControllerSelector
    {
        protected readonly System.Web.Http.HttpConfiguration Configuration;

        public DefaultHttpControllerSelector(System.Web.Http.HttpConfiguration configuration)
        {
            Configuration = configuration;
        }

        public virtual string GetControllerName(HttpRequestMessage request) { return null; }

        public virtual System.Web.Http.Controllers.HttpControllerDescriptor SelectController(HttpRequestMessage request)
        {
            return null;
        }

        public virtual IDictionary<string, System.Web.Http.Controllers.HttpControllerDescriptor> GetControllerMapping()
        {
            return null;
        }
    }
}

namespace System.Web.Http.Filters
{
    public interface IActionFilter
    {
        bool AllowMultiple { get; }
        Task<HttpResponseMessage> ExecuteActionFilterAsync(
            System.Web.Http.Controllers.HttpActionContext actionContext,
            CancellationToken cancellationToken,
            Func<Task<HttpResponseMessage>> continuation);
    }

    public interface IAuthorizationFilter
    {
        bool AllowMultiple { get; }
        Task<HttpResponseMessage> ExecuteAuthorizationFilterAsync(
            System.Web.Http.Controllers.HttpActionContext actionContext,
            CancellationToken cancellationToken,
            Func<Task<HttpResponseMessage>> continuation);
    }

    public interface IExceptionFilter
    {
        bool AllowMultiple { get; }
        Task ExecuteExceptionFilterAsync(
            HttpActionExecutedContext actionExecutedContext,
            CancellationToken cancellationToken);
    }

    public class HttpActionExecutedContext
    {
        public System.Web.Http.Controllers.HttpActionContext ActionContext { get; set; }
        public Exception Exception { get; set; }
        public HttpResponseMessage Response { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public abstract class ExceptionFilterAttribute : Attribute, IExceptionFilter
    {
        public virtual bool AllowMultiple => false;

        public virtual Task ExecuteExceptionFilterAsync(
            HttpActionExecutedContext actionExecutedContext,
            CancellationToken cancellationToken)
        {
            OnException(actionExecutedContext);
            return Task.CompletedTask;
        }

        public virtual void OnException(HttpActionExecutedContext actionExecutedContext) { }
    }
}

namespace System.Web.Http.Routing
{
    public interface IHttpRoute
    {
        IDictionary<string, object> Defaults { get; }
        IDictionary<string, object> Constraints { get; }
    }

    public interface IHttpRouteData
    {
        IHttpRoute Route { get; }
        IDictionary<string, object> Values { get; }
    }

    public class HttpRouteData : IHttpRouteData
    {
        public IHttpRoute Route { get; set; }
        public IDictionary<string, object> Values { get; set; } = new Dictionary<string, object>();
    }
}
