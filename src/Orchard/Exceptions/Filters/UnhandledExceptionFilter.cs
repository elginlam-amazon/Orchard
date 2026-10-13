using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Orchard.Logging;
using Orchard.Mvc;
using Orchard.Mvc.Extensions;
using Orchard.Utility.Extensions;
using Orchard.Mvc.Filters;
using IFilterProvider = Orchard.Mvc.Filters.IFilterProvider;

namespace Orchard.Exceptions.Filters
{
    public class UnhandledExceptionFilter : FilterProvider, IActionFilter, IResultFilter
    {
        private readonly IExceptionPolicy _exceptionPolicy;
        private readonly IOrchardServices _orchardServices;
        private readonly Lazy<IEnumerable<IFilterProvider>> _filterProviders;

        public UnhandledExceptionFilter(
            IExceptionPolicy exceptionPolicy,
            IOrchardServices orchardServices,
            Lazy<IEnumerable<IFilterProvider>> filters)
        {
            _exceptionPolicy = exceptionPolicy;
            _orchardServices = orchardServices;
            _filterProviders = filters;
            Logger = NullLogger.Instance;
        }

        public ILogger Logger { get; set; }

        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
        }

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
            // for exceptions which occurred during the action execution

            // don't provide custom errors if the action has some custom code to handle exceptions
            if (!filterContext.ActionDescriptor.GetCustomAttributes(typeof(HandleErrorAttribute), false).Any())
            {
                if (!filterContext.ExceptionHandled && filterContext.Exception != null)
                {
                    if (_exceptionPolicy.HandleException(this, filterContext.Exception))
                    {
                        filterContext.ExceptionHandled = true;

                        // inform exception filters of the exception that was suppressed
                        var exceptionContext = new ExceptionContext(filterContext, new List<IFilterMetadata>()) { Exception = filterContext.Exception };
                        foreach (var exceptionFilter in _filterProviders.Value.OfType<IExceptionFilter>())
                        {
                            exceptionFilter.OnException(exceptionContext);
                        }

                        if (exceptionContext.ExceptionHandled)
                        {
                            filterContext.Result = exceptionContext.Result;
                        }
                        else
                        {
                            var shape = _orchardServices.New.ErrorPage();
                            shape.Message = filterContext.Exception.Message;
                            shape.Exception = filterContext.Exception;

                            filterContext.Result = new ShapeResult(filterContext.Controller, shape);
                            filterContext.HttpContext.Response.StatusCode = 500;

                        }
                    }
                }
            }

            if (filterContext.Result is NotFoundResult)
            {
                var model = _orchardServices.New.NotFound();
                var request = filterContext.HttpContext.Request;
                var url = request.GetRawUrl();
                var displayUrl = request.GetDisplayUrl();

                // If the url is relative then replace with Requested path
                model.RequestedUrl = displayUrl.Contains(url) & displayUrl != url ?
                    displayUrl : url;

                // Dont get the user stuck in a 'retry loop' by
                // allowing the Referrer to be the same as the Request
                var urlReferrer = request.GetUrlReferrer();
                model.ReferrerUrl = urlReferrer != null &&
                                    urlReferrer.OriginalString != model.RequestedUrl ?
                    urlReferrer.OriginalString : null;

                filterContext.Result = new ShapeResult(filterContext.Controller, model);
                filterContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                filterContext.ExceptionHandled = true;

            }
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {

        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
            // for exceptions which occurred during the action execution

            // don't provide custom errors if the action has some custom code to handle exceptions
            if (!filterContext.ExceptionHandled && filterContext.Exception != null)
            {
                if (_exceptionPolicy.HandleException(this, filterContext.Exception))
                {
                    // inform exception filters of the exception that was suppressed
                    var exceptionContext = new ExceptionContext(filterContext, new List<IFilterMetadata>()) { Exception = filterContext.Exception };
                    foreach (var exceptionFilter in _filterProviders.Value.OfType<IExceptionFilter>())
                    {
                        exceptionFilter.OnException(exceptionContext);
                    }
                }
            }
        }
    }
}
