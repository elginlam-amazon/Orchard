using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace Orchard.OutputCache.Helpers
{
    public static class OutputCacheAttributeExtensions
    {
        /// <summary>
        /// Returns true if the Location of the specified response cache attribute matches any of the specified list of locations.
        /// </summary>
        public static bool LocationIsIn(this ResponseCacheAttribute attribute, params ResponseCacheLocation[] locations)
        {
            return locations.Contains(attribute.Location);
        }
    }
}
