using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Orchard.Localization;

namespace Orchard.Tokens.Providers
{
    public class UrlHelperTokens : ITokenProvider
    {
        private readonly IWorkContextAccessor _workContextAccessor;
        private readonly IUrlHelper _urlHelper;

        public UrlHelperTokens(IWorkContextAccessor workContextAccessor, IUrlHelper urlHelper)
        {
            _workContextAccessor = workContextAccessor;
            _urlHelper = urlHelper;
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        public void Describe(DescribeContext context)
        {
            context.For("Url", T("IUrlHelper actions"), T("IUrlHelper actions."))
                .Token("Action:*", T("Action:<actionname,controllername[,area=MyModuleName][,customValue=42]>"), T("The route values to evaluate."));
        }

        public void Evaluate(EvaluateContext context)
        {
            if (_workContextAccessor.GetContext().HttpContext == null)
            {
                return;
            }

            context.For("Url", () => _urlHelper)
                .Token(token => token.StartsWith("Action:", StringComparison.OrdinalIgnoreCase) ? token.Substring("Action:".Length) : null, GetRoute);
        }

        private object GetRoute(string token, IUrlHelper url)
        {
            var items = token.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var actionName = items[0];
            var controllerName = items[1];
            var routeValues = new RouteValueDictionary {
                {"action", actionName},
                {"controller", controllerName}
            };

            foreach (var parts in items.Skip(2).Select(item => item.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries)))
            {
                routeValues[parts[0]] = parts[1];
            }

            return url.RouteUrl(routeValues);
        }
    }
}