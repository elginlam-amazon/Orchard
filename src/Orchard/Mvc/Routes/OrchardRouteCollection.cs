using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;

namespace Orchard.Mvc.Routes
{
    /// <summary>
    /// The application wide, mutable, ordered collection of routes into which the tenants publish their routes.
    /// </summary>
    public class OrchardRouteCollection : IRouter, IEnumerable<IRouter>
    {
        private readonly List<IRouter> _routes = new List<IRouter>();
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public int Count
        {
            get
            {
                using (GetReadLock())
                {
                    return _routes.Count;
                }
            }
        }

        public IRouter this[int index]
        {
            get
            {
                using (GetReadLock())
                {
                    return _routes[index];
                }
            }
        }

        /// <summary>
        /// Returns the route with the specified name, or <c>null</c>.
        /// </summary>
        public IRouter this[string name]
        {
            get
            {
                using (GetReadLock())
                {
                    return _routes.FirstOrDefault(r => r is INamedRouter named && string.Equals(named.Name, name, StringComparison.OrdinalIgnoreCase));
                }
            }
        }

        public void Add(IRouter route)
        {
            using (GetWriteLock())
            {
                _routes.Add(route);
            }
        }

        public void Insert(int index, IRouter route)
        {
            using (GetWriteLock())
            {
                _routes.Insert(index, route);
            }
        }

        public bool Remove(IRouter route)
        {
            using (GetWriteLock())
            {
                return _routes.Remove(route);
            }
        }

        public void Clear()
        {
            using (GetWriteLock())
            {
                _routes.Clear();
            }
        }

        public IDisposable GetReadLock()
        {
            _lock.EnterReadLock();
            return new LockReleaser(_lock.ExitReadLock);
        }

        public IDisposable GetWriteLock()
        {
            _lock.EnterWriteLock();
            return new LockReleaser(_lock.ExitWriteLock);
        }

        public async Task RouteAsync(RouteContext context)
        {
            IRouter[] routes;
            using (GetReadLock())
            {
                routes = _routes.ToArray();
            }

            foreach (var route in routes)
            {
                var snapshot = context.RouteData.PushState(route, null, null);
                await route.RouteAsync(context);

                if (context.Handler != null)
                {
                    return;
                }

                snapshot.Restore();
            }
        }

        public VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            IRouter[] routes;
            using (GetReadLock())
            {
                routes = _routes.ToArray();
            }

            if (!string.IsNullOrEmpty(context.RouteName))
            {
                routes = routes.Where(r => r is INamedRouter named && string.Equals(named.Name, context.RouteName, StringComparison.OrdinalIgnoreCase)).ToArray();
            }

            foreach (var route in routes)
            {
                var virtualPath = route.GetVirtualPath(context);
                if (virtualPath != null)
                {
                    return virtualPath;
                }
            }

            return null;
        }

        public IEnumerator<IRouter> GetEnumerator()
        {
            IRouter[] routes;
            using (GetReadLock())
            {
                routes = _routes.ToArray();
            }
            return ((IEnumerable<IRouter>)routes).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class LockReleaser : IDisposable
        {
            private Action _release;

            public LockReleaser(Action release)
            {
                _release = release;
            }

            public void Dispose()
            {
                _release?.Invoke();
                _release = null;
            }
        }
    }
}
