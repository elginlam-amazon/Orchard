// Compatibility stubs for System.Web.Mvc types that don't exist in .NET 10
// These are minimal stubs to allow compilation only - not for runtime use.
#pragma warning disable CS0108 // member hides inherited member
#pragma warning disable CS0114 // member hides inherited member, missing override
#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Routing;

namespace System.Web.Mvc
{
    // ---- Core types ----

    public class MvcHtmlString : IHtmlString
    {
        private readonly string _value;
        public static readonly MvcHtmlString Empty = new MvcHtmlString("");

        public MvcHtmlString(string value) { _value = value ?? ""; }

        public static MvcHtmlString Create(string value) => new MvcHtmlString(value);
        public string ToHtmlString() => _value;
        public override string ToString() => _value;

        public static bool IsNullOrEmpty(MvcHtmlString value)
            => value == null || value._value.Length == 0;
    }

    public abstract class ActionResult
    {
        public abstract void ExecuteResult(ControllerContext context);
    }

    public class ContentResult : ActionResult
    {
        public string Content { get; set; }
        public override void ExecuteResult(ControllerContext context) { }
    }

    public class RedirectResult : ActionResult
    {
        public RedirectResult(string url) { Url = url; }
        public RedirectResult(string url, bool permanent) { Url = url; Permanent = permanent; }
        public string Url { get; }
        public bool Permanent { get; }
        public override void ExecuteResult(ControllerContext context) { }
    }

    public class RedirectToRouteResult : ActionResult
    {
        public RedirectToRouteResult(RouteValueDictionary routeValues) { RouteValues = routeValues; }
        public RedirectToRouteResult(string routeName, RouteValueDictionary routeValues) { RouteName = routeName; RouteValues = routeValues; }
        public string RouteName { get; }
        public RouteValueDictionary RouteValues { get; }
        public override void ExecuteResult(ControllerContext context) { }
    }

    public class HttpNotFoundResult : ActionResult
    {
        public override void ExecuteResult(ControllerContext context) { }
    }

    public class EmptyResult : ActionResult
    {
        public override void ExecuteResult(ControllerContext context) { }
    }

    public class JsonResult : ActionResult
    {
        public object Data { get; set; }
        public override void ExecuteResult(ControllerContext context) { }
    }

    public abstract class ControllerBase : IController
    {
        public ViewDataDictionary ViewData { get; set; } = new ViewDataDictionary();
        public TempDataDictionary TempData { get; set; } = new TempDataDictionary();
        public ControllerContext ControllerContext { get; set; }
        public System.Web.Routing.RouteData RouteData => ControllerContext?.RouteData;
        public Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary ModelState { get; }
            = new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary();
        protected virtual void ExecuteCore() { }
        void IController.Execute(System.Web.Routing.RequestContext requestContext) { }
    }

    public abstract class Controller : ControllerBase, IDisposable
    {
        public HttpRequestBase Request => ControllerContext?.HttpContext?.Request;
        public HttpResponseBase Response => ControllerContext?.HttpContext?.Response;
        public HttpContextBase HttpContext => ControllerContext?.HttpContext;
        public HttpServerUtilityBase Server => ControllerContext?.HttpContext?.Server;
        public HttpSessionStateBase Session => ControllerContext?.HttpContext?.Session;
        public UrlHelper Url { get; set; }
        public IValueProvider ValueProvider { get; set; }

        protected virtual ViewResult View() => null;
        protected virtual ViewResult View(object model) => null;
        protected virtual ViewResult View(string viewName, object model = null) => null;
        protected virtual RedirectResult Redirect(string url) => new RedirectResult(url);
        protected virtual RedirectToRouteResult RedirectToAction(string actionName)
            => new RedirectToRouteResult(new RouteValueDictionary { ["action"] = actionName });
        protected virtual RedirectToRouteResult RedirectToAction(string actionName, object routeValues)
            => new RedirectToRouteResult(new RouteValueDictionary { ["action"] = actionName });
        protected virtual RedirectToRouteResult RedirectToAction(string actionName, string controllerName)
            => new RedirectToRouteResult(new RouteValueDictionary { ["action"] = actionName, ["controller"] = controllerName });
        protected virtual RedirectToRouteResult RedirectToAction(string actionName, string controllerName, object routeValues)
            => new RedirectToRouteResult(new RouteValueDictionary { ["action"] = actionName, ["controller"] = controllerName });
        protected virtual RedirectToRouteResult RedirectToRoute(RouteValueDictionary routeValues)
            => new RedirectToRouteResult(routeValues);
        protected virtual RedirectToRouteResult RedirectToRoute(string routeName)
            => new RedirectToRouteResult(routeName, new RouteValueDictionary());
        protected virtual ContentResult Content(string content, string contentType = null)
            => new ContentResult { Content = content };
        protected virtual HttpNotFoundResult HttpNotFound(string statusDescription = null) => new HttpNotFoundResult();
        protected virtual JsonResult Json(object data) => new JsonResult { Data = data };
        protected virtual JsonResult Json(object data, JsonRequestBehavior behavior) => new JsonResult { Data = data };

        protected virtual PartialViewResult PartialView() => new PartialViewResult();
        protected virtual PartialViewResult PartialView(object model) => new PartialViewResult();
        protected virtual PartialViewResult PartialView(string viewName) => new PartialViewResult();
        protected virtual PartialViewResult PartialView(string viewName, object model) => new PartialViewResult();
        protected virtual FileContentResult File(byte[] fileContents, string contentType) => new FileContentResult(fileContents, contentType);
        protected virtual FilePathResult File(string fileName, string contentType, string fileDownloadName = null) => new FilePathResult(fileName, contentType);
        protected virtual FileStreamResult File(System.IO.Stream fileStream, string contentType, string fileDownloadName = null) => new FileStreamResult(fileStream, contentType);

        protected virtual void OnActionExecuting(ActionExecutingContext filterContext) { }
        protected virtual void OnActionExecuted(ActionExecutedContext filterContext) { }

        public bool TryUpdateModel<TModel>(TModel model) where TModel : class => false;
        public bool TryUpdateModel<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties) where TModel : class => false;
        public bool UpdateModel<TModel>(TModel model) where TModel : class => false;
        public bool UpdateModel<TModel>(TModel model, string prefix) where TModel : class => false;
        public bool UpdateModel<TModel>(TModel model, string prefix, string[] includeProperties) where TModel : class => false;

        public dynamic ViewBag => new System.Dynamic.ExpandoObject();

        protected virtual void Dispose(bool disposing) { }
        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
    }

    public class ControllerContext
    {
        public ControllerContext() { }
        public ControllerContext(RequestContext requestContext, ControllerBase controller)
        {
            HttpContext = requestContext?.HttpContext;
            RouteData = requestContext?.RouteData;
            RequestContext = requestContext;
            Controller = controller;
        }
        public ControllerContext(HttpContextBase httpContext, RouteData routeData, ControllerBase controller)
        {
            HttpContext = httpContext;
            RouteData = routeData;
            RequestContext = new RequestContext(httpContext, routeData);
            Controller = controller;
        }

        public HttpContextBase HttpContext { get; set; }
        public RouteData RouteData { get; set; }
        public RequestContext RequestContext { get; set; }
        public ControllerBase Controller { get; set; }
    }

    public interface IController
    {
        void Execute(RequestContext requestContext);
    }

    public class DefaultControllerFactory : IControllerFactory
    {
        protected virtual Type GetControllerType(RequestContext requestContext, string controllerName) => null;
        protected virtual IController GetControllerInstance(RequestContext requestContext, Type controllerType) => null;
        public IController CreateController(RequestContext requestContext, string controllerName) => null;
        public System.Web.SessionState.SessionStateBehavior GetControllerSessionBehavior(RequestContext requestContext, string controllerName)
            => System.Web.SessionState.SessionStateBehavior.Default;
        public void ReleaseController(IController controller) { }
    }

    public class MvcHandler
    {
        public MvcHandler(RequestContext requestContext) { RequestContext = requestContext; }
        public RequestContext RequestContext { get; }
    }

    // ---- View types ----

    public interface IView
    {
        void Render(ViewContext viewContext, TextWriter writer);
    }

    public interface IViewEngine
    {
        ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache);
        ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, bool useCache);
        void ReleaseView(ControllerContext controllerContext, IView view);
    }

    public class ViewEngineResult
    {
        public ViewEngineResult(IView view, IViewEngine viewEngine) { View = view; ViewEngine = viewEngine; }
        public ViewEngineResult(IEnumerable<string> searchedLocations) { SearchedLocations = searchedLocations; }
        public IView View { get; }
        public IViewEngine ViewEngine { get; }
        public IEnumerable<string> SearchedLocations { get; } = Enumerable.Empty<string>();
    }

    public class ViewEngineCollection : Collection<IViewEngine>
    {
        public ViewEngineCollection() { }
        public ViewEngineCollection(IList<IViewEngine> list) : base(list) { }
    }

    public interface IViewDataContainer
    {
        ViewDataDictionary ViewData { get; set; }
    }

    public class TemplateInfo
    {
        public string HtmlFieldPrefix { get; set; } = "";
        public string GetFullHtmlFieldName(string partialFieldName)
        {
            if (string.IsNullOrEmpty(partialFieldName)) return HtmlFieldPrefix;
            if (string.IsNullOrEmpty(HtmlFieldPrefix)) return partialFieldName;
            return HtmlFieldPrefix + "." + partialFieldName;
        }
        public string GetFullHtmlFieldId(string partialFieldName)
            => GetFullHtmlFieldName(partialFieldName).Replace('.', '_').Replace('[', '_').Replace(']', '_');
    }

    public class ViewDataDictionary : IDictionary<string, object>
    {
        private readonly Dictionary<string, object> _data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        public object Model { get; set; }
        public TemplateInfo TemplateInfo { get; set; } = new TemplateInfo();
        public Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary ModelState { get; }
            = new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary();

        public object this[string key] { get => _data.TryGetValue(key, out var v) ? v : null; set => _data[key] = value; }
        public ICollection<string> Keys => _data.Keys;
        public ICollection<object> Values => _data.Values;
        public int Count => _data.Count;
        public bool IsReadOnly => false;
        public void Add(string key, object value) => _data.Add(key, value);
        public bool ContainsKey(string key) => _data.ContainsKey(key);
        public bool Remove(string key) => _data.Remove(key);
        public bool TryGetValue(string key, out object value) => _data.TryGetValue(key, out value);
        public void Add(KeyValuePair<string, object> item) => _data.Add(item.Key, item.Value);
        public void Clear() => _data.Clear();
        public bool Contains(KeyValuePair<string, object> item) => ((IDictionary<string, object>)_data).Contains(item);
        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => ((IDictionary<string, object>)_data).CopyTo(array, arrayIndex);
        public bool Remove(KeyValuePair<string, object> item) => ((IDictionary<string, object>)_data).Remove(item);
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _data.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _data.GetEnumerator();

        public ViewDataDictionary() { }
        public ViewDataDictionary(object model) { Model = model; }
        public ViewDataDictionary(ViewDataDictionary other)
        {
            if (other != null)
            {
                foreach (var kv in other._data) _data[kv.Key] = kv.Value;
                Model = other.Model;
                TemplateInfo = other.TemplateInfo;
            }
        }
    }

    public class ViewDataDictionary<TModel> : ViewDataDictionary
    {
        public new TModel Model { get => (TModel)base.Model; set => base.Model = value; }
        public ViewDataDictionary() { }
        public ViewDataDictionary(ViewDataDictionary other) : base(other) { }
    }

    public class TempDataDictionary : IDictionary<string, object>
    {
        private readonly Dictionary<string, object> _data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        public object this[string key] { get => _data.TryGetValue(key, out var v) ? v : null; set => _data[key] = value; }
        public ICollection<string> Keys => _data.Keys;
        public ICollection<object> Values => _data.Values;
        public int Count => _data.Count;
        public bool IsReadOnly => false;
        public void Add(string key, object value) => _data.Add(key, value);
        public bool ContainsKey(string key) => _data.ContainsKey(key);
        public bool Remove(string key) => _data.Remove(key);
        public bool TryGetValue(string key, out object value) => _data.TryGetValue(key, out value);
        public void Add(KeyValuePair<string, object> item) => _data.Add(item.Key, item.Value);
        public void Clear() => _data.Clear();
        public bool Contains(KeyValuePair<string, object> item) => ((IDictionary<string, object>)_data).Contains(item);
        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => ((IDictionary<string, object>)_data).CopyTo(array, arrayIndex);
        public bool Remove(KeyValuePair<string, object> item) => ((IDictionary<string, object>)_data).Remove(item);
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _data.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _data.GetEnumerator();
    }

    public class ViewContext : ControllerContext
    {
        public ViewContext() { }
        public ViewContext(ControllerContext controllerContext, IView view, ViewDataDictionary viewData, TempDataDictionary tempData, TextWriter writer)
        {
            HttpContext = controllerContext?.HttpContext;
            RouteData = controllerContext?.RouteData;
            RequestContext = controllerContext?.RequestContext;
            Controller = controllerContext?.Controller;
            View = view;
            ViewData = viewData;
            TempData = tempData;
            Writer = writer;
        }
        public ViewContext(ViewContext viewContext, IView view, ViewDataDictionary viewData, TempDataDictionary tempData, TextWriter writer)
            : this((ControllerContext)viewContext, view, viewData, tempData, writer) { }

        public IView View { get; set; }
        public new ViewDataDictionary ViewData { get; set; } = new ViewDataDictionary();
        public new TempDataDictionary TempData { get; set; } = new TempDataDictionary();
        public TextWriter Writer { get; set; }
    }

    public abstract class ViewResultBase : ActionResult
    {
        public string ViewName { get; set; }
        public ViewDataDictionary ViewData { get; set; } = new ViewDataDictionary();
        public TempDataDictionary TempData { get; set; } = new TempDataDictionary();
        public ViewEngineCollection ViewEngineCollection { get; set; } = new ViewEngineCollection();
        public override void ExecuteResult(ControllerContext context) { }
    }

    public class ViewResult : ViewResultBase { }

    public class PartialViewResult : ViewResultBase { }

    public interface IViewLocationCache
    {
        string GetViewLocation(HttpContextBase httpContext, string key);
        void InsertViewLocation(HttpContextBase httpContext, string key, string virtualPath);
    }

    public class WebFormViewEngine : IViewEngine
    {
        public ViewEngineResult FindView(ControllerContext cc, string viewName, string masterName, bool useCache)
            => new ViewEngineResult(Enumerable.Empty<string>());
        public ViewEngineResult FindPartialView(ControllerContext cc, string partialViewName, bool useCache)
            => new ViewEngineResult(Enumerable.Empty<string>());
        public void ReleaseView(ControllerContext cc, IView view) { }
    }

    public interface IActionInvoker
    {
        bool InvokeAction(ControllerContext controllerContext, string actionName);
    }

    public class ControllerActionInvoker : IActionInvoker
    {
        public bool InvokeAction(ControllerContext controllerContext, string actionName) => false;
    }

    public class RazorViewEngine : IViewEngine
    {
        public string[] MasterLocationFormats { get; set; } = Array.Empty<string>();
        public string[] ViewLocationFormats { get; set; } = Array.Empty<string>();
        public string[] PartialViewLocationFormats { get; set; } = Array.Empty<string>();
        public string[] AreaMasterLocationFormats { get; set; } = Array.Empty<string>();
        public string[] AreaViewLocationFormats { get; set; } = Array.Empty<string>();
        public string[] AreaPartialViewLocationFormats { get; set; } = Array.Empty<string>();
        public IViewLocationCache ViewLocationCache { get; set; }

        public ViewEngineResult FindView(ControllerContext cc, string viewName, string masterName, bool useCache)
            => new ViewEngineResult(Enumerable.Empty<string>());
        public ViewEngineResult FindPartialView(ControllerContext cc, string partialViewName, bool useCache)
            => new ViewEngineResult(Enumerable.Empty<string>());
        public void ReleaseView(ControllerContext cc, IView view) { }
    }

    // ---- TagBuilder (string-based InnerHtml, unlike ASP.NET Core's) ----

    public enum TagRenderMode { Normal, StartTag, EndTag, SelfClosing }

    public class TagBuilder
    {
        public TagBuilder(string tagName) { TagName = tagName; }
        public string TagName { get; }
        public string InnerHtml { get; set; } = "";
        public IDictionary<string, string> Attributes { get; } = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public void AddCssClass(string value)
        {
            if (Attributes.TryGetValue("class", out var cls))
                Attributes["class"] = cls + " " + value;
            else
                Attributes["class"] = value;
        }

        public void MergeAttribute(string key, string value, bool replaceExisting = false)
        {
            if (replaceExisting || !Attributes.ContainsKey(key))
                Attributes[key] = value;
        }

        public void MergeAttributes<TKey, TValue>(IDictionary<TKey, TValue> attributes, bool replaceExisting = false)
        {
            if (attributes == null) return;
            foreach (var attr in attributes)
                MergeAttribute(Convert.ToString(attr.Key), Convert.ToString(attr.Value), replaceExisting);
        }

        public void GenerateId(string id)
        {
            if (!string.IsNullOrWhiteSpace(id))
                MergeAttribute("id", id);
        }

        public void SetInnerText(string innerText) => InnerHtml = HttpUtility.HtmlEncode(innerText);

        public string ToString(TagRenderMode renderMode)
        {
            var sb = new StringBuilder();
            switch (renderMode)
            {
                case TagRenderMode.StartTag:
                    sb.Append('<').Append(TagName);
                    AppendAttributes(sb);
                    sb.Append('>');
                    break;
                case TagRenderMode.EndTag:
                    sb.Append("</").Append(TagName).Append('>');
                    break;
                case TagRenderMode.SelfClosing:
                    sb.Append('<').Append(TagName);
                    AppendAttributes(sb);
                    sb.Append(" />");
                    break;
                default:
                    sb.Append('<').Append(TagName);
                    AppendAttributes(sb);
                    sb.Append('>').Append(InnerHtml).Append("</").Append(TagName).Append('>');
                    break;
            }
            return sb.ToString();
        }

        public override string ToString() => ToString(TagRenderMode.Normal);

        private void AppendAttributes(StringBuilder sb)
        {
            foreach (var attr in Attributes)
                sb.Append(' ').Append(attr.Key).Append("=\"").Append(HttpUtility.HtmlEncode(attr.Value)).Append('"');
        }
    }

    // ---- HtmlHelper ----

    public class HtmlHelper
    {
        public HtmlHelper(ViewContext viewContext, IViewDataContainer viewDataContainer)
        {
            ViewContext = viewContext;
            ViewData = viewDataContainer?.ViewData ?? new ViewDataDictionary();
        }
        public HtmlHelper(ViewContext viewContext, IViewDataContainer viewDataContainer, RouteCollection routeCollection)
            : this(viewContext, viewDataContainer) { RouteCollection = routeCollection; }

        public ViewContext ViewContext { get; }
        public ViewDataDictionary ViewData { get; }
        public RouteCollection RouteCollection { get; }
        public IViewDataContainer ViewDataContainer { get; }

        public string Encode(string value) => HttpUtility.HtmlEncode(value);
        public string Encode(object value) => HttpUtility.HtmlEncode(value);
        public string AttributeEncode(string value) => HttpUtility.HtmlAttributeEncode(value ?? "");
        public string AttributeEncode(object value) => HttpUtility.HtmlAttributeEncode(Convert.ToString(value) ?? "");
        public MvcHtmlString Raw(string value) => MvcHtmlString.Create(value);
        public MvcHtmlString Raw(object value) => MvcHtmlString.Create(Convert.ToString(value));

        public MvcHtmlString AntiForgeryToken() => MvcHtmlString.Create("");
        public MvcHtmlString AntiForgeryToken(string salt) => MvcHtmlString.Create("");

        public static RouteValueDictionary AnonymousObjectToHtmlAttributes(object htmlAttributes)
        {
            var dict = new RouteValueDictionary();
            if (htmlAttributes == null) return dict;
            foreach (var prop in htmlAttributes.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                dict[prop.Name.Replace('_', '-')] = prop.GetValue(htmlAttributes);
            return dict;
        }

        public static string GetFormMethodString(FormMethod method)
            => method == FormMethod.Get ? "get" : "post";

        public MvcHtmlString ActionLink(string linkText, string actionName, object routeValues, IDictionary<string, object> htmlAttributes = null)
            => MvcHtmlString.Create("");
        public MvcHtmlString ActionLink(string linkText, string actionName, RouteValueDictionary routeValues, IDictionary<string, object> htmlAttributes = null)
            => MvcHtmlString.Create("");
        public MvcHtmlString ActionLink(string linkText, string actionName, string controllerName, object routeValues, object htmlAttributes)
            => MvcHtmlString.Create("");

        public void RenderPartial(string partialViewName) { }
        public void RenderPartial(string partialViewName, object model) { }
        public MvcHtmlString Partial(string partialViewName, object model = null) => MvcHtmlString.Create("");

        public MvcForm BeginForm() => null;
        public MvcForm BeginForm(string actionName, string controllerName) => null;

        public static string ValidationMessageCssClassName { get; set; } = "field-validation-error";
        public static string ValidationInputCssClassName { get; set; } = "input-validation-error";
        public static string ValidationSummaryCssClassName { get; set; } = "validation-summary-errors";
    }

    public class HtmlHelper<TModel> : HtmlHelper
    {
        public HtmlHelper(ViewContext viewContext, IViewDataContainer viewDataContainer)
            : base(viewContext, viewDataContainer) { }
        public HtmlHelper(ViewContext viewContext, IViewDataContainer viewDataContainer, RouteCollection routeCollection)
            : base(viewContext, viewDataContainer, routeCollection) { }

        public new ViewDataDictionary<TModel> ViewData => base.ViewData as ViewDataDictionary<TModel> ?? new ViewDataDictionary<TModel>();
    }

    public static class ExpressionHelper
    {
        public static string GetExpressionText(LambdaExpression expression)
        {
            if (expression.Body is MemberExpression memberExpr)
                return memberExpr.Member.Name;
            return "";
        }
        public static string GetExpressionText<TModel, TProperty>(Expression<Func<TModel, TProperty>> expression)
            => GetExpressionText((LambdaExpression)expression);
    }

    // ---- UrlHelper ----

    public class UrlHelper
    {
        public UrlHelper(RequestContext requestContext) { RequestContext = requestContext; }
        public UrlHelper(RequestContext requestContext, RouteCollection routeCollection)
        {
            RequestContext = requestContext;
            RouteCollection = routeCollection;
        }

        public RequestContext RequestContext { get; }
        public RouteCollection RouteCollection { get; }

        public string Action(string actionName) => "";
        public string Action(string actionName, object routeValues) => "";
        public string Action(string actionName, string controllerName) => "";
        public string Action(string actionName, string controllerName, object routeValues) => "";
        public string Action(string actionName, RouteValueDictionary routeValues) => "";
        public string Content(string contentPath) => contentPath;
        public string RouteUrl(object routeValues) => "";
        public string RouteUrl(RouteValueDictionary routeValues) => "";
        public string RouteUrl(string routeName) => "";
        public bool IsLocalUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            return url.StartsWith("/") || url.StartsWith("~/");
        }

        public static string GenerateUrl(string routeName, string actionName, string controllerName,
            string protocol, string hostName, string fragment, RouteValueDictionary routeValues,
            RouteCollection routeCollection, RequestContext requestContext, bool includeImplicitMvcValues)
            => "";

        public string MakeAbsolute(string relativeUrl) => relativeUrl;
        public string MakeAbsolute(string relativeUrl, string protocol) => relativeUrl;

        public string AbsoluteAction(Func<string> urlAction) => urlAction?.Invoke() ?? "";
        public string AbsoluteAction(string actionName) => Action(actionName);
        public string AbsoluteAction(string actionName, string controllerName) => Action(actionName, controllerName);
        public string AbsoluteAction(string actionName, string controllerName, object routeValues) => Action(actionName, controllerName, routeValues);
    }

    public static class ModelStateDictionaryExtensions
    {
        public static bool IsValidField(this Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState, string key)
        {
            if (modelState == null || !modelState.ContainsKey(key)) return true;
            return modelState[key]?.ValidationState != Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Invalid;
        }
    }

    public static class AjaxRequestExtensions
    {
        public static bool IsAjaxRequest(this HttpRequestBase request)
            => request != null && "XMLHttpRequest".Equals(request["X-Requested-With"], StringComparison.OrdinalIgnoreCase);
    }

    // ---- MvcForm ----

    public class MvcForm : IDisposable
    {
        private readonly ViewContext _viewContext;
        public MvcForm(ViewContext viewContext) { _viewContext = viewContext; }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing) _viewContext?.Writer?.Write("</form>");
        }
        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
    }

    // ---- Filter types ----

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class FilterAttribute : Attribute
    {
        public int Order { get; set; }
        public bool AllowMultiple => false;
    }

    public abstract class ActionFilterAttribute : FilterAttribute, IActionFilter, IResultFilter
    {
        public virtual void OnActionExecuting(ActionExecutingContext filterContext) { }
        public virtual void OnActionExecuted(ActionExecutedContext filterContext) { }
        public virtual void OnResultExecuting(ResultExecutingContext filterContext) { }
        public virtual void OnResultExecuted(ResultExecutedContext filterContext) { }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public abstract class ActionMethodSelectorAttribute : Attribute
    {
        public abstract bool IsValidForRequest(ControllerContext controllerContext, MethodInfo methodInfo);
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class HttpGetAttribute : ActionMethodSelectorAttribute
    {
        public override bool IsValidForRequest(ControllerContext controllerContext, MethodInfo methodInfo)
            => string.Equals(controllerContext?.HttpContext?.Request?.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase);
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class HttpPostAttribute : ActionMethodSelectorAttribute
    {
        public override bool IsValidForRequest(ControllerContext controllerContext, MethodInfo methodInfo)
            => string.Equals(controllerContext?.HttpContext?.Request?.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase);
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class HttpPutAttribute : ActionMethodSelectorAttribute
    {
        public override bool IsValidForRequest(ControllerContext controllerContext, MethodInfo methodInfo)
            => string.Equals(controllerContext?.HttpContext?.Request?.HttpMethod, "PUT", StringComparison.OrdinalIgnoreCase);
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class HttpDeleteAttribute : ActionMethodSelectorAttribute
    {
        public override bool IsValidForRequest(ControllerContext controllerContext, MethodInfo methodInfo)
            => string.Equals(controllerContext?.HttpContext?.Request?.HttpMethod, "DELETE", StringComparison.OrdinalIgnoreCase);
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class ActionNameAttribute : Attribute
    {
        public ActionNameAttribute(string name) { Name = name; }
        public string Name { get; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class ValidateInputAttribute : FilterAttribute
    {
        public ValidateInputAttribute(bool enableValidation) { EnableValidation = enableValidation; }
        public bool EnableValidation { get; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false)]
    public class HiddenInputAttribute : Attribute
    {
        public HiddenInputAttribute() { DisplayValue = true; }
        public bool DisplayValue { get; set; }
    }

    public abstract class ActionDescriptor
    {
        public virtual string ActionName { get; }
        public virtual object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return Array.CreateInstance(attributeType, 0) as object[] ?? Array.Empty<object>();
        }
        public virtual object[] GetCustomAttributes(bool inherit) => Array.Empty<object>();
        public virtual ControllerDescriptor ControllerDescriptor { get; }
    }

    public abstract class ControllerDescriptor
    {
        public virtual string ControllerName { get; }
        public virtual Type ControllerType { get; }
        public virtual object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return Array.CreateInstance(attributeType, 0) as object[] ?? Array.Empty<object>();
        }
        public virtual ActionDescriptor FindAction(ControllerContext context, string actionName) => null;
    }

    public class ReflectedControllerDescriptor : ControllerDescriptor
    {
        public ReflectedControllerDescriptor(Type controllerType) { _controllerType = controllerType; }
        private readonly Type _controllerType;
        public override Type ControllerType => _controllerType;
        public override string ControllerName => _controllerType?.Name.Replace("Controller", "");
        public override ActionDescriptor FindAction(ControllerContext context, string actionName) => null;
    }

    public class RequireHttpsAttribute : FilterAttribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationContext filterContext) { }
    }

    public interface IAuthorizationFilter
    {
        void OnAuthorization(AuthorizationContext filterContext);
    }

    public class AuthorizationContext : ControllerContext
    {
        public AuthorizationContext() { }
        public AuthorizationContext(ControllerContext controllerContext, ActionDescriptor actionDescriptor)
        {
            HttpContext = controllerContext?.HttpContext;
            RouteData = controllerContext?.RouteData;
            RequestContext = controllerContext?.RequestContext;
            Controller = controllerContext?.Controller;
            ActionDescriptor = actionDescriptor;
        }
        public ActionDescriptor ActionDescriptor { get; set; }
        public ActionResult Result { get; set; }
    }

    public interface IActionFilter
    {
        void OnActionExecuting(ActionExecutingContext filterContext);
        void OnActionExecuted(ActionExecutedContext filterContext);
    }

    public class ActionExecutingContext : ControllerContext
    {
        public ActionDescriptor ActionDescriptor { get; set; }
        public IDictionary<string, object> ActionParameters { get; set; }
        public ActionResult Result { get; set; }
        public bool IsChildAction => false;
    }

    public class ActionExecutedContext : ControllerContext
    {
        public ActionExecutedContext() { }
        public ActionExecutedContext(ControllerContext controllerContext, ActionDescriptor actionDescriptor, bool canceled, Exception exception)
        {
            HttpContext = controllerContext?.HttpContext;
            RouteData = controllerContext?.RouteData;
            RequestContext = controllerContext?.RequestContext;
            Controller = controllerContext?.Controller;
            ActionDescriptor = actionDescriptor;
            Canceled = canceled;
            Exception = exception;
        }
        public ActionDescriptor ActionDescriptor { get; set; }
        public bool Canceled { get; set; }
        public Exception Exception { get; set; }
        public bool ExceptionHandled { get; set; }
        public ActionResult Result { get; set; }
    }

    public interface IResultFilter
    {
        void OnResultExecuting(ResultExecutingContext filterContext);
        void OnResultExecuted(ResultExecutedContext filterContext);
    }

    public class ResultExecutingContext : ControllerContext
    {
        public ActionResult Result { get; set; }
        public bool Cancel { get; set; }
        public bool IsChildAction => false;
    }

    public class ResultExecutedContext : ControllerContext
    {
        public ActionResult Result { get; set; }
        public bool Canceled { get; set; }
        public Exception Exception { get; set; }
        public bool ExceptionHandled { get; set; }
        public bool IsChildAction => false;
    }

    public interface IFilterProvider
    {
        IEnumerable<Filter> GetFilters(ControllerContext controllerContext, ActionDescriptor actionDescriptor);
    }

    public interface IExceptionFilter
    {
        void OnException(ExceptionContext filterContext);
    }

    public class ExceptionContext : ControllerContext
    {
        public ExceptionContext() { }
        public ExceptionContext(ControllerContext controllerContext, Exception exception)
        {
            if (controllerContext != null)
            {
                Controller = controllerContext.Controller;
                HttpContext = controllerContext.HttpContext;
                RouteData = controllerContext.RouteData;
            }
            Exception = exception;
        }
        public Exception Exception { get; set; }
        public bool ExceptionHandled { get; set; }
        public ActionResult Result { get; set; }
    }

    public enum FilterScope { First = 0, Global = 10, Controller = 20, Action = 30, Last = 100 }

    public class Filter
    {
        public Filter(object instance, FilterScope scope, int? order)
        {
            Instance = instance;
            Scope = scope;
            Order = order ?? -1;
        }
        public object Instance { get; }
        public FilterScope Scope { get; }
        public int Order { get; }
    }

    // ---- Model Binder types ----

    public interface IModelBinder
    {
        object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext);
    }

    public interface IValueProvider
    {
        bool ContainsPrefix(string prefix);
        ValueProviderResult GetValue(string key);
    }

    public class ValueProviderResult
    {
        public ValueProviderResult(object rawValue, string attemptedValue, System.Globalization.CultureInfo culture)
        {
            RawValue = rawValue;
            AttemptedValue = attemptedValue;
        }
        public object RawValue { get; }
        public string AttemptedValue { get; }

        public object ConvertTo(Type type) => ConvertTo(type, null);
        public object ConvertTo(Type type, System.Globalization.CultureInfo culture)
        {
            if (RawValue == null) return type.IsValueType ? Activator.CreateInstance(type) : null;
            try { return Convert.ChangeType(RawValue, type, culture ?? System.Globalization.CultureInfo.CurrentCulture); }
            catch { return type.IsValueType ? Activator.CreateInstance(type) : null; }
        }
    }

    public class ModelBindingContext
    {
        public string ModelName { get; set; }
        public IValueProvider ValueProvider { get; set; }
        public Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary ModelState { get; set; }
            = new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary();
        public Type ModelType { get; set; }
        public object Model { get; set; }
        public ModelMetadata ModelMetadata { get; set; }
    }

    public class ModelBinderDictionary : Dictionary<Type, IModelBinder>
    {
        public new IModelBinder this[Type key]
        {
            get => TryGetValue(key, out var v) ? v : null;
            set => base[key] = value;
        }
    }

    // ---- Data Annotations ----

    public class ModelMetadata
    {
        public Type ContainerType { get; set; }
        public Type ModelType { get; set; }
        public string DisplayName { get; set; }
    }

    public abstract class ModelValidator
    {
        public abstract IEnumerable<ModelValidationResult> Validate(object container);
    }

    public class ModelValidationResult
    {
        public string MemberName { get; set; }
        public string Message { get; set; }
    }

    public class DefaultModelBinder : IModelBinder
    {
        public virtual object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext) => null;
    }

    public class ModelMetadataProviders
    {
        private static readonly ModelMetadataProviders _current = new ModelMetadataProviders();
        public static ModelMetadataProviders Current => _current;
        public ModelMetadata GetMetadataForType(Func<object> modelAccessor, Type modelType)
            => new ModelMetadata { ModelType = modelType };
    }

    public class NameValueCollectionValueProvider : IValueProvider
    {
        public NameValueCollectionValueProvider(System.Collections.Specialized.NameValueCollection collection, System.Globalization.CultureInfo culture) { }
        public bool ContainsPrefix(string prefix) => false;
        public ValueProviderResult GetValue(string key) => null;
    }

    public static class UrlParameter
    {
        public static readonly object Optional = new object();
    }

    public class DataAnnotationsModelValidatorProvider
    {
        public static void RegisterAdapter(Type attributeType, Type adapterType) { }

        protected virtual IEnumerable<ModelValidator> GetValidators(ModelMetadata metadata, ControllerContext context, IEnumerable<Attribute> attributes)
            => Enumerable.Empty<ModelValidator>();
    }

    public class RequiredAttributeAdapter { }
    public class RangeAttributeAdapter { }
    public class StringLengthAttributeAdapter { }
    public class RegularExpressionAttributeAdapter { }

    // ---- Web View base classes ----

    public abstract class WebViewPage<TModel> : System.Web.WebPages.WebPageBase, IViewDataContainer
    {
        public HtmlHelper<TModel> Html { get; set; }
        public UrlHelper Url { get; set; }
        public ViewContext ViewContext { get; set; }
        public TModel Model { get; private set; }
        public ViewDataDictionary<TModel> ViewData { get; set; } = new ViewDataDictionary<TModel>();
        ViewDataDictionary IViewDataContainer.ViewData { get => ViewData; set { } }
        public string VirtualPath { get; set; }
        public AjaxHelper<TModel> Ajax { get; set; }

        public virtual void InitHelpers() { }
        public virtual string Href(string path, params object[] pathParts) => path;
    }

    public abstract class WebViewPage : WebViewPage<dynamic> { }

    public abstract class ViewPage<TModel> : System.Web.UI.Page, IOrchardShim, IViewDataContainer
    {
        public HtmlHelper<TModel> Html { get; set; }
        public UrlHelper Url { get; set; }
        public ViewContext ViewContext { get; set; }
        public TModel Model { get { var v = ViewData; return v == null ? default : (TModel)(object)v.Model; } }
        public ViewDataDictionary<TModel> ViewData { get; set; } = new ViewDataDictionary<TModel>();
        ViewDataDictionary IViewDataContainer.ViewData { get => ViewData; set { } }
        public string AppRelativeVirtualPath { get; set; }
        public System.Web.UI.HtmlTextWriter Writer { get; set; }

        public virtual void InitHelpers() { }
        public virtual void RenderView(ViewContext viewContext) { ViewContext = viewContext; }
    }

    public abstract class ViewPage : ViewPage<dynamic> { }

    public abstract class ViewUserControl<TModel> : System.Web.UI.UserControl, IOrchardShim, IViewDataContainer
    {
        public HtmlHelper<TModel> Html { get; set; }
        public UrlHelper Url { get; set; }
        public ViewContext ViewContext { get; set; }
        public TModel Model { get { var v = ViewData; return v == null ? default : (TModel)(object)v.Model; } }
        public ViewDataDictionary<TModel> ViewData { get; set; } = new ViewDataDictionary<TModel>();
        ViewDataDictionary IViewDataContainer.ViewData { get => ViewData; set { } }
        public string AppRelativeVirtualPath { get; set; }
        public System.Web.UI.HtmlTextWriter Writer { get; set; }

        public virtual void RenderView(ViewContext viewContext) { ViewContext = viewContext; }
    }

    public abstract class ViewUserControl : ViewUserControl<dynamic> { }

    // used to identify shim base classes
    internal interface IOrchardShim { }

    // ---- Misc ----

    public enum FormMethod { Get, Post }

    [Flags]
    public enum HttpVerbs { Get = 1, Post = 2, Put = 4, Delete = 8, Head = 16, Patch = 32, Options = 64 }

    public enum JsonRequestBehavior { AllowGet, DenyGet }

    public class FormCollection : System.Collections.Specialized.NameValueCollection, IValueProvider
    {
        public FormCollection() { }
        public FormCollection(System.Collections.Specialized.NameValueCollection collection) : base(collection) { }

        bool IValueProvider.ContainsPrefix(string prefix)
            => AllKeys != null && System.Array.Exists(AllKeys, k => k != null && (k == prefix || k.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase)));

        ValueProviderResult IValueProvider.GetValue(string key)
        {
            var val = this[key];
            return val == null ? null : new ValueProviderResult(val, val, System.Globalization.CultureInfo.CurrentCulture);
        }
    }

    public static class NameValueCollectionExtensions
    {
        public static void CopyTo(this System.Collections.Specialized.NameValueCollection collection, System.Collections.Generic.IDictionary<string, object> destination)
        {
            if (collection == null) return;
            foreach (string key in collection)
                if (key != null) destination[key] = collection[key];
        }
    }

    public static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source) action(item);
        }
    }

    public class ControllerBuilder
    {
        private static readonly ControllerBuilder _current = new ControllerBuilder();
        public static ControllerBuilder Current => _current;
        public System.Collections.Generic.HashSet<string> DefaultNamespaces { get; } = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private Func<IControllerFactory> _factoryThunk;
        public void SetControllerFactory(IControllerFactory controllerFactory) { _factoryThunk = () => controllerFactory; }
        public void SetControllerFactory(Type controllerFactoryType) { }
        public IControllerFactory GetControllerFactory() => _factoryThunk?.Invoke() ?? new DefaultControllerFactory();
    }

    public interface IControllerFactory
    {
        IController CreateController(RequestContext requestContext, string controllerName);
        System.Web.SessionState.SessionStateBehavior GetControllerSessionBehavior(RequestContext requestContext, string controllerName);
        void ReleaseController(IController controller);
    }

    public class HttpAntiForgeryException : HttpException
    {
        public HttpAntiForgeryException() { }
        public HttpAntiForgeryException(string message) : base(message) { }
    }

    public class ValidateAntiForgeryTokenAttribute : Attribute
    {
        public void OnAuthorization(AuthorizationContext filterContext) { }
    }

    public class AjaxHelper<T> { }

    public static class HtmlHelperExtensions_Mvc
    {
    }

    public static class ModelBinders
    {
        public static ModelBinderDictionary Binders { get; } = new ModelBinderDictionary();
    }

    public static class ViewEngines
    {
        public static ViewEngineCollection Engines { get; } = new ViewEngineCollection();
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class AuthorizeAttribute : FilterAttribute, IAuthorizationFilter
    {
        public string Roles { get; set; }
        public string Users { get; set; }
        public void OnAuthorization(AuthorizationContext filterContext) { }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class OutputCacheAttribute : ActionFilterAttribute
    {
        public int Duration { get; set; }
        public bool NoStore { get; set; }
        public string VaryByParam { get; set; }
        public string VaryByHeader { get; set; }
        public string VaryByCustom { get; set; }
        public OutputCacheLocation Location { get; set; }
    }

    public enum OutputCacheLocation { Any, Client, Downstream, Server, None, ServerAndClient }

    public class HandleErrorAttribute : FilterAttribute, IExceptionFilter
    {
        public string View { get; set; }
        public Type ExceptionType { get; set; }
        public void OnException(ExceptionContext filterContext) { }
    }

    public class HttpUnauthorizedResult : ActionResult
    {
        public HttpUnauthorizedResult() { }
        public override void ExecuteResult(ControllerContext context) { }
    }

    // ---- HTTP result types ----

    public abstract class FileResult : ActionResult
    {
        protected FileResult(string contentType) { ContentType = contentType; }
        public string ContentType { get; }
        public string FileDownloadName { get; set; }
        public override void ExecuteResult(ControllerContext context) { }
    }

    public class FileContentResult : FileResult
    {
        public FileContentResult(byte[] fileContents, string contentType) : base(contentType)
        {
            FileContents = fileContents;
        }
        public byte[] FileContents { get; }
    }

    public class FilePathResult : FileResult
    {
        public FilePathResult(string fileName, string contentType) : base(contentType)
        {
            FileName = fileName;
        }
        public string FileName { get; }
    }

    public class FileStreamResult : FileResult
    {
        public FileStreamResult(System.IO.Stream fileStream, string contentType) : base(contentType)
        {
            FileStream = fileStream;
        }
        public System.IO.Stream FileStream { get; }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class BindAttribute : Attribute
    {
        public string Include { get; set; }
        public string Exclude { get; set; }
        public string Prefix { get; set; }
    }

    public class HttpStatusCodeResult : ActionResult
    {
        public HttpStatusCodeResult(int statusCode, string statusDescription = null)
        {
            StatusCode = statusCode;
            StatusDescription = statusDescription;
        }
        public HttpStatusCodeResult(System.Net.HttpStatusCode statusCode, string statusDescription = null)
            : this((int)statusCode, statusDescription) { }
        public int StatusCode { get; }
        public string StatusDescription { get; }
        public override void ExecuteResult(ControllerContext context) { }
    }

    // ---- Value provider types ----

    public class DictionaryValueProvider<TValue> : IValueProvider
    {
        public DictionaryValueProvider(IDictionary<string, TValue> dictionary, System.Globalization.CultureInfo culture) { }
        public bool ContainsPrefix(string prefix) => false;
        public ValueProviderResult GetValue(string key) => null;
    }

    // ---- SelectList types ----

    public class SelectListGroup
    {
        public string Name { get; set; }
        public bool Disabled { get; set; }
    }

    public class SelectListItem
    {
        public string Text { get; set; }
        public string Value { get; set; }
        public bool Selected { get; set; }
        public bool Disabled { get; set; }
        public SelectListGroup Group { get; set; }
    }

    public class SelectList : IEnumerable<SelectListItem>
    {
        private readonly List<SelectListItem> _items = new List<SelectListItem>();
        public SelectList(IEnumerable items) { }
        public SelectList(IEnumerable items, object selectedValue) { }
        public SelectList(IEnumerable items, string dataValueField, string dataTextField) { }
        public SelectList(IEnumerable items, string dataValueField, string dataTextField, object selectedValue) { }
        public IEnumerator<SelectListItem> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
    }

    public class MultiSelectList : IEnumerable<SelectListItem>
    {
        private readonly List<SelectListItem> _items = new List<SelectListItem>();
        public MultiSelectList(IEnumerable items) { }
        public MultiSelectList(IEnumerable items, IEnumerable selectedValues) { }
        public MultiSelectList(IEnumerable items, string dataValueField, string dataTextField) { }
        public MultiSelectList(IEnumerable items, string dataValueField, string dataTextField, IEnumerable selectedValues) { }
        public IEnumerator<SelectListItem> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
    }
}

// ---- WebPages stubs ----
namespace System.Web.WebPages
{
    public class HelperResult : IHtmlString
    {
        private readonly Action<TextWriter> _action;
        public HelperResult(Action<TextWriter> action) { _action = action; }
        public void WriteTo(TextWriter writer) => _action?.Invoke(writer);
        public string ToHtmlString()
        {
            using var sw = new StringWriter();
            _action?.Invoke(sw);
            return sw.ToString();
        }
        public override string ToString() => ToHtmlString();
    }

    public abstract class WebPageBase
    {
        private readonly Stack<TextWriter> _outputStack = new Stack<TextWriter>();
        public Stack<TextWriter> OutputStack => _outputStack;
        public abstract void Execute();
        public virtual string Href(string path, params object[] pathParts) => path;
        public string VirtualPath { get; set; }
        public TextWriter Output => _outputStack.Count > 0 ? _outputStack.Peek() : TextWriter.Null;
        public virtual void WriteLiteral(string value) { }
        public virtual void Write(object value) { }
        public virtual void PushContext(WebPageContext pageContext, TextWriter writer) { }
        public virtual void PopContext() { }
    }

    public class WebPageContext
    {
        public WebPageContext() { }
        public WebPageContext(HttpContextBase httpContext, WebPageBase page, object model) { }
        public HttpContextBase HttpContext { get; set; }
        public WebPageBase Page { get; set; }
        public object Model { get; set; }
    }

    public abstract class WebPageRenderingBase : WebPageBase { }
}

// ---- System.Web.UI stubs ----
namespace System.Web.UI
{
    public class HtmlTextWriter : TextWriter
    {
        private TextWriter _innerWriter;
        public HtmlTextWriter(TextWriter writer) { _innerWriter = writer; }
        public HtmlTextWriter() { _innerWriter = new StringWriter(); }
        public TextWriter InnerWriter { get => _innerWriter; set => _innerWriter = value; }
        public override Encoding Encoding => _innerWriter.Encoding;
        public override void Write(string value) => _innerWriter.Write(value);
        public override void Flush() => _innerWriter.Flush();
    }

    public class Page : IDisposable
    {
        public virtual void Dispose() { }
    }

    public class UserControl { }

    public class TemplateControl
    {
        public string AppRelativeVirtualPath { get; set; }
        public virtual string ResolveUrl(string relativeUrl) => relativeUrl;
    }
}

// ---- System.Web.HttpRequest compat extension (RequestContext) ----
namespace System.Web
{
    public static class HttpRequestBaseCompat
    {
        public static System.Web.Routing.RequestContext RequestContext(this HttpRequestBase request)
            => new System.Web.Routing.RequestContext(null, new System.Web.Routing.RouteData());
    }
}

// ---- System.Web.Hosting stubs (supplement SystemWebAdapters) ----
namespace System.Web.Hosting
{
    public interface IRegisteredObject
    {
        void Stop(bool immediate);
    }

    public abstract class SimpleWorkerRequest
    {
        // Header index constants matching System.Web.HttpWorkerRequest
        public const int HeaderContentLength = 11;
        public const int HeaderContentType = 12;
        public const int HeaderCookie = 29;
        public const int HeaderHost = 28;
        public const int HeaderSetCookie = 39;
        public const int HeaderLocation = 46;

        protected SimpleWorkerRequest(string page, string query, System.IO.TextWriter output) { }

        public virtual string GetHttpVerbName() => "GET";
        public virtual string GetKnownRequestHeader(int index) => null;
        public virtual byte[] GetPreloadedEntityBody() => null;
        public virtual void SendStatus(int statusCode, string statusDescription) { }
        public virtual void SendKnownResponseHeader(int index, string value) { }
        public virtual void SendUnknownResponseHeader(string name, string value) { }
        public virtual void SendResponseFromFile(string filename, long offset, long length) { }
        public virtual void SendResponseFromMemory(byte[] data, int length) { }
        public virtual void FlushResponse(bool finalFlush) { }
        public virtual void EndOfRequest() { }
        public virtual string GetUriPath() => "/";
        public virtual string GetQueryString() => "";
        public virtual string GetRawUrl() => "/";
        public virtual string GetHttpVersion() => "HTTP/1.1";
        public virtual string GetRemoteAddress() => "127.0.0.1";
        public virtual int GetRemotePort() => 0;
        public virtual string GetLocalAddress() => "127.0.0.1";
        public virtual int GetLocalPort() => 80;
        public virtual string GetServerName() => "localhost";
        public virtual string GetFilePath() => "/";
        public virtual string GetFilePathTranslated() => "";
        public virtual string GetPathInfo() => "";
        public virtual string GetAppPath() => "/";
        public virtual string GetAppPathTranslated() => "";
        public virtual string[] GetUnknownRequestHeaders() => null;
        public virtual byte[] GetQueryStringRawBytes() => null;
        public virtual string GetUnknownRequestHeader(string name) => null;
        public virtual bool IsClientConnected() => true;
    }
}

// Additional System.Web.Hosting stubs for legacy ASP.NET 4.x in-process hosting APIs
namespace System.Web.Hosting
{
    public static class ApplicationHost
    {
        public static object CreateApplicationHost(Type hostType, string virtualDir, string physicalDir)
            => Activator.CreateInstance(hostType);
    }

    public static partial class HostingEnvironmentExtensions
    {
        public static void InitiateShutdown() { }
    }
}

// HttpRuntime legacy members not available on .NET Core
namespace System.Web
{
    public static class HttpRuntimeLegacy
    {
        public static void ProcessRequest(System.Web.Hosting.SimpleWorkerRequest wr) { }
        public static string CodegenDir => System.IO.Path.GetTempPath();
    }
}

// System.Web.Hosting.HostingEnvironment is provided by Microsoft.AspNetCore.SystemWebAdapters

// ---- System.Web.MimeMapping stub ----
namespace System.Web
{
    public static class MimeMapping
    {
        private static readonly System.Collections.Generic.Dictionary<string, string> _map =
            new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            {".html", "text/html"}, {".htm", "text/html"}, {".txt", "text/plain"},
            {".css", "text/css"}, {".js", "application/javascript"}, {".json", "application/json"},
            {".png", "image/png"}, {".jpg", "image/jpeg"}, {".jpeg", "image/jpeg"},
            {".gif", "image/gif"}, {".svg", "image/svg+xml"}, {".ico", "image/x-icon"},
            {".pdf", "application/pdf"}, {".zip", "application/zip"},
            {".xml", "application/xml"}, {".mp4", "video/mp4"}, {".mp3", "audio/mpeg"},
            {".woff", "font/woff"}, {".woff2", "font/woff2"}, {".ttf", "font/ttf"},
        };

        public static string GetMimeMapping(string fileName)
        {
            var ext = System.IO.Path.GetExtension(fileName);
            return _map.TryGetValue(ext, out var mime) ? mime : "application/octet-stream";
        }
    }
}

// ---- System.Web.Instrumentation stubs ----
namespace System.Web.Instrumentation
{
    public class PageInstrumentationService { }
}

// ---- System.Web.SessionState stubs ----
// SessionStateBehavior is provided by Microsoft.AspNetCore.SystemWebAdapters - only add interfaces
namespace System.Web.SessionState
{
    public interface IRequiresSessionState { }
    public interface IReadOnlySessionState { }
}

// ---- System.Web.Razor.Generator stubs ----
namespace System.Web.Razor.Generator
{
    public class CodeGenerationCompleteEventArgs : EventArgs { }
}

// ---- System.Web.WebPages.Razor stubs ----
namespace System.Web.WebPages.Razor
{
    public class RazorBuildProvider
    {
        public static event EventHandler<EventArgs> CodeGenerationStarted;
        public static event EventHandler<System.Web.Razor.Generator.CodeGenerationCompleteEventArgs> CodeGenerationCompleted;
        public string VirtualPath { get; set; }
        public IEnumerable<string> VirtualPathDependencies { get; }
        public RazorBuildProviderAssemblyBuilder AssemblyBuilder { get; }
        public void AddVirtualPathDependency(string virtualPath) { }
    }

    public class RazorBuildProviderAssemblyBuilder
    {
        public void AddAssemblyReference(System.Reflection.Assembly assembly) { }
    }
}

// ---- System.Web.Mvc.Html stub namespace (empty - types are in System.Web.Mvc) ----
namespace System.Web.Mvc.Html
{
}

// ---- Additional System.Web stubs ----
namespace System.Web
{
    public delegate string HttpResponseSubstitutionCallback(HttpContextBase context);
}

// ---- HttpApplication async event helpers ----
namespace System.Web
{
    public delegate IAsyncResult BeginEventHandler(object sender, EventArgs e, AsyncCallback cb, object extraData);
    public delegate void EndEventHandler(IAsyncResult ar);

    public static class HttpApplicationExtensions
    {
        public static void AddOnBeginRequestAsync(this HttpApplication app, BeginEventHandler beg, EndEventHandler end, object extraData)
        {
            // Warmup async pipeline hook - no-op stub for net10 migration
        }
    }
}

// ---- System.Web.Http stubs ----
namespace System.Web.Routing
{
    public static class RouteCollectionMvcExtensions
    {
        public static void IgnoreRoute(this RouteCollection routes, string url, object constraints = null) { }
    }
}

namespace System.Web.Http
{
    public static class HttpRouteCollectionExtensions
    {
        public static System.Web.Routing.RouteBase MapHttpRoute(
            this System.Web.Routing.RouteCollection routes,
            string name, string routeTemplate, object defaults = null, object constraints = null)
            => null;
    }
}

// ---- System.Web.Script.Serialization stubs ----
namespace System.Web.Script.Serialization
{
    public class JavaScriptSerializer
    {
        public string Serialize(object obj)
            => System.Text.Json.JsonSerializer.Serialize(obj);

        public T Deserialize<T>(string input)
            => System.Text.Json.JsonSerializer.Deserialize<T>(input);

        public object DeserializeObject(string input)
            => System.Text.Json.JsonSerializer.Deserialize<object>(input);
    }
}

// ---- HttpRuntime additional stubs ----
namespace System.Web
{
    public static partial class HttpRuntimeExtensions
    {
        // HttpRuntime is provided by SystemWebAdapters; add missing members as extension-equivalent
    }
}

// ---- System.Web.Helpers additional stubs ----
namespace System.Web.Helpers
{
    public static class AntiForgeryConfig
    {
        public static string UniqueClaimTypeIdentifier { get; set; }
        public static bool SuppressXFrameOptionsHeader { get; set; }
        public static string CookieName { get; set; } = "__RequestVerificationToken";
    }
}

// ---- System.Web.Security additional stubs ----
namespace System.Web.Security
{
    public static class Membership
    {
        public static string GeneratePassword(int length, int numberOfNonAlphanumericCharacters)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*";
            var sb = new System.Text.StringBuilder();
            var bytes = new byte[length];
            System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
            foreach (var b in bytes)
                sb.Append(chars[b % chars.Length]);
            return sb.ToString();
        }
    }

}

// IsEmpty extension in System.Web.WebPages where it's commonly imported
namespace System.Web.WebPages
{
    public static class StringExtensions
    {
        public static bool IsEmpty(this string s) => string.IsNullOrEmpty(s);
    }
}

// ---- System.Web.Mvc.Filters stubs ----
namespace System.Web.Mvc.Filters
{
    using System.Web.Mvc;

    public interface IAuthenticationFilter
    {
        void OnAuthentication(AuthenticationContext filterContext);
        void OnAuthenticationChallenge(AuthenticationChallengeContext filterContext);
    }

    public class AuthenticationContext : ControllerContext
    {
        public ActionDescriptor ActionDescriptor { get; set; }
        public System.Security.Principal.IPrincipal Principal { get; set; }
        public ActionResult Result { get; set; }
    }

    public class AuthenticationChallengeContext : ControllerContext
    {
        public ActionDescriptor ActionDescriptor { get; set; }
        public ActionResult Result { get; set; }
    }
}

// ---- System.Web.Security stubs ----
namespace System.Web.Security
{
    // MembershipPasswordFormat lives in Orchard.Security - do not redefine here

    public enum MembershipCreateStatus
    {
        Success = 0, InvalidUserName, InvalidPassword, InvalidQuestion, InvalidAnswer,
        InvalidEmail, DuplicateUserName, DuplicateEmail, UserRejected,
        InvalidProviderUserKey, DuplicateProviderUserKey, ProviderError
    }
}

// ---- System.Web.Helpers stubs ----
namespace System.Web.Helpers
{
    public static class Crypto
    {
        public static string HashPassword(string password)
        {
            var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        public static bool VerifyHashedPassword(string hashedPassword, string password)
        {
            if (hashedPassword == null || password == null) return false;
            try
            {
                var expected = HashPassword(password);
                return string.Equals(expected, hashedPassword, System.StringComparison.Ordinal);
            }
            catch { return false; }
        }
    }
}

// ---- System.Net.Configuration stubs ----
namespace System.Net.Configuration
{
    public class SmtpNetworkElement
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool EnableSsl { get; set; }
        public bool DefaultCredentials { get; set; }
    }

    public class SmtpSection
    {
        public string From { get; set; }
        public System.Net.Mail.SmtpDeliveryMethod DeliveryMethod { get; set; }
        public SmtpNetworkElement Network { get; } = new SmtpNetworkElement();
    }
}

// ---- System.Web.Razor stubs (old Razor 2.x engine not available on .NET Core) ----
namespace System.Web.Razor
{
    public class RazorCodeLanguage
    {
        public virtual string LanguageName { get; }
    }

    public class CSharpRazorCodeLanguage : RazorCodeLanguage
    {
        public override string LanguageName => "csharp";
    }

    public class RazorEngineHost
    {
        public RazorEngineHost(RazorCodeLanguage language) { }
        public string DefaultBaseClass { get; set; }
        public string DefaultClassName { get; set; }
        public string DefaultNamespace { get; set; }
        public System.Collections.Generic.HashSet<string> NamespaceImports { get; }
            = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
    }

    public class GeneratorResults
    {
        public System.CodeDom.CodeCompileUnit GeneratedCode { get; set; }
            = new System.CodeDom.CodeCompileUnit();
        public bool Success { get; set; }
    }

    public class RazorTemplateEngine
    {
        public RazorTemplateEngine(RazorEngineHost host) { Host = host; }
        public RazorEngineHost Host { get; }
        public GeneratorResults GenerateCode(System.IO.TextReader input)
            => new GeneratorResults { Success = true };
    }
}

// System.Web.Http stubs are in Orchard/WebApi/SystemWebHttpCompat.cs
