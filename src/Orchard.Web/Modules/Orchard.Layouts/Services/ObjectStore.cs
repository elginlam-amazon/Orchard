using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Newtonsoft.Json;
using Orchard.Validation;

namespace Orchard.Layouts.Services
{
    /// <summary>
    /// Stores objects in the session of the user.
    /// </summary>
    /// <remarks>
    /// The ASP.NET Core session only stores bytes: the objects are serialized as JSON, along with their type name
    /// (the session data is only stored on the server).
    /// </remarks>
    public class ObjectStore : IObjectStore
    {
        private const char Separator = '\n';
        private readonly IWorkContextAccessor _wca;

        public ObjectStore(IWorkContextAccessor wca)
        {
            _wca = wca;
        }

        private ISession Session
        {
            get
            {
                var session = _wca.GetContext().HttpContext?.Features.Get<ISessionFeature>()?.Session;
                if (session == null)
                {
                    throw new InvalidOperationException("The session state is not available.");
                }
                return session;
            }
        }

        public void Set(string key, object value)
        {
            Argument.ThrowIfNull(key, "key");
            if (value == null)
            {
                Session.Remove(key);
                return;
            }

            Session.SetString(key, value.GetType().AssemblyQualifiedName + Separator + JsonConvert.SerializeObject(value));
        }

        public object Get(string key, Func<object> defaultValue = null)
        {
            Argument.ThrowIfNull(key, "key");
            var data = Session.GetString(key);
            if (data != null)
            {
                var separatorIndex = data.IndexOf(Separator);
                var type = separatorIndex > 0 ? Type.GetType(data.Substring(0, separatorIndex), false) : null;
                if (type != null)
                {
                    return JsonConvert.DeserializeObject(data.Substring(separatorIndex + 1), type);
                }
            }

            return defaultValue != null ? defaultValue() : null;
        }

        public string GenerateKey()
        {
            return Guid.NewGuid().ToString();
        }

        public void Remove(string key)
        {
            Session.Remove(key);
        }
    }
}
