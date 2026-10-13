using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace Orchard.Mvc.Html
{
    public static class LinkExtensions
    {
        public static IHtmlContent ActionLink(this IHtmlHelper htmlHelper, IHtmlContent linkText, string actionName)
        {
            return ActionLink(htmlHelper, linkText, actionName, null /* controllerName */, new RouteValueDictionary(), new RouteValueDictionary());
        }

        public static IHtmlContent ActionLink(this IHtmlHelper htmlHelper, IHtmlContent linkText, string actionName, object routeValues)
        {
            return ActionLink(htmlHelper, linkText, actionName, null /* controllerName */, ObjectToDictionary(routeValues), new RouteValueDictionary());
        }

        public static IHtmlContent ActionLink(this IHtmlHelper htmlHelper, IHtmlContent linkText, string actionName, object routeValues, object htmlAttributes)
        {
            return ActionLink(htmlHelper, linkText, actionName, null /* controllerName */, ObjectToDictionary(routeValues), HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static IHtmlContent ActionLink(this IHtmlHelper htmlHelper, IHtmlContent linkText, string actionName, RouteValueDictionary routeValues)
        {
            return ActionLink(htmlHelper, linkText, actionName, null /* controllerName */, routeValues, new RouteValueDictionary());
        }

        public static IHtmlContent ActionLink(this IHtmlHelper htmlHelper, IHtmlContent linkText, string actionName, RouteValueDictionary routeValues, IDictionary<string, object> htmlAttributes)
        {
            return ActionLink(htmlHelper, linkText, actionName, null /* controllerName */, routeValues, htmlAttributes);
        }

        public static IHtmlContent ActionLink(this IHtmlHelper htmlHelper, IHtmlContent linkText, string actionName, string controllerName)
        {
            return ActionLink(htmlHelper, linkText, actionName, controllerName, new RouteValueDictionary(), new RouteValueDictionary());
        }

        public static IHtmlContent ActionLink(this IHtmlHelper htmlHelper, IHtmlContent linkText, string actionName, string controllerName, object routeValues, object htmlAttributes)
        {
            return ActionLink(htmlHelper, linkText, actionName, controllerName, ObjectToDictionary(routeValues), HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static IHtmlContent ActionLink(this IHtmlHelper htmlHelper, IHtmlContent linkText, string actionName, string controllerName, RouteValueDictionary routeValues, IDictionary<string, object> htmlAttributes)
        {
            if (string.IsNullOrEmpty(linkText.ToString()))
            {
                throw new ArgumentException("Argument must be a non empty string", "linkText");
            }
            return new HtmlString(GenerateLink(htmlHelper.ViewContext, linkText, null /* routeName */, actionName, controllerName, routeValues, htmlAttributes));
        }

        public static IHtmlContent ActionLink(this IHtmlHelper htmlHelper, IHtmlContent linkText, string actionName, string controllerName, string protocol, string hostName, string fragment, object routeValues, object htmlAttributes)
        {
            return ActionLink(htmlHelper, linkText, actionName, controllerName, protocol, hostName, fragment, ObjectToDictionary(routeValues), HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static IHtmlContent ActionLink(this IHtmlHelper htmlHelper, IHtmlContent linkText, string actionName, string controllerName, string protocol, string hostName, string fragment, RouteValueDictionary routeValues, IDictionary<string, object> htmlAttributes)
        {
            if (string.IsNullOrEmpty(linkText.ToString()))
            {
                throw new ArgumentException("Argument must be a non empty string", "linkText");
            }
            return new HtmlString(GenerateLink(htmlHelper.ViewContext, linkText, null /* routeName */, actionName, controllerName, protocol, hostName, fragment, routeValues, htmlAttributes));
        }

        public static IHtmlContent RouteLink(this IHtmlHelper htmlHelper, IHtmlContent linkText, object routeValues)
        {
            return RouteLink(htmlHelper, linkText, ObjectToDictionary(routeValues));
        }

        public static IHtmlContent RouteLink(this IHtmlHelper htmlHelper, IHtmlContent linkText, RouteValueDictionary routeValues)
        {
            return RouteLink(htmlHelper, linkText, routeValues, new RouteValueDictionary());
        }

        public static IHtmlContent RouteLink(this IHtmlHelper htmlHelper, IHtmlContent linkText, string routeName)
        {
            return RouteLink(htmlHelper, linkText, routeName, (object)null /* routeValues */);
        }

        public static IHtmlContent RouteLink(this IHtmlHelper htmlHelper, IHtmlContent linkText, string routeName, object routeValues)
        {
            return RouteLink(htmlHelper, linkText, routeName, ObjectToDictionary(routeValues));
        }

        public static IHtmlContent RouteLink(this IHtmlHelper htmlHelper, IHtmlContent linkText, string routeName, RouteValueDictionary routeValues)
        {
            return RouteLink(htmlHelper, linkText, routeName, routeValues, new RouteValueDictionary());
        }

        public static IHtmlContent RouteLink(this IHtmlHelper htmlHelper, IHtmlContent linkText, object routeValues, object htmlAttributes)
        {
            return RouteLink(htmlHelper, linkText, ObjectToDictionary(routeValues), HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static IHtmlContent RouteLink(this IHtmlHelper htmlHelper, IHtmlContent linkText, RouteValueDictionary routeValues, IDictionary<string, object> htmlAttributes)
        {
            return RouteLink(htmlHelper, linkText, null /* routeName */, routeValues, htmlAttributes);
        }

        public static IHtmlContent RouteLink(this IHtmlHelper htmlHelper, IHtmlContent linkText, string routeName, object routeValues, object htmlAttributes)
        {
            return RouteLink(htmlHelper, linkText, routeName, ObjectToDictionary(routeValues), HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static IHtmlContent RouteLink(this IHtmlHelper htmlHelper, IHtmlContent linkText, string routeName, RouteValueDictionary routeValues, IDictionary<string, object> htmlAttributes)
        {
            if (string.IsNullOrEmpty(linkText.ToString()))
            {
                throw new ArgumentException("Argument must be a non empty string", "linkText");
            }
            return new HtmlString(GenerateRouteLink(htmlHelper.ViewContext, linkText, routeName, routeValues, htmlAttributes));
        }

        public static IHtmlContent RouteLink(this IHtmlHelper htmlHelper, IHtmlContent linkText, string routeName, string protocol, string hostName, string fragment, object routeValues, object htmlAttributes)
        {
            return RouteLink(htmlHelper, linkText, routeName, protocol, hostName, fragment, ObjectToDictionary(routeValues), HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static IHtmlContent RouteLink(this IHtmlHelper htmlHelper, IHtmlContent linkText, string routeName, string protocol, string hostName, string fragment, RouteValueDictionary routeValues, IDictionary<string, object> htmlAttributes)
        {
            if (string.IsNullOrEmpty(linkText.ToString()))
            {
                throw new ArgumentException("Argument must be a non empty string", "linkText");
            }
            return new HtmlString(GenerateRouteLink(htmlHelper.ViewContext, linkText, routeName, protocol, hostName, fragment, routeValues, htmlAttributes));
        }

        public static string GenerateLink(ActionContext requestContext, IHtmlContent linkText, string routeName, string actionName, string controllerName, RouteValueDictionary routeValues, IDictionary<string, object> htmlAttributes)
        {
            return GenerateLink(requestContext, linkText, routeName, actionName, controllerName, null /* protocol */, null /* hostName */, null /* fragment */, routeValues, htmlAttributes);
        }

        public static string GenerateLink(ActionContext requestContext, IHtmlContent linkText, string routeName, string actionName, string controllerName, string protocol, string hostName, string fragment, RouteValueDictionary routeValues, IDictionary<string, object> htmlAttributes)
        {
            return GenerateLinkInternal(requestContext, linkText, routeName, actionName, controllerName, protocol, hostName, fragment, routeValues, htmlAttributes, true /* includeImplicitMvcValues */);
        }

        public static string GenerateRouteLink(ActionContext requestContext, IHtmlContent linkText, string routeName, RouteValueDictionary routeValues, IDictionary<string, object> htmlAttributes)
        {
            return GenerateRouteLink(requestContext, linkText, routeName, null /* protocol */, null /* hostName */, null /* fragment */, routeValues, htmlAttributes);
        }

        public static string GenerateRouteLink(ActionContext requestContext, IHtmlContent linkText, string routeName, string protocol, string hostName, string fragment, RouteValueDictionary routeValues, IDictionary<string, object> htmlAttributes)
        {
            return GenerateLinkInternal(requestContext, linkText, routeName, null /* actionName */, null /* controllerName */, protocol, hostName, fragment, routeValues, htmlAttributes, false /* includeImplicitMvcValues */);
        }
        private static string GenerateLinkInternal(ActionContext requestContext, IHtmlContent linkText, string routeName, string actionName, string controllerName, string protocol, string hostName, string fragment, RouteValueDictionary routeValues, IDictionary<string, object> htmlAttributes, bool includeImplicitMvcValues)
        {
            var urlHelper = HtmlHelperFactory.CreateUrlHelper(requestContext);
            string url = includeImplicitMvcValues
                ? urlHelper.Action(new UrlActionContext { Action = actionName, Controller = controllerName, Values = routeValues, Protocol = protocol, Host = hostName, Fragment = fragment })
                : urlHelper.RouteUrl(new UrlRouteContext { RouteName = routeName, Values = routeValues, Protocol = protocol, Host = hostName, Fragment = fragment });
            TagBuilder tagBuilder = new TagBuilder("a");
            tagBuilder.InnerHtml.AppendHtml(linkText);
            tagBuilder.MergeAttributes(htmlAttributes);
            tagBuilder.MergeAttribute("href", url);
            return tagBuilder.ToString(TagRenderMode.Normal);
        }

        private static RouteValueDictionary ObjectToDictionary(object value)
        {
            RouteValueDictionary dictionary = new RouteValueDictionary();

            if (value != null)
            {
                foreach (var prop in value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    dictionary.Add(prop.Name, prop.GetValue(value));
                }
            }

            return dictionary;
        }
    }
}
