using System.Web;
using System.Web.SessionState;
using Orchard.Mvc.Wrappers;

namespace Orchard.Mvc.Routes
{
    public class UrlPrefixAdjustedHttpContext : HttpContextBaseWrapper
    {
        private readonly UrlPrefix _prefix;

        public UrlPrefixAdjustedHttpContext(HttpContextBase httpContextBase, UrlPrefix prefix)
            : base(httpContextBase)
        {
            _prefix = prefix;
        }

        public override HttpRequestBase Request => new AdjustedRequest(_httpContextBase.Request, _prefix);

        // SetSessionStateBehavior removed: not in slim HttpContextBase from SystemWebAdapters 2.3.0

        class AdjustedRequest : HttpRequestBaseWrapper
        {
            private readonly UrlPrefix _prefix;

            public AdjustedRequest(HttpRequestBase httpRequestBase, UrlPrefix prefix)
                : base(httpRequestBase)
            {
                _prefix = prefix;
            }

            public override string AppRelativeCurrentExecutionFilePath => _prefix.RemoveLeadingSegments(_httpRequestBase.AppRelativeCurrentExecutionFilePath);
        }

    }
}
