using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Orchard.Mvc.Extensions
{
    public static class RequestValuesExtensions
    {
        /// <summary>
        /// Copies the query string or form values to a dictionary (the former MVC NameValueCollection.CopyTo extension):
        /// the existing entries are kept unless <paramref name="replaceEntries"/> is set.
        /// </summary>
        public static void CopyTo(this IEnumerable<KeyValuePair<string, StringValues>> values, IDictionary<string, object> destination, bool replaceEntries = false)
        {
            if (values == null)
            {
                return;
            }

            foreach (var value in values)
            {
                if (replaceEntries || !destination.ContainsKey(value.Key))
                {
                    destination[value.Key] = value.Value.ToString();
                }
            }
        }
    }
}
