using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Orchard.Mvc.ModelBinders
{
    /// <summary>
    /// Binds a model from dictionary keys ({prefix}.{Property}, {prefix}.{Property}[{index}]),
    /// with the conventions of the former MVC DefaultModelBinder used for the settings and the element data.
    /// </summary>
    public static class DictionaryModelBinder
    {
        public static object Bind(IDictionary<string, string> settings, Type modelType, string prefix)
        {
            // the value provider of the MVC model binder is case insensitive
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in settings)
            {
                if (entry.Key != null)
                {
                    values[entry.Key] = entry.Value;
                }
            }

            if (!ContainsPrefix(values, prefix))
            {
                return null;
            }

            return BindModel(values, modelType, prefix);
        }

        private static object BindModel(IDictionary<string, string> values, Type modelType, string prefix)
        {
            var underlyingType = Nullable.GetUnderlyingType(modelType) ?? modelType;

            if (IsSimpleType(underlyingType))
            {
                return values.TryGetValue(prefix, out var value) ? ConvertSimple(value, modelType) : null;
            }

            if (modelType.IsArray)
            {
                var elementType = modelType.GetElementType();
                var items = BindCollection(values, elementType, prefix);
                var array = Array.CreateInstance(elementType, items.Count);
                for (var i = 0; i < items.Count; i++)
                {
                    array.SetValue(items[i], i);
                }
                return array;
            }

            var enumerableType = GetGenericEnumerableType(modelType);
            if (enumerableType != null)
            {
                var elementType = enumerableType.GetGenericArguments()[0];
                var items = BindCollection(values, elementType, prefix);
                var list = (IList)Activator.CreateInstance(modelType.IsInterface ? typeof(List<>).MakeGenericType(elementType) : modelType);
                foreach (var item in items)
                {
                    list.Add(item);
                }
                return list;
            }

            if (modelType.IsAbstract || modelType.IsInterface || modelType.GetConstructor(Type.EmptyTypes) == null)
            {
                return null;
            }

            var model = Activator.CreateInstance(modelType);
            foreach (var property in modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.CanWrite || property.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                var propertyPrefix = string.IsNullOrEmpty(prefix) ? property.Name : prefix + "." + property.Name;
                if (!ContainsPrefix(values, propertyPrefix))
                {
                    continue;
                }

                try
                {
                    var propertyValue = BindModel(values, property.PropertyType, propertyPrefix);
                    if (propertyValue != null || !property.PropertyType.IsValueType || Nullable.GetUnderlyingType(property.PropertyType) != null)
                    {
                        property.SetValue(model, propertyValue);
                    }
                }
                catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is NotSupportedException || ex is ArgumentException)
                {
                    // As with the MVC model binding, invalid values are ignored.
                }
            }

            return model;
        }

        private static List<object> BindCollection(IDictionary<string, string> values, Type elementType, string prefix)
        {
            var items = new List<object>();
            if (values.TryGetValue(prefix, out var value) && IsSimpleType(Nullable.GetUnderlyingType(elementType) ?? elementType))
            {
                // a single value, possibly a comma separated list
                if (!string.IsNullOrEmpty(value))
                {
                    foreach (var part in value.Split(','))
                    {
                        items.Add(ConvertSimple(part, elementType));
                    }
                }
                return items;
            }

            for (var index = 0; ; index++)
            {
                var indexPrefix = prefix + "[" + index.ToString(CultureInfo.InvariantCulture) + "]";
                if (!ContainsPrefix(values, indexPrefix))
                {
                    break;
                }
                items.Add(BindModel(values, elementType, indexPrefix));
            }

            return items;
        }

        private static object ConvertSimple(string value, Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (value == null || (value.Length == 0 && underlyingType != typeof(string)))
            {
                return null;
            }

            if (underlyingType == typeof(string))
            {
                return value;
            }

            if (underlyingType.IsEnum)
            {
                return Enum.Parse(underlyingType, value, true);
            }

            if (underlyingType == typeof(bool))
            {
                // a checkbox posts "true,false"
                return value.Split(',').Any(v => bool.Parse(v.Trim()));
            }

            var converter = TypeDescriptor.GetConverter(underlyingType);
            return converter.ConvertFromString(null, CultureInfo.InvariantCulture, value);
        }

        private static bool IsSimpleType(Type type)
        {
            return type.IsPrimitive
                || type.IsEnum
                || type == typeof(string)
                || type == typeof(decimal)
                || type == typeof(DateTime)
                || type == typeof(DateTimeOffset)
                || type == typeof(TimeSpan)
                || type == typeof(Guid)
                || TypeDescriptor.GetConverter(type).CanConvertFrom(typeof(string)) && !typeof(IEnumerable).IsAssignableFrom(type);
        }

        private static Type GetGenericEnumerableType(Type type)
        {
            if (type == typeof(string))
            {
                return null;
            }

            if (type.IsGenericType)
            {
                var definition = type.GetGenericTypeDefinition();
                if (definition == typeof(IEnumerable<>) || definition == typeof(IList<>) || definition == typeof(ICollection<>) || definition == typeof(List<>))
                {
                    return typeof(IEnumerable<>).MakeGenericType(type.GetGenericArguments()[0]);
                }
            }

            return null;
        }

        private static bool ContainsPrefix(IDictionary<string, string> values, string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                return values.Count > 0;
            }

            foreach (var key in values.Keys)
            {
                if (key == null)
                {
                    continue;
                }

                if (key.Equals(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (key.Length > prefix.Length
                    && key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                    && (key[prefix.Length] == '.' || key[prefix.Length] == '['))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
