using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;

// Minimal stubs for System.Data.Services.Client / System.Data.Services.Common
// These are WCF Data Services types not available in .NET Core.
// Runtime OData gallery calls will throw NotSupportedException; compile-time is satisfied.

namespace System.Data.Services.Common
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class DataServiceKeyAttribute : Attribute
    {
        public DataServiceKeyAttribute(params string[] keyNames) { }
    }
}

namespace System.Data.Services.Client
{
    public class SendingRequestEventArgs : EventArgs
    {
        public WebRequest Request { get; internal set; }
    }

    public class DataServiceQuery<T> : IQueryable<T>
    {
        private readonly IQueryable<T> _inner = Enumerable.Empty<T>().AsQueryable();

        public DataServiceQuery<T> Expand(string expandPath) => this;

        public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();
        public Type ElementType => typeof(T);
        public Expression Expression => _inner.Expression;
        public IQueryProvider Provider => _inner.Provider;
    }

    public abstract class DataServiceContext
    {
        protected DataServiceContext(Uri serviceRoot) { }

        public bool IgnoreMissingProperties { get; set; }
        public Func<Type, string> ResolveName { get; set; }
        public Func<string, Type> ResolveType { get; set; }

        public event EventHandler<SendingRequestEventArgs> SendingRequest;

        public Uri GetReadStreamUri(object entity) => null;

        public DataServiceQuery<T> CreateQuery<T>(string entitySetName)
            => new DataServiceQuery<T>();

        public void AddObject(string entitySetName, object entity) { }

        protected void RaiseSendingRequest(SendingRequestEventArgs args)
            => SendingRequest?.Invoke(this, args);
    }
}
