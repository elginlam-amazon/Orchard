using Microsoft.AspNetCore.Http;
using Orchard.Localization.Services;

namespace Orchard.Tests.Stubs
{
    public class StubCultureSelector : ICultureSelector
    {
        private readonly string _cultureName;

        public StubCultureSelector(string cultureName)
        {
            _cultureName = cultureName;
        }

        public CultureSelectorResult GetCulture(HttpContext context)
        {
            return new CultureSelectorResult { Priority = 1, CultureName = _cultureName };
        }
    }
}