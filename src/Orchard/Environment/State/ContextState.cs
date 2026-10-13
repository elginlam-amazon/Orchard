using System;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Orchard.Mvc.Extensions;

namespace Orchard.Environment.State
{
    /// <summary>
    /// Holds some state for the current HttpContext or Logical Context
    /// </summary>
    /// <typeparam name="T">The type of data to store</typeparam>
    public class ContextState<T> where T : class
    {
        private static readonly HttpContextAccessor _httpContextAccessor = new HttpContextAccessor();

        private readonly string _name;
        private readonly Func<T> _defaultValue;
        // logical (async flow) data replacing the former CallContext logical data.
        private readonly AsyncLocal<T> _logicalData = new AsyncLocal<T>();

        public ContextState(string name)
        {
            _name = name;
        }

        public ContextState(string name, Func<T> defaultValue)
        {
            _name = name;
            _defaultValue = defaultValue;
        }

        public T GetState()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext.IsBackgroundHttpContext())
            {
                var data = _logicalData.Value;

                if (data == null)
                {
                    if (_defaultValue != null)
                    {
                        _logicalData.Value = data = _defaultValue();
                        return data;
                    }
                }

                return data;
            }

            if (!httpContext.Items.TryGetValue(_name, out var value) || value == null)
            {
                httpContext.Items[_name] = value = _defaultValue == null ? null : _defaultValue();
            }

            return value as T;
        }

        public void SetState(T state)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext.IsBackgroundHttpContext())
            {
                _logicalData.Value = state;
            }
            else
            {
                httpContext.Items[_name] = state;
            }
        }
    }
}
