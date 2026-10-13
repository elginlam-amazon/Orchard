using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Orchard.MediaLibrary.WebSearch.Providers;
using Orchard.Security;
using Orchard.WebApi;

namespace Orchard.MediaLibrary.WebSearch.Controllers.Api
{
    public class WebSearchApiController : ApiController
    {
        private readonly IAuthorizer _authorizer;
        private readonly IEnumerable<IWebSearchProvider> _wsp;

        public WebSearchApiController(IAuthorizer authorizer, IEnumerable<IWebSearchProvider> wsp)
        {
            _authorizer = authorizer;
            _wsp = wsp;
        }

        [HttpGet]
        public IActionResult GetImages(string query, string providerType)
        {
            if (!_authorizer.Authorize(Permissions.AccessMediaWebSearch))
            {
                return StatusCode((int)HttpStatusCode.Unauthorized);
            }

            var selectedProvider = _wsp.FirstOrDefault(provider => provider.Name == providerType);
            if (selectedProvider == null || !selectedProvider.IsValid())
            {
                return NotFound();
            }

            return new JsonResult(selectedProvider.GetImages(query));
        }
    }
}