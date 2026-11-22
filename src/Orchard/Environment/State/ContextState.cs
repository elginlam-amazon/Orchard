using System;
using System.Collections.Generic;
using System.Threading;
using System.Web;
using Orchard.Mvc.Extensions;

namespace Orchard.Environment.State
{
    /// <summary>
    /// Holds some state for the current HttpContext or Logical Context
    /// </summary>
    /// <typeparam name="T">The type of data to store</typeparam>
    public class ContextState<T> where T : class
    {
        // AsyncLocal replaces CallContext.LogicalSetData/GetData for .NET Core async-flow storage.
        private static readonly AsyncLocal<Dictionary<string, object>> _asyncLocalStorage =
            new AsyncLocal<Dictionary<string, object>>();

        private readonly string _name;
        private readonly Func<T> _defaultValue;

        public ContextState(string name)
        {
            _name = name;
        }

        public ContextState(string name, Func<T> defaultValue)
        {
            _name = name;
            _defaultValue = defaultValue;
        }

        private static Dictionary<string, object> GetOrCreateStorage()
        {
            if (_asyncLocalStorage.Value == null)
                _asyncLocalStorage.Value = new Dictionary<string, object>();
            return _asyncLocalStorage.Value;
        }

        public T GetState()
        {
            if (HttpContext.Current.IsBackgroundHttpContext())
            {
                var storage = GetOrCreateStorage();
                storage.TryGetValue(_name, out var data);

                if (data == null)
                {
                    if (_defaultValue != null)
                    {
                        storage[_name] = data = _defaultValue();
                        return data as T;
                    }
                }

                return data as T;
            }

            if (HttpContext.Current.Items[_name] == null)
            {
                HttpContext.Current.Items[_name] = _defaultValue == null ? null : _defaultValue();
            }

            return HttpContext.Current.Items[_name] as T;
        }

        public void SetState(T state)
        {
            if (HttpContext.Current.IsBackgroundHttpContext())
            {
                GetOrCreateStorage()[_name] = state;
            }
            else
            {
                HttpContext.Current.Items[_name] = state;
            }
        }
    }
}
