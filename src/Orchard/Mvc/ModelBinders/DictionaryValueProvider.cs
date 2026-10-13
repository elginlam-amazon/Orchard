using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;

namespace Orchard.Mvc.ModelBinders
{
    /// <summary>
    /// A value provider over a dictionary of values (the former MVC DictionaryValueProvider).
    /// </summary>
    public class DictionaryValueProvider<TValue> : IValueProvider
    {
        private readonly IDictionary<string, TValue> _values;
        private readonly CultureInfo _culture;
        private readonly PrefixContainer _prefixContainer;

        public DictionaryValueProvider(IDictionary<string, TValue> dictionary, CultureInfo culture)
        {
            _values = new Dictionary<string, TValue>(dictionary ?? new Dictionary<string, TValue>(), StringComparer.OrdinalIgnoreCase);
            _culture = culture;
            _prefixContainer = new PrefixContainer(_values.Keys.ToList());
        }

        public bool ContainsPrefix(string prefix)
        {
            return _prefixContainer.ContainsPrefix(prefix);
        }

        public ValueProviderResult GetValue(string key)
        {
            if (key == null || !_values.TryGetValue(key, out var value))
            {
                return ValueProviderResult.None;
            }

            if (value is string[] strings)
            {
                return new ValueProviderResult(new StringValues(strings), _culture);
            }

            return new ValueProviderResult(new StringValues(Convert.ToString(value, _culture)), _culture);
        }
    }

    public static class FormCollectionValueProviderExtensions
    {
        /// <summary>
        /// Returns a value provider for the form values (the former MVC FormCollection was a value provider).
        /// </summary>
        public static IValueProvider ToValueProvider(this IFormCollection formCollection)
        {
            return new FormValueProvider(BindingSource.Form, formCollection, CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Returns a value provider for the specified values (the former MVC NameValueCollectionValueProvider).
        /// </summary>
        public static IValueProvider ToValueProvider(this System.Collections.Specialized.NameValueCollection values, CultureInfo culture)
        {
            var dictionary = values.AllKeys
                .Where(key => key != null)
                .ToDictionary(key => key, key => values.GetValues(key), StringComparer.OrdinalIgnoreCase);

            return new DictionaryValueProvider<string[]>(dictionary, culture);
        }
    }
}
